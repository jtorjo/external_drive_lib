using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using Shell32;

namespace external_drive_lib.android
{
    internal class android_folder : IFolder2 {
        private FolderItem fi_;
        private android_drive drive_;

        public android_folder(android_drive drive,FolderItem fi) {
            drive_ = drive;
            fi_ = fi;
            Debug.Assert(fi.IsFolder);
        }
        
        public string name { get; }

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
                var full = fi_.Path;
                Debug.Assert(full.StartsWith(drive_.root_name));
                var id = "{" + drive_.unique_id + "}";
                full = full.Replace(drive_.root_name, id);
                return full;
            }
        }
        public IDrive drive {
            get { return drive_; }
        }

        public IFolder parent => new android_folder(drive_, fi_.Parent);
        public IEnumerable<IFile> files { get; }
        public IEnumerable<IFolder> child_folders { get; }

        public void delete() {
        }

        public void copy_file(IFile file) {
        }
    }
}
