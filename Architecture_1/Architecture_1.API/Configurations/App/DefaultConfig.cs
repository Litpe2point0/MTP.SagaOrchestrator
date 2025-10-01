namespace Architecture_1.API.Configurations.App
{
    public static class DefaultConfig
    {
        public static void AppAppDefaultConfig(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
        }

    }
}
