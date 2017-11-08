using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace external_drive_lib.util
{
    internal static class win32_util
    {

        // Gets the classname of a window.
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32")]
        private static extern bool MoveWindow(IntPtr hWnd,int X,int Y,int nWidth,int nHeight,bool bRepaint );

        private static string window_class_name(IntPtr hWnd)
        {
            int result = 0;
            StringBuilder name = new StringBuilder(32);            
            result = GetClassName(hWnd, name, name.Capacity);
            if (result != 0)
                return name.ToString();
            else
                return "";
        }

        public static void check_for_dialogs_thread() {
            enum_process_windows find_windows = new enum_process_windows();
            HashSet<IntPtr> processed_windows_ = new HashSet<IntPtr>();

            while (true) {
                var check_sleep_ms = drive_root.inst.auto_close_win_dialogs ? 50 : 500;
                Thread.Sleep(check_sleep_ms);
                if (drive_root.inst.auto_close_win_dialogs) {
                    var windows = find_windows.get_all_top_windows();
                    foreach ( var w in windows)
                        if (!processed_windows_.Contains(w)) {
                            processed_windows_.Add(w);
                            var class_name = window_class_name(w);
                            if (class_name == "#32770") {
                                var children = find_windows.get_child_windows(w);
                                var class_names = children.Select(window_class_name).ToList();
                                var is_windows_progress_dialog = class_names.Any(c => c == "DirectUIHWND") && class_names.Any(c => c == "msctls_progress32");
                                if (is_windows_progress_dialog) {
                                    // found a shell copy/move/delete window
                                    // this would minimize it
                                    //ShowWindow(w, 6);
                                    // hiding the window doesn't work - so, just move it outside the screen
                                    MoveWindow(w, -100000, 10, 600, 300, false);
                                    //logger.Debug("closed " + w);
                                }
                            }
                        }
                }
            }
        }
    }
}
