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
        private FolderItem2 fi_;
        private android_drive drive_;
        public android_file(android_drive drive, FolderItem2 fi) {
            drive_ = drive;
            fi_ = fi;
            Debug.Assert(!fi.IsFolder);

        }

        public string name => fi_.Name;

        public IFolder folder => new android_folder(drive_, (fi_.Parent as Folder2).Self);

        public string full_path => drive_.parse_android_path(fi_);

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
                try {
                    var sz = (long)fi_.ExtendedProperty("size");
                    return sz;
                } catch {
                }
                try {
                    // this will return something like, "3.34 KB" or so
                    var size_str = (fi_.Parent as Folder).GetDetailsOf(fi_, 2).ToLower();

                    var multiply_by = 1;
                    if (size_str.EndsWith("kb")) {
                        multiply_by = 1024;
                        size_str = size_str.Substring(0, size_str.Length - 2);
                    } else if (size_str.EndsWith("mb")) {
                        multiply_by = 1024 * 1024;
                        size_str = size_str.Substring(0, size_str.Length - 2);
                    }
                    size_str = size_str.Trim();

                    double size_double = 0;
                    double.TryParse(size_str, out size_double);
                    return (long) (size_double * multiply_by);
                } catch {
                    return -1;
                }
            }
        }

        public DateTime last_write_time {
            get {
                try {
                    var dt = (DateTime)fi_.ExtendedProperty("write");
                    return dt;
                } catch {
                }
                try {
                    // this will return something like "5/11/2017 08:29"
                    var date_str = (fi_.Parent as Folder).GetDetailsOf(fi_, 3).ToLower();
                    var dt_backup = DateTime.Parse(date_str);
                    return dt_backup;
                } catch {
                    return DateTime.MinValue;
                }
            }
        }

        public void copy(string dest_path) {
        }

        
        public void delete() {
            // https://stackoverflow.com/questions/22693693/delete-a-folderitem
        }
    }
}
