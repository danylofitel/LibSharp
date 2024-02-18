// Copyright (c) LibSharp. All rights reserved.

using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections
{
    /// <summary>
    /// A reverse comparer.
    /// </summary>
    /// <typeparam name="TComparable">Comparable type.</typeparam>
    public class ReverseComparer<TComparable> : IComparer<TComparable>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReverseComparer{TComparable}"/> class.
        /// </summary>
        /// <param name="comparer">A comparer.</param>
        public ReverseComparer(IComparer<TComparable> comparer)
        {
            Argument.NotNull(comparer, nameof(comparer));

            m_comparer = comparer;
        }

        /// <inheritdoc/>
        public int Compare(TComparable x, TComparable y)
        {
            return m_comparer.Compare(y, x);
        }

        /// <summary>
        /// The comparer.
        /// </summary>
        private readonly IComparer<TComparable> m_comparer;
    }
}
