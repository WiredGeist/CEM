using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;

namespace MyFirstApp.Core
{
    public static class WindowManager
    {
        // Win32 API Imports
        [DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Styles
        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000; 
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_BORDER = 0x00800000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000; 

        private static IntPtr _picoWindowHandle = IntPtr.Zero;

        public static void EmbedPicoGK(Panel targetPanel)
        {
            if (targetPanel == null || targetPanel.IsDisposed) return;

            // 1. Search for the window if we don't have it
            if (_picoWindowHandle == IntPtr.Zero)
            {
                int myPid = Process.GetCurrentProcess().Id;
                IntPtr mainFormHandle = targetPanel.FindForm().Handle;

                EnumWindows((hWnd, lParam) => 
                {
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    
                    if (pid == myPid && hWnd != mainFormHandle)
                    {
                        // Get the title to be sure
                        StringBuilder sb = new StringBuilder(256);
                        GetWindowText(hWnd, sb, 256);
                        string title = sb.ToString();

                        // PicoGK usually sets its title to "PicoGK"
                        if (title.Contains("PicoGK"))
                        {
                            _picoWindowHandle = hWnd; 
                            return false; // Found it, stop searching
                        }
                    }
                    return true; // Keep searching
                }, IntPtr.Zero);
            }

            // 2. Glue it
            if (_picoWindowHandle != IntPtr.Zero)
            {
                // Strip borders
                int style = GetWindowLong(_picoWindowHandle, GWL_STYLE);
                int newStyle = (style & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_POPUP) | WS_CHILD | WS_VISIBLE;
                SetWindowLong(_picoWindowHandle, GWL_STYLE, newStyle); 
                
                // Embed
                SetParent(_picoWindowHandle, targetPanel.Handle);
                
                // Resize
                UpdateSize(targetPanel);
            }
        }

        public static void UpdateSize(Panel parent)
        {
            if (_picoWindowHandle != IntPtr.Zero && parent != null && !parent.IsDisposed)
            {
                MoveWindow(_picoWindowHandle, 0, 0, parent.Width, parent.Height, true);
            }
        }
    }
}