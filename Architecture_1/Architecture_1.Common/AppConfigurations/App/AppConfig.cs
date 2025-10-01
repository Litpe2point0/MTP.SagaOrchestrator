using Microsoft.Extensions.Configuration;
using Architecture_1.Common.AppConfigurations.App.interfaces;

namespace Architecture_1.Common.AppConfigurations.App
{
    public class AppConfigModel
    {
        public int APP_PORT { get; set; }
        public string APP_BASE_URL { get; set; }
        public string HEALTH_CHECK_ENDPOINT { get; set; }
        public string IMAGE_SRC { get; set; }
    }
    public class AppConfig : IAppConfig
    {
        public int APP_PORT { get; set; }
        public string APP_BASE_URL { get; set; }
        public string HEALTH_CHECK_ENDPOINT { get; set; }
        public string IMAGE_SRC { get; set; }


        public AppConfig(IConfiguration configuration)
        {
            var appConfig = configuration.GetSection("AppSettings").Get<AppConfigModel>();
            APP_PORT = appConfig.APP_PORT;
            APP_BASE_URL = appConfig.APP_BASE_URL;
            HEALTH_CHECK_ENDPOINT = appConfig.HEALTH_CHECK_ENDPOINT;
            IMAGE_SRC = appConfig.IMAGE_SRC;

        }

        
    }
}
