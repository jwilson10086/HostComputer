using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HostComputer.Common.Base
{
    public static class ConsoleHelper
    {
        #region WinAPI
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern nint GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

        private delegate bool ConsoleCtrlDelegate(int ctrlType);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        #endregion

        private static bool _initialized = false;

        static ConsoleHelper()
        {
            // 拦截控制台关闭按钮
            SetConsoleCtrlHandler(ConsoleCtrlHandler, true);
        }

        public static void Show()
        {
            if (GetConsoleWindow() == nint.Zero)
            {
                AllocConsole();
                InitEncoding();
            }
            else
            {
                ShowWindow(GetConsoleWindow(), SW_SHOW);
            }
        }

        public static void Hide()
        {
            var hwnd = GetConsoleWindow();
            if (hwnd != nint.Zero)
            {
                ShowWindow(hwnd, SW_HIDE);
            }
        }

        public static void WriteLine(string message)
        {
            try
            {
                Console.WriteLine(message);
            }
            catch
            {
                // 如果控制台未初始化，重新打开
                Show();
                Console.WriteLine(message);
            }
        }

        private static void InitEncoding()
        {
            if (_initialized) return;
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            _initialized = true;
        }

        private static bool ConsoleCtrlHandler(int ctrlType)
        {
            if (ctrlType == 2) // CTRL_CLOSE_EVENT
            {
                Hide(); // 点 X 只隐藏，不退出进程
                return true;
            }
            return false;
        }
    }
}
