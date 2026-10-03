using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckYouServer
{
    // 서버 자체 설정 (실행파일\config\server.json).
    //   - url       : 서버가 수신할 주소. 로컬 전용이면 http://localhost:5080,
    //                 같은 네트워크(LAN)에서 접근하려면 http://0.0.0.0:5080.
    //   - keepAwake : true 면 서버가 도는 동안 PC가 절전에 들어가지 않도록 막는다.
    public sealed class ServerConfig
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "http://localhost:5080";

        [JsonPropertyName("keepAwake")]
        public bool KeepAwake { get; set; } = true;

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
        };

        public static ServerConfig Load()
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "config");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "server.json");

            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    ServerConfig? cfg = JsonSerializer.Deserialize<ServerConfig>(json, Options);
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.Url))
                    {
                        return cfg;
                    }
                }
                else
                {
                    // 파일이 없으면 기본값으로 하나 만들어 둔다.
                    ServerConfig def = new();
                    File.WriteAllText(path, JsonSerializer.Serialize(def, Options));
                    return def;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[server config] 읽기 실패, 기본값 사용: {ex.Message}");
            }

            return new ServerConfig();
        }
    }
}
