using System.Collections;
using System.Globalization;
using System.Linq.Expressions;

namespace LawyerCustomerApp.External.Validation;

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public IList<ValidationFailure> Errors { get; }
    public ValidationResult(IEnumerable<ValidationFailure> failures) => Errors = failures.ToList();
}

public class ValidationFailure
{
    public string PropertyName { get; }
    public string ErrorMessage { get; }
    public object? CustomState { get; }

    public ValidationFailure(string propertyName, string errorMessage, object? customState = null)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        CustomState  = customState;
    }
}

public class ValidationContext<T>
{
    public T Instance { get; }
    public ValidationContext(T instance) => Instance = instance;
}

public interface IValidator<T>
{
    ValidationResult Validate(T instance);
}

public abstract class AbstractValidator<T> : IValidator<T>
{
    private readonly List<IValidationRule<T>> _rules = new();

    protected IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var rule = new PropertyRule<T, TProperty>(expression);
        _rules.Add(rule);
        return new RuleBuilder<T, TProperty>(this, rule);
    }

    protected IRuleBuilderForEach<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression)
        => new RuleBuilderForEach<T, TElement>(this, expression);

    protected IRuleBuilderForEach<T, TElement> ForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression)
        => RuleForEach(expression);

    internal void AddRule(IValidationRule<T> rule) => _rules.Add(rule);

    public ValidationResult Validate(T instance)
    {
        var context = new ValidationContext<T>(instance);
        var failures = _rules.SelectMany(r => r.Validate(context)).ToList();
        return new ValidationResult(failures);
    }
}

internal interface IValidationRule<T>
{
    IEnumerable<ValidationFailure> Validate(ValidationContext<T> context);
}

internal class PropertyRule<T, TProperty> : IValidationRule<T>
{
    public string PropertyName { get; }
    internal Func<T, TProperty> PropertyFunc { get; }
    private readonly List<IPropertyValidator<T, TProperty>> _validators = new();

    // Conditional execution
    public Func<T, bool>? Condition { get; set; }
    public Func<T, TProperty, bool>? ConditionWithValue { get; set; }

    public PropertyRule(Expression<Func<T, TProperty>> expression)
    {
        var member = (MemberExpression)expression.Body;
        PropertyName = member.Member.Name;
        PropertyFunc = expression.Compile();
    }

    public void AddValidator(IPropertyValidator<T, TProperty> validator) => _validators.Add(validator);

    public IEnumerable<ValidationFailure> Validate(ValidationContext<T> context)
    {
        // instance-only condition
        if (Condition != null && !Condition(context.Instance))
            yield break;

        var value = PropertyFunc(context.Instance);

        // instance+value condition
        if (ConditionWithValue != null && !ConditionWithValue(context.Instance, value))
            yield break;

        foreach (var validator in _validators)
        {
            if (!validator.IsValid(value))
            {
                var msg   = validator.ErrorMessageTemplate.Replace("{PropertyName}", PropertyName);
                var state = validator.CustomStateProvider?.Invoke(context.Instance, value);
                yield return new ValidationFailure(PropertyName, msg, state);
            }
        }
    }
}

internal interface IPropertyValidator<T, TProperty>
{
    bool IsValid(TProperty value);
    string ErrorMessageTemplate { get; set; }
    Func<T, TProperty, object?>? CustomStateProvider { get; set; }
}

internal class NotNullValidator<T, TProperty> : IPropertyValidator<T, TProperty>
{
    public string ErrorMessageTemplate { get; set; } = "{PropertyName} must not be null.";
    public Func<T, TProperty, object?>? CustomStateProvider { get; set; }
    public bool IsValid(TProperty value) => value != null;
}

