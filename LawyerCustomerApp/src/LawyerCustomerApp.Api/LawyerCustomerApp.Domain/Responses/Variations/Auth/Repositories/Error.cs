using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Domain.Auth.Responses.Repositories.Error;

internal class Error { }

public class TokenAuthenticationError : Constructor
{
    public override string Identity => "TokenAuthenticationError";
    public override Type Resource => typeof(Error);
}

public class TokenAuthenticationInsertionError : Constructor
{
    public override string Identity => "TokenAuthenticationInsertionError";
    public override Type Resource => typeof(Error);
}

public class TokenRefreshError : Constructor
{
    public override string Identity => "TokenRefreshError";
    public override Type Resource => typeof(Error);
}

public class TokenJwtNotExpiredError : Constructor
{
    public override string Identity => "TokenJwtNotExpiredError";
    public override Type Resource => typeof(Error);
}

public class TokenJwtExpiredError : Constructor
{
    public override string Identity => "TokenJwtExpiredError";
    public override Type Resource => typeof(Error);
}


public class TokenRefreshExpiredError : Constructor
{
    public override string Identity => "TokenRefreshExpiredError";
    public override Type Resource => typeof(Error);
}

public class TokenRefreshInsertionError : Constructor
{
    public override string Identity => "TokenRefreshInsertionError";
    public override Type Resource => typeof(Error);
}

public class TokenInvalidatedError : Constructor
{
    public override string Identity => "TokenInvalidatedError";
    public override Type Resource => typeof(Error);
}

public class TokenInvalidateUpdateError : Constructor
{
    public override string Identity => "TokenInvalidateUpdateError";
    public override Type Resource => typeof(Error);
}

public class TokenValidationError : Constructor
{
    public override string Identity => "TokenValidationError";
    public override Type Resource => typeof(Error);
}
