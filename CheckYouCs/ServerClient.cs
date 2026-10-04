using System.Net.Http.Json;

namespace CheckYou
{
    // CheckYou 서버와 통신한다.
    //   - GetFlagsAsync : 매 주기마다 서버의 현재 설정(제한목록+플래그)을 가져옴
    // 서버가 설정을 소유하므로 클라이언트는 목록을 올리지 않는다(읽기 전용).
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

        public async Task<BlockState?> GetFlagsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<BlockState>("/api/flags");
            }
            catch (Exception ex)
            {
                Logger.Error($"[server] flags 조회 실패: {ex.Message}");
                return null;
            }
        }
    }
}