internal class NotEmptyValidator<T, TProperty> : IPropertyValidator<T, TProperty>
{
    public string ErrorMessageTemplate { get; set; } = "{PropertyName} must not be empty.";
    public Func<T, TProperty, object?>? CustomStateProvider { get; set; }
    public bool IsValid(TProperty value)
    {
        if (value == null) return false;
        return value switch
        {
            string s => !string.IsNullOrEmpty(s),
            IEnumerable e => e.Cast<object>().Any(),
            _ => true
        };
    }
}

public interface IRuleBuilderInitial<T, TProperty> { }
public interface IRuleBuilder<T, TProperty> : IRuleBuilderInitial<T, TProperty>
{
    IRuleBuilder<T, TProperty> NotNull();
    IRuleBuilder<T, TProperty> NotEmpty();
    IRuleBuilder<T, TProperty> WithMessage(string message);
    IRuleBuilder<T, TProperty> WithState(Func<T, TProperty, object> stateProvider);
    IRuleBuilder<T, TProperty> WithState(object state);
    IRuleBuilder<T, TProperty> SetValidator(IValidator<TProperty> validator);
    IRuleBuilder<T, TProperty> When(Func<T, bool> predicate);
    IRuleBuilder<T, TProperty> When(Func<T, TProperty, bool> predicate);
}

public class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty>
{
    private readonly AbstractValidator<T> _parent;
    private readonly PropertyRule<T, TProperty> _rule;
    private IPropertyValidator<T, TProperty>? _lastValidator;

    internal RuleBuilder(AbstractValidator<T> parent, PropertyRule<T, TProperty> rule)
    {
        _parent = parent;
        _rule = rule;
    }

    internal void AddValidator(IPropertyValidator<T, TProperty> validator)
    {
        _rule.AddValidator(validator);
        _lastValidator = validator;
    }

    public IRuleBuilder<T, TProperty> NotNull()
    {
        var v = new NotNullValidator<T, TProperty>();
        AddValidator(v);
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEmpty()
    {
        var v = new NotEmptyValidator<T, TProperty>();
        AddValidator(v);
        return this;
    }



    public IRuleBuilder<T, TProperty> WithMessage(string message)
    {
        if (_lastValidator == null)
            throw new InvalidOperationException("No validator available to set a message on.");
        _lastValidator.ErrorMessageTemplate = message;
        return this;
    }

    public IRuleBuilder<T, TProperty> WithState(Func<T, TProperty, object> stateProvider)
    {
        if (_lastValidator == null)
            throw new InvalidOperationException("No validator available to set state on.");
        _lastValidator.CustomStateProvider = stateProvider;
        return this;
    }

    public IRuleBuilder<T, TProperty> WithState(object state)
        => WithState((instance, val) => state);

    public IRuleBuilder<T, TProperty> SetValidator(IValidator<TProperty> validator)
    {
        var adaptor = new ChildValidatorAdaptorRule<T, TProperty>(_rule.PropertyName, _rule.PropertyFunc, validator);
        _parent.AddRule(adaptor);
        return this;
    }

    public IRuleBuilder<T, TProperty> When(Func<T, bool> predicate)
    {
        _rule.Condition = predicate;
        return this;
    }

    public IRuleBuilder<T, TProperty> When(Func<T, TProperty, bool> predicate)
    {
        _rule.ConditionWithValue = predicate;
        return this;
    }
}

internal class ChildValidatorAdaptorRule<T, TProperty> : IValidationRule<T>
{
    private readonly string _propertyName;
    private readonly Func<T, TProperty> _propertyFunc;
    private readonly IValidator<TProperty> _validator;

    public ChildValidatorAdaptorRule(string propertyName, Func<T, TProperty> propertyFunc, IValidator<TProperty> validator)
    {
        _propertyName = propertyName;
        _propertyFunc = propertyFunc;
        _validator = validator;
    }

