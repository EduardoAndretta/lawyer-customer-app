namespace LawyerCustomerApp.Infrastructure.Case.Entities;

using Lawyer = LawyerCustomerApp.Infrastructure.Lawyer.Entities.Model;
using User   = LawyerCustomerApp.Infrastructure.User.Entities.Model;

public record Model
{
    public int Id { get; init; } = 0;
    public int CustomerId { get; init; } = 0;
    public int? LawyerId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public DateTime StartDate { get; init; } = default;
    public DateTime? EndDate { get; init; }

    // [Navigation properties]
    public User Customer { get; init; } = null!;
    public Lawyer Lawyer { get; init; } = null!;
}