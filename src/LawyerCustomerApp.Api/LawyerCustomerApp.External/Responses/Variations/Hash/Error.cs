using LawyerCustomerApp.External.Responses.Error.Models;
using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.External.Hash.Responses.Error;

internal class Error { }

internal class NotFoundEncrptyKeyError : Constructor
{
    public override string Identity => "NotFoundEncrptyKeyError";
    public override Type Resource => typeof(Error);
}

internal class InvalidEncrptyKeyError : Constructor
{
    public override string Identity => "InvalidEncrptyKeyError";
    public override Type Resource => typeof(Error);

    // [Parameters]
    public required string Reason { get; init; }

    public override string[] Parameters => new string[1] { Reason };
}