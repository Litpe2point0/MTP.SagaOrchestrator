using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Architecture_1.Common.AppConfigurations.Jwt;
using Architecture_1.DataAccess.Data;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SystemIO = System.IO;


namespace Architecture_1.DataAccess.Registrations
{
    public static class DuendeIdentityServerRegistration
    {
        public static IServiceCollection AddDuendeIdentityServer(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtConfig = new JwtConfig(configuration);
            var identityServerClientConfig = new IdentityServerClientConfig(configuration);

            string basePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(SystemIO.Path.DirectorySeparatorChar);
            // string publicKeyRelativePath = configuration["Jwt:OpenSSL_PublicKey_Path"].Replace("\\", SystemIO.Path.DirectorySeparatorChar.ToString()).TrimStart(SystemIO.Path.DirectorySeparatorChar);
            // string privateKeyRelativePath = configuration["Jwt:OpenSSL_PrivateKey_Path"].Replace("\\", SystemIO.Path.DirectorySeparatorChar.ToString()).TrimStart(SystemIO.Path.DirectorySeparatorChar);
            string publicKeyRelativePath = jwtConfig.OpenSSL_PublicKey_Path;
            string privateKeyRelativePath = jwtConfig.OpenSSL_PrivateKey_Path;
            Console.WriteLine($"Private Key relative Path: {privateKeyRelativePath}");


            var publicKeyPath = SystemIO.Path.Combine(basePath, publicKeyRelativePath);
            var privateKeyPath = SystemIO.Path.Combine(basePath, privateKeyRelativePath);

            Console.WriteLine($"Public Key Path: {publicKeyPath}");
            Console.WriteLine($"Private Key Path: {privateKeyPath}");

            var publicKey = File.ReadAllText(SystemIO.Path.Combine(basePath, publicKeyRelativePath));
            var privateKey = File.ReadAllText(SystemIO.Path.Combine(basePath, privateKeyRelativePath));

            var privateKeyPem = File.ReadAllText(privateKeyPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem.ToCharArray());

            // // Add DbContext registrations
            // services.AddDbContext<IdentityServerConfigurationDbContext>(options =>
            //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
            //         sql => sql.MigrationsAssembly(typeof(DuendeIdentityServerRegistration).Assembly.FullName)));
            // services.AddDbContext<IdentityServerPersistedGrantDbContext>(options =>
            //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
            //         sql => sql.MigrationsAssembly(typeof(DuendeIdentityServerRegistration).Assembly.FullName)));

            services.AddIdentityServer(options =>
                {
                    options.IssuerUri = jwtConfig.Issuer;
                })
                // .AddInMemoryClients(identityServerClientConfig.GetClients())
                // .AddInMemoryApiScopes(identityServerClientConfig.GetApiScopes())
                // .AddInMemoryIdentityResources(identityServerClientConfig.GetIdentityResources())
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                        sql => sql.MigrationsAssembly(typeof(DuendeIdentityServerRegistration).Assembly.FullName)
                        // sql =>
                        // {
                        //     sql.MigrationsAssembly(typeof(DuendeIdentityServerRegistration).Assembly.FullName);
                        //     sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                        // }
                    );
                    options.DefaultSchema = "identity";
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                        sql => sql.MigrationsAssembly(typeof(DuendeIdentityServerRegistration).Assembly.FullName)
                        // sql =>
                        // {
                        //     sql.MigrationsAssembly(typeof(DuendeIdentityServerRegistration).Assembly.FullName);
                        //     sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                        // }
                    );
                    options.DefaultSchema = "identity";

                    // Optional: tự xóa token đã hết hạn
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 3600;
                })
                .AddSigningCredential(new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256));



            return services;
        }
    }
}
