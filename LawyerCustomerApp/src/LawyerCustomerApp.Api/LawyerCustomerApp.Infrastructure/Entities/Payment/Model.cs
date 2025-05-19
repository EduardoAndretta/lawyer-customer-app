namespace LawyerCustomerApp.Infrastructure.Payment.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int CaseId { get; init; } = 0;
    public int CustomerId { get; init; } = 0;

    public decimal Amount { get; init; } = 0;

    public DateTime PaymentDate { get; init; } = default;

    public string Method { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}