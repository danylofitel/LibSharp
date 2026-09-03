// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LibSharp.Common;

/// <summary>
/// A wrapper for any type that may or may not hold a value.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Equality is defined only between two <see cref="Optional{T}"/> instances; the type implements
/// <see cref="IEquatable{T}"/> of <see cref="Optional{T}"/> only. A bare <typeparamref name="T"/>
/// participates in equality by first being implicitly converted to a present optional (see the
/// implicit conversion operator), so <c>new Optional&lt;int&gt;(1).Equals(1)</c> is <c>true</c>.
/// A boxed value typed as <see cref="object"/> is never converted and never compares equal.
/// This differs from <see cref="Nullable{T}"/>, which the runtime special-cases so that even a
/// boxed nullable compares equal to its boxed underlying value.
/// </remarks>
public readonly struct Optional<T> : IEquatable<Optional<T>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Optional{T}"/> struct with the given value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    public Optional(T value)
    {
        HasValue = true;
        m_value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the optional has a value.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets the value if it exists, throws an exception if it doesn't.
    /// </summary>
    public T Value
    {
        get
        {
            if (!HasValue)
            {
                throw new InvalidOperationException("The optional does not hold a value.");
            }

            return m_value;
        }
    }

    /// <summary>
    /// Returns the value if it exists, or <paramref name="fallback"/> if it doesn't.
    /// </summary>
    /// <param name="fallback">The fallback value. Defaults to <c>default(T)</c>.</param>
    public T? GetValueOrDefault(T? fallback = default)
    {
        return HasValue ? m_value : fallback;
    }

    /// <summary>
    /// Returns true and sets <paramref name="value"/> to the wrapped value if the optional has one;
    /// otherwise returns false and sets <paramref name="value"/> to <c>default(T)</c>.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = m_value;
        return HasValue;
    }

    /// <summary>
    /// Projects the wrapped value through <paramref name="onValue"/> if present, or invokes
    /// <paramref name="onNone"/> if not, and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="onValue">Invoked with the wrapped value when the optional has one.</param>
    /// <param name="onNone">Invoked when the optional is empty.</param>
    /// <returns>The result of the invoked delegate.</returns>
    public TResult Match<TResult>(Func<T, TResult> onValue, Func<TResult> onNone)
    {
        Argument.NotNull(onValue);
        Argument.NotNull(onNone);

        return HasValue ? onValue(m_value) : onNone();
    }

    /// <summary>
    /// Invokes <paramref name="onValue"/> with the wrapped value if present, or
    /// <paramref name="onNone"/> if not.
    /// </summary>
    /// <param name="onValue">Invoked with the wrapped value when the optional has one.</param>
    /// <param name="onNone">Invoked when the optional is empty.</param>
    public void Match(Action<T> onValue, Action onNone)
    {
        Argument.NotNull(onValue);
        Argument.NotNull(onNone);

        if (HasValue)
        {
            onValue(m_value);
        }
        else
        {
            onNone();
        }
    }

    /// <summary>
    /// Transforms the wrapped value with <paramref name="selector"/> if present, returning a new
    /// optional; returns an empty optional if this one is empty.
    /// </summary>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="selector">The transform to apply to the wrapped value.</param>
    /// <returns>An optional holding the transformed value, or an empty optional.</returns>
    public Optional<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        Argument.NotNull(selector);

        return HasValue ? new Optional<TResult>(selector(m_value)) : default;
    }

    /// <summary>
    /// Transforms the wrapped value with <paramref name="selector"/> into another optional if
    /// present; returns an empty optional if this one is empty.
    /// </summary>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="selector">The transform producing the next optional from the wrapped value.</param>
    /// <returns>The optional produced by <paramref name="selector"/>, or an empty optional.</returns>
    public Optional<TResult> Bind<TResult>(Func<T, Optional<TResult>> selector)
    {
        Argument.NotNull(selector);

        return HasValue ? selector(m_value) : default;
    }

    /// <inheritdoc/>
    public bool Equals(Optional<T> other)
    {
        if (HasValue != other.HasValue)
        {
            return false;
        }

        return !HasValue || EqualityComparer<T>.Default.Equals(m_value, other.m_value);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Optional<T> other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(HasValue, m_value);
    }

    /// <summary>
    /// Returns the string representation of the value, or an empty string when there is none.
    /// </summary>
    /// <returns>The string representation.</returns>
    /// <remarks>
    /// An empty optional and one holding <c>null</c> both render as an empty string, so this cannot
    /// tell them apart even though they are distinct states that compare unequal. Use
    /// <see cref="HasValue"/> for that. The empty case follows <see cref="Nullable{T}"/>, which also
    /// renders as an empty string.
    /// </remarks>
    public override string ToString()
    {
        return HasValue ? (m_value?.ToString() ?? string.Empty) : string.Empty;
    }

    /// <summary>
    /// Wraps a value in an optional that holds it.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <remarks>
    /// The result always has a value, even when <paramref name="value"/> is <c>null</c>: a present
    /// null and an empty optional are distinct states. Use <c>default</c> for an empty optional.
    /// </remarks>
    public static implicit operator Optional<T>(T value)
    {
        return new Optional<T>(value);
    }

    /// <summary>
    /// Equals operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True of the operands are equal, false otherwise.</returns>
    public static bool operator ==(Optional<T> left, Optional<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Not equals operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True of the operands are not equal, false otherwise.</returns>
    public static bool operator !=(Optional<T> left, Optional<T> right)
    {
        return !(left == right);
    }

    private readonly T m_value;
}
