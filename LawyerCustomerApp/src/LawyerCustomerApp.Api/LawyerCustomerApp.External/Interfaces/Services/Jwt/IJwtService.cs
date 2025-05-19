using LawyerCustomerApp.External.Jwt.Common.Models;
using LawyerCustomerApp.External.Models;

namespace LawyerCustomerApp.External.Interfaces;

public interface IJwtService
{
    Result<string> GenerateJwtToken(JwtConfiguration configuration);
    Result<string> GenerateRefreshToken();
}
