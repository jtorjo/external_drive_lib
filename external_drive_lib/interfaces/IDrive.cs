using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace external_drive_lib.interfaces
{
    public enum drive_type {
        // if this, we're not sure if it's phone or tablet or whatever
        android, 
        // it's an android phone
        android_phone, 
        // it's an android tablet
        android_tablet, 
        // SD Card
        sd_card, 
        // external hard drive
        external_hdd,

        // it's the Windows HDD - this is used internally
        _internal_hdd
    }

    public interface IDrive {
        // returns true if the drive is connected
        // note: not as a property, since this could actually take time to find out - we don't want to break debugging
        bool is_connected();

        drive_type type { get; }

        // this is the drive path, such as "c:\" - however, for non-conventional drives, it can be a really weird path
        string root_name { get; }

        // the drive's Unique ID - it is the same between program runs
        string unique_id { get; }

        // a friendly name for the drive
        string friendly_name { get; }

        // this is a friendly name for the drive, with a bit more info, in order to uniquely identify it
        // this is more specific than friendly_name ONLY IF POSSIBLE
        //
        // Example: the friendly name can be "Samsung S6", while the full friendly name can be "Samsunt S6 232899932"
        string full_friendly_name { get; }

        IEnumerable<IFolder> folders { get; }
        IEnumerable<IFile> files { get; }

        IFile parse_file(string path);
        IFolder parse_folder(string path);
    }

}
