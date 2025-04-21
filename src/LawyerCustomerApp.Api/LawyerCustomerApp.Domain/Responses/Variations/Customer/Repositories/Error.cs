using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.Customer.Responses.Repositories.Error;

internal class Error { }

public class UserAlreadyHaveCustomerAccountError : Constructor
{
    public override string Identity => "UserAlreadyHaveCustomerAccountError";
    public override Type Resource => typeof(Error);
}

public class RegisterCustomerInsertionError : Constructor
{
    public override string Identity => "RegisterCustomerInsertionError";
    public override Type Resource => typeof(Error);
}
