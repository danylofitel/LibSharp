// Copyright (c) LibSharp. All rights reserved.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// A thread-safe hash set implemented as a wrapper around <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public sealed class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
{
    /// <summary>
    /// Initializes a new empty instance of <see cref="ConcurrentHashSet{T}"/> using the default equality comparer.
    /// </summary>
    public ConcurrentHashSet()
    {
        m_dictionary = new ConcurrentDictionary<T, byte>();
    }

    /// <summary>
    /// Initializes a new empty instance of <see cref="ConcurrentHashSet{T}"/> using the specified equality comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use when comparing elements.</param>
    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        Argument.NotNull(comparer, nameof(comparer));

        m_dictionary = new ConcurrentDictionary<T, byte>(comparer);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConcurrentHashSet{T}"/> that contains elements copied
    /// from the specified collection, using the default equality comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied into the set.</param>
    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        Argument.NotNull(collection, nameof(collection));

        m_dictionary = new ConcurrentDictionary<T, byte>();
        foreach (T item in collection)
        {
            _ = m_dictionary.TryAdd(item, 0);
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConcurrentHashSet{T}"/> that contains elements copied
    /// from the specified collection, using the specified equality comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied into the set.</param>
    /// <param name="comparer">The equality comparer to use when comparing elements.</param>
    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        Argument.NotNull(collection, nameof(collection));
        Argument.NotNull(comparer, nameof(comparer));

        m_dictionary = new ConcurrentDictionary<T, byte>(comparer);
        foreach (T item in collection)
        {
            _ = m_dictionary.TryAdd(item, 0);
        }
    }

    /// <inheritdoc/>
    public int Count => m_dictionary.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <summary>
    /// Attempts to add the specified element to the set.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <returns><c>true</c> if the element was added; <c>false</c> if it was already present.</returns>
    public bool Add(T item)
    {
        return m_dictionary.TryAdd(item, 0);
    }

    /// <inheritdoc/>
    void ICollection<T>.Add(T item)
    {
        _ = m_dictionary.TryAdd(item, 0);
    }

    /// <summary>
    /// Attempts to remove the specified element from the set.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns><c>true</c> if the element was removed; <c>false</c> if it was not present.</returns>
    public bool Remove(T item)
    {
        return m_dictionary.TryRemove(item, out _);
    }

    /// <summary>
    /// Determines whether the set contains the specified element.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns><c>true</c> if the element is in the set; otherwise <c>false</c>.</returns>
    public bool Contains(T item)
    {
        return m_dictionary.ContainsKey(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        m_dictionary.Clear();
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        Argument.NotNull(array, nameof(array));
        Argument.GreaterThanOrEqualTo(arrayIndex, 0, nameof(arrayIndex));

        ((ICollection<T>)m_dictionary.Keys).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        return m_dictionary.Keys.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private readonly ConcurrentDictionary<T, byte> m_dictionary;
}
