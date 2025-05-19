namespace LawyerCustomerApp.Infrastructure.Lawyer.Entities;

using User = LawyerCustomerApp.Infrastructure.User.Entities.Model;

public record Model
{
    public int Id { get; init; } = 0;
    public int UserId { get; init; } = 0;

    public string Specialty { get; init; } = string.Empty;

    // [Navigation property (loaded manually with Dapper)]
    public User User { get; init; } = null!;
}