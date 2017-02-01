using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SmartType
{
    public abstract class KeyInjector
    {
        public static void Send(char[] arr, int start, int len)
        {
            for (int i = start; i < len; i++) SendKey(VkToChar.GetVkCode(arr[i]));
        }

        public static void Send(StringBuilder sb, int start, int len)
        {
            for (int i = start; i < len; i++) SendKey(VkToChar.GetVkCode(sb[i]));
        }

        public static void Send(string str)
        {
            for (int i = 0; i < str.Length; i++) SendKey(VkToChar.GetVkCode(str[i]));
        }

        public static void Send(String str, int start)
        {
            for (int i = start; i < str.Length; i++) SendKey(VkToChar.GetVkCode(str[i]));
        }

        private static void SendKey(byte vkCode)
        {
            keybd_event(vkCode, 0, 0, IntPtr.Zero);
            keybd_event(vkCode, 0, 2, IntPtr.Zero);
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, IntPtr dwExtraInfo);
    }
}
