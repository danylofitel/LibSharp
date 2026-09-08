// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections;
using System.Collections.Generic;

namespace LibSharp.UnitTests.Collections;

internal class NonGenericCollection<T> : IEnumerable<T>, ICollection
{
    public NonGenericCollection(List<T> collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public bool IsSynchronized => false;

    public object SyncRoot => this;

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public IEnumerator GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    private readonly List<T> _collection;
}