    public IEnumerable<ValidationFailure> Validate(ValidationContext<T> context)
    {
        var value = _propertyFunc(context.Instance);
        if (value == null) yield break;
        var result = _validator.Validate(value);
        foreach (var failure in result.Errors)
        {
            var name = $"{_propertyName}.{failure.PropertyName}";
            yield return new ValidationFailure(name, failure.ErrorMessage, failure.CustomState);
        }
    }
}

public interface IRuleBuilderForEach<T, TElement>
{
    IRuleBuilderForEach<T, TElement> WithMessage(string message);
    IRuleBuilderForEach<T, TElement> WithState(Func<T, TElement, object> stateProvider);
    IRuleBuilderForEach<T, TElement> WithState(object state);
    void SetValidator(IValidator<TElement> validator);
}

public class RuleBuilderForEach<T, TElement> : IRuleBuilderForEach<T, TElement>
{
    private readonly AbstractValidator<T> _parent;
    private readonly string _propertyName;
    private readonly Func<T, IEnumerable<TElement>> _propertyFunc;
    private string _messageTemplate = "{PropertyName}[{CollectionIndex}] validation failed.";
    private Func<T, TElement, object?>? _stateProvider;

    internal RuleBuilderForEach(AbstractValidator<T> parent, Expression<Func<T, IEnumerable<TElement>>> expression)
    {
        _parent = parent;
        var member = (MemberExpression)expression.Body;
        _propertyName = member.Member.Name;
        _propertyFunc = expression.Compile();
    }

    public IRuleBuilderForEach<T, TElement> WithMessage(string message)
    {
        _messageTemplate = message;
        return this;
    }

    public IRuleBuilderForEach<T, TElement> WithState(Func<T, TElement, object?> stateProvider)
    {
        _stateProvider = stateProvider;
        return this;
    }

    public IRuleBuilderForEach<T, TElement> WithState(object? state)
        => WithState((instance, elem) => state);

    public void SetValidator(IValidator<TElement> validator)
    {
        var rule = new ForEachRule<T, TElement>(_propertyName, _propertyFunc, validator, _messageTemplate, _stateProvider);
        _parent.AddRule(rule);
    }
}

internal class ForEachRule<T, TElement> : IValidationRule<T>
{
    private readonly string _propertyName;
    private readonly Func<T, IEnumerable<TElement>> _propertyFunc;
    private readonly IValidator<TElement> _validator;
    private readonly string _messageTemplate;
    private readonly Func<T, TElement, object?>? _stateProvider;

    public ForEachRule(
        string propertyName,
        Func<T, IEnumerable<TElement>> propertyFunc,
        IValidator<TElement> validator,
        string messageTemplate,
        Func<T, TElement, object?>? stateProvider)
    {
        _propertyName = propertyName;
        _propertyFunc = propertyFunc;
        _validator = validator;
        _messageTemplate = messageTemplate;
        _stateProvider = stateProvider;
    }

