using Microsoft.Extensions.Configuration;
using Duende.IdentityServer.Models;
using Architecture_1.Common.AppConfigurations.IdentityServer.interfaces;

namespace Architecture_1.Common.AppConfigurations.Jwt
{
    public class IdentityServerClientConfig : IIdentityServerClientConfig
    {
        private readonly IConfiguration _configuration;
        private readonly List<ClientConfigModel> _clients = new List<ClientConfigModel>();

        public IdentityServerClientConfig(IConfiguration configuration)
        {
            // _configuration = configuration;
            _clients = configuration.GetSection("IdentityServer:Clients").Get<List<ClientConfigModel>>();
        }

        public IEnumerable<Client> GetClients()
        {
            var clients = new List<Client>();


            foreach (var clientConfig in _clients)
            {
                Console.WriteLine($"\n\n\n");
                Console.WriteLine($"ClientId: {clientConfig.ClientId}");
                Console.WriteLine($"ClientSecret: {clientConfig.ClientSecret}");
                Console.WriteLine($"AllowedScopes: {string.Join(", ", clientConfig.AllowedScopes)}");
                Console.WriteLine($"GrantTypes: {string.Join(", ", clientConfig.GrantTypes)}");
                Console.WriteLine($"AccessTokenLifetime: {clientConfig.AccessTokenLifetime}");
                Console.WriteLine($"AbsoluteRefreshTokenLifetime: {clientConfig.AbsoluteRefreshTokenLifetime}");
                Console.WriteLine($"AllowOfflineAccess: {clientConfig.AllowOfflineAccess}");
                Console.WriteLine($"----------------------------------------");
                var client = new Client
                {
                    ClientId = clientConfig.ClientId,
                    ClientSecrets = new List<Secret> { new Secret(clientConfig.ClientSecret.Sha256()) },
                    AllowedScopes = clientConfig.AllowedScopes,
                    AllowedGrantTypes = clientConfig.GrantTypes,
                    AllowOfflineAccess = clientConfig.AllowOfflineAccess,
                    RequireClientSecret = clientConfig.RequireClientSecret,
                    AccessTokenLifetime = clientConfig.AccessTokenLifetime,
                    AbsoluteRefreshTokenLifetime = clientConfig.AbsoluteRefreshTokenLifetime,
                    RedirectUris = clientConfig.RedirectUris,

                };

                clients.Add(client);
            }

            return clients;
        }

        public IEnumerable<ApiScope> GetApiScopes() => new List<ApiScope>
        {
            new ApiScope("scope1", "Scope 1"),
            new ApiScope("scope2", "Scope 2"),
            new ApiScope("scope3", "Scope 3"),
            new ApiScope("scope4", "Scope 4"),
            new ApiScope("offline_access", "Offline Access")
        };

        public IEnumerable<IdentityResource> GetIdentityResources() =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };

    }

    public class ClientConfigModel
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public List<string> AllowedScopes { get; set; }

        public List<string> GrantTypes { get; set; }
        public int AccessTokenLifetime { get; set; }
        public int AbsoluteRefreshTokenLifetime { get; set; }
        public bool AllowOfflineAccess { get; set; }
        public bool RequireClientSecret { get; set; } = true;
        public List<string> RedirectUris { get; set; } = new List<string>();
    }
}
