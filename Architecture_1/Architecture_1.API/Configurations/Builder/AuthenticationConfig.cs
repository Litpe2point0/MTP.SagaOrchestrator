using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SystemIO = System.IO;


namespace Architecture_1.API.Configurations.Builder
{
    public static class AuthenticationConfig
    {

        public static void AddBuilderAuthenticationConfig(this WebApplicationBuilder builder)
        {
            builder.AddJwtOneSecretKeyAuth();
            builder.AddJwtPublicPrivateKeyAuth();

            builder.AddDefaultAuthentication();
        }

        public static void AddDefaultAuthentication(this WebApplicationBuilder builder)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            // builder.Services.AddAuthentication(options =>
            // {
            //     options.DefaultAuthenticateScheme = builder.Configuration["AppSettings:DEFAULT_AUTHENTICATION:Scheme"];
            // });
            var defaultScheme = builder.Configuration["AppSettings:DEFAULT_AUTHENTICATION:Scheme"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = defaultScheme;
                options.DefaultChallengeScheme = defaultScheme; // ← BỔ SUNG DÒNG NÀY
                options.DefaultScheme = defaultScheme;
            });
        }

        public static void AddJwtOneSecretKeyAuth(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication()
            .AddJwtBearer("Jwt_SecretKeyAuth", options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("Authentication failed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated: " + context.SecurityToken);
                        return Task.CompletedTask;
                    }
                };
                options.MapInboundClaims = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                };

                // XEM KẾT QUẢ
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("\nAuthentication failed: " + context.Exception.Message+"\n");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated: " + context.SecurityToken);
                        return Task.CompletedTask;
                    }
                };
            });
        }

        public static void AddJwtPublicPrivateKeyAuth(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication() // Bạn có thể không cần thiết lập Default ở đây nữa nếu đã cấu hình ở hàm trước
            .AddJwtBearer("Jwt_PublicPrivateKeyAuth", options =>
            {
                //string basePath = AppDomain.CurrentDomain.BaseDirectory;
                //string fullPath = Path.Combine(basePath, builder.Configuration["Jwt:OpenSSL_PublicKey_Path"]);
                string basePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(SystemIO.Path.DirectorySeparatorChar);
                string relativePath = builder.Configuration["Jwt:OpenSSL_PublicKey_Path"].Replace("\\", SystemIO.Path.DirectorySeparatorChar.ToString()).TrimStart(SystemIO.Path.DirectorySeparatorChar);
                string fullPath = SystemIO.Path.Combine(basePath, relativePath);

                var publicKey = File.ReadAllText(fullPath);

                // Console.WriteLine("basePath: " + basePath);
                // Console.WriteLine("publicKey " + publicKey);

                var rsa = RSA.Create();
                rsa.ImportFromPem(publicKey.ToCharArray());
                options.MapInboundClaims = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"]
                };

                // XEM KẾT QUẢ
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("\nAuthentication failed: " + context.Exception.Message+"\n");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated: " + context.SecurityToken);
                        // giá trị exp
                        Console.WriteLine("Token exp: " + context.SecurityToken.ValidTo.ToString("yyyy-MM-dd HH:mm:ss"));
                        return Task.CompletedTask;
                    }
                };
            });
        }

    }
}
