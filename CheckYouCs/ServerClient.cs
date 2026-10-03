using System.Net.Http.Json;

namespace CheckYou
{
    // CheckYou 서버와 통신한다.
    //   - InitAsync : 시작 시 config 목록을 올려 전부 제한적용으로 리셋
    //   - GetFlagsAsync : 매 주기마다 현재 플래그 상태를 가져옴
    internal sealed class ServerClient
    {
        private readonly HttpClient _http;

        public ServerClient(string baseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(4),
            };
        }

        public async Task<BlockState?> InitAsync(BlockState state)
        {
            try
            {
                HttpResponseMessage res = await _http.PostAsJsonAsync("/api/init", state);
                res.EnsureSuccessStatusCode();
                return await res.Content.ReadFromJsonAsync<BlockState>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[server] init 실패: {ex.Message}");
                return null;
            }
        }

        public async Task<BlockState?> GetFlagsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<BlockState>("/api/flags");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[server] flags 조회 실패: {ex.Message}");
                return null;
            }
        }
    }
}
