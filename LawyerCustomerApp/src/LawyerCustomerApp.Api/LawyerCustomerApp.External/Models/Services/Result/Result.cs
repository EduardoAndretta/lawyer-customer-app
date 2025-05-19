using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Base = LawyerCustomerApp.External.Responses.Common.Models;

using Error   = LawyerCustomerApp.External.Responses.Error.Models;
using Success = LawyerCustomerApp.External.Responses.Success.Models;
using Warning = LawyerCustomerApp.External.Responses.Warning.Models;

namespace LawyerCustomerApp.External.Models;

public class ResultConstructor
{
    private List<Warning.Constructor>? _warnings;
    private Base.Constructor? _contructor;

    public ResultConstructor() { }

    public void AddWarning(Warning.Constructor constructor)
    {
        if (_warnings == null)
            _warnings = new List<Warning.Constructor>();

        _warnings.Add(constructor);
    }

    public void SetConstructor(Success.Constructor constructor)
    {
        _contructor = constructor;
    }

    public void SetConstructor(Error.Constructor constructor)
    {
        _contructor = constructor;
    }

    public ResultNullable<TValue> BuildNullable<TValue>(TValue? value)
    {
        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    return new ResultNullable<TValue>(successConstructor, _warnings);
                return new ResultNullable<TValue>(successConstructor);

            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    return new ResultNullable<TValue>(errorConstructor, _warnings);
                return new ResultNullable<TValue>(errorConstructor);
            }
        }
        return new ResultNullable<TValue>(value);
    }

    public ResultNullable<TValue> BuildNullable<TValue>()
    {
        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    return new ResultNullable<TValue>(successConstructor, _warnings);
                return new ResultNullable<TValue>(successConstructor);

            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    return new ResultNullable<TValue>(errorConstructor, _warnings);
                return new ResultNullable<TValue>(errorConstructor);
            }
        }
        return new ResultNullable<TValue>();
    }

    public Result<TValue> Build<TValue>(TValue value)
    {
        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    return new Result<TValue>(successConstructor, _warnings);
                return new Result<TValue>(successConstructor);

            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    return new Result<TValue>(errorConstructor, _warnings);
                return new Result<TValue>(errorConstructor);
            }
        }
        return new Result<TValue>(value);
    }

    public Result<TValue> Build<TValue>()
    {
        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    return new Result<TValue>(successConstructor, _warnings);
                return new Result<TValue>(successConstructor);

            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    return new Result<TValue>(errorConstructor, _warnings);
                return new Result<TValue>(errorConstructor);
            }
        }
        return new Result<TValue>();
    }

    public Result Build()
    {
        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    return new Result(successConstructor, _warnings);
                return new Result(successConstructor);

            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    return new Result(errorConstructor, _warnings);
                return new Result(errorConstructor);
            }
        }
        return new Result();
    }
}

public class ConstructorSpecification
{
    private IReadOnlyList<Warning.Constructor>? _warnings;
    private Base.Constructor?                   _contructor;

    public ConstructorSpecification()
    {
    }

    public ConstructorSpecification(Success.Constructor contructor)
    {
        _contructor = contructor;
    }

    public ConstructorSpecification(Error.Constructor contructor)
    {
        _contructor = contructor;
    }

    public ConstructorSpecification(Success.Constructor contructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _contructor = contructor;
        _warnings   = warnings;
    }

    public ConstructorSpecification(Error.Constructor contructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _contructor = contructor;
        _warnings   = warnings;
    }

    public bool IsEmpty => ValuesExtensions.GetValue(() =>
    {
        if (_contructor == null)
            return true;

        if (_contructor is not Success.Constructor _ and not Error.Constructor _)
            return true;

        return false;
    });

    public bool IsSuccess => ValuesExtensions.GetValue(() =>
    {
        if (_contructor == null)
            return false;

        if (_contructor is Success.Constructor _)
            return true;

        return false;
    });

