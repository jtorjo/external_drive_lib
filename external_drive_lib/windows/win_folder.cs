using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;

namespace external_drive_lib.windows
{
    class win_folder : IFolder2
    {
        public string name { get; }
        public IDrive parent_drive { get; }
        public IFolder parent { get; }
        public IEnumerable<IFile> files { get; }
        public IEnumerable<IFolder> child_folders { get; }
        public void copy_file(IFile file) {
        }
    }
}
