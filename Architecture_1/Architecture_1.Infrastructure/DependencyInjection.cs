using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Architecture_1.Infrastructure.Registrations;

namespace Architecture_1.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddConfiguration();
            services.AddServices(configuration);
            return services;
        }
    }
}
