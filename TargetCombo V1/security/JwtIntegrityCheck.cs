using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TargetCombo_V1.security;

public class JwtIntegrityCheck
{
    private static readonly string SecretKey = "TARGETULPOBAV2-LICENSE-KEY-TARGETULPOBAV2";

    public static bool ValidateJwtSignature(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}