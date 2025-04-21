namespace LawyerCustomerApp.Infrastructure.Contact.Entities;

public record Model
{
    public int Id { get; init; } = 0;

    public string PersonalNumber { get; init; } = string.Empty;
    public string CommercialNumber { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}