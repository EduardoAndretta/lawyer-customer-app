using System; // Added
using System.Collections;
using System.Collections.Generic; // Added
using System.Globalization;
using System.Linq; // Added
using System.Linq.Expressions;
using System.Threading; // Added
using System.Threading.Tasks; // Added

namespace LawyerCustomerApp.External.Validation
{
    // --- ValidationResult & ValidationFailure (Mostly Unchanged) ---
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public IList<ValidationFailure> Errors { get; }

        // Added constructor for empty results
        public ValidationResult()
        {
            Errors = new List<ValidationFailure>();
        }

        public ValidationResult(IEnumerable<ValidationFailure> failures)
        {
            Errors = failures?.ToList() ?? new List<ValidationFailure>();
        }
    }

    public class ValidationFailure
    {
        public string PropertyName { get; set; } // Made setter public for easier construction if needed
        public string ErrorMessage { get; set; } // Made setter public
        public object? CustomState { get; set; } // Made setter public

        public ValidationFailure(string propertyName, string errorMessage, object? customState = null)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
            CustomState = customState;
        }
    }

    // --- Modified ValidationContext ---
    public class ValidationContext<T>
    {
        private List<ValidationFailure> _failures = new List<ValidationFailure>();

        public T InstanceToValidate { get; }
        public IReadOnlyList<ValidationFailure> Failures => _failures;

        // Property being validated in the current rule execution
        public string PropertyName { get; internal set; } = string.Empty;

        // Cancellation Token for async operations
        public CancellationToken CancellationToken { get; internal set; }

        public ValidationContext(T instance, CancellationToken cancellationToken = default)
        {
            InstanceToValidate = instance;
            CancellationToken = cancellationToken;
        }

        public void AddFailure(ValidationFailure failure)
        {
            if (failure == null) throw new ArgumentNullException(nameof(failure));
            // Ensure the failure is associated with the correct property if not already set
            if (string.IsNullOrEmpty(failure.PropertyName))
            {
                failure.PropertyName = PropertyName;
            }
            _failures.Add(failure);
        }

        public void AddFailure(string propertyName, string errorMessage, object? customState = null)
        {
            AddFailure(new ValidationFailure(propertyName, errorMessage, customState));
        }

        // Convenience overload using the context's current PropertyName
        public void AddFailure(string errorMessage, object? customState = null)
        {
            AddFailure(new ValidationFailure(PropertyName, errorMessage, customState));
        }
    }

    // --- Modified IValidator and AbstractValidator ---
    public interface IValidator<T>
    {
        // Now async
        Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
        ValidationResult Validate(T instance); // Keep sync version for convenience? Or remove? Let's keep it for now.
    }

    public abstract class AbstractValidator<T> : IValidator<T>
    {
        // Changed List type to store the async version of rules
        private readonly List<IAsyncValidationRule<T>> _rules = new();

        protected IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            // PropertyRule now implements IAsyncValidationRule
            var rule = PropertyRule<T, TProperty>.Create(expression);
            _rules.Add(rule);
            return new RuleBuilder<T, TProperty>(this, rule);
        }

        // RuleForEach needs async adaptation too (simplified for now, focus on RuleFor)
        protected IRuleBuilderForEach<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression)
            => new RuleBuilderForEach<T, TElement>(this, expression);

        protected IRuleBuilderForEach<T, TElement> ForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression)
            => RuleForEach(expression);

        // Internal method to add rules (needs async interface)
        internal void AddRule(IAsyncValidationRule<T> rule) => _rules.Add(rule);

        // Main validation method is now async
        public virtual async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
        {
            var context = new ValidationContext<T>(instance, cancellationToken);
            foreach (var rule in _rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await rule.ValidateAsync(context, cancellationToken);
            }
            return new ValidationResult(context.Failures);
        }

        // Sync wrapper (can be removed if strict async is desired)
        public ValidationResult Validate(T instance)
        {
            // WARNING: Blocking async code like this can lead to deadlocks in some environments (e.g., ASP.NET Classic).
            // Prefer using the async API directly.
            return ValidateAsync(instance).GetAwaiter().GetResult();
        }
    }

    // --- New Async Interfaces ---
    internal interface IAsyncValidationRule<T>
    {
        Task ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken);
    }

    internal interface IAsyncPropertyValidator<T, TProperty>
    {
        // Now returns Task<bool>
        Task<bool> IsValidAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellationToken);
        string ErrorMessageTemplate { get; set; }
        // Custom state provider might need async context too if needed
        Func<T, TProperty, object?>? CustomStateProvider { get; set; }
    }


    // --- Modified PropertyRule ---
    internal class PropertyRule<T, TProperty> : IAsyncValidationRule<T> // Implements async interface
    {
        public string PropertyName { get; }
        internal Func<T, TProperty> PropertyFunc { get; }
        private readonly List<IAsyncPropertyValidator<T, TProperty>> _validators = new(); // Stores async validators

        // Custom validation actions
        private readonly List<Action<TProperty, ValidationContext<T>>> _syncCustomValidators = new();
        private readonly List<Func<TProperty, ValidationContext<T>, CancellationToken, Task>> _asyncCustomValidators = new();


        // Conditional execution (sync for now, could be async if needed)
        public Func<T, bool>? Condition { get; set; }
        public Func<T, TProperty, bool>? ConditionWithValue { get; set; }

        // Private constructor, use Create
        private PropertyRule(Expression<Func<T, TProperty>> expression)
        {
            var member = GetMemberExpression(expression);
            PropertyName = member.Member.Name;
            PropertyFunc = expression.Compile();
        }

        public static MemberExpression GetMemberExpression(Expression expression)
        {
            if (expression is not LambdaExpression lambda)
                throw new NotSupportedException($"Expression '{expression}' is not a LambdaExpression.");

            return lambda.Body switch
            {
                MemberExpression m => m,
                UnaryExpression u when u.Operand is MemberExpression m => m,
                _ => throw new NotSupportedException($"Expression '{expression}' is not a valid member expression.")
            };
        }

        // Factory method
        public static PropertyRule<T, TProperty> Create(Expression<Func<T, TProperty>> expression)
        {
            return new PropertyRule<T, TProperty>(expression);
        }

        public void AddValidator(IAsyncPropertyValidator<T, TProperty> validator) => _validators.Add(validator);

        // Method to add custom sync action
        public void AddCustomValidator(Action<TProperty, ValidationContext<T>> customValidator) =>
            _syncCustomValidators.Add(customValidator);

        // Method to add custom async action
        public void AddCustomValidator(Func<TProperty, ValidationContext<T>, CancellationToken, Task> customValidator) =>
            _asyncCustomValidators.Add(customValidator);


        // Validate method is now async
        public async Task ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken)
        {
            // instance-only condition
            if (Condition != null && !Condition(context.InstanceToValidate))
                return;

            TProperty value = default!;
            try
            {
                value = PropertyFunc(context.InstanceToValidate);
            }
            catch (Exception ex)
            {
                context.AddFailure(PropertyName, $"Failed to retrieve property value: {ex.Message}");

                Console.WriteLine($"WARN: Failed to retrieve property '{PropertyName}': {ex.Message}");

                return;
            }


            // instance+value condition
            if (ConditionWithValue != null && !ConditionWithValue(context.InstanceToValidate, value))
                return;

            // Set property name in context for validators and custom actions
            context.PropertyName = PropertyName;

            // Execute standard validators
            foreach (var validator in _validators)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!await validator.IsValidAsync(context, value, cancellationToken))
                {
                    var msg = validator.ErrorMessageTemplate.Replace("{PropertyName}", PropertyName);
                    var state = validator.CustomStateProvider?.Invoke(context.InstanceToValidate, value);
                    context.AddFailure(new ValidationFailure(PropertyName, msg, state));
                }
            }

            // Execute synchronous custom validators
            foreach (var customValidator in _syncCustomValidators)
            {
                cancellationToken.ThrowIfCancellationRequested();
                customValidator(value, context); // Pass value and context
            }

            // Execute asynchronous custom validators
            foreach (var customValidator in _asyncCustomValidators)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await customValidator(value, context, cancellationToken); // Pass value, context, and token
            }
        }
    }


    // --- Modified IPropertyValidator Implementations (Now Async) ---

    // Base class for simple sync validators adapting to async interface
    internal abstract class AsyncPropertyValidator<T, TProperty> : IAsyncPropertyValidator<T, TProperty>
    {
        public abstract string ErrorMessageTemplate { get; set; }
        public Func<T, TProperty, object?>? CustomStateProvider { get; set; }

        // Implement IsValidAsync by calling a synchronous IsValid method
        public Task<bool> IsValidAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsValid(value));
        }

        // Synchronous validation logic implemented by derived classes
        public abstract bool IsValid(TProperty value);
    }


    internal class NotNullValidator<T, TProperty> : AsyncPropertyValidator<T, TProperty> // Inherits helper base
    {
        public override string ErrorMessageTemplate { get; set; } = "{PropertyName} must not be null.";
        public override bool IsValid(TProperty value) => value != null;
    }

    internal class NotEmptyValidator<T, TProperty> : AsyncPropertyValidator<T, TProperty>
    {
        public override string ErrorMessageTemplate { get; set; } = "{PropertyName} must not be empty.";
        public override bool IsValid(TProperty value)
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

    // --- Modified IRuleBuilder and RuleBuilder ---
    public interface IRuleBuilderInitial<T, TProperty> { }

    public interface IRuleBuilder<T, TProperty> : IRuleBuilderInitial<T, TProperty>
    {
        IRuleBuilder<T, TProperty> NotNull();
        IRuleBuilder<T, TProperty> NotEmpty();
        IRuleBuilder<T, TProperty> WithMessage(string message);
        IRuleBuilder<T, TProperty> WithState(Func<T, TProperty, object> stateProvider);
        IRuleBuilder<T, TProperty> WithState(object state);
        // SetValidator now accepts IValidator<TProperty> which needs async internally
        IRuleBuilder<T, TProperty> SetValidator(IValidator<TProperty> validator);
        IRuleBuilder<T, TProperty> When(Func<T, bool> predicate);
        IRuleBuilder<T, TProperty> When(Func<T, TProperty, bool> predicate);

        // NEW: Custom validation methods
        IRuleBuilder<T, TProperty> Custom(Action<TProperty, ValidationContext<T>> action);
        IRuleBuilder<T, TProperty> Custom(Func<TProperty, ValidationContext<T>, CancellationToken, Task> asyncAction);
    }


    public class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty>
    {
        private readonly AbstractValidator<T> _parent;
        private readonly PropertyRule<T, TProperty> _rule;
        // Track the last *async* validator added
        private IAsyncPropertyValidator<T, TProperty>? _lastValidator;

        internal RuleBuilder(AbstractValidator<T> parent, PropertyRule<T, TProperty> rule)
        {
            _parent = parent;
            _rule = rule;
        }

        // Internal helper now adds async validators
        internal void AddValidator(IAsyncPropertyValidator<T, TProperty> validator)
        {
            _rule.AddValidator(validator);
            _lastValidator = validator;
        }

        public IRuleBuilder<T, TProperty> NotNull()
        {
            AddValidator(new NotNullValidator<T, TProperty>());
            return this;
        }

        public IRuleBuilder<T, TProperty> NotEmpty()
        {
            AddValidator(new NotEmptyValidator<T, TProperty>());
            return this;
        }

        public IRuleBuilder<T, TProperty> WithMessage(string message)
        {
            if (_lastValidator == null)
                throw new InvalidOperationException("No validator available to set a message on. Call a validator method (e.g., NotNull, NotEmpty, Custom) first.");
            _lastValidator.ErrorMessageTemplate = message;
            return this;
        }

        public IRuleBuilder<T, TProperty> WithState(Func<T, TProperty, object> stateProvider)
        {
            if (_lastValidator == null)
                throw new InvalidOperationException("No validator available to set state on. Call a validator method (e.g., NotNull, NotEmpty, Custom) first.");
            _lastValidator.CustomStateProvider = stateProvider;
            return this;
        }

        public IRuleBuilder<T, TProperty> WithState(object state)
            => WithState((instance, val) => state);

        // SetValidator needs adaptation for async
        public IRuleBuilder<T, TProperty> SetValidator(IValidator<TProperty> validator)
        {
            // ChildValidatorAdaptorRule needs to be async
            var adaptor = new ChildValidatorAdaptorRule<T, TProperty>(_rule.PropertyName, _rule.PropertyFunc, validator);
            _parent.AddRule(adaptor);
            // Note: SetValidator doesn't add to _lastValidator, so WithMessage/WithState after it won't work as expected.
            // This matches original FluentValidation behavior.
            _lastValidator = null;
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

        // --- NEW Custom Method Implementations ---

        public IRuleBuilder<T, TProperty> Custom(Action<TProperty, ValidationContext<T>> action)
        {
            _rule.AddCustomValidator(action);
            // Custom doesn't add a standard validator, so reset _lastValidator
            _lastValidator = null;
            return this;
        }

        public IRuleBuilder<T, TProperty> Custom(Func<TProperty, ValidationContext<T>, CancellationToken, Task> asyncAction)
        {
            _rule.AddCustomValidator(asyncAction);
            // Custom doesn't add a standard validator, so reset _lastValidator
            _lastValidator = null;
            return this;
        }
    }


    // --- Modified ChildValidatorAdaptorRule (Async) ---
    internal class ChildValidatorAdaptorRule<T, TProperty> : IAsyncValidationRule<T> // Implements async interface
    {
        private readonly string _propertyName;
        private readonly Func<T, TProperty> _propertyFunc;
        private readonly IValidator<TProperty> _validator; // The child validator itself

        public ChildValidatorAdaptorRule(string propertyName, Func<T, TProperty> propertyFunc, IValidator<TProperty> validator)
        {
            _propertyName = propertyName;
            _propertyFunc = propertyFunc;
            _validator = validator;
        }

        // Validate method is now async
        public async Task ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken)
        {
            TProperty value = default!;
            try
            {
                value = _propertyFunc(context.InstanceToValidate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARN: Failed to retrieve property '{_propertyName}' for child validation: {ex.Message}");
                return;
            }

            if (value == null) return; // Don't validate null children

            cancellationToken.ThrowIfCancellationRequested();

            // Call the child validator's async method
            var result = await _validator.ValidateAsync(value, cancellationToken);

            foreach (var failure in result.Errors)
            {
                // Prepend the parent property name
                var name = $"{_propertyName}.{failure.PropertyName}";
                // Use the context's AddFailure method
                context.AddFailure(new ValidationFailure(name, failure.ErrorMessage, failure.CustomState));
            }
        }
    }


    // --- RuleBuilderForEach & ForEachRule (Needs Async Adaptation - Simplified Placeholder) ---
    // IMPORTANT: Fully implementing async for RuleForEach requires similar changes
    //            to RuleBuilderForEach and ForEachRule as done for RuleFor/PropertyRule.
    //            This is a simplified version for now.

    public interface IRuleBuilderForEach<T, TElement>
    {
        IRuleBuilderForEach<T, TElement> WithMessage(string message);
        IRuleBuilderForEach<T, TElement> WithState(Func<T, TElement, object> stateProvider);
        IRuleBuilderForEach<T, TElement> WithState(object state);
        // This needs to accept IValidator<TElement> which is internally async
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
            var member = PropertyRule<T, IEnumerable<TElement>>.GetMemberExpression(expression); // Use helper
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
            // ForEachRule needs to become async
            var rule = new ForEachRule<T, TElement>(_propertyName, _propertyFunc, validator, _messageTemplate, _stateProvider);
            _parent.AddRule(rule);
        }
    }

    // ForEachRule needs to implement IAsyncValidationRule and use ValidateAsync
    internal class ForEachRule<T, TElement> : IAsyncValidationRule<T> // Implements async interface
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

        // Validate method is now async
        public async Task ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken)
        {
            IEnumerable<TElement> collection = default!;
            try
            {
                collection = _propertyFunc(context.InstanceToValidate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARN: Failed to retrieve collection '{_propertyName}' for ForEach validation: {ex.Message}");
                return;
            }

            if (collection == null) return;

            int index = 0;
            foreach (var element in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (element == null)
                {
                    // Handle null elements in collection if necessary
                    // context.AddFailure($"{_propertyName}[{index}]", "Item in collection cannot be null.");
                    index++;
                    continue;
                }


                // Create a temporary property name for the context within the loop
                var originalPropName = context.PropertyName;
                var indexedPropName = $"{_propertyName}[{index}]";
                context.PropertyName = indexedPropName; // Set context for child validator

                // Validate the element using its async validator
                var result = await _validator.ValidateAsync(element, cancellationToken);

                foreach (var failure in result.Errors)
                {
                    // Prepend the collection property name and index
                    var propName = $"{indexedPropName}.{failure.PropertyName}";
                    var msg = failure.ErrorMessage ?? _messageTemplate
                        .Replace("{PropertyName}", propName)
                        .Replace("{CollectionIndex}", index.ToString());
                    var state = failure.CustomState ?? _stateProvider?.Invoke(context.InstanceToValidate, element);
                    // Add failure using the main context
                    context.AddFailure(propName, msg, state);
                }

                // Restore original property name in context (important if context is reused later)
                context.PropertyName = originalPropName;

                index++;
            }
        }
    }


    // --- Comparison Validators (Need Async Adaptation) ---
    internal enum ComparisonOperator { Equal, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual }

    // Use AsyncPropertyValidator base class
    internal class NullableComparisonValidator<T, TProperty>
        : AsyncPropertyValidator<T, TProperty?>
        where TProperty : struct, IComparable<TProperty>
    {
        private readonly ComparisonOperator _operator;
        private readonly TProperty _compareTo;

        public override string ErrorMessageTemplate { get; set; }

        public NullableComparisonValidator(ComparisonOperator op, TProperty compareTo)
        {
            _operator = op;
            _compareTo = compareTo;
            ErrorMessageTemplate = "{PropertyName} comparison failed."; // Set default message
        }

        public override bool IsValid(TProperty? value) // Implement sync logic
        {
            if (!value.HasValue)
                return false; // Or true depending on desired behavior for null

            var cmp = value.Value.CompareTo(_compareTo);
            return _operator switch
            {
                ComparisonOperator.Equal => cmp == 0,
                ComparisonOperator.GreaterThan => cmp > 0,
                ComparisonOperator.GreaterThanOrEqual => cmp >= 0,
                ComparisonOperator.LessThan => cmp < 0,
                ComparisonOperator.LessThanOrEqual => cmp <= 0,
                _ => throw new InvalidOperationException(),
            };
        }
    }

    // Use AsyncPropertyValidator base class
    internal class ComparisonValidator<T, TProperty> : AsyncPropertyValidator<T, TProperty>
        where TProperty : IComparable<TProperty>
    {
        private readonly ComparisonOperator _operator;
        private readonly TProperty _compareTo;
        public override string ErrorMessageTemplate { get; set; }

        public ComparisonValidator(ComparisonOperator op, TProperty compareTo)
        {
            _operator = op;
            _compareTo = compareTo;
            ErrorMessageTemplate = "{PropertyName} comparison failed."; // Set default message
        }

        public override bool IsValid(TProperty value) // Implement sync logic
        {
            if (value == null) return false; // Or true depending on desired behavior
            var cmp = value.CompareTo(_compareTo);
            return _operator switch
            {
                ComparisonOperator.Equal => cmp == 0,
                ComparisonOperator.GreaterThan => cmp > 0,
                ComparisonOperator.GreaterThanOrEqual => cmp >= 0,
                ComparisonOperator.LessThan => cmp < 0,
                ComparisonOperator.LessThanOrEqual => cmp <= 0,
                _ => throw new InvalidOperationException(),
            };
        }
    }

    // --- Length Validators (Need Async Adaptation) ---

    // Use AsyncPropertyValidator base class
    internal class LengthValidator<T> : AsyncPropertyValidator<T, string?>
    {
        private readonly int _min, _max;
        public override string ErrorMessageTemplate { get; set; }

        public LengthValidator(int min, int max)
        {
            _min = min; _max = max;
            ErrorMessageTemplate = $"{{PropertyName}} must be between {_min} and {_max} characters long.";
        }

        public override bool IsValid(string? value)
            => !string.IsNullOrEmpty(value) && value.Length >= _min && value.Length <= _max;
    }

    // Use AsyncPropertyValidator base class
    internal class MaxLengthValidator<T> : AsyncPropertyValidator<T, string?>
    {
        private readonly int _max;
        public override string ErrorMessageTemplate { get; set; }

        public MaxLengthValidator(int max)
        {
            _max = max;
            ErrorMessageTemplate = $"{{PropertyName}} must have a maximum length of {_max}.";
        }

        public override bool IsValid(string? value)
            => value == null || value.Length <= _max; // Allow null/empty strings usually
    }

    // Use AsyncPropertyValidator base class
    internal class MinLengthValidator<T> : AsyncPropertyValidator<T, string?>
    {
        private readonly int _min;
        public override string ErrorMessageTemplate { get; set; }

        public MinLengthValidator(int min)
        {
            _min = min;
            ErrorMessageTemplate = $"{{PropertyName}} must have a minimum length of {_min}.";
        }

        public override bool IsValid(string? value)
            => !string.IsNullOrEmpty(value) && value.Length >= _min;
    }

    // --- Precision/Scale Validator (Needs Async Adaptation) ---

    // Use AsyncPropertyValidator base class
    internal class PrecisionAndScaleValidator<T> : AsyncPropertyValidator<T, decimal?>
    {
        private readonly int _precision, _scale;
        public override string ErrorMessageTemplate { get; set; }

        public PrecisionAndScaleValidator(int precision, int scale)
        {
            _precision = precision; _scale = scale;
            ErrorMessageTemplate = $"{{PropertyName}} must not exceed precision {_precision} and scale {_scale}.";
        }

        public override bool IsValid(decimal? value)
        {
            if (value == null) return true; // Usually allow null unless NotNull is used

            // Simplified check (original FluentValidation uses more robust logic)
            var s = value.Value.ToString(CultureInfo.InvariantCulture);
            var parts = s.Split('.');
            var integerPartLength = parts[0].TrimStart('-').Length;
            var scalePartLength = parts.Length > 1 ? parts[1].Length : 0;

            return integerPartLength + scalePartLength <= _precision && scalePartLength <= _scale;
        }
    }


    // --- Modified RuleBuilderExtensions (Use Async Validators) ---
    public static class RuleBuilderExtensions
    {
        // Helper to get the concrete RuleBuilder instance
        private static RuleBuilder<T, TProperty> GetBuilder<T, TProperty>(IRuleBuilder<T, TProperty> builder)
        {
            if (builder is RuleBuilder<T, TProperty> rb)
            {
                return rb;
            }
            throw new InvalidOperationException("Internal error: builder is not of expected type RuleBuilder<T, TProperty>.");
        }

        private static RuleBuilder<T, TProperty?> GetNullableBuilder<T, TProperty>(IRuleBuilder<T, TProperty?> builder)
           where TProperty : struct
        {
            if (builder is RuleBuilder<T, TProperty?> rb)
            {
                return rb;
            }
            throw new InvalidOperationException("Internal error: builder is not of expected type RuleBuilder<T, TProperty?>.");
        }


        public static IRuleBuilder<T, TProperty> EqualTo<T, TProperty>(
            this IRuleBuilder<T, TProperty> builder, TProperty value)
            where TProperty : IComparable<TProperty>
        {
            GetBuilder(builder).AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.Equal, value));
            return builder;
        }

        // [GreaterThan]
        public static IRuleBuilder<T, TProperty> GreaterThan<T, TProperty>(
            this IRuleBuilder<T, TProperty> builder, TProperty value)
            where TProperty : IComparable<TProperty>
        {
            GetBuilder(builder).AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThan, value));
            return builder;
        }

        public static IRuleBuilder<T, TProperty?> GreaterThan<T, TProperty>(
            this IRuleBuilder<T, TProperty?> builder, TProperty value)
            where TProperty : struct, IComparable<TProperty>
        {
            GetNullableBuilder(builder).AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThan, value));
            return builder;
        }

        // [GreaterThanOrEqualTo]
        public static IRuleBuilder<T, TProperty> GreaterThanOrEqualTo<T, TProperty>(
            this IRuleBuilder<T, TProperty> builder, TProperty value)
            where TProperty : IComparable<TProperty>
        {
            GetBuilder(builder).AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThanOrEqual, value));
            return builder;
        }

        public static IRuleBuilder<T, TProperty?> GreaterThanOrEqualTo<T, TProperty>(
            this IRuleBuilder<T, TProperty?> builder, TProperty value)
            where TProperty : struct, IComparable<TProperty>
        {
            GetNullableBuilder(builder).AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.GreaterThanOrEqual, value));
            return builder;
        }

        // [LessThan]
        public static IRuleBuilder<T, TProperty> LessThan<T, TProperty>(
            this IRuleBuilder<T, TProperty> builder, TProperty value)
            where TProperty : IComparable<TProperty>
        {
            GetBuilder(builder).AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.LessThan, value));
            return builder;
        }

        public static IRuleBuilder<T, TProperty?> LessThan<T, TProperty>(
            this IRuleBuilder<T, TProperty?> builder, TProperty value)
            where TProperty : struct, IComparable<TProperty>
        {
            GetNullableBuilder(builder).AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.LessThan, value));
            return builder;
        }

        // [LessThanOrEqual]
        public static IRuleBuilder<T, TProperty> LessThanOrEqual<T, TProperty>(
            this IRuleBuilder<T, TProperty> builder, TProperty value)
            where TProperty : IComparable<TProperty>
        {
            GetBuilder(builder).AddValidator(new ComparisonValidator<T, TProperty>(ComparisonOperator.LessThanOrEqual, value));
            return builder;
        }

        public static IRuleBuilder<T, TProperty?> LessThanOrEqual<T, TProperty>(
            this IRuleBuilder<T, TProperty?> builder, TProperty value)
            where TProperty : struct, IComparable<TProperty>
        {
            GetNullableBuilder(builder).AddValidator(new NullableComparisonValidator<T, TProperty>(ComparisonOperator.LessThanOrEqual, value));
            return builder;
        }

        // [Length]
        public static IRuleBuilder<T, string?> Length<T>(
            this IRuleBuilder<T, string?> builder, int min, int max)
        {
            GetBuilder(builder).AddValidator(new LengthValidator<T>(min, max));
            return builder;
        }

        // [MaxLength] - Renamed from MaxLenght
        public static IRuleBuilder<T, string?> MaxLength<T>(
            this IRuleBuilder<T, string?> builder, int max)
        {
            GetBuilder(builder).AddValidator(new MaxLengthValidator<T>(max));
            return builder;
        }

        // [MinLength] - Renamed from MinLenght
        public static IRuleBuilder<T, string?> MinLength<T>(
            this IRuleBuilder<T, string?> builder, int min)
        {
            GetBuilder(builder).AddValidator(new MinLengthValidator<T>(min));
            return builder;
        }

        // [PrecisionAndScale]
        public static IRuleBuilder<T, decimal?> PrecisionScale<T>( // Renamed for consistency
           this IRuleBuilder<T, decimal?> builder, int precision, int scale)
        {
            GetNullableBuilder(builder).AddValidator(new PrecisionAndScaleValidator<T>(precision, scale));
            return builder;
        }
    }
}