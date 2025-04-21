namespace LawyerCustomerApp.Domain.User.Common.Models;

public class RegisterParametersDto
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? Name { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {
            Email    = this.Email    ?? string.Empty,
            Password = this.Password ?? string.Empty,
            Name     = this.Name     ?? string.Empty
        };
    }
}

public class RegisterParameters
{
    public required string Email { get; init; } = string.Empty;
    public required string Password { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;

    public RegisterParametersDto ToDto()
    {
        return new RegisterParametersDto
        {
            Email    = this.Email,
            Password = this.Password,
            Name     = this.Name
        };
    }
}