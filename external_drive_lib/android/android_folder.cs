using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using Shell32;

namespace external_drive_lib.android
{
    internal class android_folder : IFolder2 {
        private FolderItem fi_;
        public android_folder(FolderItem fi) {
            fi_ = fi;
        }
        
        public string name { get; }
        public IDrive parent_drive { get; }
        public IFolder parent { get; }
        public IEnumerable<IFile> files { get; }
        public IEnumerable<IFolder> child_folders { get; }
        public void copy_file(IFile file) {
        }
    }
}
