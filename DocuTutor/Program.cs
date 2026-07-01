
using DocuTutor.Application.DTOs.Auth.Register;
using DocuTutor.Domain.Entities;
using DocuTutor.Infrastructure.ExternalInterfaces.IEmailInterface;
using DocuTutor.Infrastructure.ExternalServices.EmailService;
using DocuTutor.Infrastructure.ServiceRegistration;
using DocuTutor.Validarors.Auth;
using FluentValidation;
using System.Text.Json.Serialization;
namespace DocuTutor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpClient();
            builder.Services.AddInfrastructureServices(builder.Configuration);

            //Temp registration of validators
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<RefreshTokenValidator>();


            var app = builder.Build();
            app.UseCors("AllowAngularApp");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
