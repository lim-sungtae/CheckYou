using System.Diagnostics;

namespace CheckYou
{
    // CheckYou (C# 포팅 + 서버 연동)
    //
    // 1) 시작하면 로컬 config(실행파일\config\config.json)를 읽는다.
    // 2) config 목록을 서버에 올려 "전부 제한적용" 상태로 리셋한다. (재실행 시마다)
    // 3) 매 주기(기본 5초)마다 프로세스 확인 '전에' 서버에 플래그를 질의하고,
    //    제한적용(enabled=true)인 항목만 종료한다.
    //    - 서버 연결 실패 시에는 마지막으로 받은 플래그를 유지한다(안전측: 계속 제한).
    internal static class Program
    {
        private static Config _config = new();
        private static ServerClient? _server;
        private static BlockState _current = new();

        private static async Task<int> Main()
        {
            string baseDir = AppContext.BaseDirectory;
            Logger.Init(baseDir);

            string configPath = Path.Combine(baseDir, "config", "config.json");
            _config = Config.Load(configPath);

            _server = new ServerClient(_config.ServerUrl);

            // 시작 시: config 목록으로 서버를 "전부 제한적용"으로 리셋.
            BlockState startup = BlockState.FromConfig(_config);
            BlockState? initialized = await _server.InitAsync(startup);
            _current = initialized ?? startup; // 서버 실패 시 로컬 config(전부 적용)로 동작

            Logger.Info("CheckYou (C#) 시작");
            Logger.Info($"  서버 주소      : {_config.ServerUrl}");
            Logger.Info($"  검사 주기      : {_current.IntervalSeconds}초");
            Logger.Info($"  제한 항목 수   : {_current.Items.Count}개 (시작 시 전부 제한적용)");
            Logger.Info($"  서버 연동      : {(initialized != null ? "성공" : "실패 - 로컬 config로 동작")}");

            using CancellationTokenSource cts = new();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await RunLoopAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }

            Logger.Info("CheckYou 종료");
            return 0;
        }

        private static async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // (1) 프로세스 확인 전에 서버에 플래그 질의.
                BlockState? flags = await _server!.GetFlagsAsync();
                if (flags != null)
                {
                    _current = flags;
                }
                // flags == null 이면 마지막 상태(_current)를 그대로 사용.

                // (2) 제한적용 항목만 종료.
                EnforceEnabled();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _current.IntervalSeconds)), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private static void EnforceEnabled()
        {
            // 제한적용(enabled) 항목만 추려 종류별로 적용.
            List<string> processNames = new();
            List<string> titleKeywords = new();

            foreach (BlockItem item in _current.Items)
            {
                if (!item.Enabled || string.IsNullOrWhiteSpace(item.Value))
                {
                    continue;
                }

                if (item.Type == "title")
                {
                    titleKeywords.Add(item.Value);
                }
                else
                {
                    processNames.Add(item.Value);
                }
            }

            TerminateByProcessName(processNames);
            TerminateByWindowTitle(titleKeywords);
        }

        // 프로세스 이름 기준 차단. 창이 있든 없든 이름이 맞으면 종료한다.
        private static void TerminateByProcessName(List<string> names)
        {
            foreach (string rawName in names)
            {
                string name = rawName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? rawName[..^4]
                    : rawName;

                Process[] procs;
                try
                {
                    procs = Process.GetProcessesByName(name);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[실패] '{name}' 조회 불가: {ex.Message}");
                    continue;
                }

                foreach (Process proc in procs)
                {
                    using (proc)
                    {
                        TryKill(proc, $"프로세스 이름 '{name}'");
                    }
                }
            }
        }

        // 창 제목 기준 차단. 원본 CallbackEnumWindowProc 의 역할.
        private static void TerminateByWindowTitle(List<string> keywords)
        {
            if (keywords.Count == 0)
            {
                return;
            }

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                string title = NativeMethods.GetWindowTitle(hWnd);
                if (title.Length == 0)
                {
                    return true;
                }

                bool blocked = false;
                foreach (string kw in keywords)
                {
                    if (title.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    return true;
                }

                NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == 0)
                {
                    return true;
                }

                try
                {
                    using Process proc = Process.GetProcessById((int)processId);
                    TryKill(proc, $"창 제목 '{title}'");
                }
                catch (ArgumentException)
                {
                    // 이미 종료됨
                }

                return true;
            }, IntPtr.Zero);
        }

        private static void TryKill(Process proc, string reason)
        {
            try
            {
                int pid = proc.Id;
                string procName = proc.ProcessName;
                proc.Kill();
                Logger.Info($"[종료] pid={pid} ({procName}) / 사유: {reason}");
            }
            catch (InvalidOperationException)
            {
                // 이미 종료됨 - 무시
            }
            catch (Exception ex)
            {
                Logger.Error($"[실패] 종료 불가 ({reason}): {ex.Message}");
            }
        }
    }
}
