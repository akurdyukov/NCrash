//mycode
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using NCrash.Storage;
using System.Drawing;

namespace NCrash.Core.ScreenShots
{
    
    internal static class ScreenShotWriter
    {
        public delegate bool WindowEnumCallback(int hwnd, int lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(WindowEnumCallback lpEnumFunc, int lParam);
        
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(int h);

        [DllImport("user32")]
        public static extern uint GetWindowThreadProcessId(int hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(int hWnd, ref Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static List<int> Windows = new List<int>();
        private static List<string> screenshotNames = new List<string>();
        private static int screenshotNum;
        private static Bitmap screenshot;
        private static string screenshotName;

        internal static bool AddWnd(int hwnd, int lparam)
        {
            Process currentProcess = Process.GetCurrentProcess();
            uint currentProcessId = (uint)currentProcess.Id;
            uint windowProcessId = new uint();
            GetWindowThreadProcessId(hwnd, out windowProcessId);
            if (currentProcessId == windowProcessId && IsWindowVisible(hwnd))
                Windows.Add(hwnd);
            return true;
        }
        internal static List<string> Write()
        {
            try
            {
                EnumWindows(new WindowEnumCallback(AddWnd), 0);
                screenshotNum = 1;
                screenshotNames.Clear();
                foreach (int hwnd in Windows)
                {

                    screenshot = Capture(hwnd);
                    screenshotName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                                    "Application_screenshot_" + screenshotNum.ToString() + ".jpg"); ;
                    screenshot.Save(screenshotName);
                    screenshotNum++;
                    screenshotNames.Add(screenshotName);
                }
            }
            catch (Exception e)
            {
            }
            return screenshotNames;
        }

        internal static Bitmap Capture(int hwnd)
        {
            var rect = new Rect();
            GetWindowRect(hwnd, ref rect);
            Rectangle bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            
            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var g = Graphics.FromImage(result))
            {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            }

            return result;
        }
    }
}
//mycode