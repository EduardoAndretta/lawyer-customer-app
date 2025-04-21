using LawyerCustomerApp.External.Responses.Warning.Models;

namespace LawyerCustomerApp.External.Interfaces;

public interface IWarningGeneratorService
{
    void CreateWarning(Constructor constructor);
}
