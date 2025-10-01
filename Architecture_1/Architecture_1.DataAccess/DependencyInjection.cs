using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Architecture_1.DataAccess.Registrations;

namespace Architecture_1.DataAccess
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration configuration)

        {
            services.AddDbContext(configuration);
            services.AddDuendeIdentityServer(configuration);
            services.AddRepositories();
            services.AddSeeders();
            return services;
        }
    }
}
