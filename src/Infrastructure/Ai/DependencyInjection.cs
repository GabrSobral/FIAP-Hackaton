using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Ai;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraAi(this IServiceCollection services, IConfiguration configuration)
    {
        var hasLocalAi   = !string.IsNullOrWhiteSpace(configuration["LocalAi:BaseUrl"]);
        var hasGemini    = !string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"]);
        var hasAnthropic = !string.IsNullOrWhiteSpace(configuration["Anthropic:ApiKey"]);
        var hasOpenAi    = !string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"]);

        if (hasLocalAi)
        {
            services.AddHttpClient("localai", client =>
            {
                // Local model inference can be slow (download + load + GPU/CPU inference).
                // 15 minutes covers the worst-case first-run cold start.
                client.Timeout = TimeSpan.FromMinutes(15);
            });
            services.AddScoped<IAiAnalysisService, LocalAiAnalysisService>();
        }
        else if (hasGemini)
        {
            services.AddHttpClient("gemini", client => client.Timeout = TimeSpan.FromMinutes(2));
            services.AddScoped<IAiAnalysisService, GeminiAnalysisService>();
        }
        else if (hasAnthropic)
        {
            services.AddHttpClient("anthropic", client => client.Timeout = TimeSpan.FromMinutes(2));
            services.AddScoped<IAiAnalysisService, ClaudeAnalysisService>();
        }
        else if (hasOpenAi)
        {
            services.AddHttpClient("openai", client => client.Timeout = TimeSpan.FromMinutes(2));
            services.AddScoped<IAiAnalysisService, OpenAiAnalysisService>();
        }
        else
        {
            services.AddScoped<IAiAnalysisService, StubAiAnalysisService>();
        }

        return services;
    }
}
