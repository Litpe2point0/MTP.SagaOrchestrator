using Architecture_1.BusinessLogic.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Architecture_1.BusinessLogic
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services)
        {
            //services.AddConfiguration();
            services.AddBackgroundServices();
            services.AddDbServices();    
            services.AddHelpers();
            services.AddMessagingServices(); // 🔧 NEW
            return services;
        }
    }
}