    public IEnumerable<ValidationFailure> Validate(ValidationContext<T> context)
    {
        var collection = _propertyFunc(context.Instance);
        if (collection == null) yield break;
        int index = 0;
        foreach (var element in collection)
        {
            var result = _validator.Validate(element);
            foreach (var failure in result.Errors)
            {
                var propName = $"{_propertyName}[{index}].{failure.PropertyName}";
                var msg = failure.ErrorMessage ?? _messageTemplate
                    .Replace("{PropertyName}", propName)
                    .Replace("{CollectionIndex}", index.ToString());
                var state = failure.CustomState ?? _stateProvider?.Invoke(context.Instance, element);
                yield return new ValidationFailure(propName, msg, state);
            }
            index++;
        }
    }
}

internal enum ComparisonOperator { Equal, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual }

internal class NullableComparisonValidator<T, TProperty>
        : IPropertyValidator<T, TProperty?>
        where TProperty : struct, IComparable<TProperty>
{
    private readonly ComparisonOperator _operator;
    private readonly TProperty _compareTo;

    public string ErrorMessageTemplate { get; set; }
    public Func<T, TProperty?, object?>? CustomStateProvider { get; set; }

    public NullableComparisonValidator(ComparisonOperator op, TProperty compareTo)
    {
        _operator  = op;
        _compareTo = compareTo;
        ErrorMessageTemplate = "{PropertyName} comparison failed.";
    }

    public bool IsValid(TProperty? value)
    {
        if (!value.HasValue)
            return false;

        var cmp = value.Value.CompareTo(_compareTo);
        return _operator switch
        {
            ComparisonOperator.Equal              => cmp == 0,
            ComparisonOperator.GreaterThan        => cmp >  0,
            ComparisonOperator.GreaterThanOrEqual => cmp >= 0,
            ComparisonOperator.LessThan           => cmp <  0,
            ComparisonOperator.LessThanOrEqual    => cmp <= 0,
            _ => throw new InvalidOperationException(),
        };
    }
}

internal class ComparisonValidator<T, TProperty> : IPropertyValidator<T, TProperty>
    where TProperty : IComparable<TProperty>
{
    private readonly ComparisonOperator _operator;
    private readonly TProperty _compareTo;
    public string ErrorMessageTemplate { get; set; }
    public Func<T, TProperty, object?>? CustomStateProvider { get; set; }

    public ComparisonValidator(ComparisonOperator op, TProperty compareTo)
    {
        _operator = op;
        _compareTo = compareTo;
        ErrorMessageTemplate = "{PropertyName} comparison failed.";
    }

    public bool IsValid(TProperty value)
    {
        if (value == null) return false;
        var cmp = value.CompareTo(_compareTo);
        return _operator switch
        {
            ComparisonOperator.Equal              => cmp == 0,
            ComparisonOperator.GreaterThan        => cmp > 0,
            ComparisonOperator.GreaterThanOrEqual => cmp >= 0,
            ComparisonOperator.LessThan           => cmp < 0,
            ComparisonOperator.LessThanOrEqual    => cmp <= 0,
            _ => throw new InvalidOperationException(),
        };
    }
}

public static class RuleBuilderExtensions
{
    public static IRuleBuilder<T, TProperty> EqualTo<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty> rb)
        {
            var v = new ComparisonValidator<T, TProperty>(ComparisonOperator.Equal, value);
            rb.AddValidator(v);
            return builder;
        }
        throw new InvalidOperationException("Invalid builder");
    }

    // [GreaterThan]

    public static IRuleBuilder<T, TProperty> GreaterThan<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty> rb)
            rb.AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThan, value));
        return builder;
    }

    public static IRuleBuilder<T, TProperty?> GreaterThan<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder, TProperty value)
        where TProperty : struct, IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty?> rb)
            rb.AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThan, value));
        return builder;
    }

    // [GreaterThanOrEqualTo]

    public static IRuleBuilder<T, TProperty> GreaterThanOrEqualTo<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty> rb)
            rb.AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThanOrEqual, value));
        return builder;
    }

    public static IRuleBuilder<T, TProperty?> GreaterThanOrEqualTo<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder, TProperty value)
        where TProperty : struct, IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty?> rb)
            rb.AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThanOrEqual, value));
        return builder;
    }

    // [LessThan]

    public static IRuleBuilder<T, TProperty> LessThan<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty> rb)
            rb.AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.LessThan, value));
        return builder;
    }

    public static IRuleBuilder<T, TProperty?> LessThan<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder, TProperty value)
        where TProperty : struct, IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty?> rb)
            rb.AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.LessThan, value));
        return builder;
    }

    // [LessThanOrEqual]

    public static IRuleBuilder<T, TProperty> LessThanOrEqual<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty> rb)
            rb.AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.LessThanOrEqual, value));
        return builder;
    }

    public static IRuleBuilder<T, TProperty?> LessThanOrEqual<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder, TProperty value)
        where TProperty : struct, IComparable<TProperty>
    {
        if (builder is RuleBuilder<T, TProperty?> rb)
            rb.AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.LessThanOrEqual, value));
        return builder;
    }

    // [Length]

    public static IRuleBuilder<T, string?> Length<T>(
        this IRuleBuilder<T, string?> builder, int min, int max)
    {
        if (builder is RuleBuilder<T, string?> rb)
            rb.AddValidator(new LengthValidator<T>(min, max));
        return builder;
    }

    // [MaxLenght]

    public static IRuleBuilder<T, string?> MaxLenght<T>(
        this IRuleBuilder<T, string?> builder, int max)
    {
        if (builder is RuleBuilder<T, string?> rb)
            rb.AddValidator(new MaxLengthValidator<T>(max));
        return builder;
    }

    // [MinLenght]

    public static IRuleBuilder<T, string?> MinLenght<T>(
        this IRuleBuilder<T, string?> builder, int min)
    {
        if (builder is RuleBuilder<T, string?> rb)
            rb.AddValidator(new MinLengthValidator<T>(min));
        return builder;
    }

    // [PrecisionAndScale]

    public static IRuleBuilder<T, decimal?> PrecisionAndScale<T>(
        this IRuleBuilder<T, decimal?> builder, int precision, int scale)
    {
        if (builder is RuleBuilder<T, decimal?> rb)
        {
            var v = new PrecisionAndScaleValidator<T>(precision, scale);
            rb.AddValidator(v);
            return builder;
        }
        throw new InvalidOperationException("Invalid builder");
    }
}

