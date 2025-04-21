using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.Domain.User.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.User.Controllers;

[Tags("user")]
[Route("api/user")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Post(
        [FromBody] RegisterParametersDto parameters,
        CancellationToken cancellationToken = default)
    {
        var contextualizer = Contextualizer.Init(cancellationToken);

        if (!ModelState.IsValid)
        {
            var resultContructor = new ResultConstructor();

            resultContructor.SetConstructor(
                new ModelStateError()
                {
                    Status = 400,
                    SourceCode = this.GetType().Name,
                    Errors = string.Join("; ", ModelState.Values.SelectMany(e => e.Errors).Select(em => em.ErrorMessage))
                });

            return resultContructor.Build().HandleActionResult(this);
        }

        var result = await _service.RegisterAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }
}
