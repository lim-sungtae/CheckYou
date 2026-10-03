using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckYou
{
    // config.json 매핑 클래스. 재컴파일 없이 차단 대상/주기를 바꿀 수 있다.
    //
    // 차단 방식은 두 가지를 함께 지원한다.
    //   - blockedTitleKeywords : 창 "제목"에 이 단어가 들어가면 종료 (브라우저 탭 등에 유용)
    //   - blockedProcessNames  : "프로세스 이름"이 일치하면 제목과 무관하게 종료 (게임 등에 유용)
    internal sealed class Config
    {
        [JsonPropertyName("serverUrl")]
        public string ServerUrl { get; set; } = "http://localhost:5080";

        [JsonPropertyName("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 5;

        // true 면 시작 시 콘솔 창을 숨겨 백그라운드로 동작한다. (디버깅 시 false 로)
        [JsonPropertyName("hideConsole")]
        public bool HideConsole { get; set; } = true;

        [JsonPropertyName("blockedTitleKeywords")]
        public List<string> BlockedTitleKeywords { get; set; } = new();

        [JsonPropertyName("blockedProcessNames")]
        public List<string> BlockedProcessNames { get; set; } = new();

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        // 실행 파일과 같은 폴더의 config.json 을 읽는다.
        // 파일이 없거나 깨졌으면 기본값으로 동작한다.
        public static Config Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    Config? cfg = JsonSerializer.Deserialize<Config>(json, Options);
                    if (cfg != null && (cfg.BlockedTitleKeywords.Count > 0 || cfg.BlockedProcessNames.Count > 0))
                    {
                        if (cfg.IntervalSeconds <= 0)
                        {
                            cfg.IntervalSeconds = 5;
                        }
                        cfg.Normalize();
                        return cfg;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[config] 읽기 실패, 기본값 사용: {ex.Message}");
            }

            Config fallback = new()
            {
                IntervalSeconds = 5,
                BlockedTitleKeywords = new List<string> { "YouTube", "Roblox" },
                BlockedProcessNames = new List<string> { "Overwatch" },
            };
            fallback.Normalize();
            return fallback;
        }

        // 프로세스 이름 비교를 쉽게 하기 위해, 뒤에 붙은 .exe 를 떼고 공백을 정리한다.
        // (Process.ProcessName 은 확장자 없이 "Overwatch" 형태를 돌려주기 때문)
        private void Normalize()
        {
            for (int i = 0; i < BlockedProcessNames.Count; i++)
            {
                string name = BlockedProcessNames[i].Trim();
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    name = name[..^4];
                }
                BlockedProcessNames[i] = name;
            }
        }
    }
}
