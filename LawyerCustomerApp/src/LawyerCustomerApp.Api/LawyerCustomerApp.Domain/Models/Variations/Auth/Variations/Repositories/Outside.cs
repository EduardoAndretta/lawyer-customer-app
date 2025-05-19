namespace LawyerCustomerApp.Domain.Auth.Repositories.Models;

internal record AuthenticateDatabaseInformation
{
    public int UserId { get; set; } = 0;
    public string Email { get; set; } = string.Empty;
}

internal record RefreshDatabaseInformation
{
    public int UserId { get; set; } = 0;
    public string Email { get; set; } = string.Empty;
    public string JwtToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime JwtTokenLimitDate { get; set; } = default;
    public DateTime RefreshTokenLimitDate { get; set; } = default;
}

internal record InvalidateDatabaseInformation
{
    public int Id { get; set; } = 0;

    public bool Invalidated { get; set; } = false;
}

internal record ValidateDatabaseInformation
{
    public DateTime JwtTokenLimitDate { get; set; } = default;

    public bool Invalidated { get; set; } = false;
}