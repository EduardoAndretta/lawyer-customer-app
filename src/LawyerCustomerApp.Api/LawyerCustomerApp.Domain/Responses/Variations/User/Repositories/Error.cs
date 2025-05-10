using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.User.Responses.Repositories.Error;

internal class Error { }

public class RegisterUserDeniedError : Constructor
{
    public override string Identity => "RegisterUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class EmailAlreadyInUseError : Constructor
{
    public override string Identity => "EmailAlreadyInUseError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class UserInsertionError : Constructor
{
    public override string Identity => "UserInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}


public class EditDeniedError : Constructor
{
    public override string Identity => "EditDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class ViewPermissionsDeniedError : Constructor
{
    public override string Identity => "ViewPermissionsDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}


public class GrantPermissionDeniedError : Constructor
{
    public override string Identity => "GrantPermissionDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class ForbiddenPermissionToGrantError : Constructor
{
    public override string Identity => "ForbiddenPermissionToGrantError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class GrantPermissionsForSpecificUserDeniedError : Constructor
{
    public override string Identity => "GrantPermissionsForSpecificUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class GrantPermissionsInsertionError : Constructor
{
    public override string Identity => "GrantPermissionsInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionDeniedError : Constructor
{
    public override string Identity => "RevokePermissionDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class ForbiddenPermissionToRevokeError : Constructor
{
    public override string Identity => "ForbiddenPermissionToRevokeError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsForSpecificUserDeniedError : Constructor
{
    public override string Identity => "RevokePermissionsForSpecificUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsInsertionError : Constructor
{
    public override string Identity => "RevokePermissionsInsertionError";
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
    public override string Identity => "AttributeNotFoundError";
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