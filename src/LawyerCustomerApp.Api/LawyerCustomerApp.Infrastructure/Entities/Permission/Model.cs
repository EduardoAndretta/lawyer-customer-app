namespace LawyerCustomerApp.Infrastructure.Permission.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int EntityId { get; init; } = 0;

    // ['user' or 'case']
    public string EntityType { get; init; } = string.Empty;

    public bool IsPublic { get; init; } = false;
}