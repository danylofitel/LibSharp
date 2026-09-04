// Copyright (c) 2026 Danylo Fitel

using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// Extension methods for ICollection.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Adds a collection of elements to the set.
    /// </summary>
    /// <typeparam name="TSource">Type of elements in the set.</typeparam>
    /// <param name="source">The set to add elements to.</param>
    /// <param name="collection">The collection of elements that should be added to the set.</param>
    /// <remarks>
    /// Defers to <see cref="List{T}.AddRange"/> when the target is a <see cref="List{T}"/>, which
    /// grows the backing array once from the source's count rather than repeatedly as items arrive.
    /// </remarks>
    public static void AddRange<TSource>(this ICollection<TSource> source, IEnumerable<TSource> collection)
    {
        Argument.NotNull(source);
        Argument.NotNull(collection);

        if (source is List<TSource> list)
        {
            list.AddRange(collection);
            return;
        }

        foreach (TSource item in collection)
        {
            source.Add(item);
        }
    }
}
