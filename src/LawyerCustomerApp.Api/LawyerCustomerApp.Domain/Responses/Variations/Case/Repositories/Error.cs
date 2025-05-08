using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.Case.Responses.Repositories.Error;

internal class Error { }

public class RegisterCaseDeniedError : Constructor
{
    public override string Identity => "RegisterCaseDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RegisterCaseInsertionError : Constructor
{
    public override string Identity => "RegisterCaseInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class CaseNotFoundError : Constructor
{
    public override string Identity => "CaseNotFoundError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class AssignLawyerDeniedError : Constructor
{
    public override string Identity => "AssignLawyerDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class AssignCustomerDeniedError : Constructor
{
    public override string Identity => "AssignCustomerDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class EditDeniedError : Constructor
{
    public override string Identity => "EditDeniedError";
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