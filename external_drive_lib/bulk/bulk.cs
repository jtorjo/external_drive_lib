using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.android;
using external_drive_lib.interfaces;
using external_drive_lib.portable;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.bulk
{
    public static class bulk
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // copies the folder's files - NOT its sub-folders
        public static void bulk_copy_sync(string src_folder, string dest_folder) {
            bulk_copy_sync( drive_root.inst.parse_folder(src_folder).files, dest_folder);
        }
        public static void bulk_copy_async(string src_folder, string dest_folder) {
            bulk_copy_async( drive_root.inst.parse_folder(src_folder).files, dest_folder);
        }

        public static void bulk_copy_sync(IEnumerable<IFile> src_files, string dest_folder) {
            bulk_copy(src_files, dest_folder, true);
        }
        public static void bulk_copy_async(IEnumerable<IFile> src_files, string dest_folder) {
            bulk_copy(src_files, dest_folder, false);
        }

        private static void bulk_copy_win_sync(IEnumerable<string> src_files, string dest_folder_name) {            
            foreach (var f in src_files) {
                var name = Path.GetFileName(f);
                File.Copy(f, dest_folder_name + "\\" + name, true);
            }
        }

        private static void bulk_copy_win(IEnumerable<string> src_files, string dest_folder_name, bool synchronous) {
            if (synchronous)
                bulk_copy_win_sync(src_files, dest_folder_name);
            else
                Task.Run(() => bulk_copy_win_sync(src_files, dest_folder_name));
        }

        private static void bulk_copy(IEnumerable<IFile> src_files, string dest_folder_name, bool synchronous) {
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
                bulk_copy_win( src_files.Select(f => (f as win_file).full_path), dest_folder_name, synchronous);
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
                if (src_items.Count == f.Value.Count) 
                    dest_parent_shell_folder.CopyHere(src_items, copy_options);
                else {
                    // "amazing" - for Android, the filter spec doesn't work - we need to copy each of them separately
                    Debug.Assert(f.Value[0] is portable_file);
                    foreach ( var file in f.Value)
                        dest_parent_shell_folder.CopyHere((file as portable_file).raw_folder_item(), copy_options);
                }

                if ( synchronous)
                    wait_for_copy_complete(f.Value, dest_folder_name);
            }
        }

        private static void wait_for_copy_complete(List<IFile> src_files, string dest_folder_name) {
            Debug.Assert(src_files.Count > 0);
            var dest_folder = drive_root.inst.parse_folder(dest_folder_name);
            var dest_android = dest_folder is portable_folder;
            var dest_win = dest_folder is win_folder;
            Debug.Assert(dest_android || dest_win);

            foreach (var f in src_files) {
                var dest_file = dest_folder_name + "\\" + f.name;
                if ( dest_win)
                    win_util.wait_for_win_copy_complete(f.size, dest_file);
                else if ( dest_android)
                    win_util.wait_for_android_copy_complete(dest_file, f.size);
                logger.Debug("bulk " + dest_file);
            }
        }

    }
}
