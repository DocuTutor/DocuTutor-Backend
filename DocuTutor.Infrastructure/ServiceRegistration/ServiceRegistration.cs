
using DocuTutor.Application.DTOs;
using DocuTutor.Application.Interfaces;
using DocuTutor.Infrastructure.Data.Context;
using DocuTutor.Infrastructure.ExternalServices;
using DocuTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocuTutor.Infrastructure.ServiceRegistration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            // Database Registration
            services.AddDbContext<DocuTutorDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            );

            // External Services Registration
            services.Configure<CloudinarySettings>(options =>
            {
                options.CloudName = configuration["CloudinarySettings:CloudName"] ?? string.Empty;
                options.ApiKey = configuration["CloudinarySettings:ApiKey"] ?? string.Empty;
                options.ApiSecret = configuration["CloudinarySettings:ApiSecret"] ?? string.Empty;
            });

            // Application Services Registration
            services.AddScoped<IDocStorageService, CloudinaryService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IDocumentIngestionService>(provider =>
                new DocumentIngestionService(
                    provider.GetRequiredService<IHttpClientFactory>(),
                    provider.GetRequiredService<IConfiguration>(),
                    provider.GetRequiredService<IServiceScopeFactory>(),
                    provider.GetRequiredService<ILogger<DocumentIngestionService>>()
                ));

            services.AddHttpClient("LangflowRetrieval", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(3);
            });
            services.AddScoped<IRetrievalService, LangflowRetrievalService>();

            return services;
        }
    }
}
