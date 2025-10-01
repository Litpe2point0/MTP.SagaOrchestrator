using System.Threading.Tasks;
using Architecture_1.API.Configurations.App;
using Architecture_1.API.Configurations.Builder;
using Architecture_1.BusinessLogic;
using Architecture_1.Infrastructure;
using Architecture_1.Common;
using Architecture_1.DataAccess;

namespace Architecture_1.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Add Layers
            builder.Services.AddCommonLayer();
            builder.Services.AddBusinessLogicLayer();
            builder.Services.AddInfrastructureLayer(builder.Configuration);
            builder.Services.AddDataAccessLayer(builder.Configuration);


            // appConf
            builder.AddBuilderDefaultConfig();
            builder.AddBuilderCorsConfig();

            // authConf
            builder.AddBuilderAuthenticationConfig();
            builder.AddBuilderAuthorizationConfig();

            // healthCheckConf
            builder.AddBuilderHealthCheckConfig(); // [CONSUL] Register health check endpoint


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.AppAppDefaultConfig();

            app.AddAppCorsConfig();

            app.AddAppMiddlewareConfig();

            app.MapControllers();

            app.UseAppHealthCheckConfig(); // [CONSUL] Register health check endpoint

            app.Run();
        }
    }
}