    public bool IsError => ValuesExtensions.GetValue(() =>
    {
        if (_contructor == null)
            return false;

        if (_contructor is Error.Constructor _)
            return true;

        return false;
    });

    public bool TryGetContructor(out Base.Constructor? constructor)
    {
        constructor = default;

        if (_contructor == null)
            return false;

        switch (_contructor)
        {
            case Success.Constructor parsedConstructor:
                constructor = parsedConstructor;
                return true;

            case Error.Constructor parsedConstructor:
                constructor = parsedConstructor;
                return true;

            default:
                return false;
        }
    }

    public bool TryGetResponse(IServiceProvider serviceProvider, out Base.Response? response)
    {
        response = default;

        if (IsEmpty)
            return false;

        if (!TryGetContructor(out var constructor))
            return false;

        if (_warnings != null && _warnings.Any())
        {
            var warningGeneratorService = serviceProvider.GetRequiredService<IWarningGeneratorService>();

            foreach (var warning in _warnings)
                warningGeneratorService.CreateWarning(warning);
        }

        if (IsSuccess)
        {
            var successGeneratorService = serviceProvider.GetRequiredService<ISuccessGeneratorService>();

            if (constructor is not Success.Constructor parsedConstructor)
                return false;

            response = successGeneratorService.CreateSuccess(parsedConstructor);

            return true;
        }

        if (IsError)
        {
            var errorGeneratorService = serviceProvider.GetRequiredService<IErrorGeneratorService>();

            if (constructor is not Error.Constructor parsedConstructor)
                return false;

            response = errorGeneratorService.CreateError(parsedConstructor);

            return true;
        }

        return false;
    }
}

public class Result<TValue>
{
    private bool _isFinished;
    private TValue? _value;

    private IReadOnlyList<Warning.Constructor>? _warnings;
    private Base.Constructor?                   _contructor;

    public Result()
    {
        _isFinished = true;
    }

    public Result(TValue value)
    {
        _isFinished = false;
        _value = value;
    }

    public Result(Success.Constructor constructor)
    {
        _isFinished = true;
        _contructor = constructor;
    }

    public Result(Error.Constructor constructor)
    {
        _isFinished = true;
        _contructor = constructor;
    }

    public Result(Success.Constructor constructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _isFinished = true;
        _contructor = constructor;
        _warnings   = warnings;
    }

    public Result(Error.Constructor constructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _isFinished = true;
        _contructor = constructor;
        _warnings   = warnings;
    }

    public Result<TValue> Incorporate<TOutsideValue>(Result<TOutsideValue> result)
    {
        _isFinished = result.IsFinished;

        _warnings = ValuesExtensions.GetValue(() =>
        {
            if (_warnings == null || !_warnings.Any())
                return result.Warnings;

            if (_warnings.Any() && (result.Warnings != null && result.Warnings.Any()))
            {
                List<Warning.Constructor> warnings = new List<Warning.Constructor>();

                foreach (Warning.Constructor warning in result.Warnings.Concat(_warnings))
                    warnings.Add(warning);

                return warnings;
            }
            return _warnings;
        });

        _contructor = result.Constructor;

        return this;
    }

    public Result<TValue> Incorporate(Result result)
    {
        _isFinished = result.IsFinished;

        _warnings = ValuesExtensions.GetValue(() =>
        {
            if (_warnings == null || !_warnings.Any())
                return result.Warnings;

            if (_warnings.Any() && (result.Warnings != null && result.Warnings.Any()))
            {
                List<Warning.Constructor> warnings = new List<Warning.Constructor>();

                foreach (Warning.Constructor warning in result.Warnings.Concat(_warnings))
                    warnings.Add(warning);

                return warnings;
            }
            return _warnings;
        });

        _contructor = result.Constructor;

        return this;
    }

