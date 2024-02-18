// Copyright (c) LibSharp. All rights reserved.

using System.Collections;
using System.Collections.Generic;

namespace LibSharp.Collections
{
    /// <summary>
    /// Interface for a priority queue.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    public interface IPriorityQueue<T> : IReadOnlyCollection<T>, ICollection<T>, ICollection
    {
        /// <summary>
        /// Returns the object at the beginning of the queue without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the queue.</returns>
        T Peek();

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
    }
}
