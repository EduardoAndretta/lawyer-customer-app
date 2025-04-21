using LawyerCustomerApp.Domain.Chat.Interfaces.Services;
using LawyerCustomerApp.Domain.Chat.Models.Common;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.Chat.Controllers;

[Tags("chat")]
[Route("api/chat")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service; 
    }

    [HttpPost("messages"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<bool>> Messages(
        [FromBody] GetParametersDto parameters,
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

        var result = await _service.GetAsync(parameters);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }
}
