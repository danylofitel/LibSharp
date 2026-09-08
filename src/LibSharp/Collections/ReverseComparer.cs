// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections;

/// <summary>
/// A reverse comparer.
/// </summary>
/// <typeparam name="TComparable">Comparable type.</typeparam>
public sealed class ReverseComparer<TComparable> : IComparer<TComparable>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReverseComparer{TComparable}"/> class.
    /// </summary>
    /// <param name="comparer">A comparer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is <c>null</c>.</exception>
    public ReverseComparer(IComparer<TComparable> comparer)
    {
        Argument.NotNull(comparer);

        _comparer = comparer;
    }

    /// <inheritdoc/>
    public int Compare(TComparable? x, TComparable? y)
    {
        return _comparer.Compare(y, x);
    }

    private readonly IComparer<TComparable> _comparer;
}
