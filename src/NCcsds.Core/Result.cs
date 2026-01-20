using System.Diagnostics.CodeAnalysis;

namespace NCcsds.Core;

/// <summary>
/// Represents the result of an operation that may succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
public readonly struct Result<T, TError>
{
    private readonly T? _value;
    private readonly TError? _error;

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing on a failed result.</exception>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value on a failed result.");

    /// <summary>
    /// Gets the error value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing on a successful result.</exception>
    public TError Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access Error on a successful result.");

    private Result(T value)
    {
        _value = value;
        _error = default;
        IsSuccess = true;
    }

    private Result(TError error, bool _)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The success value.</param>
    public static Result<T, TError> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error value.</param>
    public static Result<T, TError> Failure(TError error) => new(error, false);

    /// <summary>
    /// Gets the value or a default if failed.
    /// </summary>
    /// <param name="defaultValue">The default value to return if failed.</param>
    public T GetValueOrDefault(T defaultValue) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Gets the value or the result of a factory function if failed.
    /// </summary>
    /// <param name="factory">The factory function to call if failed.</param>
    public T GetValueOrDefault(Func<TError, T> factory) => IsSuccess ? _value! : factory(_error!);

    /// <summary>
    /// Maps the success value to a new type.
    /// </summary>
    public Result<TNew, TError> Map<TNew>(Func<T, TNew> mapper) =>
        IsSuccess ? Result<TNew, TError>.Success(mapper(_value!)) : Result<TNew, TError>.Failure(_error!);

    /// <summary>
    /// Maps the error value to a new type.
    /// </summary>
    public Result<T, TNewError> MapError<TNewError>(Func<TError, TNewError> mapper) =>
        IsSuccess ? Result<T, TNewError>.Success(_value!) : Result<T, TNewError>.Failure(mapper(_error!));

    /// <summary>
    /// Chains another operation on success.
    /// </summary>
    public Result<TNew, TError> Bind<TNew>(Func<T, Result<TNew, TError>> binder) =>
        IsSuccess ? binder(_value!) : Result<TNew, TError>.Failure(_error!);

    /// <summary>
    /// Matches on success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    /// <summary>
    /// Executes an action on success or failure.
    /// </summary>
    public void Match(Action<T> onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess)
            onSuccess(_value!);
        else
            onFailure(_error!);
    }

    public static implicit operator Result<T, TError>(T value) => Success(value);

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}

/// <summary>
/// Represents the result of an operation that may succeed or fail with a string error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;
    private readonly string? _error;

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value.
    /// </summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Cannot access Value on a failed result: {_error}");

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access Error on a successful result.");

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(string error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Gets the value or a default if failed.
    /// </summary>
    public T GetValueOrDefault(T defaultValue) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Maps the success value to a new type.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper) =>
        IsSuccess ? Result<TNew>.Success(mapper(_value!)) : Result<TNew>.Failure(_error!);

    /// <summary>
    /// Chains another operation on success.
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder) =>
        IsSuccess ? binder(_value!) : Result<TNew>.Failure(_error!);

    /// <summary>
    /// Matches on success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public static implicit operator Result<T>(T value) => Success(value);

    public bool Equals(Result<T> other) =>
        IsSuccess == other.IsSuccess &&
        EqualityComparer<T?>.Default.Equals(_value, other._value) &&
        _error == other._error;

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(IsSuccess, _value, _error);

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}
