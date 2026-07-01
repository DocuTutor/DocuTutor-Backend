
using DocuTutor.Application.DTOs;
using DocuTutor.Application.Interfaces;
using DocuTutor.Application.Interfaces.Auth;
using DocuTutor.Application.Interfaces.Payments;
using DocuTutor.Application.Response;
using DocuTutor.Domain.Entities;
using DocuTutor.Infrastructure.Data.Context;
using DocuTutor.Infrastructure.ExternalInterfaces.IEmailInterface;
using DocuTutor.Infrastructure.ExternalInterfaces.IJwtTokenService;
using DocuTutor.Infrastructure.ExternalServices;
using DocuTutor.Infrastructure.ExternalServices.EmailService;
using DocuTutor.Infrastructure.ExternalServices.Payments;
using DocuTutor.Infrastructure.Repositories;
using DocuTutor.Infrastructure.Services.AuthService;
using DocuTutor.Infrastructure.Services.JWTService;
using DocuTutor.Infrastructure.Services.SubscriptionService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

            services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<DocuTutorDbContext>()
            .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(opt =>
                    opt.TokenLifespan = TimeSpan.FromHours(2));

            // External Services Registration
            services.Configure<CloudinarySettings>(options =>
            {
                options.CloudName = configuration["CloudinarySettings:CloudName"] ?? string.Empty;
                options.ApiKey = configuration["CloudinarySettings:ApiKey"] ?? string.Empty;
                options.ApiSecret = configuration["CloudinarySettings:ApiSecret"] ?? string.Empty;
            });

    

            services.Configure<EmailConfiguration>(configuration.GetSection("EmailConfiguration"));
            services.AddScoped<IEmailSender, EmailSender>();

            //External Services Registration
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["Jwt:Issuer"],
                            ValidAudience = configuration["Jwt:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(configuration["Jwt:Key"])
                )
                        };
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
            services.AddScoped<IAuthService, AuthService>();

            // Stripe / Subscription Services Registration
            services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
            services.AddScoped<IStripeService, StripeService>();
            services.AddScoped<ISubscriptionService, DocuTutor.Infrastructure.Services.SubscriptionService.SubscriptionService>();


            //Global Validation response configuration
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    var response = Response<string>.Failure(
                        "",
                        "Validation failed",
                        400,
                        errors);

                    return new BadRequestObjectResult(response);
                };
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200") // Angular dev server
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });




            return services;
        }
    }
}
