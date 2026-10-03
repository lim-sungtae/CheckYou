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

    // 상태 보관소. 메모리 + 파일(config\state.json)에 영속화하며 스레드 안전하게 접근한다.
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
            _path = Path.Combine(dir, "state.json");
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
                    if (loaded != null)
                    {
                        _state = loaded;
                    }
                }
            }
            catch
            {
                // 파일이 깨졌으면 빈 상태로 시작
                _state = new BlockState();
            }
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

        // CheckYou 시작 시 호출. 클라이언트 config 목록으로 항목을 재구성하고 모두 제한적용으로 리셋한다.
        public BlockState Init(BlockState incoming)
        {
            lock (_lock)
            {
                BlockState fresh = new()
                {
                    IntervalSeconds = incoming.IntervalSeconds > 0 ? incoming.IntervalSeconds : 5,
                    Items = new List<BlockItem>(),
                };

                foreach (BlockItem item in incoming.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.Value))
                    {
                        continue;
                    }

                    fresh.Items.Add(new BlockItem
                    {
                        Type = NormalizeType(item.Type),
                        Value = item.Value.Trim(),
                        Enabled = true, // 재실행 시 전부 제한적용
                    });
                }

                _state = fresh;
                SaveNoLock();
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
