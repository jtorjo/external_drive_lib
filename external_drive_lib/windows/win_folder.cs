using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using external_drive_lib.android;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using Shell32;

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

        public IFolder parent {
            get {
                if (parent_is_drive())
                    return null;
                var di = new DirectoryInfo(parent_);
                return new win_folder(di.Parent.FullName, di.Name);
            }
        }

        private string folder_name() {
            return parent_ + (parent_is_drive() ? "" : "\\") + name_;
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

        public void delete_async() {
            Task.Run(() => delete_sync());
        }

        public void delete_sync() {
            Directory.Delete(folder_name(), true);
        }


        public void copy_file(IFile file, bool synchronous) {
            var copy_options = 4 | 16 | 512 | 1024;
            var andoid = file as android_file;
            var win = file as win_file;
            // it can either be android or windows
            Debug.Assert(andoid != null || win != null);

            var fn = folder_name();
            var dest_path = fn + "\\" + file.name;
            if (win != null) {
                if (synchronous)
                    File.Copy(file.full_path, dest_path, true);
                else
                    Task.Run(() => File.Copy(file.full_path, dest_path, true));
            } else if (andoid != null) {
                // android file to windows:

                // Windows stupidity - if file exists, it will display a stupid "Do you want to replace" dialog,
                // even if we speicifically told it not to (via the copy options)
                if (File.Exists(dest_path))
                    File.Delete(dest_path);
                var shell_folder = win_util.get_shell32_folder(fn);
                shell_folder.CopyHere(andoid.folder_item(), copy_options);
                if ( synchronous)
                    wait_for_copy_complete(file, dest_path);
            }
        }

        private long wait_for_file_size(string file_name, long size, int retry_count) {
            long cur_size = -1;
            for (int i = 0; i < retry_count && cur_size < size; ++i) {
                if ( File.Exists(file_name))
                    try {
                        cur_size = new FileInfo(file_name).Length;
                    } catch {
                    }
                if ( cur_size < size)
                    Thread.Sleep(25);
            }
            return cur_size;
        }

        private void wait_for_copy_complete(IFile file, string dest_path) {
            var size = file.size;

            long last_size = -1;
            // the idea is - if after waiting a while, something got copied (size has changed), we keep waiting
            while (last_size < size) {
                var cur_size = wait_for_file_size(dest_path, size, 10);
                if ( cur_size == last_size)
                    throw new exception("File may have not been copied - " + dest_path + " got " + cur_size + ", expected " + size);
                last_size = cur_size;
            }
        }

    }
}
