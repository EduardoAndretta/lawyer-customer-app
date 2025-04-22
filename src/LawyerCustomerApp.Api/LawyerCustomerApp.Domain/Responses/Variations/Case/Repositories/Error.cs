using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.Case.Responses.Repositories.Error;

internal class Error { }

public class RegisterCaseDeniedError : Constructor
{
    public override string Identity => "RegisterCaseDeniedError";
    public override Type Resource => typeof(Error);
}

public class RegisterCaseInsertionError : Constructor
{
    public override string Identity => "RegisterCaseInsertionError";
    public override Type Resource => typeof(Error);
}

public class CaseNotFoundError : Constructor
{
    public override string Identity => "CaseNotFoundError";
    public override Type Resource => typeof(Error);
}

public class AssignLawyerDeniedError : Constructor
{
    public override string Identity => "AssignLawyerDeniedError";
    public override Type Resource => typeof(Error);
}