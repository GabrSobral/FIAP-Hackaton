using fiap_hackaton.Application.Services.Analyses.CreateAnalysis;
using fiap_hackaton.Application.Services.Analyses.GetAnalysis;
using fiap_hackaton.Application.Services.Analyses.GetAnalysisReport;
using fiap_hackaton.Application.Services.Analyses.GetAnalysisStatus;
using fiap_hackaton.Application.Services.Analyses.ListAnalyses;

namespace fiap_hackaton.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateAnalysisService>();
        services.AddScoped<GetAnalysisService>();
        services.AddScoped<GetAnalysisStatusService>();
        services.AddScoped<GetAnalysisReportService>();
        services.AddScoped<ListAnalysesService>();

        return services;
    }
}
