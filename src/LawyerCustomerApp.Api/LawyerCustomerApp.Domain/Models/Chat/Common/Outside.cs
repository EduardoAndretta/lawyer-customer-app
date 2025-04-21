namespace LawyerCustomerApp.Domain.Chat.Models.Common;

public class GetParametersDto
{
    public int? UserId { get; init; }

    public DateTime? BeginDate { get; init; }
    public DateTime? EndDate { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public class PaginationProperties
    {
        public int? BeginIndex { get; init; }
        public int? EndIndex { get; init; }
    }

    public GetParameters ToOrdinary()
    {
        return new GetParameters
        {
            UserId = this.UserId ?? 0,

            BeginDate = this.BeginDate ?? default,
            EndDate   = this.EndDate   ?? default,

            Pagination = new()
            {
                BeginIndex = this.Pagination?.BeginIndex ?? 0,
                EndIndex   = this.Pagination?.EndIndex   ?? 0
            }
        };
    }
}

public class GetParameters
{
    public required int UserId { get; init; }

    public required DateTime BeginDate { get; init; }
    public required DateTime EndDate { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int BeginIndex { get; init; }
        public required int EndIndex { get; init; }
    }

    public GetParametersDto ToDto()
    {
        return new GetParametersDto
        {
            UserId = this.UserId,

            BeginDate = this.BeginDate,
            EndDate   = this.EndDate,

            Pagination = new()
            {
                BeginIndex = this.Pagination?.BeginIndex,
                EndIndex   = this.Pagination?.EndIndex
            }
        };
    }
}