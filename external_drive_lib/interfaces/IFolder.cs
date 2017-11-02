using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace external_drive_lib.interfaces
{
    public interface IFolder {
        // guaranteed to NOT THROW
        string name { get; }

        // guaranteed to NOT THROW
        bool exists { get; }

        string full_path { get; }

        IDrive drive { get; }

        // can return null if this is a folder from the drive
        IFolder parent { get; }

        IEnumerable<IFile> files { get; }
        IEnumerable<IFolder> child_folders { get; }

        // throws if there's an error
        void delete_async();

        // throws if there's an error
        void delete_sync();
    }

    // this is not exposed - so that users only use IFile.copy() instead
    internal interface IFolder2 : IFolder {
        // this is the only way to make sure a file gets copied where it should, no matter where the destination is
        // (since we could copy a file from android to sd card or whereever)
        void copy_file(IFile file, bool synchronous);
        
    }
}
