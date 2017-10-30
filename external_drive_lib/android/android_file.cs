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
            Debug.Assert((fi.Parent as FolderItem).IsFolder);
        }

        public string name => fi_.Name;
        public IFolder folder => new android_folder(drive_, fi_.Parent as FolderItem);
        // need to replace the drive's root id? TOTHINK (when getting full path)
        public string full_path { get; }

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

        public IDrive drive { get; }

        public long size => fi_.Size;
        public DateTime last_write_time => fi_.ModifyDate;

        public void copy(string dest_path) {
        }

        
        public void delete() {
            // https://stackoverflow.com/questions/22693693/delete-a-folderitem
        }
    }
}
