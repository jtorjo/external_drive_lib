using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace external_drive_lib.interfaces
{
    public interface IFolder {
        string name { get; }

        // this is non-null only if the parent folder is null!
        IDrive parent_drive { get; }

        IFolder parent { get; }

        IEnumerable<IFile> files { get; }
        IEnumerable<IFolder> child_folders { get; }

    }

    // this is not exposed - so that users only use IFile.copy() instead
    internal interface IFolder2 : IFolder {
        // this is the only way to make sure a file gets copied where it should, no matter where the destination is
        // (since we could copy a file from android to sd card or whereever)
        void copy_file(IFile file);
        
    }
}
