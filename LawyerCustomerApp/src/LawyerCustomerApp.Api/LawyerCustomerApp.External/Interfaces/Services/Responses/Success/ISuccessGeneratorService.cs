using LawyerCustomerApp.External.Responses.Success.Models;

namespace LawyerCustomerApp.External.Interfaces;

public interface ISuccessGeneratorService
{
    Response CreateSuccess(Constructor constructor);
}
