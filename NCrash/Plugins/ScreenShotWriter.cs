using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using Common.Logging;

namespace NCrash.Plugins
{
    /// <summary>
    /// Generationg screenshots for current application plugin.
    /// Add files to AdditionalFiles list
    /// </summary>
    
    public class ScreenShotWriter:IPlugin
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(WindowEnumCallback lpEnumFunc, int lParam);
        
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(int h);

        [DllImport("user32")]
        static extern uint GetWindowThreadProcessId(int hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowRect(int hWnd, ref Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private IList<int> _windows = new List<int>();
        private IList<string> _screenshotNames = new List<string>();
        private int _screenshotNum;
        private Bitmap _screenshot;
        private string _screenshotName;
        private readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private string _path;
        private delegate bool WindowEnumCallback(int hwnd, int lparam);

        public ScreenShotWriter()
        {
            _path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }

        public ScreenShotWriter(string path)
        {
            if (path == string.Empty)
                _path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            else
                _path = path;
        }

        /// <summary>
        /// Generating windows handle list
        /// </summary>

        private bool AddWnd(int hwnd, int lparam)
        {
            Process currentProcess = Process.GetCurrentProcess();
            uint currentProcessId = (uint)currentProcess.Id;
            uint windowProcessId = new uint();
            GetWindowThreadProcessId(hwnd, out windowProcessId);
            if (currentProcessId == windowProcessId && IsWindowVisible(hwnd))
                _windows.Add(hwnd);
            return true;
        }

        /// <summary>
        /// Capturing window area at the screen
        /// </summary>

        private Bitmap Capture(int hwnd)
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

        ///<summary>
        ///Geneating screenshots
        /// </summary>

        public void PreProcess(ISettings settings)
        {
            try
            {
                EnumWindows(new WindowEnumCallback(AddWnd), 0);
                _screenshotNum = 1;
                _screenshotNames.Clear();
                if(settings.AdditionalReportFiles == null)
                    settings.AdditionalReportFiles = new List<string>();
                foreach (int hwnd in _windows)
                {

                    _screenshot = Capture(hwnd);
                    _screenshotName = Path.Combine(_path,"Application_screenshot_" + _screenshotNum.ToString() + ".jpg");
                    _screenshot.Save(_screenshotName);
                    _screenshotNames.Add(_screenshotName);
                    settings.AdditionalReportFiles.Add(_screenshotName);
                    _screenshotNum++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occurred during screenshot generation.", ex);
            }
        }
                
        /// <summary>
        /// Deleting the list of screenshots
        /// </summary>

        public void PostProcess(ISettings settings)
        {
            foreach (string screenshot in _screenshotNames)
            {
                File.Delete(screenshot);
            }
        }
    }
}