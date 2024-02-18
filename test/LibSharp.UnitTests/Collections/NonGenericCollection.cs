// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;

namespace LibSharp.Collections.UnitTests
{
    internal class NonGenericCollection<T> : IEnumerable<T>, ICollection
    {
        public NonGenericCollection(List<T> collection)
        {
            m_collection = collection;
        }

        public int Count => m_collection.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return m_collection.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_collection.GetEnumerator();
        }

        private readonly List<T> m_collection;
    }
}
