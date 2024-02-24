// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibSharp.Common;

namespace LibSharp.Collections
{
    /// <summary>
    /// A binary heap implementation of a minimum priority queue.
    /// This implementation is not thread-safe.
    /// </summary>
    /// <typeparam name="T">Comparable type of queue items.</typeparam>
    public class MinPriorityQueue<T> : IPriorityQueue<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        public MinPriorityQueue()
            : this(InitialCapacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="comparison">Value comparison.</param>
        public MinPriorityQueue(Comparison<T> comparison)
            : this(InitialCapacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="comparer">Value comparer.</param>
        public MinPriorityQueue(IComparer<T> comparer)
            : this(InitialCapacity, Enumerable.Empty<T>(), comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        public MinPriorityQueue(int capacity)
            : this(capacity, Enumerable.Empty<T>(), TypeExtensions.GetDefaultComparer<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        /// <param name="comparison">Value comparison.</param>
        public MinPriorityQueue(int capacity, Comparison<T> comparison)
            : this(capacity, Enumerable.Empty<T>(), Comparer<T>.Create(comparison))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        /// <param name="comparer">Value comparer.</param>
        public MinPriorityQueue(int capacity, IComparer<T> comparer)
            : this(capacity, Enumerable.Empty<T>(), comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="collection">The collection to add to the queue.</param>
        public MinPriorityQueue(IEnumerable<T> collection)
            : this(InitialCapacity, collection, TypeExtensions.GetDefaultComparer<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="collection">The collection to add to the queue.</param>
        /// <param name="comparison">Value comparer.</param>
        public MinPriorityQueue(IEnumerable<T> collection, Comparison<T> comparison)
            : this(InitialCapacity, collection, Comparer<T>.Create(comparison))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="collection">The collection to add to the queue.</param>
        /// <param name="comparer">Value comparer.</param>
        public MinPriorityQueue(IEnumerable<T> collection, IComparer<T> comparer)
            : this(InitialCapacity, collection, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinPriorityQueue{Item}"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        /// <param name="collection">The collection to add to the queue.</param>
        /// <param name="comparer">Value comparer.</param>
        public MinPriorityQueue(int capacity, IEnumerable<T> collection, IComparer<T> comparer)
        {
            Argument.GreaterThanOrEqualTo(capacity, 0, nameof(capacity));
            Argument.NotNull(collection, nameof(collection));
            Argument.NotNull(comparer, nameof(comparer));

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

            m_comparer = comparer;
            m_heap = new T[initialCapacity + 1];
            m_version = 0L;
            Count = 0;

            foreach (T item in collection)
            {
                Enqueue(item);
            }
        }

        /// <inheritdoc/>
        public int Count { get; private set; }

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public bool IsSynchronized => false;

        /// <inheritdoc/>
        public object SyncRoot => this;

        /// <summary>
        /// Returns the smallest item without removing it from the queue.
        /// </summary>
        /// <returns>Smallest item in the queue.</returns>
        public T Peek()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Cannot peek into an empty queue.");
            }

            return m_heap[1];
        }

        /// <inheritdoc/>
        public void Enqueue(T item)
        {
            ++m_version;

            Enlarge();

            m_heap[++Count] = item;
            Swim(Count);
        }

        /// <summary>
        /// Returns the smallest item and removes it from the queue.
        /// </summary>
        /// <returns>The smallest item in the queue.</returns>
        public T Dequeue()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Cannot dequeue from an empty queue.");
            }

            ++m_version;

            T min = m_heap[1];

            Exchange(1, Count--);
            Sink(1);

            m_heap[Count + 1] = default;
            Shrink();

            return min;
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
                ++m_version;

                m_heap = new T[InitialCapacity + 1];
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
            Argument.NotNull(array, nameof(array));
            Argument.GreaterThanOrEqualTo(arrayIndex, 0, nameof(arrayIndex));
            Argument.LessThanOrEqualTo(arrayIndex, array.Length, nameof(arrayIndex));
            Argument.GreaterThanOrEqualTo(array.Length - arrayIndex, Count, "Array offset");

            Array.Copy(m_heap, 1, array, arrayIndex, Count);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            int firstIndex = FirstIndexOf(item);

            if (firstIndex > 0)
            {
                ++m_version;

                Exchange(firstIndex, Count--);
                Sink(firstIndex);
                Swim(firstIndex);

                m_heap[Count + 1] = default;
                Shrink();

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return new MinPriorityQueueEnumerator<T>(this);
        }

        /// <inheritdoc/>
        public void CopyTo(Array array, int index)
        {
            Argument.NotNull(array, nameof(array));
            Argument.EqualTo(array.Rank, 1, nameof(array.Rank));
            Argument.GreaterThanOrEqualTo(index, 0, nameof(index));
            Argument.LessThanOrEqualTo(index, array.Length, nameof(index));
            Argument.GreaterThanOrEqualTo(array.Length - index, Count, "Array offset");

            Array.Copy(m_heap, 1, array, index, Count);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MinPriorityQueueEnumerator<T>(this);
        }

        /// <summary>
        /// Gets the index of the first occurrence of the item in the heap.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>Index of the item in the heap, -1 if it was not found.</returns>
        private int FirstIndexOf(T item)
        {
            for (int i = 1; i <= Count; ++i)
            {
                T currentItem = m_heap[i];
                if ((item is null && currentItem is null) || (item is not null && item.Equals(currentItem)))
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
            return m_comparer.Compare(m_heap[i], m_heap[j]) > 0;
        }

        /// <summary>
        /// Exchanges items at specified indexes in the heap.
        /// </summary>
        /// <param name="i">Index of the first item.</param>
        /// <param name="j">Index of the second item.</param>
        private void Exchange(int i, int j)
        {
            (m_heap[j], m_heap[i]) = (m_heap[i], m_heap[j]);
        }

        /// <summary>
        /// Enlarges the array under the heap.
        /// </summary>
        private void Enlarge()
        {
            if (Count == m_heap.Length - 1)
            {
                T[] largerPQ = new T[2 * m_heap.Length];

                for (int i = 1; i <= Count; ++i)
                {
                    largerPQ[i] = m_heap[i];
                    m_heap[i] = default;
                }

                m_heap = largerPQ;
            }
        }

        /// <summary>
        /// Shrinks the array under the heap.
        /// </summary>
        private void Shrink()
        {
            if (Count * 4 < m_heap.Length && m_heap.Length >= InitialCapacity * 2)
            {
                T[] smallerPQ = new T[m_heap.Length / 2];

                for (int i = 1; i <= Count; ++i)
                {
                    smallerPQ[i] = m_heap[i];
                    m_heap[i] = default;
                }

                m_heap = smallerPQ;
            }
        }

        /// <summary>
        /// The default initial capacity.
        /// </summary>
        private const int InitialCapacity = 1;

        /// <summary>
        /// The value comparer.
        /// </summary>
        private readonly IComparer<T> m_comparer;

        /// <summary>
        /// The binary heap organized as an array, indexing starts at 1.
        /// </summary>
        private T[] m_heap;

        /// <summary>
        /// Used to keep track of modifications by enumerators.
        /// </summary>
        private long m_version;

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
                m_version = queue.m_version;
                m_queue = queue;
                m_index = -1;
            }

            /// <inheritdoc/>
            public readonly TItem Current
            {
                get
                {
                    Validate();

                    if (m_index >= m_queue.Count)
                    {
                        throw new InvalidOperationException("Enumerator has enumerated all items and needs to be reset.");
                    }

                    return m_queue.m_heap[m_index + 1];
                }
            }

            /// <inheritdoc/>
            readonly object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                Validate();

                ++m_index;
                return m_index < m_queue.Count;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                Validate();

                m_index = -1;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                m_queue = null;
            }

            /// <summary>
            /// Ensures that the enumerator is in a valid state, e.g. it has not been disposed, and the collection has not been modified.
            /// </summary>
            private readonly void Validate()
            {
                if (m_queue == null)
                {
                    throw new ObjectDisposedException(nameof(MinPriorityQueueEnumerator<T>));
                }
                else if (m_version != m_queue.m_version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }
            }

            /// <summary>
            /// Queue version at the time the enumerator was created. The enumerator is valid only for that version.
            /// </summary>
            private readonly long m_version;

            /// <summary>
            /// Reference to the queue being enumerated.
            /// </summary>
            private MinPriorityQueue<TItem> m_queue;

            /// <summary>
            /// Current of the enumerator.
            /// </summary>
            private int m_index;
        }
    }
}
