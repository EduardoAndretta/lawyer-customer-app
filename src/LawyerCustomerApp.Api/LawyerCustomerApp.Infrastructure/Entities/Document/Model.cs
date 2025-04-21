namespace LawyerCustomerApp.Infrastructure.Document.Entities;

public record Model
{
    public int Id { get; init; } = 0;
    public int CaseId { get; init; } = 0;

    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;

    public DateTime UploadDate { get; init; } = default;
}