using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib
{
    /* the root - the one that contains all external drives 
     */
    public class drive_root {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static drive_root inst { get; } = new drive_root();

        private bool auto_refresh_ = false;
        
        public bool auto_refresh {
            get { return auto_refresh_; }
            set {
                if (auto_refresh_ == value)
                    return;
                auto_refresh_ = value;
            }
        }

        private drive_root() {
            refresh();
        }

        // returns all drives, even the internal HDDs - you might need this if you want to copy a file onto an external drive
        public IReadOnlyList<IDrive> all_drives {
            get { lock(this) return drives_; }
        }
        public IReadOnlyList<IDrive> external_drives {
            get { lock(this) return external_drives_; }
        }

        // this includes all drives, even the internal ones
        private List<IDrive> drives_ = new List<IDrive>();
        private List<IDrive> external_drives_ = new List<IDrive>();

        public void refresh() {
            List<IDrive> drives_now = new List<IDrive>();
            try {
                drives_now.AddRange(get_android_drives());
            } catch (Exception e) {
                logger.Error("error getting android drives " + e);
            }
            try {
                drives_now.AddRange(get_win_drives());
            } catch (Exception e) {
                logger.Error("error getting win drives " + e);
            }
            var external = drives_now.Where(d => d.type != drive_type.hdd).ToList();
            lock (this) {
                drives_ = drives_now;
                external_drives_ = external;
            }
        }

        public IDrive try_get_drive(string unique_id_or_drive_id) {
            // case insensitive
            foreach ( var d in all_drives)
                if (string.Compare(d.root_name, unique_id_or_drive_id, StringComparison.CurrentCultureIgnoreCase) == 0 ||
                    string.Compare(d.unique_id, unique_id_or_drive_id, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return d;
            return null;
        }
        // throws if drive not found
        public IDrive get_drive(string unique_id_or_drive_id) {
            // case insensitive
            var d = try_get_drive(unique_id_or_drive_id);
            if ( d == null)
                throw new exception("invalid drive " + unique_id_or_drive_id);
            return d;
        }

        private void split_into_drive_and_folder_path(string path, out string drive, out string folder_or_file) {
            var end_of_drive = path.IndexOf(":\\");
            if (end_of_drive >= 0) {
                drive = path.Substring(0, end_of_drive + 2);
                folder_or_file = path.Substring(end_of_drive + 2);
            } else
                drive = folder_or_file = null;
        }

        // throws if anything goes wrong
        public IFile parse_file(string path) {
            // split into drive + path
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new exception("invalid path " + path);
            var drive = get_drive(drive_str);
            return drive.parse_file(folder_or_file);
        }

        // throws if anything goes wrong
        public IFolder parse_folder(string path) {
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new exception("invalid path " + path);
            var drive = get_drive(drive_str);
            return drive.parse_folder(folder_or_file);
        }

        // creates all folders up to the given path
        public IFolder new_folder(string path) {
            // FIXME
            throw new exception("not implemented yet");
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Android

        private static Folder get_shell32_folder(object folder_path)
        {
            Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
            Object shell = Activator.CreateInstance(shellAppType);
            return (Folder)shellAppType.InvokeMember("NameSpace",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { folder_path });
        }

        private static Folder get_my_computer() {
            return get_shell32_folder(0x11);
        }

        private static List<FolderItem> get_android_connected_device_drives() {
            var usb_drives = new List<FolderItem>();

            foreach (FolderItem fi in get_my_computer().Items()) {
                if (Directory.Exists(fi.Path) || fi.Path.Contains(":\\"))
                    continue;
                usb_drives.Add(fi);
            }
            return usb_drives;
        }

        private List<IDrive> get_android_drives() {
            return get_android_connected_device_drives().Select(d => new android.android_drive(d) as IDrive).ToList();
        }

        // END OF Android
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Windows

        // for now, I return all drives - don't care about which is External, Removable, whatever

        private List<IDrive> get_win_drives() {
            return DriveInfo.GetDrives().Select(d => new win_drive(d) as IDrive).ToList();
        }
        // END OF Windows
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
