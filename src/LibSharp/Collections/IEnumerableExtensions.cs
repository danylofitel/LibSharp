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
        /// Splits a sequence into batches of the given size.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the sequence.</typeparam>
        /// <param name="source">The sequence of elements to split.</param>
        /// <param name="batchSize">The maximum number of elements in a batch.</param>
        /// <returns>A sequence of batches.</returns>
        public static IEnumerable<IReadOnlyList<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int batchSize)
        {
            Argument.NotNull(source, nameof(source));
            Argument.GreaterThan(batchSize, 0, nameof(batchSize));

            return source.Batch(batchSize, double.MaxValue, item => 0.0);
        }

        /// <summary>
        /// Splits a sequence into batches.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the sequence.</typeparam>
        /// <param name="source">The sequence of elements to split.</param>
        /// <param name="batchSize">The maximum number of elements in a batch.</param>
        /// <param name="batchWeight">The maximum total weight of elements in a batch.</param>
        /// <param name="itemWeight">The item weight selector.</param>
        /// <returns>A sequence of batches.</returns>
        public static IEnumerable<List<TSource>> Batch<TSource>(
            this IEnumerable<TSource> source,
            int batchSize,
            double batchWeight,
            Func<TSource, double> itemWeight)
        {
            Argument.NotNull(source, nameof(source));
            Argument.GreaterThan(batchSize, 0, nameof(batchSize));
            Argument.GreaterThan(batchWeight, 0.0, nameof(batchWeight));
            Argument.NotNull(itemWeight, nameof(itemWeight));

            List<TSource> currentBatch = new List<TSource>(batchSize);
            double currentBatchWeight = 0.0;

            foreach (TSource item in source)
            {
                double currentItemWeight = itemWeight(item);

                if (currentItemWeight > batchWeight)
                {
                    throw new ArgumentException($"Weight of an item {currentItemWeight} exceeds maximum weight of a batch {batchWeight}");
                }

                if (currentBatch.Count == batchSize || currentBatchWeight + currentItemWeight > batchWeight)
                {
                    yield return currentBatch;
                    currentBatch = new List<TSource>(batchSize);
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
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The sequence of elements to shuffle.</param>
        /// <returns>A randomly shuffled sequence.</returns>
        public static IEnumerable<TSource> Shuffle<TSource>(this IEnumerable<TSource> source)
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
