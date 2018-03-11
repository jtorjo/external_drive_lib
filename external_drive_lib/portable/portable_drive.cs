using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using external_drive_lib.util;
using Shell32;

namespace external_drive_lib.portable
{
    internal class portable_drive : IDrive {
        private const int RETRY_TIMES = 5;
        private const int SLEEP_BEFORE_RETRY_MS = 200;

        private FolderItem root_;
        private drive_type drive_type_;

        private string friendly_name_;

        private string root_path_;

        /* A USB device that is plugged in identifies itself by its VID/PID combination. 
         * A VID is a 16-bit vendor number (Vendor ID). A PID is a 16-bit product number (Product ID). 
         * The PC uses the VID/PID combination to find the drivers (if any) that are to be used for the USB device.
        */
        private string vid_pid_ = "";

        // this portable device's unique ID - think of it as a serial number
        private string unique_id_ = "";

        private bool enumerated_children_ = false;
        private List<IFolder> folders_ = new List<IFolder>();
        private List<IFile> files_ = new List<IFile>();

        private bool connected_via_usb_ = true;

        public portable_drive(FolderItem fi) {
            root_ = fi;
            friendly_name_ = root_.Name;
            root_path_ = root_.Path;

            if ( usb_util.portable_path_to_vidpid(root_path_, ref vid_pid_))
                unique_id_ = vid_pid_;
            // 1.2.3+ - sometimes, we can't match vidpid to unique id (for instance, iphones). in this case, do our best and just
            //          use the unique id from the path itself
            var unique_id_from_path = usb_util.unique_id_from_root_path(root_path_);
            if (unique_id_from_path != "")
                unique_id_ = unique_id_from_path;

            find_drive_type();
        }

        private void find_drive_type() {
            drive_type_ = drive_type.portable;

            bool is_android = false, is_phone = false, is_tablet = false, is_apple = false, is_iphone = false;

            if (friendly_name_.ToLower().StartsWith("apple"))
                is_apple = true;
            if (friendly_name_.ToLower().Contains(" iphone"))
                is_iphone = true;

            try {
                if (root_.IsFolder) {
                    var items = (root_.GetFolder as Folder).Items();
                    if (items.Count == 1) {
                        var child = items.Item(0) as FolderItem;
                        var name = child.Name;
                        if (child.IsFolder) {
                            if (name == "Phone")
                                is_phone = true;
                            else if (name == "Tablet")
                                is_tablet = true;
                            // at this point, see if child has a sub-folder called Android
                            var folder = (child.GetFolder as Folder).ParseName("android");
                            is_android = folder != null;
                        }
                    }
                }
                if (is_phone)
                    drive_type_ = is_android ? drive_type.android_phone : drive_type.iphone;
                else if (is_tablet)
                    drive_type_ = is_android ? drive_type.android_tablet : drive_type.ipad;
                else if (is_android)
                    drive_type_ = drive_type.android_unknown;
                if (is_apple)
                    drive_type_ = is_iphone ? drive_type.iphone : drive_type.iOS_unknown;
            } catch {
                // just leave drive type as portable
            }
        }

        internal bool connected_via_usb {
            get { return connected_via_usb_; }
            set { connected_via_usb_ = value; }
        }

        public bool is_connected() {
            return connected_via_usb;
        }

