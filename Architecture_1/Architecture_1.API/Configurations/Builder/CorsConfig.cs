namespace Architecture_1.API.Configurations.Builder
{
    public static class CorsConfig
    {
        public static void AddBuilderCorsConfig(this WebApplicationBuilder builder)
        {

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder
                               .AllowAnyMethod()
                               .AllowAnyHeader()
                               .AllowCredentials()
                               .SetIsOriginAllowed(origin => true)
                               .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
                        ;
                    });
            });

        }
    }
}
