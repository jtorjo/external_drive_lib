using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace external_drive_lib.interfaces {
    public interface IFile {
        // guaranteed to NOT THROW
        string name { get; }

        IFolder folder { get; }
        string full_path { get; }

        // guaranteed to NOT THROW
        bool exists { get; }

        long size { get; }
        DateTime last_write_time { get; }

        // note: dest_path can be to another external drive
        // throws if there's an error
        //
        // note: move it implemented via copy() + delete()
        void copy(string dest_path);
        // throws if there's an error
        void delete();
    }

}

