using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SmartType
{
    public abstract class KeyboardHook
    {
        public enum WordAction { WordCompleted, WordTerminated }

        public delegate void WordActionHandler(WordAction action);
        public delegate void CharacterAddedHandler(char c);
        public delegate void CharacterRemovedHandler();
        public delegate bool AutoCompleteRequestHandler();
        public delegate bool ArrowKeysPressedHandler(bool up);
        public delegate void ActivatedHandler();
        public delegate void DeactivatedHandler();

        public static event WordActionHandler WordActionHappened;
        public static event CharacterAddedHandler CharacterAdded;
        public static event CharacterRemovedHandler CharacterRemoved;
        public static event AutoCompleteRequestHandler AutoCompleteRequested;
        public static event ArrowKeysPressedHandler ArrowKeysPressed;
        public static event ActivatedHandler Activated;
        public static event DeactivatedHandler Deactivated;

        private static WindowInfo currentWindow, lastWindow;
        private static int currentLang, lastLang;
        private static bool active = true;

        private static List<string> autoEnableProcs = new List<string>();
        private static List<string> autoDisableProcs = new List<string>();

        static Thread checkThread;

        public static int Language
        {
            get { return currentLang; }
        }

        public static bool Active
        {
            get { return active; }
        }

        private static int tabCode = 0x09;
        private static List<int> endCodes = new List<int>{ 0x20, 0xBC, 0xBE };

        #region Hook stuff

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        public static void SetHook()
        {
            ReadFile("autoenable.txt", autoEnableProcs);
            ReadFile("autodisable.txt", autoDisableProcs);

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }

            checkThread = new Thread(new ThreadStart(WindowChecker));
            checkThread.Start();
        }

        public static void Kill()
        {
            checkThread.Abort();
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                if(active)
                {
                    if (ActiveCallback(nCode, wParam, lParam) != IntPtr.Zero) return (IntPtr)1;
                }
                else
                {
                    if (InactiveCallback(nCode, wParam, lParam) != IntPtr.Zero) return (IntPtr)1;
                }
                
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static IntPtr ActiveCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            currentWindow = WindowInfo.GetActiveWindowInfo();      
            currentLang = currentWindow.keyboardLayout & 0xFFFF;

            int vkCode = Marshal.ReadInt32(lParam);
            int flags = Marshal.ReadInt32(lParam + 8);

            if (vkCode == 0x76) // F7
            {
                active = false;
                Deactivated?.Invoke();
            }

            if ((flags & 16) != 0) return IntPtr.Zero; // инжектиран

            if (vkCode == 0x26 || vkCode == 0x28) // up & down
            {
                if (ArrowKeysPressed != null)
                {
                    if (ArrowKeysPressed(vkCode == 0x26)) return (IntPtr)1;
                    else return IntPtr.Zero;
                }
            }
            else if (vkCode == 8)
            {
                CharacterRemoved?.Invoke();
                return IntPtr.Zero;
            }

            char ch = VkToChar.GetChar(vkCode, currentLang);

            bool cond1 = (lastWindow != null) && (!currentWindow.Equals(lastWindow));
            bool cond2 = (lastLang != 0) && (currentLang != lastLang);
            bool terminateWord = cond1 || cond2;
            if (terminateWord)
            {
                Console.WriteLine("{0} {1}", cond1, cond2);
            }

            bool block = false;

            if (terminateWord) WordActionHappened?.Invoke(WordAction.WordTerminated);

            if (vkCode == tabCode) block = AutoCompleteRequested != null ? AutoCompleteRequested() : false;
            else if (endCodes.Contains(vkCode)) WordActionHappened?.Invoke(WordAction.WordCompleted);
            else if (ch != 0) CharacterAdded?.Invoke(ch);
            else WordActionHappened?.Invoke(WordAction.WordTerminated);

            lastLang = currentLang;
            lastWindow = currentWindow;

            if (block) return (IntPtr)1;
            else return IntPtr.Zero;
        }

        private static IntPtr InactiveCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            currentWindow = WindowInfo.GetActiveWindowInfo();

            int vkCode = Marshal.ReadInt32(lParam);
            if(vkCode == 0x76) // F7
            {
                active = true;
                Activated?.Invoke();
                SetForegroundWindow(currentWindow.hwnd);
            }

            return IntPtr.Zero;
        }

        private static void ReadFile(string filename, List<string> list)
        {
            String[] lines;
            try
            {
                lines = File.ReadAllLines(filename);
            }
            catch(Exception e)
            {
                return;
            }

            list.AddRange(lines);
        }

        private static void WindowChecker()
        {
            while(true)
            {
                WindowInfo foregroundWnd = WindowInfo.GetActiveWindowInfo();
                //Console.WriteLine(foregroundWnd.processFileName);

                if(active)
                {
                    if(autoDisableProcs.Contains(foregroundWnd.processFileName))
                    {
                        active = false;
                        Deactivated?.Invoke();
                    }
                }
                else
                {
                    if (autoEnableProcs.Contains(foregroundWnd.processFileName))
                    {
                        active = true;
                        Activated?.Invoke();
                        SetForegroundWindow(foregroundWnd.hwnd);
                    }
                }
                Thread.Sleep(50);
            }
        }

        #region DLLs
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern int ToUnicode(int virtualKeyCode, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)]
            StringBuilder receivingBuffer, int bufferSize, uint flags);

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(int hwnd);
        #endregion
    }
}
