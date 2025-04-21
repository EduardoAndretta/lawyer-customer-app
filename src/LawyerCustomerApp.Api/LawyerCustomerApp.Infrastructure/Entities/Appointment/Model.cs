namespace LawyerCustomerApp.Infrastructure.Appointment.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int CaseId { get; init; } = 0;
    public int LawyerId { get; init; } = 0;
    public int CustomerId { get; init; } = 0;

    public DateTime AppointmentDate { get; init; } = default;

    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}