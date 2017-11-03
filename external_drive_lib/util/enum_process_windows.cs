using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace external_drive_lib.util
{
    class enum_process_windows
    {
        private delegate bool EnumWindowDelegate(IntPtr hwnd, IntPtr lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowDelegate lpEnumFunc, IntPtr lParam);

        // computing the threads can be CPU intensive, avoid it if possible
        private const int MAX_REUSE_THREADS = 20;

        private Process process_;
        private List<int> thread_ids_ = new List<int>();
        private int reused_thread_count_ = 0;

        private List<IntPtr> toplevel_windows_ = new List<IntPtr>();
        private List<IntPtr> child_windows_ = new List<IntPtr>();

        public enum_process_windows() {
            refresh_process();

        }

        private void refresh_process() {
            try {
                var thread_ids = new List<int>();
                process_ = Process.GetCurrentProcess();
                foreach (ProcessThread t in process_.Threads)
                    thread_ids.Add(t.Id);
                lock(this)
                    thread_ids_ = thread_ids;
            } catch {
            }
        }

        private bool add_window(IntPtr hWnd, IntPtr lParam) {
            toplevel_windows_.Add(hWnd);
            return true;
        }
        private bool add_child_window(IntPtr hWnd, IntPtr lParam) {
            child_windows_.Add(hWnd);
            return true;
        }

        public IReadOnlyList<IntPtr> get_all_top_windows() {
            if (++reused_thread_count_ % MAX_REUSE_THREADS == 0)
                refresh_process();

            toplevel_windows_.Clear();
            foreach ( var t in thread_ids_)
                EnumThreadWindows((uint)t, add_window, IntPtr.Zero);

            return toplevel_windows_;
        }

        public IReadOnlyList<IntPtr> get_child_windows(IntPtr w) {
            child_windows_.Clear();
            EnumChildWindows(w, add_child_window, IntPtr.Zero);
            return child_windows_;
        }

    }
}
