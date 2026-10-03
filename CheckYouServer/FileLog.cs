using System.Text;

namespace CheckYouServer
{
    // WinExe(창 없음)에서는 콘솔 출력이 보이지 않으므로 로그를 파일로 남긴다.
    //   위치: 실행파일\logs\server.log
    // 파일이 일정 크기를 넘으면 한 번 백업(.old)하고 새로 시작한다.
    internal static class FileLog
    {
        private static readonly object _lock = new();
        private static string _path = "";
        private const long MaxBytes = 1_000_000; // 약 1MB
        private static bool _echoConsole = false;

        // 디버깅 중(콘솔 있을 때) 로그를 콘솔에도 출력하도록 켠다.
        public static void EnableConsoleEcho()
        {
            _echoConsole = true;
        }

        public static void Init(string baseDir)
        {
            try
            {
                string dir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(dir);
                _path = Path.Combine(dir, "server.log");
            }
            catch
            {
                _path = "";
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Write(string level, string message)
        {
            if (_path.Length == 0)
            {
                return;
            }

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";

            try
            {
                lock (_lock)
                {
                    Rotate();
                    File.AppendAllText(_path, line, Encoding.UTF8);

                    if (_echoConsole)
                    {
                        Console.Write(line);
                    }
                }
            }
            catch
            {
                // 로그 실패는 치명적이지 않으므로 무시
            }
        }

        private static void Rotate()
        {
            try
            {
                FileInfo fi = new(_path);
                if (fi.Exists && fi.Length > MaxBytes)
                {
                    string old = _path + ".old";
                    if (File.Exists(old))
                    {
                        File.Delete(old);
                    }
                    File.Move(_path, old);
                }
            }
            catch
            {
                // 회전 실패는 무시
            }
        }
    }
}
