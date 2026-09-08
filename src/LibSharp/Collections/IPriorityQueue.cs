// Copyright (c) 2026 Danylo Fitel

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LibSharp.Collections;

/// <summary>
/// Interface for a priority queue.
/// </summary>
/// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
public interface IPriorityQueue<T> : ICollection<T>, IReadOnlyCollection<T>
{
    /// <summary>
    /// Gets the number of items in the queue.
    /// </summary>
    /// <remarks>
    /// Re-declared to resolve the ambiguity between <see cref="ICollection{T}.Count"/> and
    /// <see cref="IReadOnlyCollection{T}.Count"/>. Without it, reading <c>Count</c> through this
    /// interface is a compile error (CS0229), which is the reason the base class library never made
    /// <see cref="ICollection{T}"/> derive from <see cref="IReadOnlyCollection{T}"/>. Implementers
    /// need do nothing extra: a single public <c>Count</c> satisfies all three declarations.
    /// </remarks>
    new int Count { get; }

    /// <summary>
    /// Returns the object at the beginning of the queue without removing it.
    /// </summary>
    /// <returns>The object at the beginning of the queue.</returns>
    T Peek();

    /// <summary>
    /// Returns the object at the beginning of the queue without removing it.
    /// </summary>
    /// <param name="item">When this method returns, contains the object at the beginning of the queue,
    /// if the operation succeeded, or the default value of <typeparamref name="T"/> if the queue was empty.</param>
    /// <returns><c>true</c> if the queue was not empty; otherwise <c>false</c>.</returns>
    bool TryPeek([MaybeNullWhen(false)] out T item);

    /// <summary>
    /// Adds an object to the queue.
    /// </summary>
    /// <param name="item">The object to add to the queue.</param>
    void Enqueue(T item);

    /// <summary>
    /// Removes and returns the object at the beginning of the queue.
    /// </summary>
    /// <returns>The object removed from the beginning of the queue.</returns>
    T Dequeue();

    /// <summary>
    /// Removes and returns the object at the beginning of the queue.
    /// </summary>
    /// <param name="item">When this method returns, contains the removed object,
    /// if the operation succeeded, or the default value of <typeparamref name="T"/> if the queue was empty.</param>
    /// <returns><c>true</c> if the queue was not empty; otherwise <c>false</c>.</returns>
    bool TryDequeue([MaybeNullWhen(false)] out T item);
}
