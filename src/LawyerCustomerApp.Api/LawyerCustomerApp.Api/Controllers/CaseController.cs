using LawyerCustomerApp.Domain.Case.Interfaces.Services;
using LawyerCustomerApp.Domain.Case.Models.Common;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.Case.Controllers;

[Tags("case")]
[Route("api/case")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service; 
    }

    [HttpPost("creation"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<bool>> Creation(
        [FromBody] CreateParametersDto parameters,
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
        var result = await _service.CreateAsync(parameters);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpDelete("deletion"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<bool>> Deletion(
        [FromBody] DeleteParametersDto parameters,
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

        var result = await _service.DeleteAsync(parameters);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }
}
