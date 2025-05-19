using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.Domain.Case.Interfaces.Services;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using static LawyerCustomerApp.External.Jwt.Common.Models.JwtConfiguration;

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

            return resultContructor.Build<DetailsInformationDto>().HandleActionResult(this);
        }
        var result = await _service.DetailsAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("register"), Authorize(Policy = "internal-jwt-bearer")]
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

    public class EditPatchExampleOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // [Only apply to the PATCH /edit endpoint]
            if (context.ApiDescription.HttpMethod == "PATCH" && (context.ApiDescription.RelativePath?.Contains("api/case/edit") ?? false))
            {
                var requestBody = operation.RequestBody?.Content["application/json"];
                if (requestBody != null)
                {
                    requestBody.Example = new OpenApiObject
                    {
                        ["relatedCaseId"] = new OpenApiInteger(0),
                        ["values"]        = new OpenApiObject
                        {
                            ["title"]       = new OpenApiString("string"),
                            ["description"] = new OpenApiString("string"),
                            ["status"]      = new OpenApiString("string"),
                            ["private"]     = new OpenApiBoolean(false)
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
        var result = await _service.EditAsync(parameters, contextualizer);
    
        if (result.IsFinished)
            return result.HandleActionResult(this);
    
        return NoContent();
    }


    [HttpPut("assign-lawyer"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Put(
        [FromBody] AssignLawyerParametersDto parameters,
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

        var result = await _service.AssignLawyerAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }

    [HttpPut("assign-customer"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Put(
        [FromBody] AssignCustomerParametersDto parameters,
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

        var result = await _service.AssignCustomerAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }
}
