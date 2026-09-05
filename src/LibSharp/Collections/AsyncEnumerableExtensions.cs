// Copyright (c) 2026 Danylo Fitel

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
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Splits an async sequence into chunks bounded by a total weight per chunk.
    /// </summary>
    /// <typeparam name="TSource">Type of the elements in the sequence.</typeparam>
    /// <param name="source">The async sequence of elements to split.</param>
    /// <param name="chunkWeight">The maximum total weight of elements in a chunk.</param>
    /// <param name="itemWeight">The item weight selector.</param>
    /// <returns>A sequence of chunks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemWeight"/> or <paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="chunkWeight"/> is outside the permitted range.</exception>
    /// <remarks>
    /// This is a weight-based variant, distinct from a fixed-element-count chunking such as the
    /// standard-library <c>AsyncEnumerable.Chunk(source, size)</c> (available on .NET 10+). Use a
    /// fixed-count chunk for a fixed number of elements per chunk, and this one when each element
    /// contributes a variable weight.
    /// <para>
    /// Weights are compared using <c>double</c> arithmetic. Accumulated floating-point
    /// rounding errors may cause items whose combined weights are exactly equal to
    /// <paramref name="chunkWeight"/> to occasionally spill into a new chunk.
    /// Use weights with sufficient margin if exact budget boundaries are required.
    /// </para>
    /// </remarks>
    public static IAsyncEnumerable<List<TSource>> Chunk<TSource>(
        this IAsyncEnumerable<TSource> source,
        double chunkWeight,
        Func<TSource, double> itemWeight)
    {
        Argument.NotNull(source);
        Argument.GreaterThan(chunkWeight, 0.0);
        Argument.NotNull(itemWeight);

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="source"/> is <c>null</c>.</exception>
    public static async Task<int> FirstIndexOfAsync<TSource>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        Argument.NotNull(source);
        Argument.NotNull(predicate);

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="source"/> is <c>null</c>.</exception>
    public static async Task<int> LastIndexOfAsync<TSource>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        Argument.NotNull(source);
        Argument.NotNull(predicate);

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

            // NaN passes every comparison below - NaN < 0 and NaN > chunkWeight are both false - and
            // then poisons the running total, after which no comparison against the budget is ever
            // true again and chunking silently stops happening. Reject it up front.
            if (!double.IsFinite(currentItemWeight))
            {
                throw new ArgumentException($"Weight of an item must be a finite number, but was {currentItemWeight}.", nameof(itemWeight));
            }

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
