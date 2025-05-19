using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.Application.Configuration.Responses.Error;

internal class Error { }

internal class NotFoundJwtKeyError : Constructor
{
    public override string Identity => "NotFoundJwtKey";
    public override Type Resource => typeof(Error);
}

internal class NotWrittenBytesJwtKeyError : Constructor
{
    public override string Identity => "NotWrittenBytesJwtKeyError";
    public override Type Resource => typeof(Error);
}