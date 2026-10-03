using System.Runtime.InteropServices;
using System.Text;

namespace CheckYou
{
    // 원본 MFC 코드가 쓰던 Win32 API를 P/Invoke로 그대로 가져온다.
    //  - EnumWindows / GetWindowText : 열린 최상위 창을 훑는다.
    //  - GetWindowThreadProcessId   : 창 -> 프로세스 ID
    // 프로세스 종료는 .NET의 System.Diagnostics.Process 를 쓰므로
    // OpenProcess/TerminateProcess P/Invoke는 필요 없다.
    internal static class NativeMethods
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLengthW(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // 콘솔 창 숨김용.
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;

        // 현재 프로세스의 콘솔 창을 숨긴다. (창이 없으면 아무 일도 안 함)
        public static void HideConsoleWindow()
        {
            IntPtr hWnd = GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_HIDE);
            }
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLengthW(hWnd);
            if (length <= 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new(length + 1);
            GetWindowTextW(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
