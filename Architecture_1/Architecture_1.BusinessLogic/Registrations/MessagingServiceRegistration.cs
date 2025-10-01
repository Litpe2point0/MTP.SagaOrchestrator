using Microsoft.Extensions.DependencyInjection;
using Architecture_1.BusinessLogic.Services.MessagingServices.interfaces;
using Architecture_1.BusinessLogic.Services.MessagingServices;
using Architecture_1.BusinessLogic.MessageHandlers;

namespace Architecture_1.BusinessLogic.Registrations
{
    public static class MessagingServiceRegistration
    {
        public static IServiceCollection AddMessagingServices(this IServiceCollection services)
        {
            // Saga handlers + routing store
            services.AddScoped<FlowMessageHandler>();
            services.AddScoped<FlowStepEmitMessageHandler>();

            services.AddScoped<IMessagingService, MessagingService>();
            services.AddSingleton<IHandlerRegistryService, HandlerRegistryService>();
            services.AddHostedService<HandlerRegistrationHostedService>();

            return services;
        }
    }
}
