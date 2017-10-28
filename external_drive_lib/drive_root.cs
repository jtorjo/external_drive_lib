using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib
{
    /* the root - the one that contains all external drives 
     */
    public class drive_root {
        private static drive_root inst_ = new drive_root();
        public static drive_root inst { get; } = inst_;

        private bool auto_refresh_ = false;
        
        public bool auto_refresh {
            get { return auto_refresh_; }
            set {
                if (auto_refresh_ == value)
                    return;
                auto_refresh_ = value;
            }
        }

        // returns all drives, even the internal HDDs - you might need this if you want to copy a file onto an external drive
        public IReadOnlyList<IDrive> all_drives {
            get { return drives_; }
        }
        public IReadOnlyList<IDrive> external_drives {
            get { return external_drives_; }
        }

        // this includes all drives, even the internal ones
        private List<IDrive> drives_ = new List<IDrive>();
        private List<IDrive> external_drives_ = new List<IDrive>();

        public void refresh() {
            List<IDrive> drives_now = new List<IDrive>();
            drives_now.AddRange(get_android_drives());
            var external = drives_now.Where(d => d.type != drive_type.hdd).ToList();
            lock (this) {
                drives_ = drives_now;
                external_drives_ = external;
            }
        }

        public IDrive drive_by_id(string unique_id_or_drive) {
            
        }

        public IFile parse_file(string path) {
            // split into drive + path
        }

        public IFolder parse_folder(string path) {
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
