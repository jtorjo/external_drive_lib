using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shell32;

namespace external_drive_lib.windows
{
    static class win_util
    {
        public static string temporary_root_dir() {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\external_drive_temp\\";
            try {
                Directory.CreateDirectory(dir);
            } catch {
            }
            return dir;
        }

        public static Folder get_shell32_folder(object folder_path)
        {
            Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
            Object shell = Activator.CreateInstance(shellAppType);
            return (Folder)shellAppType.InvokeMember("NameSpace",
                                                     System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { folder_path });
        }
    }
}
