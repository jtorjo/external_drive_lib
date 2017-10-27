using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;

namespace external_drive_lib.windows
{
    class win_drive : IDrive
    {
        public bool is_connected() {
            return false;
        }

        public drive_type type {
            get { return drive_type._internal_hdd; }
        }

        public string root_name { get; }
        public string unique_id { get; }
        public string friendly_name { get; }
        public string full_friendly_name { get; }
        public IEnumerable<IFolder> folders { get; }
        public IEnumerable<IFile> files { get; }
        public IFile parse_file(string path) {
            return null;
        }

        public IFolder parse_folder(string path) {
            return null;
        }
    }
}
