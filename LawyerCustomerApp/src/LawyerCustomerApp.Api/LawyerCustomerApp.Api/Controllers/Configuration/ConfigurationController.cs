using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.Configuration.Controllers;

[Tags("configuration")]
[Route("api/configuration")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IInitializerService _service;
    public Controller(IInitializerService service)
    {
        _service = service; 
    }

    [HttpPost("initializer/sqlite")]
    public async Task<ActionResult> Initializer(CancellationToken cancellationToken = default)
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

        var result = await _service.InitializeSqliteDatabase(contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }
}
