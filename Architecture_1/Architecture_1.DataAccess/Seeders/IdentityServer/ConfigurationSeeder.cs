using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Architecture_1.DataAccess.Seeders.IdentityServer
{
    public class ConfigurationSeeder
    {
        private readonly ConfigurationDbContext _configurationDbContext;
        private readonly IConfiguration _configuration;

        public ConfigurationSeeder(ConfigurationDbContext configurationDbContext, IConfiguration configuration)
        {
            _configurationDbContext = configurationDbContext;
            _configuration = configuration;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Seed API Scopes
                await SeedApiScopesAsync();

                // Seed Identity Resources
                await SeedIdentityResourcesAsync();

                // Seed Clients
                await SeedClientsAsync();

                await _configurationDbContext.SaveChangesAsync();
                Console.WriteLine("IdentityServer configuration seeding completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding IdentityServer configuration: {ex.Message}");
                throw;
            }
        }

        private async Task SeedApiScopesAsync()
        {
            var existingScopes = await _configurationDbContext.ApiScopes.ToListAsync();
            var scopes = _configuration.GetSection("IdentityServer:ApiScopes").Get<List<ApiScope>>();

            if (scopes != null)
            {
                foreach (var scope in scopes)
                {
                    if (!existingScopes.Any(s => s.Name == scope.Name))
                    {
                        await _configurationDbContext.ApiScopes.AddAsync(scope);
                    }
                }
            }
        }

        private async Task SeedIdentityResourcesAsync()
        {
            var existingResources = await _configurationDbContext.IdentityResources.ToListAsync();
            var resources = _configuration.GetSection("IdentityServer:IdentityResources").Get<List<IdentityResource>>();

            if (resources != null)
            {
                foreach (var resource in resources)
                {
                    if (!existingResources.Any(r => r.Name == resource.Name))
                    {
                        await _configurationDbContext.IdentityResources.AddAsync(resource);
                    }
                }
            }
        }

        private async Task SeedClientsAsync()
        {
            var existingClients = await _configurationDbContext.Clients.ToListAsync();
            var clients = _configuration.GetSection("IdentityServer:Clients").Get<List<Client>>();

            if (clients != null)
            {
                foreach (var client in clients)
                {
                    if (!existingClients.Any(c => c.ClientId == client.ClientId))
                    {
                        // Add client secrets
                        if (!string.IsNullOrEmpty(client.ClientSecrets.FirstOrDefault()?.Value))
                        {
                            List<ClientSecret> clientSecrets = new List<ClientSecret>();
                            foreach (var secret in client.ClientSecrets)
                            {
                                clientSecrets.Add(new ClientSecret
                                {
                                    Value = secret.Value,
                                    Type = secret.Type,
                                    Description = secret.Description,
                                    Created = DateTime.UtcNow
                                });
                            }
                        }

                        // Add grant types
                        if (client.AllowedGrantTypes != null)
                        {
                            client.AllowedGrantTypes = client.AllowedGrantTypes.Select(gt => new ClientGrantType
                            {
                                GrantType = gt.GrantType
                            }).ToList();
                        }
                        

                        // Add scopes
                        if (client.AllowedScopes != null)
                        {
                            client.AllowedScopes = client.AllowedScopes.Select(s => new ClientScope
                            {
                                Scope = s.Scope

                            }).ToList();
                        }

                        // Add redirect URIs
                        if (client.RedirectUris != null)
                        {
                            client.RedirectUris = client.RedirectUris.Select(uri => new ClientRedirectUri
                            {
                                RedirectUri = uri.RedirectUri
                            }).ToList();
                        }

                        // Add post logout redirect URIs
                        if (client.PostLogoutRedirectUris != null)
                        {
                            client.PostLogoutRedirectUris = client.PostLogoutRedirectUris.Select(uri => new ClientPostLogoutRedirectUri
                            {
                                PostLogoutRedirectUri = uri.PostLogoutRedirectUri
                            }).ToList();
                        }

                        // Add CORS origins
                        if (client.AllowedCorsOrigins != null)
                        {
                            client.AllowedCorsOrigins = client.AllowedCorsOrigins.Select(origin =>
                            {
                                // Console.WriteLine($"\n\n null: {(origin == null ? null : origin)}\n\n");
                                // Console.WriteLine($"\n\n Id: {origin.Id}\n\n");
                                // Console.WriteLine($"\n\n ClientId: {origin.ClientId}\n\n");
                                // Console.WriteLine($"\n\n Origin: {origin.Origin}\n\n");
                                // Console.WriteLine($"\n\n Client: {origin.Client}\n\n");
                                // Console.WriteLine($"\n\n ClientId: {origin.ClientId}\n\n");
                                // Console.WriteLine($"\n\n ClientId: {origin.Client.ClientId}\n\n");
                                // Console.WriteLine($"\n\n ClientId: {origin.Client.ClientId}\n\n");

                                return new ClientCorsOrigin
                                {
                                    Origin = origin.Origin
                                    // Origin = "https://localhost:3000"
                                };
                            }).ToList();
                        }

                        await _configurationDbContext.Clients.AddAsync(client);
                    }
                }
            }
        }
    }
}