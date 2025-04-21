using LawyerCustomerApp.External.Responses.Error.Models;
using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.External.Jwt.Responses.Error;

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