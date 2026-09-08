// Copyright (c) 2026 Danylo Fitel

using System.Collections;
using System.Collections.Generic;

namespace LibSharp.UnitTests.Collections;

internal class GenericCollection<T> : ICollection<T>
{
    public GenericCollection(List<T> collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        _collection.Add(item);
    }

    public void Clear()
    {
        _collection.Clear();
    }

    public bool Contains(T item)
    {
        return _collection.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _collection.CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    public bool Remove(T item)
    {
        return _collection.Remove(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    private readonly List<T> _collection;
}
