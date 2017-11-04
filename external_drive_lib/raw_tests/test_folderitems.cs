using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.android;
using external_drive_lib.portable;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.raw_tests
{
    public static class test_folderitems
    {

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
    }
}
