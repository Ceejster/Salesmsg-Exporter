using System.Net.Http.Headers;
using SalesmessageExporter.Config;

namespace SalesmessageExporter.Services
{
    public class SalesmessageApiService
    {
        private readonly HttpClient _httpClient;

        public SalesmessageApiService(string pat)
        {
            _httpClient = new HttpClient();

            _httpClient.BaseAddress =
                new Uri(AppSettings.BaseUrl);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", pat);

            _httpClient.Timeout =
                TimeSpan.FromSeconds(AppSettings.HttpTimeoutSeconds);
        }

        public async Task<bool> ValidatePATAsync()
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        $"{AppSettings.ConversationsEndpoint}?limit=1"
                    );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetConversationsPageRawAsync(
            int inboxId,
            int page)
        {
            string endpoint =
                $"{AppSettings.ConversationsEndpoint}" +
                $"?inbox_id={inboxId}" +
                $"&filter=open" +
                $"&limit={AppSettings.DefaultPageSize}" +
                $"&page={page}";

            HttpResponseMessage response =
                await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();

            string jsonResponse =
                await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }
    }
}