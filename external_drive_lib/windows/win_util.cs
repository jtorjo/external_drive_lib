using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shell32;

namespace external_drive_lib.windows
{
    internal static class win_util {
        private static readonly string temporary_root_dir_ = temporary_root_dir_impl();
        private static string temporary_root_dir_impl() {
            // we need a unique folder each time we're run, so that we never run into conflicts when moving stuff here
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\external_drive_temp\\" + DateTime.Now.Ticks;
            // FIXME create a task to erase all other folders (previously created that is)
            try {
                Directory.CreateDirectory(dir);
            } catch {
            }
            return dir;
        }

        public static string temporary_root_dir() {
            return temporary_root_dir_;
        }

        public static Folder get_shell32_folder(object folder_path)
        {
            Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
            Object shell = Activator.CreateInstance(shellAppType);
            return (Folder)shellAppType.InvokeMember("NameSpace",
                                                     System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { folder_path });
        }

        public static void delete_folder_item(FolderItem fi) {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/bb787874(v=vs.85).aspx
            var move_options = 4 | 16 | 512 | 1024;
            try {
                var temp = win_util.temporary_root_dir();
                var temp_folder = win_util.get_shell32_folder(temp);
                var folder_or_file_name = fi.Name;
                temp_folder.MoveHere(fi, move_options);
                var name = temp + "\\" + folder_or_file_name;
                if ( File.Exists(name))
                    File.Delete(name);
                else 
                    Directory.Delete(temp + "\\" + folder_or_file_name, true);
            } catch {
                // backup - this will prompt a confirmation dialog
                // googled it quite a bit - there's no way to disable it
                fi.InvokeVerb("delete");
            }
            
        }
    }
}
