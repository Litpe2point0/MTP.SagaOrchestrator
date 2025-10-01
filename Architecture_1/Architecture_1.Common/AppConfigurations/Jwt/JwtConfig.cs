using Microsoft.Extensions.Configuration;
using Architecture_1.Common.AppConfigurations.Jwt.interfaces;

namespace Architecture_1.Common.AppConfigurations.Jwt
{
    public class JwtConfigModel
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public int Exp { get; set; }
        public string Audience { get; set; }
        public double AccessTokenExpiration { get; set; }
        public double RefreshTokenExpiration { get; set; }
        public string OpenSSL_PrivateKey_Path { get; set; }
        public string OpenSSL_PublicKey_Path { get; set; }
    }
    public class JwtConfig : IJwtConfig
    {
        public int Exp { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public double AccessTokenExpiration { get; set; }
        public double RefreshTokenExpiration { get; set; }

        public string SecretKey { get; set; }
        public string OpenSSL_PrivateKey_Path { get; set; }
        public string OpenSSL_PublicKey_Path { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }

        public JwtConfig(IConfiguration configuration)
        {

            // SecretKey = configuration["Jwt:SecretKey"];
            // OpenSSL_PrivateKey_Path = configuration["Jwt:OpenSSL_PrivateKey_Path"];
            // OpenSSL_PublicKey_Path = configuration["Jwt:OpenSSL_PublicKey_Path"];
            // PrivateKey = GetKeyFromFile(configuration["Jwt:OpenSSL_PrivateKey_Path"]);
            // PublicKey = GetKeyFromFile(configuration["Jwt:OpenSSL_PublicKey_Path"]);
            // Exp = int.Parse(configuration["JWT:Exp"]);
            // Issuer = configuration["Jwt:Issuer"];
            // Audience = configuration["Jwt:Audience"];
            // AccessTokenExpiration = double.Parse(configuration["Jwt:AccessTokenExpiration"]);
            // RefreshTokenExpiration = double.Parse(configuration["Jwt:RefreshTokenExpiration"]);

            var jwtConfig = configuration.GetSection("Jwt").Get<JwtConfigModel>();
            Exp = jwtConfig.Exp;
            Issuer = jwtConfig.Issuer;
            Audience = jwtConfig.Audience;
            AccessTokenExpiration = jwtConfig.AccessTokenExpiration;
            RefreshTokenExpiration = jwtConfig.RefreshTokenExpiration;
            SecretKey = jwtConfig.SecretKey;
            OpenSSL_PrivateKey_Path = jwtConfig.OpenSSL_PrivateKey_Path;
            OpenSSL_PublicKey_Path = jwtConfig.OpenSSL_PublicKey_Path;
            PrivateKey = GetKeyFromFile(jwtConfig.OpenSSL_PrivateKey_Path);
            PublicKey = GetKeyFromFile(jwtConfig.OpenSSL_PublicKey_Path);
        }

        public string GetKeyFromFile(string path)
        {
            try
            {
                path = path.Replace("\\", Path.DirectorySeparatorChar.ToString());
                string basePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
                string relativePath = path.TrimStart(Path.DirectorySeparatorChar);

                string fullPath = Path.Combine(basePath, relativePath);
                var key = File.ReadAllText(fullPath);

                return key;
            } catch (Exception ex)
            {
                Console.WriteLine($"\n\n\n\nALOOOOOOOOOOOOOOOOOOO: {ex.Message}");
                return null;
            }


        }

        public override string ToString()
        {
            return $"Exp: {Exp}, Issuer: {Issuer}, Audience: {Audience}, AccessTokenExpiration: {AccessTokenExpiration}, RefreshTokenExpiration: {RefreshTokenExpiration}, SecretKey: {SecretKey}, PrivateKey: {PrivateKey}, PublicKey: {PublicKey}";
        }
    }
}
