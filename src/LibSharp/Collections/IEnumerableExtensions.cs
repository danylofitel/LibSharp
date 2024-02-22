// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using LibSharp.Common;

namespace LibSharp.Collections
{
    /// <summary>
    /// Extension methods for IEnumerable.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Splits a sequence into chunks.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the sequence.</typeparam>
        /// <param name="source">The sequence of elements to split.</param>
        /// <param name="chunkWeight">The maximum total weight of elements in a chunk.</param>
        /// <param name="itemWeight">The item weight selector.</param>
        /// <returns>A sequence of chunks.</returns>
        public static IEnumerable<List<TSource>> Chunk<TSource>(
            this IEnumerable<TSource> source,
            double chunkWeight,
            Func<TSource, double> itemWeight)
        {
            Argument.NotNull(source, nameof(source));
            Argument.GreaterThan(chunkWeight, 0.0, nameof(chunkWeight));
            Argument.NotNull(itemWeight, nameof(itemWeight));

            List<TSource> currentBatch = new List<TSource>();
            double currentBatchWeight = 0.0;

            foreach (TSource item in source)
            {
                double currentItemWeight = itemWeight(item);

                if (currentItemWeight > chunkWeight)
                {
                    throw new ArgumentException($"Weight of an item {currentItemWeight} exceeds maximum chunk weight {chunkWeight}");
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

        /// <summary>
        /// Returns index of the first element in the sequence that satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of elements.</typeparam>
        /// <param name="source">The sequence of elements.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>Index of the first element in the sequence that satisfies the condition, -1 otherwise.</returns>
        public static int FirstIndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            Argument.NotNull(source, nameof(source));
            Argument.NotNull(predicate, nameof(predicate));

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
            Argument.NotNull(source, nameof(source));
            Argument.NotNull(predicate, nameof(predicate));

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
            Argument.NotNull(source, nameof(source));

            Random generator = new Random();
            TSource[] elements = source.ToArray();

            int count = elements.Length;
            while (count > 1)
            {
                --count;
                int k = generator.Next(count + 1);
                (elements[count], elements[k]) = (elements[k], elements[count]);
            }

            return elements;
        }
    }
}
