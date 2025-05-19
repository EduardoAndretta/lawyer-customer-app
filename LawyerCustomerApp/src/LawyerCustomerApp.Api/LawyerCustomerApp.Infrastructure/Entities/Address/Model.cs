namespace LawyerCustomerApp.Infrastructure.Address.Entities;

public record Model
{
    public int Id { get; init; } = 0;

    public string Street { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string Complement { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
}
