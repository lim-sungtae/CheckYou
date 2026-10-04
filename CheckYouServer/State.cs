using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckYouServer
{
    // 제한 항목 하나. 창 제목 키워드이든 프로세스 이름이든 각자 ON/OFF 플래그를 가진다.
    public sealed class BlockItem
    {
        // "process" (프로세스 이름) 또는 "title" (창 제목 키워드)
        [JsonPropertyName("type")]
        public string Type { get; set; } = "process";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";

        // true = 제한적용(종료 대상), false = 제한해제(통과)
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    // 서버가 보관하는 전체 상태. 클라이언트가 질의해 가져가는 내용이기도 하다.
    public sealed class BlockState
    {
        [JsonPropertyName("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 5;

        [JsonPropertyName("items")]
        public List<BlockItem> Items { get; set; } = new();
    }

    // 제한목록 보관소. 서버가 config\blocklist.json 을 소유한다.
    //   - 시작 시 파일을 읽어 목록/플래그/주기를 로드한다. (클라이언트 없이도 동작)
    //   - 파일이 없으면 기본 목록으로 생성(seed)한다.
    //   - 웹에서 수정하면 이 파일에 저장 → 재기동 시 그대로 적용된다.
    public sealed class StateStore
    {
        private readonly object _lock = new();
        private readonly string _path;
        private BlockState _state = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        public StateStore()
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "config");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "blocklist.json");
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    string json = File.ReadAllText(_path);
                    BlockState? loaded = JsonSerializer.Deserialize<BlockState>(json, JsonOptions);
                    if (loaded != null && loaded.Items.Count > 0)
                    {
                        if (loaded.IntervalSeconds <= 0)
                        {
                            loaded.IntervalSeconds = 5;
                        }
                        _state = loaded;
                        return;
                    }
                }
            }
            catch
            {
                // 파일이 깨졌으면 아래 기본값으로 생성
            }

            // 파일이 없거나 비어있으면 기본 목록으로 생성한다.
            _state = Default();
            SaveNoLock();
        }

        // 기본 제한목록 (클라이언트 config 와 동일한 목록, 전부 제한).
        private static BlockState Default()
        {
            return new BlockState
            {
                IntervalSeconds = 5,
                Items = new List<BlockItem>
                {
                    new() { Type = "title", Value = "YouTube", Enabled = true },
                    new() { Type = "process", Value = "Overwatch", Enabled = true },
                    new() { Type = "process", Value = "RobloxPlayerBeta", Enabled = true },
                },
            };
        }

        private void SaveNoLock()
        {
            try
            {
                string json = JsonSerializer.Serialize(_state, JsonOptions);
                File.WriteAllText(_path, json);
            }
            catch
            {
                // 저장 실패는 치명적이지 않음 (메모리 상태는 유지)
            }
        }

        public BlockState Get()
        {
            lock (_lock)
            {
                // 복사본을 돌려준다.
                return Clone(_state);
            }
        }

        // 웹 UI에서 특정 항목의 플래그를 바꾼다.
        public BlockState Toggle(string type, string value, bool enabled)
        {
            lock (_lock)
            {
                string t = NormalizeType(type);
                foreach (BlockItem item in _state.Items)
                {
                    if (item.Type == t && string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Enabled = enabled;
                    }
                }
                SaveNoLock();
                return Clone(_state);
            }
        }

        // 전체 일괄 적용/해제.
        public BlockState SetAll(bool enabled)
        {
            lock (_lock)
            {
                foreach (BlockItem item in _state.Items)
                {
                    item.Enabled = enabled;
                }
                SaveNoLock();
                return Clone(_state);
            }
        }

        public BlockState SetInterval(int seconds)
        {
            lock (_lock)
            {
                if (seconds > 0)
                {
                    _state.IntervalSeconds = seconds;
                    SaveNoLock();
                }
                return Clone(_state);
            }
        }

        private static string NormalizeType(string type)
        {
            return string.Equals(type, "title", StringComparison.OrdinalIgnoreCase) ? "title" : "process";
        }

        private static BlockState Clone(BlockState src)
        {
            return new BlockState
            {
                IntervalSeconds = src.IntervalSeconds,
                Items = src.Items.Select(i => new BlockItem
                {
                    Type = i.Type,
                    Value = i.Value,
                    Enabled = i.Enabled,
                }).ToList(),
            };
        }
    }
}
