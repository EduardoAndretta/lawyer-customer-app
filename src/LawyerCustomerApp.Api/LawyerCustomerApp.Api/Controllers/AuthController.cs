using LawyerCustomerApp.Domain.Auth.Common.Models;
using LawyerCustomerApp.Domain.Auth.Interfaces.Services;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.Auth.Controllers;

[Tags("authentication")]
[Route("api/auth")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service; 
    }

    [HttpPost("authenticate")]
    public async Task<ActionResult<AuthenticateInformationDto>> Post(
        [FromBody] AuthenticateParametersDto parameters,
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

        var result = await _service.AuthenticateAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshInformationDto>> Post(
        [FromBody] RefreshParametersDto parameters,
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

        var result = await _service.RefreshAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPut("invalidate")]
    public async Task<ActionResult> Put(
        [FromBody] InvalidateParametersDto parameters,
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

        var result = await _service.InvalidateAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }
}
