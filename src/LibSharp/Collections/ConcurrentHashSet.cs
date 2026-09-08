// Copyright (c) 2026 Danylo Fitel

using System;
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
    where T : notnull
{
    /// <summary>
    /// Initializes a new empty instance of <see cref="ConcurrentHashSet{T}"/> using the default equality comparer.
    /// </summary>
    public ConcurrentHashSet()
    {
        _comparer = EqualityComparer<T>.Default;
        _dictionary = new ConcurrentDictionary<T, byte>(_comparer);
    }

    /// <summary>
    /// Initializes a new empty instance of <see cref="ConcurrentHashSet{T}"/> using the specified equality comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use when comparing elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is <c>null</c>.</exception>
    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        Argument.NotNull(comparer);

        _comparer = comparer;
        _dictionary = new ConcurrentDictionary<T, byte>(comparer);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConcurrentHashSet{T}"/> that contains elements copied
    /// from the specified collection, using the default equality comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied into the set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <c>null</c>.</exception>
    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        Argument.NotNull(collection);

        _comparer = EqualityComparer<T>.Default;
        _dictionary = new ConcurrentDictionary<T, byte>(_comparer);
        foreach (T item in collection)
        {
            _ = _dictionary.TryAdd(item, 0);
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConcurrentHashSet{T}"/> that contains elements copied
    /// from the specified collection, using the specified equality comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied into the set.</param>
    /// <param name="comparer">The equality comparer to use when comparing elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="comparer"/> is <c>null</c>.</exception>
    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        Argument.NotNull(collection);
        Argument.NotNull(comparer);

        _comparer = comparer;
        _dictionary = new ConcurrentDictionary<T, byte>(comparer);
        foreach (T item in collection)
        {
            _ = _dictionary.TryAdd(item, 0);
        }
    }

    /// <inheritdoc/>
    public int Count => _dictionary.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <summary>
    /// Attempts to add the specified element to the set.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <returns><c>true</c> if the element was added; <c>false</c> if it was already present.</returns>
    public bool Add(T item)
    {
        return _dictionary.TryAdd(item, 0);
    }

    /// <inheritdoc/>
    void ICollection<T>.Add(T item)
    {
        _ = _dictionary.TryAdd(item, 0);
    }

    /// <summary>
    /// Attempts to remove the specified element from the set.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns><c>true</c> if the element was removed; <c>false</c> if it was not present.</returns>
    public bool Remove(T item)
    {
        return _dictionary.TryRemove(item, out _);
    }

    /// <summary>
    /// Determines whether the set contains the specified element.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns><c>true</c> if the element is in the set; otherwise <c>false</c>.</returns>
    public bool Contains(T item)
    {
        return _dictionary.ContainsKey(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is outside the permitted range.</exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        Argument.NotNull(array);
        Argument.GreaterThanOrEqualTo(arrayIndex, 0);

        ((ICollection<T>)_dictionary.Keys).CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Adds all elements from <paramref name="other"/> that are not already in the set.
    /// </summary>
    /// <param name="other">The collection of elements to add to the set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public void UnionWith(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        foreach (T item in other)
        {
            _ = _dictionary.TryAdd(item, 0);
        }
    }

    /// <summary>
    /// Removes all elements from the set that are not also present in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The collection that defines which elements to retain.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public void IntersectWith(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        HashSet<T> otherSet = new HashSet<T>(other, _comparer);
        foreach (KeyValuePair<T, byte> entry in _dictionary)
        {
            if (!otherSet.Contains(entry.Key))
            {
                _ = _dictionary.TryRemove(entry.Key, out _);
            }
        }
    }

    /// <summary>
    /// Removes all elements from the set that are also present in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The collection of elements to remove from the set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public void ExceptWith(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        foreach (T item in other)
        {
            _ = _dictionary.TryRemove(item, out _);
        }
    }

    /// <summary>
    /// Modifies the set so that it contains only elements present in the set or in
    /// <paramref name="other"/>, but not in both. Duplicate elements in
    /// <paramref name="other"/> are ignored.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        // Deduplicate other first so that each element is toggled exactly once.
        HashSet<T> otherSet = new HashSet<T>(other, _comparer);
        foreach (T item in otherSet)
        {
            if (!_dictionary.TryRemove(item, out _))
            {
                _ = _dictionary.TryAdd(item, 0);
            }
        }
    }

    /// <summary>
    /// Determines whether the set is a subset of <paramref name="other"/>,
    /// i.e. every element in the set is also in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns><c>true</c> if the set is a subset of <paramref name="other"/>; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        if (Count == 0)
        {
            return true;
        }

        HashSet<T> otherSet = new HashSet<T>(other, _comparer);
        foreach (KeyValuePair<T, byte> entry in _dictionary)
        {
            if (!otherSet.Contains(entry.Key))
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        foreach (T item in other)
        {
            if (!_dictionary.ContainsKey(item))
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        HashSet<T> otherSet = new HashSet<T>(other, _comparer);
        if (Count >= otherSet.Count)
        {
            return false;
        }

        foreach (KeyValuePair<T, byte> entry in _dictionary)
        {
            if (!otherSet.Contains(entry.Key))
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        HashSet<T> otherSet = new HashSet<T>(other, _comparer);
        if (Count <= otherSet.Count)
        {
            return false;
        }

        foreach (T item in otherSet)
        {
            if (!_dictionary.ContainsKey(item))
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public bool Overlaps(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        foreach (T item in other)
        {
            if (_dictionary.ContainsKey(item))
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <c>null</c>.</exception>
    public bool SetEquals(IEnumerable<T> other)
    {
        Argument.NotNull(other);

        HashSet<T> otherSet = new HashSet<T>(other, _comparer);
        if (Count != otherSet.Count)
        {
            return false;
        }

        foreach (KeyValuePair<T, byte> entry in _dictionary)
        {
            if (!otherSet.Contains(entry.Key))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns an enumerator over the elements of the set.
    /// </summary>
    /// <returns>An enumerator over the elements.</returns>
    /// <remarks>
    /// Enumerates the underlying dictionary directly, which is lock-free.
    /// This is a live view rather than a snapshot: elements added or removed
    /// after enumeration begins may or may not be observed, exactly as
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> behaves.
    /// </remarks>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (KeyValuePair<T, byte> entry in _dictionary)
        {
            yield return entry.Key;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private readonly IEqualityComparer<T> _comparer;
    private readonly ConcurrentDictionary<T, byte> _dictionary;
}
