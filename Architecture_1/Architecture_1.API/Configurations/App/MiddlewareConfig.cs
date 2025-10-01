namespace Architecture_1.API.Configurations.App
{
    public static class MiddlewareConfig
    {
        public static void AddAppMiddlewareConfig(this IApplicationBuilder app)
        {

            app.UseWhen(context => context.Request.Path.StartsWithSegments("/hello"),
                builder =>
                {
                    builder.Use(async (context, next) =>
                    {
                        await context.Response.WriteAsync("Hello, World!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                        await next();
                    });
                });


            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/NhapController_1/get_products_list"),
            builder =>
            {
                builder.Use(async (context, next) =>
                {
                    Console.WriteLine("NGU");
                    await next();

                });
            });


            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api/NhapController_1/test-connection"),
            builder =>
            {
                builder.Use(async (context, next) =>
                {
                    Console.WriteLine("NGU");
                    await next();
                });

                builder.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            });





            app.Map("/api/NhapController_1/test-connection-2", builder =>
            {

                builder.Use(async (context, next) =>
                {
                    Console.WriteLine("IFLWEFKJLJFALIEFJLIEWJWWWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJWJJJJJJLWEIJFLWIEFJWIEFJ");


                    await next();
                });




                builder.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });


            });

            app.AddSignalRHubsMiddlewares();

        }

        public static void AddSignalRHubsMiddlewares(this IApplicationBuilder app)
        {
            //app.UseEndpoints(endpoints => { endpoints.MapHub<ChatHub>("/api/chatHub"); });
            //app.UseEndpoints(endpoints => { endpoints.MapHub<ChatHubUser>("/chatHub_user"); });
            //app.UseEndpoints(endpoints => { endpoints.MapHub<ChatHubClient>("/chatHub_client"); });
            //app.UseEndpoints(endpoints => { endpoints.MapHub<ChatHubGroup>("/chatHub_group"); });

        }

    }
}
