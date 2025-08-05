using Microsoft.Extensions.Configuration;
using ShuttleMate.Contract.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class SupabaseService : ISupabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseServiceRoleKey;
        private readonly string _bucketName;

        public SupabaseService(IConfiguration config, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _supabaseUrl = config["Supabase:Url"];
            _supabaseServiceRoleKey = config["Supabase:ServiceRoleKey"];
            _bucketName = config["Supabase:Bucket"];
        }

        public async Task<string?> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            var path = $"avatars/{Guid.NewGuid()}_{fileName}";
            var requestUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{path}";

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseServiceRoleKey);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return path;
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Supabase upload failed: {error}");
        }

        public string GetPublicUrl(string filePath)
        {
            return $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{filePath}";
        }

        public async Task DeleteAsync(string filePath)
        {
            var requestUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{filePath}";

            var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseServiceRoleKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase delete failed: {error}");
            }
        }
    }
}
