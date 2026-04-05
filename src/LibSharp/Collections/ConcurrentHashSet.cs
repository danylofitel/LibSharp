// Copyright (c) 2026 Danylo Fitel

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// A thread-safe hash set implemented as a wrapper around <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
/// <remarks>
/// Individual element additions and removals are thread-safe. Compound set operations such as
/// <see cref="IntersectWith"/>, <see cref="ExceptWith"/>, and <see cref="SymmetricExceptWith"/>
/// are not atomic at the collection level: other threads may observe the collection in a
/// partially-modified state while one of these operations is in progress.
/// </remarks>
/// <typeparam name="T">Element type.</typeparam>
public sealed class ConcurrentHashSet<T> : ISet<T>, IReadOnlySet<T>
{
    /// <summary>
    /// Initializes a new empty instance of <see cref="ConcurrentHashSet{T}"/> using the default equality comparer.
    /// </summary>
    public ConcurrentHashSet()
    {
        m_comparer = EqualityComparer<T>.Default;
        m_dictionary = new ConcurrentDictionary<T, byte>(m_comparer);
    }

    /// <summary>
    /// Initializes a new empty instance of <see cref="ConcurrentHashSet{T}"/> using the specified equality comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use when comparing elements.</param>
    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        Argument.NotNull(comparer, nameof(comparer));

        m_comparer = comparer;
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

        m_comparer = EqualityComparer<T>.Default;
        m_dictionary = new ConcurrentDictionary<T, byte>(m_comparer);
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

        m_comparer = comparer;
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

    /// <summary>
    /// Adds all elements from <paramref name="other"/> that are not already in the set.
    /// </summary>
    /// <param name="other">The collection of elements to add to the set.</param>
    public void UnionWith(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        foreach (T item in other)
        {
            _ = m_dictionary.TryAdd(item, 0);
        }
    }

    /// <summary>
    /// Removes all elements from the set that are not also present in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The collection that defines which elements to retain.</param>
    public void IntersectWith(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        HashSet<T> otherSet = new HashSet<T>(other, m_comparer);
        foreach (T key in m_dictionary.Keys)
        {
            if (!otherSet.Contains(key))
            {
                _ = m_dictionary.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Removes all elements from the set that are also present in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The collection of elements to remove from the set.</param>
    public void ExceptWith(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        foreach (T item in other)
        {
            _ = m_dictionary.TryRemove(item, out _);
        }
    }

    /// <summary>
    /// Modifies the set so that it contains only elements present in the set or in
    /// <paramref name="other"/>, but not in both. Duplicate elements in
    /// <paramref name="other"/> are ignored.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        // Deduplicate other first so that each element is toggled exactly once.
        HashSet<T> otherSet = new HashSet<T>(other, m_comparer);
        foreach (T item in otherSet)
        {
            if (!m_dictionary.TryRemove(item, out _))
            {
                _ = m_dictionary.TryAdd(item, 0);
            }
        }
    }

    /// <summary>
    /// Determines whether the set is a subset of <paramref name="other"/>,
    /// i.e. every element in the set is also in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns><c>true</c> if the set is a subset of <paramref name="other"/>; otherwise <c>false</c>.</returns>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        if (Count == 0)
        {
            return true;
        }

        HashSet<T> otherSet = new HashSet<T>(other, m_comparer);
        foreach (T key in m_dictionary.Keys)
        {
            if (!otherSet.Contains(key))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the set is a superset of <paramref name="other"/>,
    /// i.e. every element in <paramref name="other"/> is also in the set.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns><c>true</c> if the set is a superset of <paramref name="other"/>; otherwise <c>false</c>.</returns>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        foreach (T item in other)
        {
            if (!m_dictionary.ContainsKey(item))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the set is a proper subset of <paramref name="other"/>,
    /// i.e. it is a subset and <paramref name="other"/> contains at least one element not in the set.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns><c>true</c> if the set is a proper subset of <paramref name="other"/>; otherwise <c>false</c>.</returns>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        HashSet<T> otherSet = new HashSet<T>(other, m_comparer);
        if (Count >= otherSet.Count)
        {
            return false;
        }

        foreach (T key in m_dictionary.Keys)
        {
            if (!otherSet.Contains(key))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the set is a proper superset of <paramref name="other"/>,
    /// i.e. it is a superset and the set contains at least one element not in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns><c>true</c> if the set is a proper superset of <paramref name="other"/>; otherwise <c>false</c>.</returns>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        HashSet<T> otherSet = new HashSet<T>(other, m_comparer);
        if (Count <= otherSet.Count)
        {
            return false;
        }

        foreach (T item in otherSet)
        {
            if (!m_dictionary.ContainsKey(item))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the set and <paramref name="other"/> share at least one common element.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns><c>true</c> if the set and <paramref name="other"/> share at least one element; otherwise <c>false</c>.</returns>
    public bool Overlaps(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        foreach (T item in other)
        {
            if (m_dictionary.ContainsKey(item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the set and <paramref name="other"/> contain exactly the same elements.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns><c>true</c> if the set equals <paramref name="other"/>; otherwise <c>false</c>.</returns>
    public bool SetEquals(IEnumerable<T> other)
    {
        Argument.NotNull(other, nameof(other));

        HashSet<T> otherSet = new HashSet<T>(other, m_comparer);
        if (Count != otherSet.Count)
        {
            return false;
        }

        foreach (T key in m_dictionary.Keys)
        {
            if (!otherSet.Contains(key))
            {
                return false;
            }
        }

        return true;
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

    private readonly IEqualityComparer<T> m_comparer;
    private readonly ConcurrentDictionary<T, byte> m_dictionary;
}
