namespace LawyerCustomerApp.Domain.Lawyer.Common.Models;

public class RegisterParametersDto
{
    public int? UserId { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {
            UserId  = this.UserId  ?? 0,
            Phone   = this.Phone   ?? string.Empty,
            Address = this.Address ?? string.Empty,
        };
    }
}

public class RegisterParameters
{
    public required int UserId { get; init; } = 0;
    public required string Phone { get; init; } = string.Empty;
    public required string Address { get; init; } = string.Empty;

    public RegisterParametersDto ToDto()
    {
        return new RegisterParametersDto
        {
            UserId  = this.UserId,
            Phone   = this.Phone,
            Address = this.Address
        };
    }
}