// Copyright (c) LibSharp. All rights reserved.

using System.Collections;
using System.Collections.Generic;

namespace LibSharp.Collections.UnitTests
{
    internal class GenericCollection<T> : ICollection<T>
    {
        public GenericCollection(List<T> collection)
        {
            m_collection = collection;
        }

        public int Count => m_collection.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            m_collection.Add(item);
        }

        public void Clear()
        {
            m_collection.Clear();
        }

        public bool Contains(T item)
        {
            return m_collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_collection.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_collection.GetEnumerator();
        }

        public bool Remove(T item)
        {
            return m_collection.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_collection.GetEnumerator();
        }

        private readonly List<T> m_collection;
    }
}
