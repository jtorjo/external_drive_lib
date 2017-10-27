using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;

namespace external_drive_lib.windows
{
    class win_file : IFile
    {
        public string name { get; }
        public IFolder folder { get; }
        public string full_path { get; }
        public void copy(string dest_path) {
        }

        public void delete() {
        }
    }
}