        public bool is_available() {
            try {
                if (connected_via_usb) {
                    var items = (root_.GetFolder as Folder).Items();
                    var has_items = items.Count >= 1;

                    if (drive_type_.is_iOS() && has_items) {
                        // iphone - even if connected, until we allow "Read/Write" files, it won't be available
                        // so, we might see "Internal Storage", but that will be completely empty
                        var dcim = try_parse_folder("*/dcim");
                        return dcim != null;
                    }

                    return has_items;
                }
            } catch {
            }
            return false;
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
                    portable_util.enumerate_children(this, root_, folders_, files_);
                }
                return folders_;
            }
        }
        public IEnumerable<IFile> files {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    portable_util.enumerate_children(this, root_, folders_, files_);
                }
                return files_;                
            }
        }

        public string unique_id {
            get { return unique_id_; }
            internal set { unique_id_ = value; }
        }

        public string friendly_name {
            get { return friendly_name_; }
        }

        public string vid_pid {
            get { return vid_pid_; }
        }

        private FolderItem parse_sub_folder(IEnumerable<string> sub_folder_path) {
            var cur_folder = root_.GetFolder as Folder;
            var cur_folder_item = root_;
            var idx = 0;
            foreach (var sub in sub_folder_path) {
                if (idx == 0 && sub == "*") {
                    // special case - replace with single root folder
                    var sub_items = cur_folder.Items();
                    if (sub_items.Count == 1 && sub_items.Item(0).IsFolder) 
                        cur_folder = sub_items.Item(0).GetFolder as Folder;
                    else 
                        throw new external_drive_libexception("Root drive doesn't have a single root folder (*)");
                } else {
                    var sub_folder = cur_folder.ParseName(sub);
                    if (sub_folder == null)
                        return null;
                    cur_folder_item = sub_folder;
                    cur_folder = cur_folder_item.GetFolder as Folder;
                }
                ++idx;
            }
            return cur_folder_item;
        }

        public IFile parse_file(string path) {
            var f = try_parse_file(path);
            if ( f == null)
                throw new external_drive_libexception("invalid path " + path);
            return f;
        }

        public IFolder parse_folder(string path) {
            var f = try_parse_folder(path);
            if (f == null)
                throw new external_drive_libexception("invalid path " + path);
            return f;
        }

        public IFile try_parse_file(string path) {
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
                return null;
            var file = (raw_folder.GetFolder as Folder).ParseName(file_name);
            if ( file == null)
                return null;
            return new portable_file(this, file as FolderItem2);
        }

        public IFolder try_parse_folder(string path) {
            path = path.Replace("/", "\\");
            if (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);
            var unique_drive_id = "{" + unique_id + "}";
            if (path.StartsWith(unique_drive_id, StringComparison.CurrentCultureIgnoreCase))
                path = path.Substring(unique_drive_id.Length + 2); // ignore ":\" as well
            if (path.StartsWith(root_path_, StringComparison.CurrentCultureIgnoreCase))
                path = path.Substring(root_path_.Length + 1);

            var sub_folder_names = path.Split('\\').ToList();
            var raw_folder = parse_sub_folder(sub_folder_names);
            if (raw_folder == null)
                return null;
            return new portable_folder(this, raw_folder);
        }

        public string parse_portable_path(FolderItem fi) {
            var path = fi.Path;
            if (path.EndsWith("\\"))
                path = path.Substring(path.Length - 1);
            Debug.Assert(path.StartsWith(root_name, StringComparison.CurrentCultureIgnoreCase));
            // ignore the drive + "\\"
            path = path.Substring(root_name.Length + 1);
            var sub_folder_count = path.Count(c => c == '\\') + 1;
            var cur = fi;
            var name = "";
            for (int i = 0; i < sub_folder_count; ++i) {
                if (name != "")
                    name = "\\" + name;
                name = cur.Name + name;
                cur = (cur.Parent as Folder2).Self;
            }

            name = "{" + unique_id + "}:\\" + name;
            return name;
        }

        // if folder already exists, it returns it
        public IFolder create_folder(string path) {
            path = path.Replace("/", "\\");
            if (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);
            var id = "{" + unique_id + "}:\\";
            var contains_drive_prefix = path.StartsWith(id, StringComparison.CurrentCultureIgnoreCase);
            if (contains_drive_prefix)
                path = path.Substring(id.Length);

            var cur = root_;
            var sub_folders = path.Split('\\');
            foreach (var sub_name in sub_folders) {
                var folder_object = cur.GetFolder as Folder;
                var sub = folder_object.ParseName(sub_name);
                if (sub == null) {
                    folder_object.NewFolder(sub_name);
                    sub = folder_object.ParseName(sub_name);
                }
                if ( sub == null)
                    throw new external_drive_libexception("could not create part of path " + path);

                if ( !sub.IsFolder)
                    throw new external_drive_libexception("part of path is a file: " + path);
                cur = sub;
            }

            return new portable_folder(this, cur);
        }
    }
}
