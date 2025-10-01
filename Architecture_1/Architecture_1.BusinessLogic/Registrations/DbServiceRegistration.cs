using Architecture_1.BusinessLogic.Services.SagaServices;
using Architecture_1.BusinessLogic.Services.SagaServices.interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Architecture_1.BusinessLogic.Registrations
{
    public static class DbServiceRegistration
    {
        public static IServiceCollection AddDbServices(this IServiceCollection services)
        {
            services.AddScoped<ISagaService, SagaService>();
            return services;
        }
    }
}
