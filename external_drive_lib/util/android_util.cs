using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using external_drive_lib.portable;
using Shell32;

namespace external_drive_lib.android
{
    static class android_util
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

        public static long android_file_size(FolderItem2 fi) {
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
    }
}
