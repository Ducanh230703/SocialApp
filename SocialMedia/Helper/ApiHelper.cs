    using SocialMedia;
    using NuGet.Common;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

namespace SocialMedia.Helper
{
    public static class ApiHelper
    {
        private static readonly HttpClient client;

        static ApiHelper()
        {
            string apiHost = AppConfig.ApiSettings.ApiHost;

            client = new HttpClient
            {
                BaseAddress = new Uri(apiHost)
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task<TResponse?> PostFormAsync<TResponse>(string endpoint, MultipartFormDataContent form, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", token);
            }

            var response = await client.PostAsync(endpoint, form);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Upload ảnh thất bại: {response.StatusCode}, Nội dung lỗi: {errorContent}");
            }

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    

        public static async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, string? token = null)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", token);
            }
            var response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public static async Task<T?> GetAsync<T>(string endpoint, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", token);
            }
            var response = await client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Lỗi từ API: " + error);
                throw new HttpRequestException($"API trả về lỗi {response.StatusCode}: {error}");
            }

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        public static async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, string? token = null)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", token);
            }
            var response = await client.PutAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        public static async Task<TResponse?> DeleteAsync<TResponse>(string endpoint, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", token);
            }

            var response = await client.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }


    }
}

