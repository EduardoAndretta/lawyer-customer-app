using LawyerCustomerApp.External.Exceptions;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using IAuthenticationService = LawyerCustomerApp.Domain.Auth.Interfaces.Services.IService;

namespace LawyerCustomerApp.Application.Configuration.Events;

public class ValidationEvents : JwtBearerEvents
{
    private readonly IAuthenticationService _authenticationService;
    public ValidationEvents(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var token = context.SecurityToken?.UnsafeToString();

        var contextualizer = Contextualizer.Init(context.HttpContext.RequestAborted);

        Result result = await _authenticationService.ValidateAsync(
            new()
            {
                Token = token
            },
            contextualizer);

        if (result.IsFinished)
        {
            var constructor = result.Constructor!;

            var baseExceptionType = typeof(BaseException<>).MakeGenericType(constructor.GetType());

            var exceptionInstance = (Exception)Activator.CreateInstance(baseExceptionType, constructor)!;

            throw exceptionInstance;
        }

        if (result.IsFinished)
        {
            context.Fail("Invalid token according to business logic.");
        }
    }
}
