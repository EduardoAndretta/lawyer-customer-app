using System.Text.Json.Serialization;

namespace LawyerCustomerApp.External.Responses.Common.Models;

[JsonPolymorphic]
[JsonDerivedType(typeof(Success.Models.Response))]
[JsonDerivedType(typeof(Error.Models.Response))]
[JsonDerivedType(typeof(Warning.Models.Response))]
public abstract record Response
{
}
