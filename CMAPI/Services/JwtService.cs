using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CMAPI.Services;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

     public string GenerateAccessToken(string userId, string email, string role)
     {
         var tokenHandler = new JwtSecurityTokenHandler();
         var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
         var tokenDescriptor = new SecurityTokenDescriptor
         {
             Subject = new ClaimsIdentity(new[]
             {
                 new Claim("Id", userId), 
                 new Claim(ClaimTypes.Name, email),
                 new Claim(ClaimTypes.Role, role)  
             }),
             Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenExpiryMinutes"])),
             Issuer = _config["Jwt:Issuer"],
             Audience = _config["Jwt:Audience"],
             SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
         };
         var token = tokenHandler.CreateToken(tokenDescriptor);
         return tokenHandler.WriteToken(token);
     }


     public string GenerateRefreshToken()
     {
         var randomBytes = new byte[32];
         using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
         {
             rng.GetBytes(randomBytes);
             return Convert.ToBase64String(randomBytes);
         }
     }

     public DateTime GetRefreshTokenExpiry()
     {
         return DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenExpiryDays"]));
     }

     public bool ValidateAccessToken(string token)
     {
         var tokenHandler = new JwtSecurityTokenHandler();
         var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
         try
         {
             tokenHandler.ValidateToken(token, new TokenValidationParameters
             {
                 ValidateIssuerSigningKey = true,
                 IssuerSigningKey = new SymmetricSecurityKey(key),
                 ValidateIssuer = true,
                 ValidateAudience = true,
                 ValidIssuer = _config["Jwt:Issuer"],
                 ValidAudience = _config["Jwt:Audience"],
                 ValidateLifetime = true,
                 ClockSkew = TimeSpan.Zero
             }, out SecurityToken validatedToken);

             return true;
         }
         catch
         {
             return false;
         }
     }
     
     
     public string GetUserIdFromToken(string token)
     {
         var tokenHandler = new JwtSecurityTokenHandler();
         var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);

         var validationParameters = new TokenValidationParameters
         {
             ValidateIssuerSigningKey = true,
             IssuerSigningKey         = new SymmetricSecurityKey(key),
             ValidateIssuer           = true,
             ValidIssuer              = _config["Jwt:Issuer"],
             ValidateAudience         = true,
             ValidAudience            = _config["Jwt:Audience"],
             ValidateLifetime         = true,
             ClockSkew                = TimeSpan.Zero
         };

         // Validate and get the ClaimsPrincipal
         var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

         // Extract the "Id" claim
         var idClaim = principal.FindFirst("Id")
                       ?? throw new SecurityTokenException("Id claim not found");

         return idClaim.Value;
     }
}