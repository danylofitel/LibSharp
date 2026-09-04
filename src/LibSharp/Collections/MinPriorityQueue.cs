// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// A binary heap implementation of a minimum priority queue.
/// This implementation is not thread-safe.
/// </summary>
/// <remarks>
/// Enumeration yields every element exactly once, but in an unspecified order — the heap's internal
/// layout, not ascending order. Only <see cref="Peek"/> and <see cref="Dequeue"/> observe priority.
/// The distinction is easy to miss because the heap's first element is always the smallest, so a
/// short example can look sorted when it is not. Sort the results explicitly if order matters.
/// </remarks>
/// <typeparam name="T">Comparable type of queue items.</typeparam>
public sealed class MinPriorityQueue<T> : IPriorityQueue<T>, ICollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    public MinPriorityQueue()
        : this(InitialCapacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="comparison">Value comparison.</param>
    public MinPriorityQueue(Comparison<T> comparison)
        : this(InitialCapacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="comparer">Value comparer.</param>
    public MinPriorityQueue(IComparer<T> comparer)
        : this(InitialCapacity, Enumerable.Empty<T>(), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    public MinPriorityQueue(int capacity)
        : this(capacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    /// <param name="comparison">Value comparison.</param>
    public MinPriorityQueue(int capacity, Comparison<T> comparison)
        : this(capacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    /// <param name="comparer">Value comparer.</param>
    public MinPriorityQueue(int capacity, IComparer<T> comparer)
        : this(capacity, Enumerable.Empty<T>(), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection to add to the queue.</param>
    public MinPriorityQueue(IEnumerable<T> collection)
        : this(InitialCapacity, collection, TypeExtensions.GetDefaultComparer<T>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection to add to the queue.</param>
    /// <param name="comparison">Value comparer.</param>
    public MinPriorityQueue(IEnumerable<T> collection, Comparison<T> comparison)
        : this(InitialCapacity, collection, Comparer<T>.Create(comparison))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection to add to the queue.</param>
    /// <param name="comparer">Value comparer.</param>
    public MinPriorityQueue(IEnumerable<T> collection, IComparer<T> comparer)
        : this(InitialCapacity, collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinPriorityQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    /// <param name="collection">The collection to add to the queue.</param>
    /// <param name="comparer">Value comparer.</param>
    public MinPriorityQueue(int capacity, IEnumerable<T> collection, IComparer<T> comparer)
    {
        Argument.GreaterThanOrEqualTo(capacity, 0);
        Argument.NotNull(collection);
        Argument.NotNull(comparer);

        int initialCapacity = capacity;
        if (collection is IReadOnlyCollection<T> readOnlyCollection)
        {
            initialCapacity = Math.Max(initialCapacity, readOnlyCollection.Count);
        }
        else if (collection is ICollection<T> genericCollection)
        {
            initialCapacity = Math.Max(initialCapacity, genericCollection.Count);
        }
        else if (collection is ICollection nonGenericCollection)
        {
            initialCapacity = Math.Max(initialCapacity, nonGenericCollection.Count);
        }

        _comparer = comparer;
        _heap = new T[initialCapacity + 1];
        _version = 0L;
        Count = 0;

        foreach (T item in collection)
        {
            Enlarge();
            _heap[++Count] = item;
        }

        // Floyd's heapify: sinking every internal node bottom-up arranges the heap in O(n). Half the
        // nodes are leaves and need no work, and only the root can travel the full depth.
        //
        // This is one of many valid arrangements for the same elements. Nothing depends on which:
        // enumeration order is documented as unspecified, and a priority queue promises no
        // particular order between equal elements.
        for (int i = Count / 2; i >= 1; --i)
        {
            Sink(i);
        }
    }

    /// <inheritdoc/>
    public int Count { get; private set; }

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    // The non-generic ICollection members are implemented explicitly, so they stay off the public
    // surface while the interface is still available for legacy interop. SyncRoot and
    // IsSynchronized are the .NET 1.x synchronization pattern, which is obsolete and which this
    // type does not honour: nothing here takes a lock on SyncRoot. List<T> hides them the same way.
    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    /// <summary>
    /// Returns the smallest item without removing it from the queue.
    /// </summary>
    /// <returns>Smallest item in the queue.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
    public T Peek()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Cannot peek into an empty queue.");
        }

        return _heap[1];
    }

    /// <inheritdoc/>
    public bool TryPeek([MaybeNullWhen(false)] out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = _heap[1];
        return true;
    }

    /// <inheritdoc/>
    public void Enqueue(T item)
    {
        ++_version;

        Enlarge();

        _heap[++Count] = item;
        Swim(Count);
    }

    /// <summary>
    /// Returns the smallest item and removes it from the queue.
    /// </summary>
    /// <returns>The smallest item in the queue.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
    public T Dequeue()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Cannot dequeue from an empty queue.");
        }

        ++_version;

        T min = _heap[1];

        Exchange(1, Count--);
        Sink(1);

        _heap[Count + 1] = default!;
        Shrink();

        return min;
    }

    /// <inheritdoc/>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = Dequeue();
        return true;
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        Enqueue(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        if (Count != 0)
        {
            ++_version;

            _heap = new T[InitialCapacity + 1];
            Count = 0;
        }
    }

    /// <inheritdoc/>
    public bool Contains(T item)
    {
        return FirstIndexOf(item) > 0;
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        Argument.NotNull(array);
        Argument.GreaterThanOrEqualTo(arrayIndex, 0);
        Argument.LessThanOrEqualTo(arrayIndex, array.Length);
        Argument.GreaterThanOrEqualTo(array.Length - arrayIndex, Count, nameof(arrayIndex));

        Array.Copy(_heap, 1, array, arrayIndex, Count);
    }

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        int firstIndex = FirstIndexOf(item);

        if (firstIndex > 0)
        {
            ++_version;

            Exchange(firstIndex, Count--);
            Sink(firstIndex);
            Swim(firstIndex);

            _heap[Count + 1] = default!;
            Shrink();

            return true;
        }

        return false;
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
        return new MinPriorityQueueEnumerator<T>(this);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        Argument.NotNull(array);
        Argument.EqualTo(array.Rank, 1, nameof(array.Rank));
        Argument.GreaterThanOrEqualTo(index, 0);
        Argument.LessThanOrEqualTo(index, array.Length);
        Argument.GreaterThanOrEqualTo(array.Length - index, Count, nameof(index));

        Array.Copy(_heap, 1, array, index, Count);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return new MinPriorityQueueEnumerator<T>(this);
    }

    /// <summary>
    /// Gets the index of the first occurrence of the item in the heap.
    /// Uses <see cref="EqualityComparer{T}.Default"/> so that <see cref="Contains"/> and
    /// <see cref="Remove"/> honour the <see cref="ICollection{T}"/> contract (element equality),
    /// independently of the ordering relation used to arrange the heap.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <returns>Index of the item in the heap, or -1 if it was not found.</returns>
    private int FirstIndexOf(T item)
    {
        for (int i = 1; i <= Count; ++i)
        {
            if (s_equalityComparer.Equals(item, _heap[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Moves item at specified index up in the heap until the order is maintained.
    /// </summary>
    /// <param name="k">Index of the item.</param>
    private void Swim(int k)
    {
        while (k > 1 && Larger(k / 2, k))
        {
            Exchange(k, k / 2);
            k /= 2;
        }
    }

    /// <summary>
    /// Moves item at specified index down in the heap until the order is maintained.
    /// </summary>
    /// <param name="k">Index of the item.</param>
    private void Sink(int k)
    {
        while (2 * k <= Count)
        {
            int j = 2 * k;
            if (j < Count && Larger(j, j + 1))
            {
                j++;
            }

            if (!Larger(k, j))
            {
                break;
            }

            Exchange(k, j);
            k = j;
        }
    }

    /// <summary>
    /// Compares items at two indexes in the heap.
    /// </summary>
    /// <param name="i">The first item.</param>
    /// <param name="j">The second item.</param>
    /// <returns>True if the first item is larger than the second one.</returns>
    private bool Larger(int i, int j)
    {
        return _comparer.Compare(_heap[i], _heap[j]) > 0;
    }

    /// <summary>
    /// Exchanges items at specified indexes in the heap.
    /// </summary>
    /// <param name="i">Index of the first item.</param>
    /// <param name="j">Index of the second item.</param>
    private void Exchange(int i, int j)
    {
        (_heap[j], _heap[i]) = (_heap[i], _heap[j]);
    }

    /// <summary>
    /// Enlarges the array under the heap.
    /// </summary>
    private void Enlarge()
    {
        if (Count == _heap.Length - 1)
        {
            T[] largerPQ = new T[2 * _heap.Length];
            Array.Copy(_heap, 1, largerPQ, 1, Count);
            _heap = largerPQ;
        }
    }

    /// <summary>
    /// Shrinks the array under the heap.
    /// </summary>
    private void Shrink()
    {
        if (Count * 4 < _heap.Length && _heap.Length >= InitialCapacity * 2)
        {
            T[] smallerPQ = new T[_heap.Length / 2];
            Array.Copy(_heap, 1, smallerPQ, 1, Count);
            _heap = smallerPQ;
        }
    }

    /// <summary>
    /// The default initial capacity.
    /// </summary>
    private const int InitialCapacity = 1;

    /// <summary>
    /// Equality comparer used by <see cref="Contains"/> and <see cref="Remove"/> to locate items.
    /// </summary>
    private static readonly EqualityComparer<T> s_equalityComparer = EqualityComparer<T>.Default;

    /// <summary>
    /// The value comparer.
    /// </summary>
    private readonly IComparer<T> _comparer;

    /// <summary>
    /// The binary heap organized as an array, indexing starts at 1.
    /// </summary>
    private T[] _heap;

    /// <summary>
    /// Used to keep track of modifications by enumerators.
    /// </summary>
    private long _version;

    /// <summary>
    /// Implementation of a minimum priority queue enumerator.
    /// </summary>
    /// <typeparam name="TItem">Type of items in a queue.</typeparam>
    private struct MinPriorityQueueEnumerator<TItem> : IEnumerator<TItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueueEnumerator{TItem}"/> struct which allows to enumerate the given queue.
        /// </summary>
        /// <param name="queue">The queue instance.</param>
        public MinPriorityQueueEnumerator(MinPriorityQueue<TItem> queue)
        {
            _version = queue._version;
            _queue = queue;
            _index = -1;
        }

        /// <inheritdoc/>
        public readonly TItem Current
        {
            get
            {
                MinPriorityQueue<TItem> queue = Validate();

                if (_index < 0)
                {
                    // Heap index 0 is a deliberately unused slot, so this guard is what separates
                    // "enumeration has not started" from a real element.
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext first.");
                }

                if (_index >= queue.Count)
                {
                    throw new InvalidOperationException("Enumerator has enumerated all items and needs to be reset.");
                }

                return queue._heap[_index + 1];
            }
        }

        /// <inheritdoc/>
        readonly object? IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            MinPriorityQueue<TItem> queue = Validate();

            ++_index;
            return _index < queue.Count;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _ = Validate();

            _index = -1;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _queue = null;
        }

        /// <summary>
        /// Ensures that the enumerator is in a valid state, e.g. it has not been disposed, and the collection has not been modified.
        /// </summary>
        /// <returns>The non-null queue being enumerated.</returns>
        private readonly MinPriorityQueue<TItem> Validate()
        {
            if (_queue is null)
            {
                throw new ObjectDisposedException(nameof(MinPriorityQueueEnumerator<TItem>));
            }
            else if (_version != _queue._version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            return _queue;
        }

        /// <summary>
        /// Queue version at the time the enumerator was created. The enumerator is valid only for that version.
        /// </summary>
        private readonly long _version;

        /// <summary>
        /// Reference to the queue being enumerated.
        /// </summary>
        private MinPriorityQueue<TItem>? _queue;

        /// <summary>
        /// Current of the enumerator.
        /// </summary>
        private int _index;
    }
}
