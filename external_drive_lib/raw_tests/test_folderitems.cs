using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using external_drive_lib.portable;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.raw_tests
{
    public static class test_folderitems
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void test_win_folderitems3() {
            var dir = win_util.get_shell32_folder("D:\\cool_pics\\a00\\b0\\c0_copy");
            var items = dir.Items() as FolderItems3;
            foreach ( FolderItemVerb v in items.Verbs)
                Console.WriteLine(v.Name);

            /*
            foreach (FolderItem2 i in items) {
                var verbs = "";
                foreach (FolderItemVerb v in i.Verbs())
                    verbs += "," + v.Name;
                Console.WriteLine(verbs);
            }*/

            var a = items.Count;
            // apparently, flags is this https://msdn.microsoft.com/en-us/library/windows/desktop/bb762539(v=vs.85).aspx ?
            //items.Filter(int.MaxValue, "*7*.jpg");
            items.Filter(int.MaxValue, "20161115_082552.*;20161115_104937.*;20161115_105112.*;*7*.jpg");
            var b = items.Count;
            Console.WriteLine(a - b);
        }

        public static void test_android_folderitems3() {
            const string android_prefix = "{galaxy s6}";
            var dir = (drive_root.inst.parse_folder(android_prefix + ":/phone/dcim/bulk") as portable_folder).raw_folder_item().GetFolder as Folder3;
            IShellFolderViewDual vd = dir as IShellFolderViewDual;
            var items = dir.Items() as FolderItems3;
            foreach ( FolderItemVerb v in items.Verbs)
                Console.WriteLine(v.Name);

            /*
            foreach (FolderItem2 i in items) {
                var verbs = "";
                foreach (FolderItemVerb v in i.Verbs())
                    verbs += "," + v.Name;
                Console.WriteLine(verbs);
            }*/

            var a = items.Count;
            // apparently, flags is this https://msdn.microsoft.com/en-us/library/windows/desktop/bb762539(v=vs.85).aspx ?
            items.Filter(int.MaxValue, "2*");
            


            //items.Filter(int.MaxValue, "20161115_035718.jpg;20161115_082552.jpg");
            var b = items.Count;
            Console.WriteLine(a - b);
        }

        private static string new_temp_path() {
            var temp_dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\external_drive_temp\\test-" + DateTime.Now.Ticks;
            Directory.CreateDirectory(temp_dir);
            return temp_dir;
        }
        public static void test_long_android_copy_async(string file_name) {
            logger.Debug("android to win");
            drive_root.inst.auto_close_win_dialogs = false;
            var temp_dir = new_temp_path();
            var src_file = drive_root.inst.parse_file(file_name);
            src_file.copy_async(temp_dir);
            Thread.Sleep(15000);

            logger.Debug("android to android");
            drive_root.inst.auto_close_win_dialogs = false;
            src_file.copy_async("[a0]:/phone/dcim");
            Thread.Sleep(15000);

            logger.Debug("win to android");
            var dest_file = temp_dir + "\\" + src_file.name;
            File.Move(dest_file, dest_file + ".renamed");
            drive_root.inst.parse_file(dest_file + ".renamed").copy_async("[a0]:/phone/dcim");
            Thread.Sleep(15000);

            logger.Debug("win to win");
            var temp_dir2 = new_temp_path();
            drive_root.inst.parse_file(dest_file + ".renamed").copy_async(temp_dir2);
            Thread.Sleep(15000);
        }

    }
}
