using LawyerCustomerApp.External.Responses.Error.Models;

namespace LawyerCustomerApp.External.Interfaces;

public interface IErrorGeneratorService
{
    Response CreateError(Exception exception);
    Response CreateError(Constructor constructor);
}
