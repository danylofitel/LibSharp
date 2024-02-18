// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibSharp.Common;

namespace LibSharp.Collections
{
    /// <summary>
    /// A binary heap implementation of a maximum priority queue.
    /// This implementation is not thread-safe.
    /// </summary>
    /// <typeparam name="T">Comparable type of queue items.</typeparam>
    public class MaxPriorityQueue<T> : IPriorityQueue<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        public MaxPriorityQueue()
            : this(InitialCapacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="comparison">Value comparison.</param>
        public MaxPriorityQueue(Comparison<T> comparison)
            : this(InitialCapacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="comparer">Value comparer.</param>
        public MaxPriorityQueue(IComparer<T> comparer)
            : this(InitialCapacity, Enumerable.Empty<T>(), comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        public MaxPriorityQueue(int capacity)
            : this(capacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        /// <param name="comparison">Value comparison.</param>
        public MaxPriorityQueue(int capacity, Comparison<T> comparison)
            : this(capacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        /// <param name="comparer">Value comparer.</param>
        public MaxPriorityQueue(int capacity, IComparer<T> comparer)
            : this(capacity, Enumerable.Empty<T>(), comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="collection">The collection to add to the queue.</param>
        public MaxPriorityQueue(IEnumerable<T> collection)
            : this(InitialCapacity, collection, TypeExtensions.GetDefaultComparer<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="collection">The collection to add to the queue.</param>
        /// <param name="comparison">Value comparer.</param>
        public MaxPriorityQueue(IEnumerable<T> collection, Comparison<T> comparison)
            : this(InitialCapacity, collection, Comparer<T>.Create(comparison))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="collection">The collection to add to the queue.</param>
        /// <param name="comparer">Value comparer.</param>
        public MaxPriorityQueue(IEnumerable<T> collection, IComparer<T> comparer)
            : this(InitialCapacity, collection, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        /// <param name="collection">The collection to add to the queue.</param>
        /// <param name="comparer">Value comparer.</param>
        public MaxPriorityQueue(int capacity, IEnumerable<T> collection, IComparer<T> comparer)
        {
            Argument.GreaterThanOrEqualTo(capacity, 0, nameof(capacity));
            Argument.NotNull(collection, nameof(collection));
            Argument.NotNull(comparer, nameof(comparer));

            m_minPriorityQueue = new MinPriorityQueue<T>(capacity, collection, new ReverseComparer<T>(comparer));
        }

        /// <summary>
        /// The default initial capacity.
        /// </summary>
        private const int InitialCapacity = 1;

        private readonly MinPriorityQueue<T> m_minPriorityQueue;

        /// <inheritdoc/>
        public int Count => m_minPriorityQueue.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public bool IsSynchronized => false;

        /// <inheritdoc/>
        public object SyncRoot => this;

        /// <summary>
        /// Returns the largest item without removing it from the queue.
        /// </summary>
        /// <returns>Largest item in the queue.</returns>
        public T Peek()
        {
            return m_minPriorityQueue.Peek();
        }

        /// <inheritdoc/>
        public void Enqueue(T item)
        {
            m_minPriorityQueue.Enqueue(item);
        }

        /// <summary>
        /// Returns the largest item and removes it from the queue.
        /// </summary>
        /// <returns>The largest item in the queue.</returns>
        public T Dequeue()
        {
            return m_minPriorityQueue.Dequeue();
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            m_minPriorityQueue.Add(item);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            m_minPriorityQueue.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return m_minPriorityQueue.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            m_minPriorityQueue.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            return m_minPriorityQueue.Remove(item);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return m_minPriorityQueue.GetEnumerator();
        }

        /// <inheritdoc/>
        public void CopyTo(Array array, int index)
        {
            m_minPriorityQueue.CopyTo(array, index);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_minPriorityQueue.GetEnumerator();
        }
    }
}
