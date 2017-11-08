using System.Collections.Generic;
using System.IO;
using external_drive_lib.interfaces;
using external_drive_lib.portable;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.util
{
    static class portable_util
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void enumerate_children(portable_drive drive, FolderItem fi, List<IFolder> folders, List<IFile> files) {
            folders.Clear();
            files.Clear();

            if ( fi.IsFolder)
                foreach ( FolderItem child in (fi.GetFolder as Folder).Items())
                    if (child.IsLink) 
                        logger.Fatal("android shortcut " + child.Name);                    
                    else if (child.IsFolder) 
                        folders.Add(new portable_folder(drive, child));
                    else 
                        files.Add(new portable_file(drive, child as FolderItem2));
        }

        // for testing
        public static List<string> get_verbs(FolderItem fi) {
            var list = new List<string>();
            foreach ( FolderItemVerb verb in fi.Verbs())
                list.Add(verb.Name);
            return list;
        }

        public static long portable_file_size(FolderItem2 fi) {
            try {
                var sz = (long)fi.ExtendedProperty("size");
                return sz;
            } catch {
            }
            try {
                // this will return something like, "3.34 KB" or so
                var size_str = (fi.Parent as Folder).GetDetailsOf(fi, 2).ToLower();

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

        public static Folder get_my_computer() {
            return win_util.get_shell32_folder(0x11);
        }

        public static List<FolderItem> get_portable_connected_device_drives() {
            var usb_drives = new List<FolderItem>();

            foreach (FolderItem fi in get_my_computer().Items()) {
                var path = fi.Path;
                if (Directory.Exists(path) || path.Contains(":\\"))
                    continue;
                usb_drives.Add(fi);
            }
            return usb_drives;
        }

    }
}
