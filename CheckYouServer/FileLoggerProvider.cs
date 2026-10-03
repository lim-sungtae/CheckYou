using Microsoft.Extensions.Logging;

namespace CheckYouServer
{
    // ASP.NET Core 의 로그(ILogger)를 FileLog(파일)로 보내는 provider.
    // WinExe 라 콘솔이 없으므로, 프레임워크의 경고/오류도 파일에 남도록 한다.
    internal sealed class FileLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }

    internal sealed class FileLogger : ILogger
    {
        private readonly string _category;

        public FileLogger(string category)
        {
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        // 요청마다 찍히는 Information 소음을 막기 위해 Warning 이상만 기록한다.
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Warning;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);
            if (exception != null)
            {
                message += " | " + exception;
            }

            FileLog.Write(logLevel.ToString(), $"{_category}: {message}");
        }
    }
}
