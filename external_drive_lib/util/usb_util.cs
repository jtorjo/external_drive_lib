using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.monitor;

namespace external_drive_lib.util
{
    public static class usb_util
    {

        public static bool pnp_device_id_to_vidpid_and_unique_id(string device_id, ref string vid_pid, ref string unique_id) {
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

        // Example:
        // ::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\\\\\\?\\usb#vid_04e8&pid_6860&ms_comp_mtp&samsung_android#6&1a1242af4&0&0000#{6b327878-a6fa-4155-b985-f98e491d4f33}
        public static bool portable_path_to_vidpid(string path, ref string vid_pid) {
            var idx = path.IndexOf("vid_");
            if (idx >= 0) {
                var idx2 = path.IndexOf("pid_", idx);
                if (idx2 >= 0) {
                    var idx3 = idx2 + 4;
                    while (Char.IsDigit(path[idx3]))
                        ++idx3;
                    vid_pid = path.Substring(idx, idx3 - idx);
                    return true;
                }
            }

            Debug.Assert(false);
            return false;
        }


        // for testing - run into problems? please run this:
        /* 
            foreach ( var p in usb_util.get_all_portable_paths())
                Console.WriteLine(p);
            foreach ( var p in usb_util.get_all_usb_pnp_device_ids())
                Console.WriteLine(p);
         */
        public static List<string> get_all_portable_paths() {
            var portable_devices = portable_util.get_portable_connected_device_drives();
            return portable_devices.Select(d => d.Path).ToList();
        }

        // for testing - run into problems? please run this:
        /* 
            foreach ( var p in usb_util.get_all_portable_paths())
                Console.WriteLine(p);
            foreach ( var p in usb_util.get_all_usb_pnp_device_ids())
                Console.WriteLine(p);
         */
        public static List<string> get_all_usb_pnp_device_ids() {
            var existing_devices = find_devices.find_objects("Win32_USBHub");
            return existing_devices.Select(d => d.ContainsKey("PNPDeviceID") ? d["PNPDeviceID"] : "--INVALID-DEVICE--").ToList();            
        }
    }
}
