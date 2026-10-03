namespace CheckYouServer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // WinExe(창 없음): 로그를 파일(logs\server.log)로 남긴다.
            FileLog.Init(AppContext.BaseDirectory);

            // 디버깅 중에만 콘솔을 띄워 로그가 눈에 보이게 한다.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugConsole.Enable();
                FileLog.EnableConsoleEcho();
            }

            FileLog.Info("===== CheckYouServer 시작 =====");

            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                // 기동 중 치명적 오류도 파일에 남긴다.
                FileLog.Error($"서버 비정상 종료: {ex}");
            }

            FileLog.Info("===== CheckYouServer 종료 =====");
        }

        private static void Run(string[] args)
        {
            // 서버 설정(config\server.json) 로드.
            ServerConfig serverConfig = ServerConfig.Load();

            // 로컬 PC 운영: 서버가 도는 동안 절전에 들어가지 않도록 막는다.
            if (serverConfig.KeepAwake)
            {
                bool ok = PowerKeepAwake.Enable();
                FileLog.Info($"[power] 절전 억제: {(ok ? "활성" : "실패")}");
            }

            // ContentRoot 를 실행파일 폴더로 고정한다. (어느 작업 디렉터리에서 실행하든 wwwroot 를 찾음)
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory,
            });

            // 로그는 파일로 보낸다. 요청마다 찍히는 소음은 Warning 이상만.
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new FileLoggerProvider());

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // 디버깅 중에는 콘솔에도 프레임워크 로그를 Information 레벨까지 보여준다.
                builder.Logging.AddConsole();
                builder.Logging.SetMinimumLevel(LogLevel.Information);
            }
            else
            {
                builder.Logging.SetMinimumLevel(LogLevel.Warning);
            }

            builder.Services.AddSingleton<StateStore>();

            var app = builder.Build();

            // 수신 주소는 config\server.json 의 url 값을 사용한다.
            // 로컬 전용: http://localhost:5080 / LAN 접근: http://0.0.0.0:5080 (방화벽 포트 허용 필요)
            app.Urls.Clear();
            app.Urls.Add(serverConfig.Url);
            FileLog.Info($"[server] 수신 주소: {serverConfig.Url}");

            // 종료 시 절전 억제 해제.
            app.Lifetime.ApplicationStopping.Register(() => PowerKeepAwake.Disable());

            // wwwroot\index.html (config 편집 UI) 제공
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // CheckYou 클라이언트가 5초마다 호출: 현재 플래그 상태를 가져간다.
            app.MapGet("/api/flags", (StateStore store) => Results.Json(store.Get()));

            // CheckYou 시작 시 호출: config 목록으로 항목을 재구성하고 전부 제한적용으로 리셋.
            app.MapPost("/api/init", (BlockState incoming, StateStore store) =>
            {
                return Results.Json(store.Init(incoming));
            });

            // 웹 UI: 항목 하나의 제한적용/해제 토글.
            app.MapPost("/api/items/toggle", (ToggleRequest req, StateStore store) =>
            {
                return Results.Json(store.Toggle(req.Type, req.Value, req.Enabled));
            });

            // 웹 UI: 전체 일괄 적용/해제.
            app.MapPost("/api/all", (SetAllRequest req, StateStore store) =>
            {
                return Results.Json(store.SetAll(req.Enabled));
            });

            // 웹 UI: 검사 주기 변경.
            app.MapPost("/api/interval", (IntervalRequest req, StateStore store) =>
            {
                return Results.Json(store.SetInterval(req.IntervalSeconds));
            });

            // 여기서부터 요청을 받기 시작하고, 종료 될 때까지 블로킹된다.
            // (종료 마커는 Main 에서 출력한다.)
            app.Run();
        }
    }

    public sealed record ToggleRequest(string Type, string Value, bool Enabled);
    public sealed record SetAllRequest(bool Enabled);
    public sealed record IntervalRequest(int IntervalSeconds);
}
