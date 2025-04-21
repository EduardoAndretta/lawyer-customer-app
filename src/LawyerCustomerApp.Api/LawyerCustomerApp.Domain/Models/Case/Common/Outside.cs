namespace LawyerCustomerApp.Domain.Case.Common.Models;

public class RegisterParametersDto
{
    public int? UserId { get; init; }

    public string? Title { get; init; }
    public string? Description { get; init; }

    public int? CustomerId { get; init; }
    public int? LawyerId { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {
            UserId = this.UserId ?? 0,

            Title       = this.Title       ?? string.Empty,
            Description = this.Description ?? string.Empty,

            CustomerId = this.CustomerId ?? 0,
            LawyerId   = this.LawyerId   ?? 0
        };
    }
}

public class RegisterParameters
{
    public required int UserId { get; init; }

    public required string Title { get; init; }
    public required string Description { get; init; }

    public required int CustomerId { get; init; }
    public required int LawyerId { get; init; }

    public RegisterParametersDto ToDto()
    {
        return new RegisterParametersDto
        {
            UserId = this.UserId,

            Title       = this.Title,
            Description = this.Description,

            CustomerId = this.CustomerId,
            LawyerId   = this.LawyerId
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
    public int? UserId { get; init; }
    public int? LawyerId { get; init; }
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

    public AssignLawyerParametersDto ToDto()
    {
        return new AssignLawyerParametersDto
        {
            CaseId   = this.CaseId,
            UserId   = this.UserId,
            LawyerId = this.LawyerId,

            Persona = this.Persona switch
            {
                Personas.Lawyer   => AssignLawyerParametersDto.Personas.Lawyer,
                Personas.Customer => AssignLawyerParametersDto.Personas.Customer,
                _                 => AssignLawyerParametersDto.Personas.Customer
            }
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

    public AssignCustomerParametersDto ToDto()
    {
        return new AssignCustomerParametersDto
        {
            CaseId     = this.CaseId,
            UserId     = this.UserId,
            CustomerId = this.CustomerId
        };
    }
}