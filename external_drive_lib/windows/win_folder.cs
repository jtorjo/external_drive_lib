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
    class win_folder : IFolder2 {
        private string parent_, name_;
        public win_folder(string parent_folder, string folder_name) {
            parent_ = parent_folder;
            name_ = folder_name;

            Debug.Assert(!parent_.EndsWith("\\") || parent_is_drive());
            // drive len is 3
            Debug.Assert(parent_.Length >= 3);
        }
        public string name {
            get { return name_; }
        }

        public bool exists => Directory.Exists(folder_name());

        public string full_path => folder_name();
        public IDrive drive => new win_drive(parent_.Substring(0,3));

        private bool parent_is_drive() {
            return parent_.Length <= 3;
        }

        //public IDrive parent_drive => parent_is_drive() ? new win_drive(parent_) : null;

        public IFolder parent {
            get {
                if (parent_is_drive())
                    return null;
                var di = new DirectoryInfo(parent_);
                return new win_folder(di.Parent.FullName, di.Name);
            }
        }

        private string folder_name() {
            return parent_ + "\\" + name_;
        }

        public IEnumerable<IFile> files {
            get {
                var fn = folder_name();
                return new DirectoryInfo(fn).EnumerateFiles().Select(f => new win_file(fn, f.Name));
            }
        }

        public IEnumerable<IFolder> child_folders {
            get {
                var fn = folder_name();
                return new DirectoryInfo(fn).EnumerateDirectories().Select(f => new win_folder(fn, f.Name));
            }
        }

        public void delete() {
            Directory.Delete(folder_name(), true);
        }


        public void copy_file(IFile file) {
            // FIXME if from Android, it's different
            var dest_path = folder_name() + "\\" + file.name;
            File.Copy(file.full_path, dest_path, true);
        }
    }
}
