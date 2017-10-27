using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using Shell32;

namespace external_drive_lib.android
{
    internal class android_drive : IDrive {
        private FolderItem root_;
        private drive_type drive_type_;


        private bool enumerated_children_ = false;
        private List<IFolder> folders_ = new List<IFolder>();
        private List<IFile> files_ = new List<IFile>();

        public android_drive(FolderItem fi) {
            root_ = fi;
            find_drive_type();
        }

        private void find_drive_type() {
            drive_type_ = drive_type.android;
            if (root_.IsFolder) {
                var items = (root_.GetFolder as Folder).Items();
                if (items.Count == 1) {
                    var child = items.Item(0) as FolderItem;
                    if (child.IsFolder) {
                        if (child.Name == "Phone")
                            drive_type_ = drive_type.android_phone;
                        else if (child.Name == "Tablet")
                            drive_type_ = drive_type.android_tablet;
                    }
                }
            }
        }

        public bool is_connected() {
            try {
                var items = (root_.GetFolder as Folder).Items();
                return items.Count >= 1;
            } catch {
                return false;
            }
        }

        public drive_type type {
            get { return drive_type_; }
        }

        public string root_name {
            get { return root_.Path; }
        }
        
        public IEnumerable<IFolder> folders {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    android_util.enumerate_children(root_, folders_, files_);
                }
                return folders_;
            }
        }
        public IEnumerable<IFile> files {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    android_util.enumerate_children(root_, folders_, files_);
                }
                return files_;                
            }
        }


        public string unique_id { get; }
        public string friendly_name { get; }
        public string full_friendly_name { get; }

        public IFile parse_file(string path) {
            return null;
        }

        public IFolder parse_folder(string path) {
            return null;
        }
    }
}
