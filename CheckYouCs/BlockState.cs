using System.Text.Json.Serialization;

namespace CheckYou
{
    // 서버와 주고받는 상태 모델 (서버의 BlockItem/BlockState 와 같은 JSON 모양).
    internal sealed class BlockItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "process"; // "process" | "title"

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    internal sealed class BlockState
    {
        [JsonPropertyName("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 5;

        [JsonPropertyName("items")]
        public List<BlockItem> Items { get; set; } = new();

        // 로컬 config 로부터 "모두 제한적용" 상태의 초기 BlockState 를 만든다.
        public static BlockState FromConfig(Config cfg)
        {
            BlockState state = new() { IntervalSeconds = cfg.IntervalSeconds };

            foreach (string kw in cfg.BlockedTitleKeywords)
            {
                if (!string.IsNullOrWhiteSpace(kw))
                {
                    state.Items.Add(new BlockItem { Type = "title", Value = kw.Trim(), Enabled = true });
                }
            }

            foreach (string name in cfg.BlockedProcessNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    state.Items.Add(new BlockItem { Type = "process", Value = name.Trim(), Enabled = true });
                }
            }

            return state;
        }
    }
}
