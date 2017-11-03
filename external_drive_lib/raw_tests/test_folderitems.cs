using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib.windows;
using Shell32;

namespace external_drive_lib.raw_tests
{
    public static class test_folderitems
    {

        public static void test_folderitems3() {
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
    }
}
