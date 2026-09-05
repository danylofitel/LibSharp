// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// Extension methods for IDictionary.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Adds the value to the dictionary if it does not exist, otherwise updates it.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="addValue">The value to add.</param>
    /// <param name="updateValueFactory">The factory providing an updated value from an existing value.</param>
    /// <returns>The new value in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/>, <paramref name="key"/>, or <paramref name="updateValueFactory"/> is <c>null</c>.</exception>
    public static TValue AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue addValue,
        Func<TKey, TValue, TValue> updateValueFactory)
    {
        Argument.NotNull(dictionary);
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Argument.NotNull(updateValueFactory);

        return dictionary.AddOrUpdate(
            key,
            keyValue => addValue,
            updateValueFactory);
    }

    /// <summary>
    /// Adds the value to the dictionary if it does not exist, otherwise updates it.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="addValueFactory">The factory providing a new value.</param>
    /// <param name="updateValueFactory">The factory providing an updated value from an existing value.</param>
    /// <returns>The new value in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="addValueFactory"/>, <paramref name="dictionary"/>, <paramref name="key"/>, or <paramref name="updateValueFactory"/> is <c>null</c>.</exception>
    public static TValue AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> addValueFactory,
        Func<TKey, TValue, TValue> updateValueFactory)
    {
        Argument.NotNull(dictionary);
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Argument.NotNull(addValueFactory);
        Argument.NotNull(updateValueFactory);

        TValue newValue;

        if (dictionary.TryGetValue(key, out TValue? oldValue))
        {
            newValue = updateValueFactory(key, oldValue);
        }
        else
        {
            newValue = addValueFactory(key);
        }

        dictionary[key] = newValue;
        return newValue;
    }

    /// <summary>
    /// Adds the value to the dictionary if it does not exist, otherwise updates it.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TArg">Argument type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="addValueFactory">The factory providing a new value.</param>
    /// <param name="updateValueFactory">The factory providing an updated value from an existing value.</param>
    /// <param name="factoryArgument">Additional argument that should be passed to the factories.</param>
    /// <returns>The new value in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="addValueFactory"/>, <paramref name="dictionary"/>, <paramref name="key"/>, or <paramref name="updateValueFactory"/> is <c>null</c>.</exception>
    public static TValue AddOrUpdate<TKey, TValue, TArg>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TArg, TValue> addValueFactory,
        Func<TKey, TValue, TArg, TValue> updateValueFactory,
        TArg factoryArgument)
    {
        Argument.NotNull(dictionary);
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Argument.NotNull(addValueFactory);
        Argument.NotNull(updateValueFactory);

        // The lookup is written out so that a call allocates no closure. That is the point of this
        // overload: it carries the caller's state to the factory as an argument instead.
        if (dictionary.TryGetValue(key, out TValue? oldValue))
        {
            TValue updatedValue = updateValueFactory(key, oldValue, factoryArgument);
            dictionary[key] = updatedValue;
            return updatedValue;
        }

        TValue addedValue = addValueFactory(key, factoryArgument);
        dictionary[key] = addedValue;
        return addedValue;
    }

    /// <summary>
    /// Gets the value from the dictionary if it exists, otherwise adds a new value.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value to add if it does not exist.</param>
    /// <returns>The new value in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="key"/> is <c>null</c>.</exception>
    public static TValue GetOrAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value)
    {
        Argument.NotNull(dictionary);
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return dictionary.GetOrAdd(key, keyValue => value);
    }

    /// <summary>
    /// Gets the value from the dictionary if it exists, otherwise adds a new value.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The factory providing a new value.</param>
    /// <returns>The new value in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/>, <paramref name="key"/>, or <paramref name="valueFactory"/> is <c>null</c>.</exception>
    public static TValue GetOrAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> valueFactory)
    {
        Argument.NotNull(dictionary);
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Argument.NotNull(valueFactory);

        if (dictionary.TryGetValue(key, out TValue? oldValue))
        {
            return oldValue;
        }
        else
        {
            TValue newValue = valueFactory(key);
            dictionary[key] = newValue;
            return newValue;
        }
    }

    /// <summary>
    /// Gets the value from the dictionary if it exists, otherwise adds a new value.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TArg">Argument type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The factory providing a new value.</param>
    /// <param name="factoryArgument">Additional argument that should be passed to the factory.</param>
    /// <returns>The new value in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/>, <paramref name="key"/>, or <paramref name="valueFactory"/> is <c>null</c>.</exception>
    public static TValue GetOrAdd<TKey, TValue, TArg>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TArg, TValue> valueFactory,
        TArg factoryArgument)
    {
        Argument.NotNull(dictionary);
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Argument.NotNull(valueFactory);

        // Written out for the same reason as AddOrUpdate above: no closure per call.
        if (dictionary.TryGetValue(key, out TValue? existingValue))
        {
            return existingValue;
        }

        TValue newValue = valueFactory(key, factoryArgument);
        dictionary[key] = newValue;
        return newValue;
    }

    /// <summary>
    /// Copies a dictionary.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="source">Source dictionary.</param>
    /// <returns>A copy of the source dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The copy is always a <see cref="Dictionary{TKey, TValue}"/>, whatever the source was.
    /// <para>
    /// The source's equality comparer is preserved only when the source is itself a
    /// <see cref="Dictionary{TKey, TValue}"/>, which is the only implementation in the framework that
    /// exposes one. Copying any other <see cref="IDictionary{TKey, TValue}"/> — a
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>, an immutable
    /// dictionary, or a custom type — produces a copy that uses default equality. A source built with
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> would then yield a copy that resolves lookups
    /// case-sensitively, which is a silent change in behaviour rather than an error. Where the
    /// comparer matters and the source is not a <see cref="Dictionary{TKey, TValue}"/>, construct the
    /// destination yourself and use <see cref="CopyTo{TKey, TValue}"/> instead.
    /// </para>
    /// <para>
    /// Ordering is not carried over either: copying a <see cref="SortedDictionary{TKey, TValue}"/> or
    /// a <see cref="SortedList{TKey, TValue}"/> gives back an unordered dictionary.
    /// </para>
    /// </remarks>
    public static IDictionary<TKey, TValue> Copy<TKey, TValue>(this IDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        Argument.NotNull(source);

        // Carry the comparer across where it can be recovered, so the copy resolves lookups the way
        // the source does. IDictionary<,> exposes no comparer, so only a Dictionary can be asked.
        IEqualityComparer<TKey>? comparer = (source as Dictionary<TKey, TValue>)?.Comparer;

        return source.CopyTo(new Dictionary<TKey, TValue>(source.Count, comparer));
    }

    /// <summary>
    /// Copies all entries from the source dictionary to the destination dictionary.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="source">Source dictionary.</param>
    /// <param name="destination">Destination dictionary.</param>
    /// <returns>Destination dictionary with properties copied from the source dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> or <paramref name="source"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The caller supplies the destination, so its comparer and ordering are whatever the caller
    /// chose. This is the overload to reach for when <see cref="Copy{TKey, TValue}"/> would not
    /// carry the source's comparer across.
    /// </remarks>
    public static IDictionary<TKey, TValue> CopyTo<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> destination)
    {
        Argument.NotNull(source);
        Argument.NotNull(destination);

        foreach (KeyValuePair<TKey, TValue> pair in source)
        {
            destination[pair.Key] = pair.Value;
        }

        return destination;
    }
}
