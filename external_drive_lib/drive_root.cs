using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using external_drive_lib.monitor;
using external_drive_lib.portable;
using external_drive_lib.util;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib
{
    /* the root - the one that contains all external drives 
     */
    public class drive_root {

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
                    if (usb_util.pnp_device_id_to_vidpid_and_unique_id(device_id, ref vid_pid, ref unique_id)) {
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
        public IReadOnlyList<IDrive> drives {
            get { lock(this) return drives_; }
        }


        private void device_added(Dictionary<string, string> properties) {
            if (properties.ContainsKey("PNPDeviceID")) {
                var device_id = properties["PNPDeviceID"];
                string vid_pid = "", unique_id = "";
                if (usb_util.pnp_device_id_to_vidpid_and_unique_id(device_id, ref vid_pid, ref unique_id)) {
                    lock (this) {
                        if (vidpid_to_unique_id_.ContainsKey(vid_pid))
                            vidpid_to_unique_id_[vid_pid] = unique_id;
                        else
                            vidpid_to_unique_id_.Add(vid_pid, unique_id);
                    }
                    refresh_android_unique_ids();
                    var already_a_drive = false;
                    lock (this) {
                        var ad = drives_.FirstOrDefault(d => d.unique_id == unique_id) as portable_drive;
                        if (ad != null) {
                            ad.connected_via_usb = true;
                            already_a_drive = true;
                        }
                    }
                    if (!already_a_drive)
                        win_util.postpone(() => monitor_for_drive(vid_pid, 0), 50);
                }
            } else {
                // added usb device with no PNPDeviceID
                Debug.Assert(false);
            }
        }

        // here, we know the drive was connected, wait a bit until it's actually visible
        private void monitor_for_drive(string vidpid, int idx) {
            const int MAX_RETRIES = 10;
            var drives_now = get_portable_drives();
            var found = drives_now.FirstOrDefault(d => (d as portable_drive).vid_pid == vidpid);
            if (found != null) 
                refresh();
            else if (idx < MAX_RETRIES)
                win_util.postpone(() => monitor_for_drive(vidpid, idx + 1), 50);
            else {
                // "can't find usb connected drive " + vidpid
                Debug.Assert(false);
            }
        }

        private void device_removed(Dictionary<string, string> properties) {
            if (properties.ContainsKey("PNPDeviceID")) {
                var device_id = properties["PNPDeviceID"];
                string vid_pid = "", unique_id = "";
                if (usb_util.pnp_device_id_to_vidpid_and_unique_id(device_id, ref vid_pid, ref unique_id)) {
                    lock (this) {
                        var ad = drives_.FirstOrDefault(d => d.unique_id == unique_id) as portable_drive;
                        if (ad != null)
                            ad.connected_via_usb = false;
                    }
                    refresh();
                }
            } else {
                // deleted usb device with no PNPDeviceID
                Debug.Assert(false);
            }
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

        public void refresh() {
            List<IDrive> drives_now = new List<IDrive>();
            try {
                drives_now.AddRange(get_win_drives());
            } catch (Exception e) {
                throw new exception( "error getting win drives ", e);
            }
            try {
                drives_now.AddRange(get_portable_drives());
            } catch (Exception e) {
                throw new exception("error getting android drives ", e);
            }
            var external = drives_now.Where(d => d.type != drive_type.internal_hdd).ToList();
            lock (this) {
                drives_ = drives_now;
            }
            refresh_android_unique_ids();
        }

        private void refresh_android_unique_ids() {
            lock(this)
                foreach ( portable_drive ad in drives_.OfType<portable_drive>())
                    if ( vidpid_to_unique_id_.ContainsKey(ad.vid_pid))
                        ad.unique_id = vidpid_to_unique_id_[ad.vid_pid];
        }

        // As drive name, use any of: "{<unique_id>}:", "<drive-name>:", "[a<android-drive-index>]:", "[p<portable-index>]", "[d<drive-index>]:"
        public IDrive try_get_drive(string drive_prefix) {
            // case insensitive
            foreach ( var d in drives)
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
                        var all = drives;
                        if (all.Count > idx)
                            return all[idx];
                    }
                }
                else if (drive_prefix.StartsWith("a", StringComparison.CurrentCultureIgnoreCase)) {
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var android = drives.Where(d => d.type.is_android()).ToList();
                        if (android.Count > idx)
                            return android[idx];
                    }                    
                }
                else if (drive_prefix.StartsWith("p", StringComparison.CurrentCultureIgnoreCase)) {
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var android = drives.Where(d => d.type.is_portable()).ToList();
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

        // returns null on failure
        public IFile try_parse_file(string path) {
            // split into drive + path
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if (drive_str == null)
                return null;
            var drive = get_drive(drive_str);
            return drive.try_parse_file(folder_or_file);            
        }

        // returns null on failure
        public IFolder try_parse_folder(string path) {
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                return null;
            var drive = try_get_drive(drive_str);
            if (drive == null)
                return null;
            return drive.try_parse_folder(folder_or_file);            
        }

        // throws if anything goes wrong
        public IFile parse_file(string path) {
            // split into drive + path
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new exception("invalid path " + path);
            var drive = try_get_drive(drive_str);
            if (drive == null)
                return null;
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
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new exception("invalid path " + path);
            var drive = get_drive(drive_str);
            return drive.create_folder(folder_or_file);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Portable


        private List<IDrive> get_portable_drives() {
            var new_drives = portable_util. get_portable_connected_device_drives().Select(d => new portable_drive(d) as IDrive).ToList();
            List<IDrive> old_drives = null;
            lock (this)
                old_drives = drives_.Where(d => d is portable_drive).ToList();

            // if we already have this drive, reuse that
            List<IDrive> result = new List<IDrive>();
            foreach (var new_ in new_drives) {
                var old = old_drives.FirstOrDefault(od => od.root_name == new_.root_name);
                result.Add(old ?? new_);
            }
            return result;
        }

        // END OF Portable
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
