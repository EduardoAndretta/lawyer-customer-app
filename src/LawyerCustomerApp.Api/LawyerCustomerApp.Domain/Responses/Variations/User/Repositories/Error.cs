using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.User.Responses.Repositories.Error;

internal class Error { }

public class EmailAlreadyInUseError : Constructor
{
    public override string Identity => "EmailAlreadyInUseError";
    public override Type Resource => typeof(Error);
}

public class UserInsertionError : Constructor
{
    public override string Identity => "UserInsertionError";
    public override Type Resource => typeof(Error);
}
