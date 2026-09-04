// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// A binary heap implementation of a maximum priority queue.
/// This implementation is not thread-safe.
/// </summary>
/// <remarks>
/// Enumeration yields every element exactly once, but in an unspecified order — the heap's internal
/// layout, not descending order. Only <see cref="Peek"/> and <see cref="Dequeue"/> observe priority.
/// The distinction is easy to miss because the heap's first element is always the largest, so a
/// short example can look sorted when it is not. Sort the results explicitly if order matters.
/// </remarks>
/// <typeparam name="T">Comparable type of queue items.</typeparam>
public sealed class MaxPriorityQueue<T> : IPriorityQueue<T>, ICollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    public MaxPriorityQueue()
        : this(InitialCapacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="comparison">Value comparison.</param>
    public MaxPriorityQueue(Comparison<T> comparison)
        : this(InitialCapacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="comparer">Value comparer.</param>
    public MaxPriorityQueue(IComparer<T> comparer)
        : this(InitialCapacity, Enumerable.Empty<T>(), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    public MaxPriorityQueue(int capacity)
        : this(capacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    /// <param name="comparison">Value comparison.</param>
    public MaxPriorityQueue(int capacity, Comparison<T> comparison)
        : this(capacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    /// <param name="comparer">Value comparer.</param>
    public MaxPriorityQueue(int capacity, IComparer<T> comparer)
        : this(capacity, Enumerable.Empty<T>(), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection to add to the queue.</param>
    public MaxPriorityQueue(IEnumerable<T> collection)
        : this(InitialCapacity, collection, TypeExtensions.GetDefaultComparer<T>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection to add to the queue.</param>
    /// <param name="comparison">Value comparer.</param>
    public MaxPriorityQueue(IEnumerable<T> collection, Comparison<T> comparison)
        : this(InitialCapacity, collection, Comparer<T>.Create(comparison))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection to add to the queue.</param>
    /// <param name="comparer">Value comparer.</param>
    public MaxPriorityQueue(IEnumerable<T> collection, IComparer<T> comparer)
        : this(InitialCapacity, collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    /// <param name="collection">The collection to add to the queue.</param>
    /// <param name="comparer">Value comparer.</param>
    public MaxPriorityQueue(int capacity, IEnumerable<T> collection, IComparer<T> comparer)
    {
        Argument.GreaterThanOrEqualTo(capacity, 0);
        Argument.NotNull(collection);
        Argument.NotNull(comparer);

        _minPriorityQueue = new MinPriorityQueue<T>(capacity, collection, new ReverseComparer<T>(comparer));
    }

    /// <summary>
    /// The default initial capacity.
    /// </summary>
    private const int InitialCapacity = 1;

    private readonly MinPriorityQueue<T> _minPriorityQueue;

    /// <inheritdoc/>
    public int Count => _minPriorityQueue.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    // The non-generic ICollection members are implemented explicitly, so they stay off the public
    // surface while the interface is still available for legacy interop. SyncRoot and
    // IsSynchronized are the .NET 1.x synchronization pattern, which is obsolete and which this
    // type does not honour: nothing here takes a lock on SyncRoot. List<T> hides them the same way.
    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    /// <summary>
    /// Returns the largest item without removing it from the queue.
    /// </summary>
    /// <returns>Largest item in the queue.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
    public T Peek()
    {
        return _minPriorityQueue.Peek();
    }

    /// <inheritdoc/>
    public bool TryPeek([MaybeNullWhen(false)] out T item)
    {
        return _minPriorityQueue.TryPeek(out item);
    }

    /// <inheritdoc/>
    public void Enqueue(T item)
    {
        _minPriorityQueue.Enqueue(item);
    }

    /// <summary>
    /// Returns the largest item and removes it from the queue.
    /// </summary>
    /// <returns>The largest item in the queue.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
    public T Dequeue()
    {
        return _minPriorityQueue.Dequeue();
    }

    /// <inheritdoc/>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        return _minPriorityQueue.TryDequeue(out item);
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        _minPriorityQueue.Add(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _minPriorityQueue.Clear();
    }

    /// <inheritdoc/>
    public bool Contains(T item)
    {
        return _minPriorityQueue.Contains(item);
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        _minPriorityQueue.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        return _minPriorityQueue.Remove(item);
    }

    /// <summary>
    /// Returns an enumerator over every element in the queue.
    /// </summary>
    /// <returns>An enumerator that yields each element exactly once, in an unspecified order.</returns>
    /// <remarks>
    /// The order is the heap's internal layout, not priority order. Use <see cref="Dequeue"/> to
    /// consume elements by priority, or sort the enumerated results.
    /// </remarks>
    public IEnumerator<T> GetEnumerator()
    {
        return _minPriorityQueue.GetEnumerator();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        // The inner queue hides this member behind the interface too, so reach it the same way.
        ((ICollection)_minPriorityQueue).CopyTo(array, index);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _minPriorityQueue.GetEnumerator();
    }
}
