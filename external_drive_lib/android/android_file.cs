using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using Shell32;

namespace external_drive_lib.android
{
    internal class android_file : IFile
    {
        private FolderItem fi_;
        private android_drive drive_;
        public android_file(android_drive drive, FolderItem fi) {
            drive_ = drive;
            fi_ = fi;
            Debug.Assert(!fi.IsFolder);

            #if DEBUG
            dump_info();
            #endif
        }

        public string name => fi_.Name;

        public IFolder folder {
            get {
                // or fi_.parsename
                var parent = fi_.Parent as FolderItem;
                return new android_folder(drive_, parent);
            }
        }

        // for testing
        public void dump_info() {
            //Console.WriteLine("file " + name + " p=" + full_path + " size=" + size + " time=" + last_write_time);
            var parent = fi_.Parent as Folder;
            var headers = new List<string>();
            for (short i = 0; i < short.MaxValue; ++i) {
                var header = parent.GetDetailsOf(null, i);
                if (!string.IsNullOrEmpty(header))
                    headers.Add(header);
                else
                    break;
            }

            for (int i = 0; i < headers.Count; ++i) {
                var info = parent.GetDetailsOf(fi_, i);
                Console.WriteLine("details " + headers[i] + " = " + info);
            }
            var size = fi_.Size;
            Console.WriteLine(size);
        }

        // need to replace the drive's root id? TOTHINK (when getting full path)
        public string full_path {             
            get {
                var full = fi_.Path;
                Debug.Assert(full.StartsWith(drive_.root_name));
                var id = "{" + drive_.unique_id + "}";
                full = full.Replace(drive_.root_name, id);
                return full;
            }
        }

        // FIXME to test
        public bool exists {
            get {
                try {
                    var s = size;
                    return true;
                } catch {
                    return false;
                }
            }
        }

        public IDrive drive => drive_;

        public long size {
            get {
                var size_str = (fi_.Parent as Folder).GetDetailsOf(fi_, 1);
                long size_int = 0;
                long.TryParse(size_str, out size_int);
                return size_int;
            }
        }
        public DateTime last_write_time => fi_.ModifyDate;

        public void copy(string dest_path) {
        }

        
        public void delete() {
            // https://stackoverflow.com/questions/22693693/delete-a-folderitem
        }
    }
}
