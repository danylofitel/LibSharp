// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// Splits an async sequence into chunks.
    /// </summary>
    /// <typeparam name="TSource">Type of the elements in the sequence.</typeparam>
    /// <param name="source">The async sequence of elements to split.</param>
    /// <param name="chunkWeight">The maximum total weight of elements in a chunk.</param>
    /// <param name="itemWeight">The item weight selector.</param>
    /// <returns>A sequence of chunks.</returns>
    /// <remarks>
    /// Weights are compared using <c>double</c> arithmetic. Accumulated floating-point
    /// rounding errors may cause items whose combined weights are exactly equal to
    /// <paramref name="chunkWeight"/> to occasionally spill into a new chunk.
    /// Use weights with sufficient margin if exact budget boundaries are required.
    /// </remarks>
    public static IAsyncEnumerable<List<TSource>> Chunk<TSource>(
        this IAsyncEnumerable<TSource> source,
        double chunkWeight,
        Func<TSource, double> itemWeight)
    {
        Argument.NotNull(source, nameof(source));
        Argument.GreaterThan(chunkWeight, 0.0, nameof(chunkWeight));
        Argument.NotNull(itemWeight, nameof(itemWeight));

        return ChunkIterator(source, chunkWeight, itemWeight);
    }

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

    private static async IAsyncEnumerable<List<TSource>> ChunkIterator<TSource>(
        IAsyncEnumerable<TSource> source,
        double chunkWeight,
        Func<TSource, double> itemWeight,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<TSource> currentBatch = new List<TSource>();
        double currentBatchWeight = 0.0;

        await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            double currentItemWeight = itemWeight(item);

            if (currentItemWeight < 0.0)
            {
                throw new ArgumentException($"Weight of an item {currentItemWeight} must not be negative.", nameof(itemWeight));
            }

            if (currentItemWeight > chunkWeight)
            {
                throw new ArgumentException($"Weight of an item {currentItemWeight} exceeds maximum chunk weight {chunkWeight}.", nameof(itemWeight));
            }

            if (currentBatchWeight + currentItemWeight > chunkWeight)
            {
                yield return currentBatch;
                currentBatch = new List<TSource>();
                currentBatchWeight = 0.0;
            }

            currentBatch.Add(item);
            currentBatchWeight += currentItemWeight;
        }

        if (currentBatch.Count > 0)
        {
            yield return currentBatch;
        }
    }
}
