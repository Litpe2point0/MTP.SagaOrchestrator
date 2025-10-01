using Duende.IdentityServer.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Architecture_1.Common.AppConfigurations.IdentityServer.interfaces
{
    public interface IIdentityServerClientConfig
    {
        IEnumerable<Client> GetClients();
        IEnumerable<ApiScope> GetApiScopes();
        IEnumerable<IdentityResource> GetIdentityResources();

    }
}
