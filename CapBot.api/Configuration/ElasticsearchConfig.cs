using Elastic.Clients.Elasticsearch;

namespace CapBot.api.Configuration;

public static class ElasticsearchConfig
{
    public static void AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        var defaultIndex = configuration["Elasticsearch:DefaultIndex"] ?? "topics";

        var settings = new ElasticsearchClientSettings(new Uri(url))
            .DefaultIndex(defaultIndex)
            .DisableDirectStreaming();

        var client = new ElasticsearchClient(settings);
        services.AddSingleton<ElasticsearchClient>(client);
    }
}