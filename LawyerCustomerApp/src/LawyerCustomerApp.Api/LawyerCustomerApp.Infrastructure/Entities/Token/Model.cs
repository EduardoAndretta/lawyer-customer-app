namespace LawyerCustomerApp.Infrastructure.Token.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int UserId { get; init; } = 0;

    public string RefreshToken { get; init; } = string.Empty;
    public string TokenValue { get; init; } = string.Empty;

    public DateTime CreationDate { get; init; } = default;
}