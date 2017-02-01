using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SmartType
{
    public class WindowInfo
    {
        public string processFileName;
        public int keyboardLayout;
        public int hwnd;
        
        public WindowInfo(int hwnd, string filename, int layout)
        {
            this.hwnd = hwnd;
            processFileName = filename;
            keyboardLayout = layout;
        }

        public static WindowInfo GetActiveWindowInfo()
        {
            int hwnd = GetForegroundWindow();

            int threadId, processId;
            threadId = GetWindowThreadProcessId(hwnd, out processId);

            if (threadId == 0 || processId == 0) return new WindowInfo(-1, "unknown", -1);

            int kbLayout = GetKeyboardLayout(threadId);

            Process process = Process.GetProcessById(processId);
            return new WindowInfo(hwnd, process.MainModule.ModuleName, kbLayout);
        }

        public override bool Equals(object obj)
        {
            if(obj is WindowInfo)
            {
                WindowInfo otherWindow = obj as WindowInfo;
                return hwnd == otherWindow.hwnd;
            }
            return false;
        }

        /// <summary>
        /// Поставено просто за да не се оплаква
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return hwnd;
        }

        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetKeyboardLayout(int idThread);

        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);
    }
}
