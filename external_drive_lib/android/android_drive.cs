using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using Shell32;

namespace external_drive_lib.android
{
    internal class android_drive : IDrive {
        private FolderItem root_;
        private drive_type drive_type_;

        private string friendly_name_;

        private string root_path_;

        private bool enumerated_children_ = false;
        private List<IFolder> folders_ = new List<IFolder>();
        private List<IFile> files_ = new List<IFile>();

        public android_drive(FolderItem fi) {
            root_ = fi;
            friendly_name_ = root_.Name;
            root_path_ = root_.Path;
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
            get { return "{" + root_path_ + "}:\\"; }
        }
        
        public IEnumerable<IFolder> folders {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    android_util.enumerate_children(this, root_, folders_, files_);
                }
                return folders_;
            }
        }
        public IEnumerable<IFile> files {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    android_util.enumerate_children(this, root_, folders_, files_);
                }
                return files_;                
            }
        }

        // FIXME
        public string unique_id {
            get { return friendly_name; }
        }

        public string friendly_name {
            get { return friendly_name_; }
        }

        public IFile parse_file(string path) {
            var fi = (root_.GetFolder as Folder).ParseName( path.Replace("/", "\\") );
            if ( fi.IsFolder)
                throw new exception("not a file: " + root_name + "\\" + path);
            return new android_file(this, fi);
        }

        public IFolder parse_folder(string path) {
            var fi = (root_.GetFolder as Folder).ParseName( path.Replace("/", "\\") );
            if ( !fi.IsFolder)
                throw new exception("not a folder: " + root_name + "\\" + path);
            return new android_folder(this, fi);
        }

        public IFolder create_folder(string folder) {
            // care about drive prefix - does it contain the root name/id or not?
            return null;
        }
    }
}
