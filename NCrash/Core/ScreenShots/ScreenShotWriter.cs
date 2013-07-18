//mycode
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using Common.Logging;

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

        private static IList<int> _windows = new List<int>();
        private static IList<Tuple<string,string>> _screenshotNames = new List<Tuple<string,string>>();
        private static int _screenshotNum;
        private static Bitmap _screenshot;
        private static string _screenshotName;
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        internal static bool AddWnd(int hwnd, int lparam)
        {
            Process currentProcess = Process.GetCurrentProcess();
            uint currentProcessId = (uint)currentProcess.Id;
            uint windowProcessId = new uint();
            GetWindowThreadProcessId(hwnd, out windowProcessId);
            if (currentProcessId == windowProcessId && IsWindowVisible(hwnd))
                _windows.Add(hwnd);
            return true;
        }
        internal static IList<Tuple<string,string>> Write(string _path)
        {
            if (_path == string.Empty)
                _path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            try
            {
                EnumWindows(new WindowEnumCallback(AddWnd), 0);
                _screenshotNum = 1;
                _screenshotNames.Clear();
                foreach (int hwnd in _windows)
                {

                    _screenshot = Capture(hwnd);
                    _screenshotName = Path.Combine(_path,"Application_screenshot_" + _screenshotNum.ToString() + ".jpg");
                    _screenshot.Save(_screenshotName);
                    _screenshotNames.Add(new Tuple<string,string>(_path,"Application_screenshot_" + _screenshotNum.ToString() + ".jpg"));
                    _screenshotNum++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occurred during screenshot generation.", ex);
            }
            return _screenshotNames;
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

        internal static bool Clear(IList<Tuple<string,string>> screenshotList)
        {
            foreach (Tuple<string, string> screenshot in screenshotList)
            {
                File.Delete(Path.Combine(screenshot.Item1,screenshot.Item2));
            }
            return true;
        }
    }
}
//mycode