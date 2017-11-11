using System;
using System.Diagnostics;
using System.Threading.Tasks;
using external_drive_lib.exceptions;
using external_drive_lib.interfaces;
using external_drive_lib.util;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.portable
{
    internal class portable_file : IFile
    {
        private FolderItem2 fi_;
        private portable_drive drive_;
        public portable_file(portable_drive drive, FolderItem2 fi) {
            drive_ = drive;
            fi_ = fi;
            Debug.Assert(!fi.IsFolder);
        }

        // for android_folder.copy
        internal FolderItem2 raw_folder_item() {
            return fi_;
        }

        public string name => fi_.Name;

        public IFolder folder => new portable_folder(drive_, (fi_.Parent as Folder2).Self);

        public string full_path => drive_.parse_portable_path(fi_);

        public bool exists {
            get {
                try {
                    if (drive.is_available()) {
                        // if this throws, drive exists, but file does not
                        drive_root.inst.parse_file(full_path);
                        return true;
                    }
                } catch {
                }
                return false;
            }
        }

        public IDrive drive => drive_;

        public long size {
            get {
                return portable_util.portable_file_size(fi_);
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

        public void copy_async(string dest_path) {
            var dest = drive_root.inst.parse_folder(dest_path) as IFolder2;
            if ( dest != null)
                dest.copy_file(this, false);
            else 
                throw new exception("destination path does not exist: " + dest_path);
        }

        
        public void delete_async() {
            Task.Run( () => win_util.delete_sync_portable_file(fi_));
        }

        public void copy_sync(string dest_path) {
            var dest = drive_root.inst.parse_folder(dest_path) as IFolder2;
            if ( dest != null)
                dest.copy_file(this, true);
            else 
                throw new exception("destination path does not exist: " + dest_path);
        }

        public void delete_sync() {
            win_util.delete_sync_portable_file(fi_);
        }
    }
}
