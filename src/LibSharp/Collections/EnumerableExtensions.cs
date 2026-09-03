// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using System.Linq;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// Extension methods for IEnumerable.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Splits a sequence into chunks bounded by a total weight per chunk.
    /// </summary>
    /// <typeparam name="TSource">Type of the elements in the sequence.</typeparam>
    /// <param name="source">The sequence of elements to split.</param>
    /// <param name="chunkWeight">The maximum total weight of elements in a chunk.</param>
    /// <param name="itemWeight">The item weight selector.</param>
    /// <returns>A sequence of chunks.</returns>
    /// <remarks>
    /// This is a weight-based variant, distinct from the standard-library
    /// <see cref="System.Linq.Enumerable.Chunk{TSource}(System.Collections.Generic.IEnumerable{TSource}, int)"/>,
    /// which splits by a fixed element count. Use the standard-library overload for a fixed number of
    /// elements per chunk, and this one when each element contributes a variable weight.
    /// <para>
    /// Weights are compared using <c>double</c> arithmetic. Accumulated floating-point
    /// rounding errors may cause items whose combined weights are exactly equal to
    /// <paramref name="chunkWeight"/> to occasionally spill into a new chunk.
    /// Use weights with sufficient margin if exact budget boundaries are required.
    /// </para>
    /// </remarks>
    public static IEnumerable<List<TSource>> Chunk<TSource>(
        this IEnumerable<TSource> source,
        double chunkWeight,
        Func<TSource, double> itemWeight)
    {
        Argument.NotNull(source);
        Argument.GreaterThan(chunkWeight, 0.0);
        Argument.NotNull(itemWeight);

        return ChunkIterator(source, chunkWeight, itemWeight);
    }

    /// <summary>
    /// Returns index of the first element in the sequence that satisfies the condition.
    /// </summary>
    /// <typeparam name="TSource">The type of elements.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>Index of the first element in the sequence that satisfies the condition, -1 otherwise.</returns>
    public static int FirstIndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        Argument.NotNull(source);
        Argument.NotNull(predicate);

        int index = -1;

        foreach (TSource element in source)
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
    /// Returns index of the last element in the sequence that satisfies the condition.
    /// </summary>
    /// <typeparam name="TSource">The type of elements.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>Index of the last element in the sequence that satisfies the condition, -1 otherwise.</returns>
    public static int LastIndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        Argument.NotNull(source);
        Argument.NotNull(predicate);

        int index = -1;
        int match = -1;

        foreach (TSource element in source)
        {
            ++index;

            if (predicate(element))
            {
                match = index;
            }
        }

        return match;
    }

    /// <summary>
    /// Randomly shuffles the sequence using Fisher-Yates algorithm.
    /// Does not modify the original collection and returns a new array
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">The sequence of elements to shuffle.</param>
    /// <returns>A randomly shuffled array.</returns>
    public static TSource[] Shuffle<TSource>(this IEnumerable<TSource> source)
    {
        Argument.NotNull(source);

        TSource[] elements = source.ToArray();

        int count = elements.Length;
        while (count > 1)
        {
            --count;
            int k = Random.Shared.Next(count + 1);
            (elements[count], elements[k]) = (elements[k], elements[count]);
        }

        return elements;
    }

    private static IEnumerable<List<TSource>> ChunkIterator<TSource>(
        IEnumerable<TSource> source,
        double chunkWeight,
        Func<TSource, double> itemWeight)
    {
        List<TSource> currentBatch = new List<TSource>();
        double currentBatchWeight = 0.0;

        foreach (TSource item in source)
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

