using Microsoft.Extensions.DependencyInjection;
using Architecture_1.Common.Registrations;

namespace Architecture_1.Common
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommonLayer(this IServiceCollection services)
        {
            services.AddConfiguration();
            return services;
        }
    }
}
