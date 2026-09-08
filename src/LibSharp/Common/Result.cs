// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LibSharp.Common;

/// <summary>
/// A discriminated union that holds either a success value of type <typeparamref name="T"/>
/// or an error value of type <typeparamref name="TError"/>.
/// </summary>
/// <typeparam name="T">Success value type.</typeparam>
/// <typeparam name="TError">Error value type.</typeparam>
/// <remarks>
/// <c>default(Result&lt;T, TError&gt;)</c> is a failed state with <c>default(TError)</c> as the error.
/// Use <see cref="Ok"/> or <see cref="Fail"/> to construct instances explicitly.
/// </remarks>
public readonly struct Result<T, TError> : IEquatable<Result<T, TError>>
{
    private Result(T value, bool isSuccess, TError error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result wrapping the given value.
    /// </summary>
    /// <param name="value">The success value.</param>
    public static Result<T, TError> Ok(T value)
    {
        // The error slot is intentionally unused for a success; default! marks it as deliberately
        // absent. It is never observed because Error throws unless IsSuccess is false.
        return new(value, true, default!);
    }

    /// <summary>
    /// Creates a failed result wrapping the given error.
    /// </summary>
    /// <param name="error">The error value.</param>
    public static Result<T, TError> Fail(TError error)
    {
        // The value slot is intentionally unused for an error; default! marks it as deliberately
        // absent. It is never observed because Value throws unless IsSuccess is true.
        return new(default!, false, error);
    }

    /// <summary>
    /// Gets a value indicating whether this result represents a success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether this result represents an error.
    /// </summary>
    public bool IsError => !IsSuccess;

    /// <summary>
    /// Gets the success value. Throws if this is an error result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is an error.</exception>
    public T Value
    {
        get
        {
            if (!IsSuccess)
            {
                throw new InvalidOperationException("The result is an error and does not hold a success value.");
            }

            return _value;
        }
    }

    /// <summary>
    /// Gets the error value. Throws if this is a success result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
    public TError Error
    {
        get
        {
            if (IsSuccess)
            {
                throw new InvalidOperationException("The result is a success and does not hold an error value.");
            }

            return _error;
        }
    }

    /// <summary>
    /// Returns the success value if this is a success result, or <paramref name="fallback"/> otherwise.
    /// </summary>
    /// <param name="fallback">The fallback value. Defaults to <c>default(T)</c>.</param>
    public T? GetValueOrDefault(T? fallback = default)
    {
        return IsSuccess ? _value : fallback;
    }

    /// <summary>
    /// Returns the error value if this is an error result, or <paramref name="fallback"/> otherwise.
    /// </summary>
    /// <param name="fallback">The fallback error. Defaults to <c>default(TError)</c>.</param>
    public TError? GetErrorOrDefault(TError? fallback = default)
    {
        return IsError ? _error : fallback;
    }

    /// <summary>
    /// Returns true and sets <paramref name="value"/> to the success value if this is a success result;
    /// otherwise returns false and sets <paramref name="value"/> to <c>default(T)</c>.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Returns true and sets <paramref name="error"/> to the error value if this is an error result;
    /// otherwise returns false and sets <paramref name="error"/> to <c>default(TError)</c>.
    /// </summary>
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        error = _error;
        return IsError;
    }

    /// <summary>
    /// Projects the result through <paramref name="onSuccess"/> if it is a success, or
    /// <paramref name="onError"/> if it is an error, and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="onSuccess">Invoked with the success value when this is a success.</param>
    /// <param name="onError">Invoked with the error value when this is an error.</param>
    /// <returns>The result of the invoked delegate.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onError"/> or <paramref name="onSuccess"/> is <c>null</c>.</exception>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onError)
    {
        Argument.NotNull(onSuccess);
        Argument.NotNull(onError);

        return IsSuccess ? onSuccess(_value) : onError(_error);
    }

    /// <summary>
    /// Invokes <paramref name="onSuccess"/> with the success value if this is a success, or
    /// <paramref name="onError"/> with the error value if this is an error.
    /// </summary>
    /// <param name="onSuccess">Invoked with the success value when this is a success.</param>
    /// <param name="onError">Invoked with the error value when this is an error.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onError"/> or <paramref name="onSuccess"/> is <c>null</c>.</exception>
    public void Match(Action<T> onSuccess, Action<TError> onError)
    {
        Argument.NotNull(onSuccess);
        Argument.NotNull(onError);

        if (IsSuccess)
        {
            onSuccess(_value);
        }
        else
        {
            onError(_error);
        }
    }

    /// <summary>
    /// Transforms the success value with <paramref name="selector"/> if this is a success;
    /// otherwise propagates the error unchanged.
    /// </summary>
    /// <typeparam name="TResult">The mapped success value type.</typeparam>
    /// <param name="selector">The transform to apply to the success value.</param>
    /// <returns>A result holding the transformed value, or the original error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is <c>null</c>.</exception>
    public Result<TResult, TError> Map<TResult>(Func<T, TResult> selector)
    {
        Argument.NotNull(selector);

        return IsSuccess
            ? Result<TResult, TError>.Ok(selector(_value))
            : Result<TResult, TError>.Fail(_error);
    }

    /// <summary>
    /// Transforms the error value with <paramref name="selector"/> if this is an error;
    /// otherwise propagates the success value unchanged.
    /// </summary>
    /// <typeparam name="TErrorResult">The mapped error value type.</typeparam>
    /// <param name="selector">The transform to apply to the error value.</param>
    /// <returns>A result holding the original success value, or the transformed error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is <c>null</c>.</exception>
    public Result<T, TErrorResult> MapError<TErrorResult>(Func<TError, TErrorResult> selector)
    {
        Argument.NotNull(selector);

        return IsSuccess
            ? Result<T, TErrorResult>.Ok(_value)
            : Result<T, TErrorResult>.Fail(selector(_error));
    }

    /// <summary>
    /// Transforms the success value with <paramref name="selector"/> into another result if this
    /// is a success; otherwise propagates the error unchanged.
    /// </summary>
    /// <typeparam name="TResult">The mapped success value type.</typeparam>
    /// <param name="selector">The transform producing the next result from the success value.</param>
    /// <returns>The result produced by <paramref name="selector"/>, or the original error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is <c>null</c>.</exception>
    public Result<TResult, TError> Bind<TResult>(Func<T, Result<TResult, TError>> selector)
    {
        Argument.NotNull(selector);

        return IsSuccess
            ? selector(_value)
            : Result<TResult, TError>.Fail(_error);
    }

    /// <inheritdoc/>
    public bool Equals(Result<T, TError> other)
    {
        if (IsSuccess != other.IsSuccess)
        {
            return false;
        }

        if (IsSuccess)
        {
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        return EqualityComparer<TError>.Default.Equals(_error, other._error);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Result<T, TError> other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (IsSuccess)
        {
            return HashCode.Combine(true, _value);
        }

        return HashCode.Combine(false, _error);
    }

    /// <summary>
    /// Returns the string representation of the value on success, or of the error on failure.
    /// </summary>
    /// <returns>The string representation.</returns>
    /// <remarks>
    /// A success carrying <c>null</c> and a failure carrying <c>null</c> both render as an empty
    /// string, so this cannot tell them apart. Use <see cref="IsSuccess"/> for that.
    /// </remarks>
    public override string ToString()
    {
        if (IsSuccess)
        {
            return _value?.ToString() ?? string.Empty;
        }

        return _error?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Equals operator.
    /// </summary>
    public static bool operator ==(Result<T, TError> left, Result<T, TError> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Not equals operator.
    /// </summary>
    public static bool operator !=(Result<T, TError> left, Result<T, TError> right)
    {
        return !(left == right);
    }

    private readonly T _value;
    private readonly TError _error;
}
