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

        public void delete() {
        }

        public void copy_file(IFile file) {
        }
    }
}
