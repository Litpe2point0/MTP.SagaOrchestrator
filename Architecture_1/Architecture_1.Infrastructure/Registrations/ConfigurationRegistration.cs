using Microsoft.Extensions.DependencyInjection;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;
using Architecture_1.Infrastructure.Configurations.Kafka;


namespace Architecture_1.Infrastructure.Registrations
{
    public static class ConfigurationRegistration
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services)
        {
            // Kafka
            services.AddSingleton<IKafkaClusterConfig, KafkaClusterConfig>();
            services.AddSingleton<IKafkaProducerConfig, KafkaProducerConfig>();
            services.AddSingleton<IKafkaConsumerConfig, KafkaConsumerConfig>();
            
            return services;
        }
    }
}
