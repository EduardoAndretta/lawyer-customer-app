namespace LawyerCustomerApp.Infrastructure.User.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int AddressId { get; init; } = 0;
    public int ContactId { get; init; } = 0;

    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;

    // [Hashed in practice]
    public string Password { get; init; } = string.Empty;

    // ['customer' or 'lawyer']
    public string Type { get; init; }  = string.Empty;

    public DateTime RegistrationDate { get; init; } = default;
}