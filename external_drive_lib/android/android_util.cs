using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.interfaces;
using Shell32;

namespace external_drive_lib.android
{
    static class android_util
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void enumerate_children(android_drive drive, FolderItem fi, List<IFolder> folders, List<IFile> files) {
            folders.Clear();
            files.Clear();

            if ( fi.IsFolder)
                foreach ( FolderItem child in (fi.GetFolder as Folder).Items())
                    if (child.IsLink) 
                        logger.Fatal("android shortcut " + child.Name);                    
                    else if (child.IsFolder) 
                        folders.Add(new android_folder(drive, child));
                    else 
                        files.Add(new android_file(drive, child as FolderItem2));
        }

        // for testing
        public static List<string> get_verbs(FolderItem fi) {
            var list = new List<string>();
            foreach ( FolderItemVerb verb in fi.Verbs())
                list.Add(verb.Name);
            return list;
        }
    }
}
