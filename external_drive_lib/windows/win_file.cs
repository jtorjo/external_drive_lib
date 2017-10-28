using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;

namespace external_drive_lib.windows
{
    class win_file : IFile {
        private string path_;
        private string name_;
        public win_file(string path, string name) {
            Debug.Assert(!path.EndsWith("\\"));
            path_ = path;
            name_ = name;
        }

        public string name {
            get { return name_; }
        }

        public IFolder folder {
            get {
                var di = new DirectoryInfo(path_);
                return new win_folder( di.Parent.FullName, di.Name );
            }
        }

        public string full_path {
            get { return path_ + "\\" + name_; }
        }

        public void copy(string dest_path) {
            var dest = drive_root.inst.parse_folder(dest_path) as IFolder2;
            dest?.copy_file(this);
        }

        public void delete() {
            File.Delete(full_path);
        }
    }
}
