using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SystemIO = System.IO;


namespace Architecture_1.API.Configurations.Builder
{
    public static class AuthorizationConfig
    {

        public static void AddBuilderAuthorizationConfig(this WebApplicationBuilder builder)
        {
            builder.AddRolePolicy();
            builder.AddEmailPolicy();
            builder.AddLoginRequiredPolicy();

            builder.AddDefaultAuthorization();
        }

        public static void AddDefaultAuthorization(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy= options.GetPolicy(builder.Configuration["AppSettings:DEFAULT_AUTHORIZATION:Policy"])!;
            });
        }

        public static void AddLoginRequiredPolicy(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("LoginRequired", policy =>
                {
                    policy.RequireAuthenticatedUser();
                });
            });
        }

        public static void AddRolePolicy(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Campus Manager", "Facility Major Head", "Assignee"));
                options.AddPolicy("UserOnly", policy => policy.RequireRole("Campus Member"));
                options.AddPolicy("Require_Manager_ID_1", policy =>
                {
                    policy.RequireRole("Campus Manager");
                    policy.RequireClaim("hahaha_user_id", "1");
                });

                options.AddPolicy("Require_Member_ID_1", policy =>
                {
                    policy.RequireRole("Campus Member");
                    policy.RequireClaim("hahaha_user_id", "1");
                });

            });
        }

        public static void AddEmailPolicy(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireRootEmail", policy =>
                {
                    policy.RequireClaim(ClaimTypes.Email, "hanguyenhao.20april@gmail.com")
                        .AddAuthenticationSchemes(builder.Configuration["AppSettings:DEFAULT_AUTHENTICATION:Scheme"])
                        .RequireAuthenticatedUser()
                        ;
                });
            });
        }
    }
}
