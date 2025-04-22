using LawyerCustomerApp.External.Interfaces;

namespace LawyerCustomerApp.Domain.Case.Common.Models;

public class SearchParametersDto
{
    public enum Personas
    {
        Lawyer,
        Customer
    }

    public string? Query { get; init; }
  
    public int? UserId { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public Personas? Persona { get; init; }

    public SearchParameters ToOrdinary()
    {
        return new SearchParameters
        {
            Query = this.Query ?? string.Empty,

            UserId = this.UserId ?? 0,

            Pagination = new() 
            {
                Begin = this.Pagination?.Begin ?? 0,
                End   = this.Pagination?.End   ?? 0
            },

            Persona = this.Persona switch
            {
                Personas.Lawyer   => SearchParameters.Personas.Lawyer,
                Personas.Customer => SearchParameters.Personas.Customer,
                _                 => SearchParameters.Personas.Customer
            }
        };
    }

    public class PaginationProperties
    {
        public int? Begin { get; init; }
        public int? End { get; init; }
    }
}

public class SearchParameters
{
    public enum Personas
    {
        Lawyer,
        Customer
    }

    public required string Query { get; init; }

    public required int UserId { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public Personas? Persona { get; init; }

    public string GetPersonaIdentifier()
    {
        return this.Persona switch
        {
            Personas.Lawyer   => "LAWYER",
            Personas.Customer => "CUSTOMER",
            _ => "UNKNOW"
        };
    }

    public class PaginationProperties
    {
        public required int Begin { get; init; }
        public required int End { get; init; }
    }
}

public class RegisterParametersDto
{
    public enum Personas
    {
        Lawyer,
        Customer
    }

    public string? Title { get; init; }
    public string? Description { get; init; }

    public int? CustomerId { get; init; }
    public int? LawyerId { get; init; }
    public int? UserId { get; init; }

    public Personas? Persona { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {

            Title       = this.Title       ?? string.Empty,
            Description = this.Description ?? string.Empty,

            CustomerId = this.CustomerId ?? 0,
            LawyerId   = this.LawyerId   ?? 0,
            UserId     = this.UserId     ?? 0,

            Persona = this.Persona switch
            {
                Personas.Lawyer   => RegisterParameters.Personas.Lawyer,
                Personas.Customer => RegisterParameters.Personas.Customer,
                _                 => RegisterParameters.Personas.Customer
            }
        };
    }
}

public class RegisterParameters
{
    public enum Personas
    {
        Lawyer,
        Customer
    }

    public required string Title { get; init; }
    public required string Description { get; init; }

    public required int CustomerId { get; init; }
    public required int LawyerId { get; init; }
    public required int UserId { get; init; }

    public Personas? Persona { get; init; }

    public string GetPersonaIdentifier()
    {
        return this.Persona switch
        {
            Personas.Lawyer   => "LAWYER",
            Personas.Customer => "CUSTOMER",
            _ => "UNKNOW"
        };
    }
}

public class AssignLawyerParametersDto
{
    public enum Personas
    {
        Lawyer,
        Customer
    }

    public int? CaseId { get; init; }
    public int? LawyerId { get; init; }
    public int? UserId { get; init; }
    public Personas? Persona { get; init; }

    public AssignLawyerParameters ToOrdinary()
    {
        return new AssignLawyerParameters
        {
            CaseId   = this.CaseId   ?? 0,
            UserId   = this.UserId   ?? 0,
            LawyerId = this.LawyerId ?? 0,

            Persona = this.Persona switch
            {
                Personas.Lawyer   => AssignLawyerParameters.Personas.Lawyer,
                Personas.Customer => AssignLawyerParameters.Personas.Customer,
                _                 => AssignLawyerParameters.Personas.Customer
            }
        };
    }
}

public class AssignLawyerParameters
{
    public enum Personas
    {
        Lawyer,
        Customer
    }

    public required int CaseId { get; init; }
    public required int UserId { get; init; }
    public required int LawyerId { get; init; }
    public required Personas Persona { get; init; }

    public string GetPersonaIdentifier()
    {
        return this.Persona switch
        {
            Personas.Lawyer   => "LAWYER",
            Personas.Customer => "CUSTOMER",
            _                 => "UNKNOW"
        };
    }
}

public class AssignCustomerParametersDto
{
    public int? CaseId { get; init; }
    public int? UserId { get; init; }
    public int? CustomerId { get; init; }

    public AssignCustomerParameters ToOrdinary()
    {
        return new AssignCustomerParameters
        {
            CaseId     = this.CaseId     ?? 0,
            UserId     = this.UserId     ?? 0,
            CustomerId = this.CustomerId ?? 0
        };
    }
}

public class AssignCustomerParameters
{
    public required int CaseId { get; init; }
    public required int UserId { get; init; }
    public required int CustomerId { get; init; }
}

public record SearchInformationDto
{
    public IEnumerable<ItemProperties>? Items { get; init; }

    public class ItemProperties
    {
        public string? Title { get; init; }
        public string? Description { get; init; }

        public int? CaseId { get; init; }
        public int? UserId { get; init; }

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }
    }
}

public record SearchInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string Title { get; init; } = string.Empty;
        public required string Description { get; init; } = string.Empty;

        public required int CaseId { get; init; } = 0;
        public required int UserId { get; init; } = 0;

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }
    }

    public SearchInformationDto ToOrdinary()
    {
        return new SearchInformationDto
        {
            Items = this.Items.Select(x =>
                new SearchInformationDto.ItemProperties
                {
                    Title       = x.Title,
                    Description = x.Description,

                    CaseId = x.CaseId,
                    UserId = x.UserId,

                    CustomerId = x.CustomerId,
                    LawyerId   = x.LawyerId
                })
        };
    }
}