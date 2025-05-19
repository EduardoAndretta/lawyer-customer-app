using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Customer.Common.Models;
using LawyerCustomerApp.Domain.Customer.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.Customer.Controllers;

[Tags("customer")]
[Route("api/customer")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service;
    }

    [HttpPost("search"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<SearchInformationDto>> Post(
        [FromBody] SearchParametersDto parameters,
        CancellationToken cancellationToken = default)
    {
        int userId = int.TryParse(User.FindFirst("user_id")?.Value, out userId) ? userId : 0;
        int roleId = int.TryParse(User.FindFirst("role_id")?.Value, out roleId) ? roleId : 0;

        parameters = parameters with
        {
            UserId = userId,
            RoleId = roleId
        };

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

            return resultContructor.Build<SearchInformationDto>().HandleActionResult(this);
        }
        var result = await _service.SearchAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }
    
    [HttpPost("search/count"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<CountInformationDto>> Post(
        [FromBody] CountParametersDto parameters,
        CancellationToken cancellationToken = default)
    {
        int userId = int.TryParse(User.FindFirst("user_id")?.Value, out userId) ? userId : 0;
        int roleId = int.TryParse(User.FindFirst("role_id")?.Value, out roleId) ? roleId : 0;

        parameters = parameters with
        {
            UserId = userId,
            RoleId = roleId
        };

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

            return resultContructor.Build<CountInformationDto>().HandleActionResult(this);
        }
        var result = await _service.CountAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }
    
    [HttpPost("details"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<DetailsInformationDto>> Post(
        [FromBody] DetailsParametersDto parameters,
        CancellationToken cancellationToken = default)
    {
        int userId = int.TryParse(User.FindFirst("user_id")?.Value, out userId) ? userId : 0;
        int roleId = int.TryParse(User.FindFirst("role_id")?.Value, out roleId) ? roleId : 0;

        parameters = parameters with
        {
            UserId = userId,
            RoleId = roleId
        };

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

            return resultContructor.Build<SearchInformationDto>().HandleActionResult(this);
        }
        var result = await _service.DetailsAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("register/account"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Post(
        [FromBody] RegisterParametersDto parameters,
        CancellationToken cancellationToken = default)
    {
        int userId = int.TryParse(User.FindFirst("user_id")?.Value, out userId) ? userId : 0;
        int roleId = int.TryParse(User.FindFirst("role_id")?.Value, out roleId) ? roleId : 0;

        parameters = parameters with
        {
            UserId = userId,
            RoleId = roleId
        };

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

        var result = await _service.RegisterAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }
}
