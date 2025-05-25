using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Permission.Common.Models;
using LawyerCustomerApp.Domain.Permission.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawyerCustomerApp.Application.Permission.Controllers;

[Tags("permission")]
[Route("api/permission")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IService _service;
    public Controller(IService service)
    {
        _service = service;
    }

    #region Case

    [HttpPost("case/enlist-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<EnlistedPermissionsFromCaseInformationDto>> Post(
        [FromBody] EnlistPermissionsFromCaseParametersDto parameters,
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

            return resultContructor.Build<EnlistedPermissionsFromCaseInformationDto>().HandleActionResult(this);
        }
        var result = await _service.EnlistPermissionsFromCaseAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("case/global-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<GlobalPermissionsRelatedWithCaseInformationDto>> Post(
        [FromBody] GlobalPermissionsRelatedWithCaseParametersDto parameters,
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

            return resultContructor.Build<GlobalPermissionsRelatedWithCaseInformationDto>().HandleActionResult(this);
        }
        var result = await _service.GlobalPermissionsRelatedWithCaseAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("case/permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<PermissionsRelatedWithCaseInformationDto>> Post(
        [FromBody] PermissionsRelatedWithCaseParametersDto parameters,
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

            return resultContructor.Build<PermissionsRelatedWithCaseInformationDto>().HandleActionResult(this);
        }
        var result = await _service.PermissionsRelatedWithCaseAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("case/grant-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Post(
        [FromBody] GrantPermissionsToCaseParametersDto parameters,
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

        var result = await _service.GrantPermissionsToCaseAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }

    [HttpPost("case/revoke-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Post(
        [FromBody] RevokePermissionsToCaseParametersDto parameters,
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

        var result = await _service.RevokePermissionsToCaseAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }

    #endregion

    #region User

    [HttpPost("user/enlist-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<EnlistedPermissionsFromUserInformationDto>> Post(
        [FromBody] EnlistPermissionsFromUserParametersDto parameters,
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

            return resultContructor.Build<EnlistedPermissionsFromUserInformationDto>().HandleActionResult(this);
        }
        var result = await _service.EnlistPermissionsFromUserAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("user/global-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<GlobalPermissionsRelatedWithUserInformationDto>> Post(
        [FromBody] GlobalPermissionsRelatedWithUserParametersDto parameters,
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

            return resultContructor.Build<GlobalPermissionsRelatedWithUserInformationDto>().HandleActionResult(this);
        }
        var result = await _service.GlobalPermissionsRelatedWithUserAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("user/permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<PermissionsRelatedWithUserInformationDto>> Post(
        [FromBody] PermissionsRelatedWithUserParametersDto parameters,
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

            return resultContructor.Build<PermissionsRelatedWithUserInformationDto>().HandleActionResult(this);
        }
        var result = await _service.PermissionsRelatedWithUserAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("user/grant-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Post(
        [FromBody] GrantPermissionsToUserParametersDto parameters,
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

        var result = await _service.GrantPermissionsToUserAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }

    [HttpPost("user/revoke-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult> Post(
        [FromBody] RevokePermissionsToUserParametersDto parameters,
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

        var result = await _service.RevokePermissionsToUserAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return NoContent();
    }

    #endregion

    [HttpPost("search/enable-users-to-grant-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<SearchEnabledUsersToGrantPermissionsInformationDto>> Post(
        [FromBody] SearchEnabledUsersToGrantPermissionsParametersDto parameters,
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

        var result = await _service.SearchEnabledUsersToGrantPermissionsAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

    [HttpPost("search/enable-users-to-revoke-permissions"), Authorize(Policy = "internal-jwt-bearer")]
    public async Task<ActionResult<SearchEnabledUsersToRevokePermissionsInformationDto>> Post(
        [FromBody] SearchEnabledUsersToRevokePermissionsParametersDto parameters,
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

        var result = await _service.SearchEnabledUsersToRevokePermissionsAsync(parameters, contextualizer);

        if (result.IsFinished)
            return result.HandleActionResult(this);

        return result.Value;
    }

}
