using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using external_drive_lib.portable;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.bulk
{
    public static class bulk
    {

        // copies the folder's files - NOT its sub-folders
        //
        // callback - it's called after each file is copied. Args: the file, its index, number of files for total copy
        public static void bulk_copy_sync(string src_folder, string dest_folder, Action<string,int,int> copy_complete_callback = null) {
            bulk_copy_sync( drive_root.inst.parse_folder(src_folder).files.ToList(), dest_folder, copy_complete_callback);
        }

        // callback - it's called after each file is copied. Args: the file, its index, number of files for total copy
        public static void bulk_copy_async(string src_folder, string dest_folder, Action<string,int,int> copy_complete_callback = null) {
            bulk_copy_async( drive_root.inst.parse_folder(src_folder).files.ToList(), dest_folder, copy_complete_callback);
        }

        // callback - it's called after each file is copied. Args: the file, its index, number of files for total copy
        public static void bulk_copy_sync(IReadOnlyList<IFile> src_files, string dest_folder, Action<string,int,int> copy_complete_callback = null) {
            bulk_copy(src_files, dest_folder, true, copy_complete_callback);
        }

        // callback - it's called after each file is copied. Args: the file, its index, number of files for total copy
        public static void bulk_copy_async(IReadOnlyList<IFile> src_files, string dest_folder, Action<string,int,int> copy_complete_callback = null) {
            bulk_copy(src_files, dest_folder, false, copy_complete_callback);
        }

        private static void bulk_copy_win_sync(IReadOnlyList<string> src_files, string dest_folder_name, Action<string,int,int> copy_complete_callback ) {
            var count = src_files.Count;
            var idx = 0;
            foreach (var f in src_files) {
                var name = Path.GetFileName(f);
                File.Copy(f, dest_folder_name + "\\" + name, true);
                try {
                    copy_complete_callback?.Invoke(f,idx,count);
                } catch(Exception e) {
                    throw new exception("could not find source file to copy " + f, e);
                }
                ++idx;
            }
        }

        private static void bulk_copy_win(IReadOnlyList<string> src_files, string dest_folder_name, bool synchronous, Action<string,int,int> copy_complete_callback) {
            if (synchronous)
                bulk_copy_win_sync(src_files, dest_folder_name, copy_complete_callback);
            else
                Task.Run(() => bulk_copy_win_sync(src_files, dest_folder_name, copy_complete_callback));
        }

        private class copy_file_info {
            public string name;
            public long size;
        }

        private static void bulk_copy(IEnumerable<IFile> src_files, string dest_folder_name, bool synchronous, Action<string,int,int> copy_complete_callback) {
            dest_folder_name = dest_folder_name.Replace("/", "\\");
            Debug.Assert(!dest_folder_name.EndsWith("\\"));
            // in case destination does not exist, create it
            drive_root.inst.new_folder(dest_folder_name);

            Dictionary<string, List<IFile>> files_by_folder = new Dictionary<string, List<IFile>>();
            foreach (var f in src_files) {
                var path = f.folder.full_path;
                if ( !files_by_folder.ContainsKey(path))
                    files_by_folder.Add(path, new List<IFile>());
                files_by_folder[path].Add(f);
            }

            var dest_folder = drive_root.inst.parse_folder(dest_folder_name);
            var all_src_win = src_files.All(f => f is win_file);
            if (all_src_win && dest_folder is win_folder) {
                bulk_copy_win( src_files.Select(f => (f as win_file).full_path).ToList(), dest_folder_name, synchronous, copy_complete_callback);
                return;
            }

            //
            // here, we know the source or dest or both are android

            Folder dest_parent_shell_folder = null;
            if (dest_folder is portable_folder)
                dest_parent_shell_folder = (dest_folder as portable_folder).raw_folder_item().GetFolder as Folder;
            else if (dest_folder is win_folder) 
                dest_parent_shell_folder = win_util.get_shell32_folder((dest_folder as win_folder).full_path);                
            else 
                Debug.Assert(false);

            int count = files_by_folder.Sum(f => f.Value.Count);
            int idx = 0;
            foreach (var f in files_by_folder) {
                var src_parent = f.Value[0].folder;
                var src_parent_file_count = src_parent.files.Count();
                // filter can be specified by "file1;file2;file3.."
                string filter_spec = f.Value.Count == src_parent_file_count ? "*.*" : string.Join(";", f.Value.Select(n => n.name));

                Folder src_parent_shell_folder = null;
                if (src_parent is portable_folder)
                    src_parent_shell_folder = (src_parent as portable_folder).raw_folder_item().GetFolder as Folder;
                else if (src_parent is win_folder) 
                    src_parent_shell_folder = win_util.get_shell32_folder((src_parent as win_folder).full_path);                
                else 
                    Debug.Assert(false);

                var src_items = src_parent_shell_folder.Items() as FolderItems3;
                // here, we filter only those files that you want from the source folder 
                src_items.Filter(int.MaxValue & ~0x8000, filter_spec);
                // ... they're ignored, but still :)
                var copy_options = 4 | 16 | 512 | 1024;
                // note: we want to compute these beforehand (before the copy takes place) - if a file is being copied, access to it can be locked,
                //       so even asking "f.name" will wait until the copy is 100% complete - which is NOT what we want
                List<copy_file_info> wait_complete = f.Value.Select(src => new copy_file_info {name = src.name, size = src.size}).ToList();
                if (src_items.Count == f.Value.Count) {
                    if (synchronous && copy_complete_callback != null)
                        Task.Run(() => dest_parent_shell_folder.CopyHere(src_items, copy_options));
                    else
                        dest_parent_shell_folder.CopyHere(src_items, copy_options);
                } else {
                    // "amazing" - for Android, the filter spec doesn't work - we need to copy each of them separately
                    Debug.Assert(f.Value[0] is portable_file);
                    foreach (var file in f.Value)
                        dest_parent_shell_folder.CopyHere((file as portable_file).raw_folder_item(), copy_options);
                }

                if ( synchronous)
                    wait_for_copy_complete(wait_complete, count, ref idx, dest_folder_name, copy_complete_callback);
                else if (copy_complete_callback != null)
                    // here, we're async, but with callback
                    Task.Run(() => wait_for_copy_complete(wait_complete, count, ref idx, dest_folder_name, copy_complete_callback));
            }
        }

        private static void wait_for_copy_complete(List<copy_file_info> src_files, int count, ref int idx, string dest_folder_name, Action<string,int,int> copy_complete_callback) {
            Debug.Assert(src_files.Count > 0);
            var dest_folder = drive_root.inst.parse_folder(dest_folder_name);
            var dest_android = dest_folder is portable_folder;
            var dest_win = dest_folder is win_folder;
            Debug.Assert(dest_android || dest_win);

            /* 1.2.4+ - there are no really good defaults here for waiting for the copy to be complete. If it's a bulk copy,
             *          sometimes it just takes longer for a file's size to be non-zero, and we could end up with a exception
             *          
             *          that's why we made these defaults a lot bigger on bulk_copy. We can't make them too big though, since we don't 
             *          want to wait indefinitely for a copy that might fail (say that the user unplugs the device)
             */
            int max_retry = 75;
            int max_retry_first_time = 500;

            foreach (var f in src_files) {
                var dest_file = dest_folder_name + "\\" + f.name;
                if ( dest_win)
                    win_util.wait_for_win_copy_complete(f.size, dest_file, max_retry, max_retry_first_time);
                else if ( dest_android)
                    win_util.wait_for_portable_copy_complete(dest_file, f.size, max_retry, max_retry_first_time);
                copy_complete_callback?.Invoke(f.name, idx, count);
                ++idx;
            }
        }

    }
}
