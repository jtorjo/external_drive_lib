using System;
using System.Collections.Generic;
using System.IO;
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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableWindow(IntPtr hWnd,bool bEnable);

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
                                var class_names = children.Select(c => new Tuple<IntPtr, string>(c,window_class_name(c))).ToList();
                                var is_windows_progress_dialog = class_names.Any(c => c.Item2 == "DirectUIHWND") && class_names.Any(c => c.Item2 == "msctls_progress32");
                                if (is_windows_progress_dialog) {
                                    // found a shell copy/move/delete window

                                    // 1.2.5+ trial and error - seems that disabling the window ends up hiding it from screen as well
                                    // however, I'm still moving it, just a precaution
                                    //
                                    // note: I'm disabling the window, so that the user can't press Esc/Enter to cancel the process
                                    EnableWindow(w, false);

                                    // hiding the window doesn't work, minimizing would be useless - so, just move it outside the screen
                                    MoveWindow(w, -100000, 10, 600, 300, false);
                                }
                            }
                        }
                }
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }
        private enum GET_FILEEX_INFO_LEVELS {
            GetFileExInfoStandard,
            GetFileExMaxInfoLevel
        }
        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileAttributesEx(string lpFileName, GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA fileData);

        // note: FileInfo.Length is too slow!
        public static uint file_len(string file_name) {
            WIN32_FILE_ATTRIBUTE_DATA fa;
            if (GetFileAttributesEx(file_name, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out fa))
                return fa.nFileSizeLow;
            else if (File.Exists(file_name))
                return (uint) new FileInfo(file_name).Length;
            else
                return 0;
        }

    }


}
