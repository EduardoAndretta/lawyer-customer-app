using LawyerCustomerApp.Domain.Auth.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Auth.Interfaces.Services;

public interface IRepository
{
    Task<Result<AuthenticateInformation>> AuthenticateAsync(AuthenticateParameters parameters, Contextualizer contextualizer);
    Task<Result<RefreshInformation>> RefreshAsync(RefreshParameters parameters, Contextualizer contextualizer);
    Task<Result> InvalidateAsync(InvalidateParameters parameters, Contextualizer contextualizer);
    Task<Result> ValidateAsync(ValidateParameters parameters, Contextualizer contextualizer);
}