    private bool TryGetConstructorSpecification(out ConstructorSpecification constructorSpecification)
    {
        constructorSpecification = new ConstructorSpecification();

        if (!_isFinished)
            return false;

        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    constructorSpecification = new ConstructorSpecification(successConstructor, _warnings);
                else
                    constructorSpecification = new ConstructorSpecification(successConstructor);

                return true;
            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    constructorSpecification = new ConstructorSpecification(errorConstructor, _warnings);
                else
                    constructorSpecification = new ConstructorSpecification(errorConstructor);
                
                return true;
            }
        }

        return true;
    }

    public Base.Response BuildResponse(IServiceProvider serviceProvider)
    {
        if (!TryGetConstructorSpecification(out var constructorSpecification))
        {
            var errorGeneratorService = serviceProvider.GetRequiredService<IErrorGeneratorService>();

            return errorGeneratorService.CreateError(new Exception("Failed to create the response. ConstructorSpecification not found."));
        }

        if (!constructorSpecification.TryGetResponse(serviceProvider, out var response))
        {
            var errorGeneratorService = serviceProvider.GetRequiredService<IErrorGeneratorService>();

            return errorGeneratorService.CreateError(new Exception("Failed to create the response. Response not found."));
        }

        return response!;
    }

    public ActionResult HandleActionResult(ControllerBase controller)
    {
        if (!TryGetConstructorSpecification(out var constructorSpecification))
            return controller.StatusCode(500, new());

        if (!constructorSpecification.TryGetResponse(controller.HttpContext.RequestServices, out var response))
            return controller.StatusCode(500, new());

        if (!constructorSpecification.TryGetContructor(out var contructor))
            return controller.StatusCode(500, new());

        if (constructorSpecification.IsSuccess)
        {
            if (contructor is Success.Constructor parsedContructor)
                return controller.StatusCode(parsedContructor.Status, response);
        }

        if (constructorSpecification.IsError)
        {
            if (contructor is Error.Constructor parsedContructor)
                return controller.StatusCode(parsedContructor.Status, response);
        }

        return controller.StatusCode(500, new());
    }

    public TValue Value => ValuesExtensions.GetValue(() =>
    {
        if (_isFinished)
            throw new Exception("This result is finished. The WorkFlow ended");

        if (_value == null)
            throw new Exception("Value null. Unexpected error in result object.");

        return _value!;
    });

    public IReadOnlyList<Warning.Constructor>? Warnings => _warnings;
    public Base.Constructor? Constructor                => _contructor;

    public bool IsFinished => _isFinished;
}

public class ResultNullable<TValue>
{
    private bool _isFinished;
    private TValue? _value;

    private IReadOnlyList<Warning.Constructor>? _warnings;
    private Base.Constructor?                   _contructor;

    public ResultNullable()
    {
        _isFinished = true;
    }

    public ResultNullable(TValue? value)
    {
        _isFinished = false;
        _value      = value;
    }

    public ResultNullable(Success.Constructor constructor)
    {
        _isFinished = true;
        _contructor = constructor;
    }

    public ResultNullable(Error.Constructor constructor)
    {
        _isFinished = true;
        _contructor = constructor;
    }

    public ResultNullable(Success.Constructor constructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _isFinished = true;
        _contructor = constructor;
        _warnings   = warnings;
    }

    public ResultNullable(Error.Constructor constructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _isFinished = true;
        _contructor = constructor;
        _warnings   = warnings;
    }

    public ResultNullable<TValue> Incorporate<TOutsideValue>(Result<TOutsideValue> result)
    {
        _isFinished = result.IsFinished;

        _warnings = ValuesExtensions.GetValue(() =>
        {
            if (_warnings == null || !_warnings.Any())
                return result.Warnings;

            if (_warnings.Any() && (result.Warnings != null && result.Warnings.Any()))
            {
                List<Warning.Constructor> warnings = new List<Warning.Constructor>();

                foreach (Warning.Constructor warning in result.Warnings.Concat(_warnings))
                    warnings.Add(warning);

                return warnings;
            }
            return _warnings;
        });

        _contructor = result.Constructor;

        return this;
    }

