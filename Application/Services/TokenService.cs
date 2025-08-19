using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Services
{
    public class TokenService(IConfiguration configuration)
    {
        private string validIssuer = configuration["Jwt:ValidIssuer"] ?? throw new Exception("Set Jwt configs - missing ValidIssuer");
        private string validAudience = configuration["Jwt:ValidAudience"] ?? throw new Exception("Set Jwt configs - missing ValidAudience");
        private string secretKey = configuration["Jwt:SecretKey"] ?? throw new Exception("Set Jwt configs - missing SecretKey");

        public string CreateAccessToken(ClaimsIdentity identity)
        {
            var token = new JwtSecurityToken(
                issuer: validIssuer,
                audience: validAudience,
                claims: identity.Claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateRefreshToken()
        {
            var guid = Guid.NewGuid().ToString();
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(guid));

            return token;
        }
    }
}
