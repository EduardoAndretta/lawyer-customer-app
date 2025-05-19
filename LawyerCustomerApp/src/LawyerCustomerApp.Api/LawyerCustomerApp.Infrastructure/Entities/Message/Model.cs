namespace LawyerCustomerApp.Infrastructure.Message.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int SenderId { get; init; } = 0;
    public int ReceiverId { get; init; } = 0;

    public string Content { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = default;

    public bool IsRead { get; init; } = false;
}