internal class LengthValidator<T> : IPropertyValidator<T, string?>
{
    private readonly int _min, _max;
    public string ErrorMessageTemplate { get; set; }
    public Func<T, string?, object?>? CustomStateProvider { get; set; }

    public LengthValidator(int min, int max)
    {
        _min = min; _max = max;
        ErrorMessageTemplate = $"{{PropertyName}} must be between {_min} and {_max} characters long.";
    }

    public bool IsValid(string? value)
        => !string.IsNullOrEmpty(value) && value.Length >= _min && value.Length <= _max;
}

internal class MaxLengthValidator<T> : IPropertyValidator<T, string?>
{
    private readonly int _max;
    public string ErrorMessageTemplate { get; set; }
    public Func<T, string?, object?>? CustomStateProvider { get; set; }

    public MaxLengthValidator(int max)
    {
        _max = max;
        ErrorMessageTemplate = $"{{PropertyName}} must be {_max} max characters long.";
    }

    public bool IsValid(string? value)
        => !string.IsNullOrEmpty(value) && value.Length <= _max;
}

internal class MinLengthValidator<T> : IPropertyValidator<T, string?>
{
    private readonly int _min;
    public string ErrorMessageTemplate { get; set; }
    public Func<T, string?, object?>? CustomStateProvider { get; set; }

    public MinLengthValidator(int min)
    {
        _min = min;
        ErrorMessageTemplate = $"{{PropertyName}} must be {_min} min characters long.";
    }

    public bool IsValid(string? value)
        => !string.IsNullOrEmpty(value) && value.Length >= _min;
}

internal class PrecisionAndScaleValidator<T> : IPropertyValidator<T, decimal?>
{
    private readonly int _precision, _scale;
    public string ErrorMessageTemplate { get; set; }
    public Func<T, decimal?, object?>? CustomStateProvider { get; set; }

    public PrecisionAndScaleValidator(int precision, int scale)
    {
        _precision = precision; _scale = scale;
        ErrorMessageTemplate = $"{{PropertyName}} must have precision {_precision} and scale {_scale}.";
    }

    public bool IsValid(decimal? value)
    {
        if (value == null) return false;

        var s = value.Value.ToString(CultureInfo.InvariantCulture);

        var parts      = s.Split('.');
        var intDigits  = parts[0].TrimStart('-').Length;
        var fracDigits = parts.Length > 1 ? parts[1].Length : 0;
        return intDigits + fracDigits <= _precision && fracDigits <= _scale;
    }
}
