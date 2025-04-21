namespace LawyerCustomerApp.Domain.Search.Models.Common;

public class SearchCasesParametersDto
{
    public int? UserId { get; init; }

    public DateTime? BeginDate { get; init; }
    public DateTime? EndDate { get; init; }

    public string? Query { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public class PaginationProperties
    {
        public int? BeginIndex { get; init; }
        public int? EndIndex { get; init; }
    }

    public SearchCasesParameters ToOrdinary()
    {
        return new SearchCasesParameters
        {
            UserId = this.UserId ?? 0,

            BeginDate = this.BeginDate ?? default,
            EndDate   = this.EndDate   ?? default,

            Query = this.Query ?? string.Empty,

            Pagination = new()
            {
                BeginIndex = this.Pagination?.BeginIndex ?? 0,
                EndIndex   = this.Pagination?.EndIndex   ?? 0
            }
        };
    }
}

public class SearchCasesParameters
{
    public required int UserId { get; init; }

    public required DateTime BeginDate { get; init; }
    public required DateTime EndDate { get; init; }

    public required string Query { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int BeginIndex { get; init; }
        public required int EndIndex { get; init; }
    }

    public SearchCasesParametersDto ToDto()
    {
        return new SearchCasesParametersDto
        {
            UserId = this.UserId,

            BeginDate = this.BeginDate,
            EndDate   = this.EndDate,

            Query = this.Query,

            Pagination = new()
            {
                BeginIndex = this.Pagination?.BeginIndex,
                EndIndex   = this.Pagination?.EndIndex
            }
        };
    }
}

public class SearchLawyersParametersDto
{
    public int? UserId { get; init; }

    public string? Query { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public class PaginationProperties
    {
        public int? BeginIndex { get; init; }
        public int? EndIndex { get; init; }
    }

    public SearchLawyersParameters ToOrdinary()
    {
        return new SearchLawyersParameters
        {
            UserId = this.UserId ?? 0,

            Query = this.Query ?? string.Empty,

            Pagination = new()
            {
                BeginIndex = this.Pagination?.BeginIndex ?? 0,
                EndIndex   = this.Pagination?.EndIndex   ?? 0
            }
        };
    }
}

public class SearchLawyersParameters
{
    public required int UserId { get; init; }

    public required string Query { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int BeginIndex { get; init; }
        public required int EndIndex { get; init; }
    }
    public SearchLawyersParametersDto ToDto()
    {
        return new SearchLawyersParametersDto
        {
            UserId = this.UserId,

            Query = this.Query,

            Pagination = new()
            {
                BeginIndex = this.Pagination?.BeginIndex,
                EndIndex   = this.Pagination?.EndIndex
            }
        };
    }
}