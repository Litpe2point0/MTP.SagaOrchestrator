using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Architecture_1.DataAccess.Data;
using Architecture_1.DataAccess.Seeders;
using Architecture_1.DataAccess.Seeders.IdentityServer;

namespace Architecture_1.DataAccess.Registrations
{
    public static class SeederRegistration
    {
        public static IServiceCollection AddSeeders(this IServiceCollection services)
        {
            services.AddTransient<ConfigurationSeeder>();
            return services;
        }
    }
}