    public ResultNullable<TValue> Incorporate(Result result)
    {
        _isFinished = result.IsFinished;

        _warnings = ValuesExtensions.GetValue(() =>
        {
            if (_warnings == null || !_warnings.Any())
                return result.Warnings;

            if (_warnings.Any() && (result.Warnings != null && result.Warnings.Any()))
            {
                List<Warning.Constructor> warnings = new List<Warning.Constructor>();

                foreach (Warning.Constructor warning in result.Warnings.Concat(_warnings))
                    warnings.Add(warning);

                return warnings;
            }
            return _warnings;
        });

        _contructor = result.Constructor;

        return this;
    }

    public bool TryGetConstructorSpecification(out ConstructorSpecification constructorSpecification)
    {
        constructorSpecification = new ConstructorSpecification();

        if (!_isFinished)
            return false;

        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    constructorSpecification = new ConstructorSpecification(successConstructor, _warnings);
                else
                    constructorSpecification = new ConstructorSpecification(successConstructor);

                return true;
            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    constructorSpecification = new ConstructorSpecification(errorConstructor, _warnings);
                else
                    constructorSpecification = new ConstructorSpecification(errorConstructor);

                return true;
            }
        }

        return true;
    }

    public Base.Response BuildResponse(IServiceProvider serviceProvider)
    {
        if (!TryGetConstructorSpecification(out var constructorSpecification))
        {
            var errorGeneratorService = serviceProvider.GetRequiredService<IErrorGeneratorService>();

            return errorGeneratorService.CreateError(new Exception("Failed to create the response. ConstructorSpecification not found."));
        }

        if (!constructorSpecification.TryGetResponse(serviceProvider, out var response))
        {
            var errorGeneratorService = serviceProvider.GetRequiredService<IErrorGeneratorService>();

            return errorGeneratorService.CreateError(new Exception("Failed to create the response. Response not found."));
        }

        return response!;
    }

    public TValue? Value => ValuesExtensions.GetValue(() =>
    {
        if (_isFinished)
            throw new Exception("This result is finished. The WorkFlow ended");

        return _value;
    });

    public ActionResult HandleActionResult(ControllerBase controller)
    {
        if (!TryGetConstructorSpecification(out var constructorSpecification))
            return controller.StatusCode(500, new());

        if (!constructorSpecification.TryGetResponse(controller.HttpContext.RequestServices, out var response))
            return controller.StatusCode(500, new());

        if (!constructorSpecification.TryGetContructor(out var contructor))
            return controller.StatusCode(500, new());

        if (constructorSpecification.IsSuccess)
        {
            if (contructor is Success.Constructor parsedContructor)
                return controller.StatusCode(parsedContructor.Status, response);
        }

        if (constructorSpecification.IsError)
        {
            if (contructor is Error.Constructor parsedContructor)
                return controller.StatusCode(parsedContructor.Status, response);
        }

        return controller.StatusCode(500, new());
    }

    public IReadOnlyList<Warning.Constructor>? Warnings => _warnings;
    public Base.Constructor? Constructor                => _contructor;

    public bool IsFinished => _isFinished;
}

public class Result
{
    private bool _isFinished;

    private IReadOnlyList<Warning.Constructor>? _warnings;
    private Base.Constructor?                   _contructor;

    public Result()
    {
        _isFinished = false;
    }

    public Result(Success.Constructor constructor)
    {
        _isFinished = true;
        _contructor = constructor;
    }

    public Result(Error.Constructor constructor)
    {
        _isFinished = true;
        _contructor = constructor;
    }

    public Result(Success.Constructor constructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _isFinished = true;
        _contructor = constructor;
        _warnings   = warnings;
    }

