using LawyerCustomerApp.Domain.Combo.Common.Models;
using LawyerCustomerApp.Domain.Combo.Interfaces.Services;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LawyerCustomerApp.Application.Combo.Controllers;

[Tags("combo")]
[Route("api/combo")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service; 
    }

    [HttpPost("permissions-enabled-for-grant-case"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<KeyValueInformationDto<long>>> PermissionsEnabledForGrantCase(
        [FromBody] KeyValueParametersDto parameters,
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

            return resultContructor.Build<KeyValueInformationDto<long>>().HandleActionResult(this);
        }
        var result = await _service.PermissionsEnabledForGrantCaseAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("permissions-enabled-for-revoke-case"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<KeyValueInformationDto<long>>> PermissionsEnabledForRevokeCase(
        [FromBody] KeyValueParametersDto parameters,
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

            return resultContructor.Build<KeyValueInformationDto<long>>().HandleActionResult(this);
        }
        var result = await _service.PermissionsEnabledForRevokeCaseAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("permissions-enabled-for-grant-user"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<KeyValueInformationDto<long>>> PermissionsEnabledForGrantUser(
        [FromBody] KeyValueParametersDto parameters,
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

            return resultContructor.Build<KeyValueInformationDto<long>>().HandleActionResult(this);
        }
        var result = await _service.PermissionsEnabledForGrantUserAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("permissions-enabled-for-revoke-user"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<KeyValueInformationDto<long>>> PermissionsEnabledForRevokeUser(
        [FromBody] KeyValueParametersDto parameters,
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

            return resultContructor.Build<KeyValueInformationDto<long>>().HandleActionResult(this);
        }
        var result = await _service.PermissionsEnabledForRevokeUserAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("attributes"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<KeyValueInformationDto<long>>> Attributes(
        [FromBody] KeyValueParametersDto parameters,
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

            return resultContructor.Build<KeyValueInformationDto<long>>().HandleActionResult(this);
        }
        var result = await _service.AttributesAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("roles"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<KeyValueInformationDto<long>>> Roles(
        [FromBody] KeyValueParametersDto parameters,
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

            return resultContructor.Build<KeyValueInformationDto<long>>().HandleActionResult(this);
        }
        var result = await _service.RolesAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }
}
