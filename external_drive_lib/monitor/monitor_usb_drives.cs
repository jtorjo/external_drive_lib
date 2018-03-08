using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace external_drive_lib.monitor
{
    class monitor_usb_drives {
        public bool include_size_in_unique_id = false;
        private monitor_devices monitor_;

        private Dictionary<char, string> drive_to_unique_id_ = new Dictionary<char, string>();

        public monitor_usb_drives() {
            monitor_ = new monitor_devices { added_device = on_new_drive, deleted_device = on_del_drive};
            monitor_.monitor("Win32_LogicalDisk");
            try {
                foreach (var drive in find_devices.find_objects("Win32_LogicalDisk"))
                    on_new_drive(drive);
            } catch {
            }
        }

        // returns the drive's unique id or null if drive not found
        public string unique_id(char drive) {
            lock(this)
                if (drive_to_unique_id_.ContainsKey(drive))
                    return drive_to_unique_id_[drive];
            return null;
        }

        private void on_new_drive(Dictionary<string,string> properties) {
            var ok = properties.ContainsKey("DeviceID") && properties.ContainsKey("Name") && properties.ContainsKey("Size") 
                     && properties.ContainsKey("VolumeSerialNumber") && properties["DeviceID"] == properties["Name"] 
                     // it has to be like "D:"
                     && properties["DeviceID"].Length == 2;
            if (ok) {
                var drive = properties["DeviceID"].ToUpper()[0];
                var unique_id = properties["VolumeSerialNumber"];
                if (include_size_in_unique_id)
                    unique_id += "-" + properties["Size"];
                lock(this)
                    if (drive_to_unique_id_.ContainsKey(drive))
                        drive_to_unique_id_[drive] = unique_id;
                    else 
                        drive_to_unique_id_.Add(drive, unique_id);
            }
        }
        private void on_del_drive(Dictionary<string,string> properties) {
            var ok = properties.ContainsKey("DeviceID") && properties.ContainsKey("Name") && properties.ContainsKey("Size") 
                     && properties.ContainsKey("VolumeSerialNumber") && properties["DeviceID"] == properties["Name"] 
                     // it has to be like "D:"
                     && properties["DeviceID"].Length == 2;
            if (ok) {
                var drive = properties["DeviceID"].ToUpper()[0];
                lock (this)
                    drive_to_unique_id_.Remove(drive);
            }
        }
    }
}
