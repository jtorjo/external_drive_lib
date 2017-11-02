using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.android
{
    // https://blog.dotnetframework.org/2014/12/10/read-extended-properties-of-a-file-in-c/ -> this gets properties of a folder

    internal class android_folder : IFolder2 {

        private FolderItem fi_;
        private android_drive drive_;

        private bool enumerated_children_ = false;
        private List<IFolder> folders_ = new List<IFolder>();
        private List<IFile> files_ = new List<IFile>();

        public android_folder(android_drive drive,FolderItem fi) {
            drive_ = drive;
            fi_ = fi;
            Debug.Assert(fi.IsFolder);
        }

        public string name => fi_.Name;

        // FIXME to test
        public bool exists {
            get {
                try {
                    // ask for attributes - I expect this will initiate a system call, and if we're disconnected, it will throw
                    (fi_.GetFolder as Folder).GetDetailsOf(null, 4);
                    return true;
                } catch {
                    return false;
                }
            }
        }

        public string full_path {
            get {
                return drive_.parse_android_path(fi_);
            }
        }
        public IDrive drive {
            get { return drive_; }
        }

        public IFolder parent => new android_folder(drive_, (fi_.Parent as Folder2).Self);

        public IEnumerable<IFile> files {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    android_util.enumerate_children(drive_, fi_, folders_, files_);
                }
                return files_;
            }
        }
        public IEnumerable<IFolder> child_folders {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    android_util.enumerate_children(drive_, fi_, folders_, files_);
                }
                return folders_;
            }
        }


        public void delete_async() {
            win_util.delete_folder_item(fi_);
        }

        public void delete_sync() {
        }


        public void copy_file(IFile file, bool synchronous) {
            var copy_options = 4 | 8 | 16 | 512 | 1024 | 0x00400000;
            var andoid = file as android_file;
            var win = file as win_file;
            // it can either be android or windows
            Debug.Assert(andoid != null || win != null);
            FolderItem dest_item = null;
            var souce_name = file.name;
            if (andoid != null) 
                dest_item = andoid.folder_item();
            else if (win != null) {
                var win_file_name = new FileInfo(win.full_path);

                var shell_folder = win_util.get_shell32_folder(win_file_name.DirectoryName);
                var shell_file = shell_folder.ParseName(win_file_name.Name);
                Debug.Assert(shell_file != null);
                dest_item = shell_file;
            }

            // Windows stupidity - if file exists, it will display a stupid "Do you want to replace" dialog,
            // even if we speicifically told it not to (via the copy options)
            //
            // so, if file exists, delete it first
            var existing_name = (fi_.GetFolder as Folder).ParseName(souce_name);
            if ( existing_name != null)
                win_util.delete_folder_item(existing_name);

            (fi_.GetFolder as Folder).CopyHere(dest_item, copy_options);
        }
    }
}
