// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;

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
        m_value = value;
        m_error = error;
    }

    /// <summary>
    /// Creates a successful result wrapping the given value.
    /// </summary>
    /// <param name="value">The success value.</param>
    public static Result<T, TError> Ok(T value)
    {
        return new(value, true, default);
    }


    /// <summary>
    /// Creates a failed result wrapping the given error.
    /// </summary>
    /// <param name="error">The error value.</param>
    public static Result<T, TError> Fail(TError error)
    {
        return new(default, false, error);
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

            return m_value;
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

            return m_error;
        }
    }

    /// <summary>
    /// Returns the success value if this is a success result, or <paramref name="fallback"/> otherwise.
    /// </summary>
    /// <param name="fallback">The fallback value. Defaults to <c>default(T)</c>.</param>
    public T GetValueOrDefault(T fallback = default)
    {
        return IsSuccess ? m_value : fallback;
    }

    /// <summary>
    /// Returns the error value if this is an error result, or <paramref name="fallback"/> otherwise.
    /// </summary>
    /// <param name="fallback">The fallback error. Defaults to <c>default(TError)</c>.</param>
    public TError GetErrorOrDefault(TError fallback = default)
    {
        return IsError ? m_error : fallback;
    }

    /// <summary>
    /// Returns true and sets <paramref name="value"/> to the success value if this is a success result;
    /// otherwise returns false and sets <paramref name="value"/> to <c>default(T)</c>.
    /// </summary>
    public bool TryGetValue(out T value)
    {
        value = m_value;
        return IsSuccess;
    }

    /// <summary>
    /// Returns true and sets <paramref name="error"/> to the error value if this is an error result;
    /// otherwise returns false and sets <paramref name="error"/> to <c>default(TError)</c>.
    /// </summary>
    public bool TryGetError(out TError error)
    {
        error = m_error;
        return IsError;
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
            return EqualityComparer<T>.Default.Equals(m_value, other.m_value);
        }

        return EqualityComparer<TError>.Default.Equals(m_error, other.m_error);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        return obj is Result<T, TError> other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (IsSuccess)
        {
            return HashCode.Combine(true, m_value);
        }

        return HashCode.Combine(false, m_error);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsSuccess)
        {
            return m_value?.ToString() ?? string.Empty;
        }

        return m_error?.ToString() ?? string.Empty;
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

    private readonly T m_value;
    private readonly TError m_error;
}
