using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            get { return root_path_; }
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

        private FolderItem parse_sub_folder(IEnumerable<string> sub_folder_path) {
            var cur_folder = root_.GetFolder as Folder;
            var cur_folder_item = root_;
            foreach (var sub in sub_folder_path) {
                var sub_folder = cur_folder.ParseName(sub);
                if (sub_folder == null)
                    return null;
                cur_folder_item = sub_folder;
                cur_folder = cur_folder_item.GetFolder as Folder;
            }
            return cur_folder_item;
        }

        public IFile parse_file(string path) {
            var unique_drive_id = "{" + unique_id + "}";
            if (path.StartsWith(unique_drive_id, StringComparison.CurrentCultureIgnoreCase))
                path = path.Substring(unique_drive_id.Length + 2); // ignore ":\" as well
            if (path.StartsWith(root_path_, StringComparison.CurrentCultureIgnoreCase))
                path = path.Substring(root_path_.Length + 1);

            var sub_folder_names = path.Replace("/", "\\").Split('\\').ToList();
            var file_name = sub_folder_names.Last();
            sub_folder_names.RemoveAt(sub_folder_names.Count - 1);
            var raw_folder = parse_sub_folder(sub_folder_names);
            if (raw_folder == null)
                throw new exception("invalid path " + path);
            var file = (raw_folder.GetFolder as Folder).ParseName(file_name);
            if ( file == null)
                throw new exception("invalid path " + path);
            return new android_file(this, file as FolderItem2);
        }

        public IFolder parse_folder(string path) {
            var unique_drive_id = "{" + unique_id + "}";
            if (path.StartsWith(unique_drive_id, StringComparison.CurrentCultureIgnoreCase))
                path = path.Substring(unique_drive_id.Length + 2); // ignore ":\" as well
            if (path.StartsWith(root_path_, StringComparison.CurrentCultureIgnoreCase))
                path = path.Substring(root_path_.Length + 1);

            var sub_folder_names = path.Replace("/", "\\").Split('\\').ToList();
            var raw_folder = parse_sub_folder(sub_folder_names);
            if (raw_folder == null)
                throw new exception("invalid path " + path);
            return new android_folder(this, raw_folder);
        }

        public IFolder create_folder(string folder) {
            // FIXME care about drive prefix - does it contain the root name/id or not?
            return null;
        }
    }
}
