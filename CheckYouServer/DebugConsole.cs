using System.Runtime.InteropServices;

namespace CheckYouServer
{
    // WinExe 는 평소 콘솔이 없지만, 디버깅 중에는 콘솔을 띄워 로그를 눈으로 보도록 한다.
    // AllocConsole 로 프로세스에 새 콘솔을 붙인다. (디버거가 붙어 있을 때만 호출)
    internal static class DebugConsole
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        public static void Enable()
        {
            try
            {
                AllocConsole();
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.Title = "CheckYouServer (디버그 콘솔)";
            }
            catch
            {
                // 콘솔 할당 실패해도 치명적이지 않음 (파일 로그는 그대로 동작)
            }
        }
    }
}