    public Result(Error.Constructor constructor, IReadOnlyList<Warning.Constructor> warnings)
    {
        _isFinished = true;
        _contructor = constructor;
        _warnings   = warnings;
    }

    public Result Incorporate<TOutsideValue>(Result<TOutsideValue> result)
    {
        _isFinished = result.IsFinished;

        _warnings = ValuesExtensions.GetValue(() =>
        {
            if (_warnings == null || !_warnings.Any())
                return result.Warnings;

            if (_warnings.Any() && (result.Warnings != null && result.Warnings.Any()))
            {
                List<Warning.Constructor> warnings = new List<Warning.Constructor>();

                foreach (Warning.Constructor warning in result.Warnings.Concat(_warnings))
                    warnings.Add(warning);

                return warnings;
            }
            return _warnings;
        });

        _contructor = result.Constructor;

        return this;
    }

    public Result Incorporate(Result result)
    {
        _isFinished = result.IsFinished;

        _warnings = ValuesExtensions.GetValue(() =>
        {
            if (_warnings == null || !_warnings.Any())
                return result.Warnings;

            if (_warnings.Any() && (result.Warnings != null && result.Warnings.Any()))
            {
                List<Warning.Constructor> warnings = new List<Warning.Constructor>();

                foreach (Warning.Constructor warning in result.Warnings.Concat(_warnings))
                    warnings.Add(warning);

                return warnings;
            }
            return _warnings;
        });

        _contructor = result.Constructor;

        return this;
    }

    public bool TryGetConstructorSpecification(out ConstructorSpecification constructorSpecification)
    {
        constructorSpecification = new ConstructorSpecification();

        if (!_isFinished)
            return false;

        if (_contructor != null)
        {
            if (_contructor is Success.Constructor successConstructor)
            {
                if (_warnings != null)
                    constructorSpecification = new ConstructorSpecification(successConstructor, _warnings);
                else
                    constructorSpecification = new ConstructorSpecification(successConstructor);

                return true;
            }

            if (_contructor is Error.Constructor errorConstructor)
            {
                if (_warnings != null)
                    constructorSpecification = new ConstructorSpecification(errorConstructor, _warnings);
                else
                    constructorSpecification = new ConstructorSpecification(errorConstructor);

                return true;
            }
        }

        return true;
    }

    public Base.Response BuildResponse(IServiceProvider serviceProvider)
    {
        if (!TryGetConstructorSpecification(out var constructorSpecification))
        {
            var errorGeneratorService = serviceProvider.GetRequiredService<IErrorGeneratorService>();

            return errorGeneratorService.CreateError(new Exception("Failed to create the response. ConstructorSpecification not found."));
        }

        if (!constructorSpecification.TryGetResponse(serviceProvider, out var response))
        {
            var errorGeneratorService = serviceProvider.GetRequiredService<IErrorGeneratorService>();

            return errorGeneratorService.CreateError(new Exception("Failed to create the response. Response not found."));
        }

        return response!;
    }

    public ActionResult HandleActionResult(ControllerBase controller)
    {
        if (!TryGetConstructorSpecification(out var constructorSpecification))
            return controller.StatusCode(500, new());

        if (!constructorSpecification.TryGetResponse(controller.HttpContext.RequestServices, out var response))
            return controller.StatusCode(500, new());

        if (!constructorSpecification.TryGetContructor(out var contructor))
            return controller.StatusCode(500, new());

        if (constructorSpecification.IsSuccess)
        {
            if (contructor is Success.Constructor parsedContructor)
                return controller.StatusCode(parsedContructor.Status, response);
        }

        if (constructorSpecification.IsError)
        {
            if (contructor is Error.Constructor parsedContructor)
                return controller.StatusCode(parsedContructor.Status, response);
        }

        return controller.StatusCode(500, new());
    }

    public IReadOnlyList<Warning.Constructor>? Warnings => _warnings;
    public Base.Constructor? Constructor                => _contructor;

    public bool IsFinished => _isFinished;
}