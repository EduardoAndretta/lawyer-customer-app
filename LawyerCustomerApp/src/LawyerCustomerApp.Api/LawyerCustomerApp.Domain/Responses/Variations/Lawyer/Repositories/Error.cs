using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.Lawyer.Responses.Repositories.Error;

internal class Error { }

public class UserAlreadyHaveLawyerAccountError : Constructor
{
    public override string Identity => "UserAlreadyHaveLawyerAccountError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RegisterDeniedError : Constructor
{
    public override string Identity => "RegisterDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RegisterInsertionError : Constructor
{
    public override string Identity => "RegisterInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class AttributeNotFoundError : Constructor
{
    public override string Identity => "AttributeNotFoundError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class PermissionNotFoundError : Constructor
{
    public override string Identity => "PermissionNotFoundError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RoleNotFoundError : Constructor
{
    public override string Identity => "RoleNotFoundError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class UserNotFoundError : Constructor
{
    public override string Identity => "UserNotFoundError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class UserNotCapableForAttributeAccountError : Constructor
{
    public override string Identity => "UserNotCapableForAttributeAccountError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}