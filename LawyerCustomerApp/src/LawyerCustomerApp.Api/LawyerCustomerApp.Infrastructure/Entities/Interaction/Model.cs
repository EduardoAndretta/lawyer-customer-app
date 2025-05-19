namespace LawyerCustomerApp.Infrastructure.Interaction.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int CaseId { get; init; } = 0;
    public int UserId { get; init; } = 0;

    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public DateTime InteractionDate { get; init; } = default;
}