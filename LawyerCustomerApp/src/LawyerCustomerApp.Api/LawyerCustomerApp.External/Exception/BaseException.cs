using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.External.Exceptions;

public class BaseException<TConstructor> : Exception where TConstructor : Constructor
{
    public required TConstructor Constructor { get; init; }

    public BaseException(TConstructor constructor)
    {
        Constructor = constructor;
    }

    public BaseException()
    {
    }

    public BaseException(string message) : base(message)
    {
    }

    public BaseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
