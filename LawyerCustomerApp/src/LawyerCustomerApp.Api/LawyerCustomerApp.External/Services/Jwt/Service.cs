using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Jwt.Common.Models;
using LawyerCustomerApp.External.Jwt.Responses.Error;
using LawyerCustomerApp.External.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LawyerCustomerApp.External.Jwt.Services;

internal class Service : IJwtService
{
    private readonly IConfiguration _configuration;
    public Service(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Result<string> GenerateJwtToken(JwtConfiguration configuration)
    {
        var resultConstructor = new ResultConstructor();

        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            resultConstructor.SetConstructor(
                new NotFoundJwtKeyError()
                {
                    Status = 500,
                });

            return resultConstructor.Build<string>();
        }

        int byteCount = Encoding.UTF8.GetByteCount(jwtKey);

        byte[] keyBytes = new byte[byteCount];

        if (!Encoding.UTF8.TryGetBytes(jwtKey, keyBytes, out var bytesWritten))
        {
            resultConstructor.SetConstructor(
                new NotWrittenBytesJwtKeyError()
                {
                    Status = 500,
                });

            return resultConstructor.Build<string>();
        }

        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("user_id", configuration.UserId),
            new Claim("role_id", configuration.RoleId),

            new Claim(ClaimTypes.NameIdentifier, configuration.NameIdentifier),
            new Claim(ClaimTypes.Email,          configuration.Email),
            new Claim(ClaimTypes.Role,           Enum.GetName(configuration.Role) ?? "User")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),

            Expires = configuration.TimeSpecification.Type switch 
            {
                JwtConfiguration.TimeSpecificationProperties.Types.Second => configuration.TimeSpecification.Base.AddSeconds(configuration.TimeSpecification.Quantity),
                JwtConfiguration.TimeSpecificationProperties.Types.Minute => configuration.TimeSpecification.Base.AddMinutes(configuration.TimeSpecification.Quantity),
                JwtConfiguration.TimeSpecificationProperties.Types.Hour   => configuration.TimeSpecification.Base.AddHours(configuration.TimeSpecification.Quantity),
                JwtConfiguration.TimeSpecificationProperties.Types.Day    => configuration.TimeSpecification.Base.AddDays(configuration.TimeSpecification.Quantity),
                JwtConfiguration.TimeSpecificationProperties.Types.Month  => configuration.TimeSpecification.Base.AddMonths(configuration.TimeSpecification.Quantity),
                JwtConfiguration.TimeSpecificationProperties.Types.Year   => configuration.TimeSpecification.Base.AddYears(configuration.TimeSpecification.Quantity),
                _ => DateTime.Now.AddHours(3)
            },

            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();

        var token = handler.CreateEncodedJwt(tokenDescriptor);

        return resultConstructor.Build<string>(token);
    }

    public Result<string> GenerateRefreshToken()
    {
        var resultConstructor = new ResultConstructor();

        var randomBytes = new byte[128];

        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(randomBytes);

        var token = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return resultConstructor.Build<string>(token);
    }
}
