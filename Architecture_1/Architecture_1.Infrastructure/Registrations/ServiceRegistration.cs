using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Architecture_1.Infrastructure.Services.Kafka;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;
using Architecture_1.Infrastructure.Configurations.Kafka;
using Consul;
using StackExchange.Redis;
using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;

namespace Architecture_1.Infrastructure.Registrations
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add service groups
            services.AddKafkaServices(configuration);

            return services;
        }

        #region Kafka Services
        private static IServiceCollection AddKafkaServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Kafka Configuration already handled in ConfigurationRegistration
            
            // Kafka Services
            services.AddSingleton<KafkaProducerService>();
            
            // Register KafkaConsumerService as Singleton utility service
            services.AddSingleton<KafkaConsumerService>();
            

            // Health Check
            services.AddSingleton<KafkaHealthCheckService>();
            services.AddHealthChecks()
                .AddCheck<KafkaHealthCheckService>("kafka", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, new[] { "kafka", "messaging" });

            return services;
        }
        #endregion


    }
}
