using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.exceptions;
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
            // drive len is 3
            Debug.Assert(path_.Length >= 3);
        }

        public string name => name_;

        public IFolder folder {
            get {
                var di = new DirectoryInfo(path_);
                return new win_folder( di.Parent.FullName, di.Name );
            }
        }

        public string full_path => path_ + "\\" + name_;

        public bool exists => File.Exists(full_path);
        public IDrive drive => new win_drive(path_.Substring(0,3));

        public long size => new FileInfo(full_path).Length;
        public DateTime last_write_time => new FileInfo(full_path).LastWriteTime;

        public void copy_async(string dest_path) {
            var dest = drive_root.inst.parse_folder(dest_path) as IFolder2;
            if ( dest != null)
                dest.copy_file(this, false);
            else 
                throw new exception("destination path does not exist: " + dest_path);
        }

        public void copy_sync(string dest_path) {
            var dest = drive_root.inst.parse_folder(dest_path) as IFolder2;
            if ( dest != null)
                dest.copy_file(this, true);
            else 
                throw new exception("destination path does not exist: " + dest_path);
        }

        public void delete_async() {
            File.Delete(full_path);
        }

        public void delete_sync() {
            File.Delete(full_path);
        }


    }
}
