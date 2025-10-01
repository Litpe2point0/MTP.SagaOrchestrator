namespace Architecture_1.Common.AppConfigurations.Jwt.interfaces
{
    public interface IJwtConfig
    {
        int Exp { get; set; }
        string Issuer { get; set; }
        string Audience { get; set; }
        double AccessTokenExpiration { get; set; }
        double RefreshTokenExpiration { get; set; }

        string SecretKey { get; set; }
        string PrivateKey { get; set; }
        string PublicKey { get; set; }

    }
}
