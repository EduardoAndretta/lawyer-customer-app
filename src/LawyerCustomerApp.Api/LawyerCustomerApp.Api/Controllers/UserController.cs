using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.Domain.User.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

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

    [HttpPost("search"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<SearchInformationDto>> Post(
        [FromBody] SearchParametersDto parameters,
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

            return resultContructor.Build<DetailsInformationDto>().HandleActionResult(this);
        }
        var result = await _service.DetailsAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
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

    public class EditPatchExampleOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // [Only apply to the PATCH /edit endpoint]
            if (context.ApiDescription.HttpMethod == "PATCH" && (context.ApiDescription.RelativePath?.Contains("api/user/edit") ?? false))
            {
                var requestBody = operation.RequestBody?.Content["application/json"];
                if (requestBody != null)
                {
                    requestBody.Example = new OpenApiObject
                    {
                        ["relatedUserId"] = new OpenApiInteger(0),
                        ["userId"]        = new OpenApiInteger(0),
                        ["roleId"]        = new OpenApiInteger(0),
                        ["values"]        = new OpenApiObject
                        {
                            ["private"] = new OpenApiBoolean(false),
                            ["address"] = new OpenApiObject
                            {
                                ["zipCode"]     = new OpenApiString("stirng"),
                                ["houseNumber"] = new OpenApiString("stirng"),
                                ["complement"]  = new OpenApiString("stirng"),
                                ["district"]    = new OpenApiString("stirng"),
                                ["city"]        = new OpenApiString("stirng"),
                                ["state"]       = new OpenApiString("stirng"),
                                ["country"]     = new OpenApiString("stirng")
                            },
                            ["document"] = new OpenApiObject
                            {
                                ["type"]               = new OpenApiString("stirng"),
                                ["identifierDocument"] = new OpenApiString("stirng")
                            },
                            ["accounts"] = new OpenApiObject
                            {
                                ["lawyer"] = new OpenApiObject
                                {
                                    ["phone"]   = new OpenApiString("stirng"),
                                    ["private"] = new OpenApiBoolean(false),
                                    ["address"] = new OpenApiObject
                                    {
                                        ["zipCode"]     = new OpenApiString("stirng"),
                                        ["houseNumber"] = new OpenApiString("stirng"),
                                        ["complement"]  = new OpenApiString("stirng"),
                                        ["district"]    = new OpenApiString("stirng"),
                                        ["city"]        = new OpenApiString("stirng"),
                                        ["state"]       = new OpenApiString("stirng"),
                                        ["country"]     = new OpenApiString("stirng")
                                    },
                                    ["document"] = new OpenApiObject
                                    {
                                        ["type"]               = new OpenApiString("string"),
                                        ["identifierDocument"] = new OpenApiString("string")
                                    }
                                },
                                ["customer"] = new OpenApiObject
                                {
                                    ["phone"]   = new OpenApiString("string"),
                                    ["private"] = new OpenApiBoolean(false),
                                    ["address"] = new OpenApiObject
                                    {
                                        ["zipCode"]     = new OpenApiString("stirng"),
                                        ["houseNumber"] = new OpenApiString("stirng"),
                                        ["complement"]  = new OpenApiString("stirng"),
                                        ["district"]    = new OpenApiString("stirng"),
                                        ["city"]        = new OpenApiString("stirng"),
                                        ["state"]       = new OpenApiString("stirng"),
                                        ["country"]     = new OpenApiString("stirng")
                                    },
                                    ["document"] = new OpenApiObject
                                    {
                                        ["type"]               = new OpenApiString("stirng"),
                                        ["identifierDocument"] = new OpenApiString("stirng")
                                    }
                                }
                            }
                        }
                    };
                }
            }
        }
    }

    [HttpPatch("edit"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Patch(
        [FromBody] EditParametersDto parameters,
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
        var result = await _service.EditAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }

    [HttpPut("grant-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Put(
        [FromBody] GrantPermissionsParametersDto parameters,
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

        var result = await _service.GrantPermissionsAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }

    [HttpPut("revoke-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Put(
        [FromBody] RevokePermissionsParametersDto parameters,
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

        var result = await _service.RevokePermissionsAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }
}
