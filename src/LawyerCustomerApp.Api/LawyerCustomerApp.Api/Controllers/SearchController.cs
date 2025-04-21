using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Search.Interfaces.Services;
using LawyerCustomerApp.Domain.Search.Models.Common;
using LawyerCustomerApp.External.Exceptions;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.Search.Controllers;

[Tags("search")]
[Route("api/search")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service; 
    }

    [HttpPost("cases"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<bool>> Cases(
        [FromBody] SearchCasesParametersDto parameters,
        CancellationToken cancellationToken = default)
    {
        var contextualizer = Contextualizer.Init(cancellationToken);

        if (!ModelState.IsValid)
        {
            var resultContructor = new ResultConstructor();

            resultContructor.SetConstructor(
                new ModelStateError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Errors     = string.Join("; ", ModelState.Values.SelectMany(e => e.Errors).Select(em => em.ErrorMessage))
                });

            return resultContructor.Build().HandleActionResult(this);
        }

        var result = await _service.SearchCasesAsync(parameters);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("lawyers"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<bool>> Lawyers(
        [FromBody] SearchLawyersParametersDto parameters,
        CancellationToken cancellationToken = default)
    {
        var contextualizer = Contextualizer.Init(cancellationToken);

        if (!ModelState.IsValid)
            throw new BaseException<ModelStateError>()
            {
                Constructor = new()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Errors     = string.Join("; ", ModelState.Values.SelectMany(e => e.Errors).Select(em => em.ErrorMessage))
                }
            };

        var result = await _service.SearchLawyersAsync(parameters);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }
}
