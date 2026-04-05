// Copyright (c) 2026 Danylo Fitel

using System.Collections;
using System.Collections.Generic;

namespace LibSharp.Collections;

/// <summary>
/// Interface for a priority queue.
/// </summary>
/// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
public interface IPriorityQueue<T> : ICollection<T>
{
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
    bool TryPeek(out T item);

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
    bool TryDequeue(out T item);
}
