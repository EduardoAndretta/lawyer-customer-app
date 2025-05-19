using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.Permission.Responses.Repositories.Error;

internal class Error { }

#region Case

public class EnlistPermissionsFromCaseDeniedError : Constructor
{
    public override string Identity => "EnlistPermissionsFromCaseDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}


public class GrantPermissionsToCaseDeniedError : Constructor
{
    public override string Identity => "GrantPermissionsToCaseDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class ForbiddenPermissionsToGrantToCaseError : Constructor
{
    public override string Identity => "ForbiddenPermissionsToGrantToCaseError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class GrantPermissionsToCaseForSpecificUserDeniedError : Constructor
{
    public override string Identity => "GrantPermissionsToCaseForSpecificUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class GrantPermissionsToCaseInsertionError : Constructor
{
    public override string Identity => "GrantPermissionsToCaseInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsToCaseDeniedError : Constructor
{
    public override string Identity => "RevokePermissionsToCaseDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class ForbiddenPermissionsToRevokeToCaseError : Constructor
{
    public override string Identity => "ForbiddenPermissionsToRevokeToCaseError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsToCaseForSpecificUserDeniedError : Constructor
{
    public override string Identity => "RevokePermissionsToCaseForSpecificUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsToCaseInsertionError : Constructor
{
    public override string Identity => "RevokePermissionsToCaseInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

#endregion

#region User

public class EnlistPermissionsFromUserDeniedError : Constructor
{
    public override string Identity => "EnlistedPermissionsFromUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}


public class GrantPermissionsToUserDeniedError : Constructor
{
    public override string Identity => "GrantPermissionsToUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class ForbiddenPermissionsToGrantToUserError : Constructor
{
    public override string Identity => "ForbiddenPermissionsToGrantToUserError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class GrantPermissionsToUserForSpecificUserDeniedError : Constructor
{
    public override string Identity => "GrantPermissionsToUserForSpecificUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class GrantPermissionsToUserInsertionError : Constructor
{
    public override string Identity => "GrantPermissionsToUserInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsToUserDeniedError : Constructor
{
    public override string Identity => "RevokePermissionsToUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class ForbiddenPermissionsToRevokeToUserError : Constructor
{
    public override string Identity => "ForbiddenPermissionsToRevokeToUserError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsToUserForSpecificUserDeniedError : Constructor
{
    public override string Identity => "RevokePermissionsToUserForSpecificUserDeniedError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class RevokePermissionsToUserInsertionError : Constructor
{
    public override string Identity => "RevokePermissionsToUserInsertionError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

#endregion

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

public class CaseNotFoundError : Constructor
{
    public override string Identity => "CaseNotFoundError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}

public class UserNotCapableForAttributeAccountError : Constructor
{
    public override string Identity => "UserNotCapableForAttributeAccountError";
    public override Type Resource => typeof(Error);

    public override int Status => 400;
}