using Serilog.Sinks.Http;

namespace CapBot.api.Middlewares
{
    public class CustomHttpClient : IHttpClient
    {
        private readonly HttpClient _httpClient;
        public IConfiguration _configuration { get; }

        public CustomHttpClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = content
            };

            var secretKey = _configuration.GetValue<string>("AppSettings:SecretKey");
            request.Headers.Add("SecretKey", secretKey);

            return await _httpClient.SendAsync(request).ConfigureAwait(false);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream)
        {
            var streamContent = new StreamContent(contentStream);
            return await PostAsync(requestUri, streamContent);
        }

        public void Configure(IConfiguration configuration)
        {
            // Không cần implement vì đã xử lý configuration trong constructor
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}