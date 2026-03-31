// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// Extension methods for IAsyncEnumerable.
/// </summary>
public static class IAsyncEnumerableExtensions
{
    /// <summary>
    /// Returns the index of the first element in the async sequence that satisfies the condition.
    /// </summary>
    /// <typeparam name="TSource">The type of elements.</typeparam>
    /// <param name="source">The async sequence of elements.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index of the first matching element, or -1 if none match.</returns>
    public static async Task<int> FirstIndexOfAsync<TSource>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        Argument.NotNull(source, nameof(source));
        Argument.NotNull(predicate, nameof(predicate));

        int index = -1;

        await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            ++index;

            if (predicate(element))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of the last element in the async sequence that satisfies the condition.
    /// </summary>
    /// <typeparam name="TSource">The type of elements.</typeparam>
    /// <param name="source">The async sequence of elements.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index of the last matching element, or -1 if none match.</returns>
    public static async Task<int> LastIndexOfAsync<TSource>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        Argument.NotNull(source, nameof(source));
        Argument.NotNull(predicate, nameof(predicate));

        int index = -1;
        int match = -1;

        await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            ++index;

            if (predicate(element))
            {
                match = index;
            }
        }

        return match;
    }
}
