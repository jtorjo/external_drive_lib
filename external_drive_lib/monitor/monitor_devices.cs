using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.exceptions;

namespace external_drive_lib.monitor
{
    public class monitor_devices
    {
        private void device_inserted_event(object sender, EventArrivedEventArgs e)
        {
            try {
                Dictionary<string,string> properties = new Dictionary<string, string>();
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                foreach (var p in instance.Properties)
                    if ( p.Value != null)
                        properties.Add(p.Name, p.Value.ToString());

                added_device?.Invoke(properties);
            } catch (Exception ex) {
                throw new exception( "invalid device inserted", ex);
            }
        }

        private void device_removed_event(object sender, EventArrivedEventArgs e)
        {
            try {
                Dictionary<string,string> properties = new Dictionary<string, string>();
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                foreach (var p in instance.Properties)
                    if ( p.Value != null)
                        properties.Add(p.Name, p.Value.ToString());

                deleted_device?.Invoke(properties);
            } catch (Exception ex) {
                throw new exception("invalid device removed", ex);
            }
        }

        public Action<Dictionary<string, string>> added_device;
        public Action<Dictionary<string, string>> deleted_device;

        // not used yet, however, tested and works
        // examples: generic_monitor("Win32_USBHub"); generic_monitor("Win32_DiskDrive");
        public void monitor(string class_name)
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA '" + class_name + "'");

            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(device_inserted_event);
            insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA '" + class_name + "'");
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(device_removed_event);
            removeWatcher.Start();
        }

    }
}
