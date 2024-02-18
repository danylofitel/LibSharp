// Copyright (c) LibSharp. All rights reserved.

using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections
{
    /// <summary>
    /// Extension methods for ICollection.
    /// </summary>
    public static class ICollectionExtensions
    {
        /// <summary>
        /// Adds a collection of elements to the set.
        /// </summary>
        /// <typeparam name="TSource">Type of elements in the set.</typeparam>
        /// <param name="source">The set to add elements to.</param>
        /// <param name="collection">The collection of elements that should be added to the set.</param>
        public static void AddRange<TSource>(this ICollection<TSource> source, IEnumerable<TSource> collection)
        {
            Argument.NotNull(source, nameof(source));
            Argument.NotNull(collection, nameof(collection));

            foreach (TSource item in collection)
            {
                source.Add(item);
            }
        }
    }
}
