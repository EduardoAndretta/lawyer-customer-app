using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.Lawyer.Responses.Repositories.Error;

internal class Error { }

public class UserAlreadyHaveLawyerAccountError : Constructor
{
    public override string Identity => "UserAlreadyHaveLawyerAccountError";
    public override Type Resource => typeof(Error);
}

public class RegisterLawyerInsertionError : Constructor
{
    public override string Identity => "RegisterLawyerInsertionError";
    public override Type Resource => typeof(Error);
}
