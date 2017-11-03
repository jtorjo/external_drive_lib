using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using external_drive_lib.android;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using external_drive_lib.monitor;
using external_drive_lib.util;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib
{
    /* the root - the one that contains all external drives 
     */
    public class drive_root {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static drive_root inst { get; } = new drive_root();

        private bool auto_close_win_dialogs_ = true;

        private monitor_devices monitor_devices_ = new monitor_devices ();
        
        private Dictionary<string,string> vidpid_to_unique_id_ = new Dictionary<string, string>();

        private drive_root() {
            var existing_devices = find_devices.find_objects("Win32_USBHub");
            foreach (var device in existing_devices) {
                if (device.ContainsKey("PNPDeviceID")) {
                    var device_id = device["PNPDeviceID"];
                    string vid_pid = "", unique_id = "";
                    if (pnp_device_id_to_vidpid_and_unique_id(device_id, ref vid_pid, ref unique_id)) {
                        lock(this)
                            vidpid_to_unique_id_.Add(vid_pid, unique_id);
                    }
                }
            }

            refresh();

            monitor_devices_.added_device += device_added;
            monitor_devices_.deleted_device += device_removed;
            monitor_devices_.monitor("Win32_USBHub");

            new Thread(win32_util.check_for_dialogs_thread) {IsBackground = true}.Start();
        }

        // returns all drives, even the internal HDDs - you might need this if you want to copy a file onto an external drive
        public IReadOnlyList<IDrive> all_drives {
            get { lock(this) return drives_; }
        }
        public IReadOnlyList<IDrive> external_drives {
            get { lock(this) return external_drives_; }
        }

        private static bool pnp_device_id_to_vidpid_and_unique_id(string device_id, ref string vid_pid, ref string unique_id) {
            device_id = device_id.ToLower();
            vid_pid = unique_id = "";
            var valid = device_id.StartsWith("usb\\") && device_id.Contains("vid") && device_id.Contains("pid") && device_id.Count(c => c == '\\') >= 2;
            if (valid) {
                device_id = device_id.Substring(4);
                var idx = device_id.IndexOf("\\");
                vid_pid = device_id.Substring(0, idx);
                unique_id = device_id.Substring(idx + 1).Trim();
                if (vid_pid.Count(c => c == '&') > 1)
                    // some USB devices also expose an external removable drive (which can contain drivers to install) - we ignore that
                    return false;
                return true;
            }
            return false;
        }

        private void device_added(Dictionary<string, string> properties) {
            if (properties.ContainsKey("PNPDeviceID")) {
                var device_id = properties["PNPDeviceID"];
                string vid_pid = "", unique_id = "";
                if ( pnp_device_id_to_vidpid_and_unique_id(device_id, ref vid_pid, ref unique_id)) {
                    lock (this) {
                        if (vidpid_to_unique_id_.ContainsKey(vid_pid))
                            vidpid_to_unique_id_[vid_pid] = unique_id;
                        else 
                            vidpid_to_unique_id_.Add(vid_pid, unique_id);
                    }
                    refresh_android_unique_ids();
                    var already_a_drive = false;
                    lock (this) {
                        var ad = drives_.FirstOrDefault(d => d.unique_id == unique_id) as android_drive;
                        if (ad != null) {
                            ad.connected_via_usb = true;
                            already_a_drive = true;
                        }
                    }
                    if ( !already_a_drive)
                        win_util.postpone(() => monitor_for_drive(vid_pid, 0), 50);
                }
            }
            else 
                logger.Fatal("added usb device with no PNPDeviceID");
        }

        // here, we know the drive was connected, wait a bit until it's actually visible
        private void monitor_for_drive(string vidpid, int idx) {
            const int MAX_RETRIES = 10;
            var drives_now = get_android_drives();
            var found = drives_now.FirstOrDefault(d => (d as android_drive).vid_pid == vidpid);
            if (found != null) 
                refresh();
            else if ( idx < MAX_RETRIES)
                win_util.postpone(() => monitor_for_drive(vidpid, idx + 1), 50);
            else 
                logger.Fatal("can't find usb connected drive " + vidpid);
        }

        private void device_removed(Dictionary<string, string> properties) {
            if (properties.ContainsKey("PNPDeviceID")) {
                var device_id = properties["PNPDeviceID"];
                string vid_pid = "", unique_id = "";
                if (pnp_device_id_to_vidpid_and_unique_id(device_id, ref vid_pid, ref unique_id)) {
                    lock (this) {
                        var ad = drives_.FirstOrDefault(d => d.unique_id == unique_id) as android_drive;
                        if (ad != null)
                            ad.connected_via_usb = false;                        
                    }
                    refresh();
                }
            }
            else 
                logger.Fatal("deleted usb device with no PNPDeviceID");            
        }


        public bool auto_close_win_dialogs {
            get { return auto_close_win_dialogs_; }
            set {
                if (auto_close_win_dialogs_ == value)
                    return;
                auto_close_win_dialogs_ = value;
            }
        }

        // this includes all drives, even the internal ones
        private List<IDrive> drives_ = new List<IDrive>();
        private List<IDrive> external_drives_ = new List<IDrive>();

        public void refresh() {
            List<IDrive> drives_now = new List<IDrive>();
            try {
                drives_now.AddRange(get_win_drives());
            } catch (Exception e) {
                logger.Error("error getting win drives " + e);
            }
            try {
                drives_now.AddRange(get_android_drives());
            } catch (Exception e) {
                logger.Error("error getting android drives " + e);
            }
            var external = drives_now.Where(d => d.type != drive_type.hdd).ToList();
            lock (this) {
                drives_ = drives_now;
                external_drives_ = external;
            }
            refresh_android_unique_ids();
        }

        private void refresh_android_unique_ids() {
            lock(this)
                foreach ( android_drive ad in drives_.OfType<android_drive>())
                    if ( vidpid_to_unique_id_.ContainsKey(ad.vid_pid))
                        ad.unique_id = vidpid_to_unique_id_[ad.vid_pid];
        }

        // As drive name, use any of: "{<unique_id>}:", "<drive-name>:", "[a<android-drive-index>]:", "[d<drive-index>]:"
        public IDrive try_get_drive(string drive_prefix) {
            // case insensitive
            foreach ( var d in all_drives)
                if (string.Compare(d.root_name, drive_prefix, StringComparison.CurrentCultureIgnoreCase) == 0 ||
                    string.Compare("{" + d.unique_id + "}:\\", drive_prefix, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return d;

            if (drive_prefix.StartsWith("[") && drive_prefix.EndsWith("]:\\")) {
                drive_prefix = drive_prefix.Substring(1, drive_prefix.Length - 4);
                if (drive_prefix.StartsWith("d", StringComparison.CurrentCultureIgnoreCase)) {
                    // d<drive-index>
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var all = all_drives;
                        if (all.Count > idx)
                            return all[idx];
                    }
                }
                else if (drive_prefix.StartsWith("a", StringComparison.CurrentCultureIgnoreCase)) {
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var android = all_drives.Where(d => d is android_drive).ToList();
                        if (android.Count > idx)
                            return android[idx];
                    }                    
                }
            }

            return null;
        }
        // throws if drive not found
        public IDrive get_drive(string drive_prefix) {
            // case insensitive
            var d = try_get_drive(drive_prefix);
            if ( d == null)
                throw new exception("invalid drive " + drive_prefix);
            return d;
        }

        private void split_into_drive_and_folder_path(string path, out string drive, out string folder_or_file) {
            path = path.Replace("/", "\\");
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
            return drive.parse_file_name(folder_or_file);
        }

        // throws if anything goes wrong
        public IFolder parse_folder(string path) {
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new exception("invalid path " + path);
            var drive = get_drive(drive_str);
            return drive.parse_folder_name(folder_or_file);
        }

        // creates all folders up to the given path
        public IFolder new_folder(string path) {
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new exception("invalid path " + path);
            var drive = get_drive(drive_str);
            return drive.create_folder(folder_or_file);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Android

        private static Folder get_my_computer() {
            return win_util.get_shell32_folder(0x11);
        }

        private static List<FolderItem> get_android_connected_device_drives() {
            var usb_drives = new List<FolderItem>();

            foreach (FolderItem fi in get_my_computer().Items()) {
                var path = fi.Path;
                if (Directory.Exists(path) || path.Contains(":\\"))
                    continue;
                usb_drives.Add(fi);
            }
            return usb_drives;
        }

        private List<IDrive> get_android_drives() {
            var new_drives = get_android_connected_device_drives().Select(d => new android_drive(d) as IDrive).ToList();
            List<IDrive> old_drives = null;
            lock (this)
                old_drives = drives_.Where(d => d is android_drive).ToList();

            // if we already have this drive, reuse that
            List<IDrive> result = new List<IDrive>();
            foreach (var new_ in new_drives) {
                var old = old_drives.FirstOrDefault(od => od.root_name == new_.root_name);
                result.Add(old ?? new_);
            }
            return result;
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
