// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.Collections.UnitTests
{
    [TestClass]
    public class MaxPriorityQueueUnitTests
    {
        [TestMethod]
        public void Constructor_NoArguments()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Assert
            queue.Enqueue(2);
            queue.Enqueue(1);
            queue.Enqueue(3);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NoArguments_NonComparableType_Throws()
        {
            // Act
            _ = new MaxPriorityQueue<object>();
        }

        [TestMethod]
        public void Constructor_FromComparison()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>((a, b) => b.CompareTo(a));

            // Assert
            queue.Enqueue(2);
            queue.Enqueue(1);
            queue.Enqueue(3);

            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
        }

        [TestMethod]
        public void Constructor_FromComparison_NonComparableClass()
        {
            // Act
            MaxPriorityQueue<WrapperClass> queue = new MaxPriorityQueue<WrapperClass>((a, b) => b.Value.CompareTo(a.Value));

            // Assert
            queue.Enqueue(new WrapperClass { Value = 2 });
            queue.Enqueue(new WrapperClass { Value = 1 });
            queue.Enqueue(new WrapperClass { Value = 3 });

            Assert.AreEqual(1, queue.Dequeue().Value);
            Assert.AreEqual(2, queue.Dequeue().Value);
            Assert.AreEqual(3, queue.Dequeue().Value);
        }

        [TestMethod]
        public void Constructor_FromComparison_NonComparableStruct()
        {
            // Act
            MaxPriorityQueue<WrapperStruct> queue = new MaxPriorityQueue<WrapperStruct>((a, b) => b.Value.CompareTo(a.Value));

            // Assert
            queue.Enqueue(new WrapperStruct { Value = 2 });
            queue.Enqueue(new WrapperStruct { Value = 1 });
            queue.Enqueue(new WrapperStruct { Value = 3 });

            Assert.AreEqual(1, queue.Dequeue().Value);
            Assert.AreEqual(2, queue.Dequeue().Value);
            Assert.AreEqual(3, queue.Dequeue().Value);
        }

        [TestMethod]
        public void Constructor_FromComparer()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Comparer<int>.Default);

            // Assert
            queue.Enqueue(2);
            queue.Enqueue(1);
            queue.Enqueue(3);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());
        }

        [TestMethod]
        public void Constructor_FromComparer_NonComparableClass()
        {
            // Act
            MaxPriorityQueue<WrapperClass> queue = new MaxPriorityQueue<WrapperClass>(new WrapperClassComparer());

            // Assert
            queue.Enqueue(new WrapperClass { Value = 2 });
            queue.Enqueue(new WrapperClass { Value = 1 });
            queue.Enqueue(new WrapperClass { Value = 3 });

            Assert.AreEqual(3, queue.Dequeue().Value);
            Assert.AreEqual(2, queue.Dequeue().Value);
            Assert.AreEqual(1, queue.Dequeue().Value);
        }

        [TestMethod]
        public void Constructor_FromComparer_NonComparableStruct()
        {
            // Act
            MaxPriorityQueue<WrapperStruct> queue = new MaxPriorityQueue<WrapperStruct>(new WrapperStructComparer());

            // Assert
            queue.Enqueue(new WrapperStruct { Value = 2 });
            queue.Enqueue(new WrapperStruct { Value = 1 });
            queue.Enqueue(new WrapperStruct { Value = 3 });

            Assert.AreEqual(3, queue.Dequeue().Value);
            Assert.AreEqual(2, queue.Dequeue().Value);
            Assert.AreEqual(1, queue.Dequeue().Value);
        }

        [TestMethod]
        public void Constructor_FromCollection()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(new[] { 2, 1, 3 });

            // Assert
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_FromCollection_NonComparableType_Throws()
        {
            // Act
            _ = new MaxPriorityQueue<object>(new[] { new object(), new object(), new object() });
        }

        [TestMethod]
        public void Constructor_FromCollectionAndComparison()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(new[] { 2, 1, 3 }, (a, b) => b.CompareTo(a));

            // Assert
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
        }

        [TestMethod]
        public void Constructor_FromCollectionAndComparer()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(new[] { 2, 1, 3 }, Comparer<int>.Default);

            // Assert
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());
        }

        [TestMethod]
        public void Constructor_FromCapacity()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(10);

            // Assert
            Assert.AreEqual(0, queue.Count);

            queue.Enqueue(2);
            queue.Enqueue(1);
            queue.Enqueue(3);

            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Constructor_FromCapacityAndComparison()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(10, (a, b) => b.CompareTo(a));

            // Assert
            Assert.AreEqual(0, queue.Count);

            queue.Enqueue(2);
            queue.Enqueue(1);
            queue.Enqueue(3);

            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Constructor_FromCapacityAndComparer()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(10, Comparer<int>.Default);

            // Assert
            Assert.AreEqual(0, queue.Count);

            queue.Enqueue(2);
            queue.Enqueue(1);
            queue.Enqueue(3);

            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Constructor_FromCapacityAndEnumerableAndComparer()
        {
            // Act
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(10, Enumerable.Range(1, 3), Comparer<int>.Default);

            // Assert
            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Constructor_FromCapacityAndReadOnlyCollectionAndComparer()
        {
            // Act
            IReadOnlyCollection<int> collection = Enumerable.Range(1, 3).ToList().AsReadOnly();
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(10, collection, Comparer<int>.Default);

            // Assert
            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Constructor_FromCapacityAndCollectionAndComparer()
        {
            // Act
            ICollection<int> collection = new GenericCollection<int>(Enumerable.Range(1, 3).ToList());
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(10, collection, Comparer<int>.Default);

            // Assert
            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Constructor_FromCapacityAndNonGenericCollectionAndComparer()
        {
            // Act
            NonGenericCollection<int> collection = new NonGenericCollection<int>(Enumerable.Range(1, 3).ToList());
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(10, collection, Comparer<int>.Default);

            // Assert
            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void IsReadOnly_ReturnsFalse()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            Assert.IsFalse(queue.IsReadOnly);
        }

        [TestMethod]
        public void IsSynchronized_ReturnsFalse()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            Assert.IsFalse(queue.IsSynchronized);
        }

        [TestMethod]
        public void SyncRoot_ReturnsThis()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            Assert.AreEqual(queue, queue.SyncRoot);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Peek_ThrowsForEmptyQueue()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            _ = queue.Peek();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Dequeue_ThrowsForEmptyQueue()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            _ = queue.Dequeue();
        }

        [TestMethod]
        public void InsertionInAscendingOrder_ReturnsInOrder()
        {
            // Arrange
            int itemCount = 100;
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            for (int i = 0; i < itemCount; ++i)
            {
                queue.Enqueue(i);
            }

            // Assert
            Assert.AreEqual(itemCount, queue.Count);

            for (int i = itemCount - 1; i >= 0; --i)
            {
                Assert.AreEqual(i, queue.Peek());
                Assert.AreEqual(i, queue.Dequeue());
            }

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void InsertionInDescendingOrder_ReturnsInOrder()
        {
            // Arrange
            int itemCount = 100;
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            for (int i = itemCount - 1; i >= 0; --i)
            {
                queue.Enqueue(i);
            }

            // Assert
            Assert.AreEqual(itemCount, queue.Count);

            for (int i = itemCount - 1; i >= 0; --i)
            {
                Assert.AreEqual(i, queue.Peek());
                Assert.AreEqual(i, queue.Dequeue());
            }

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void RandomInsertion_ReturnsInOrder()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            queue.Enqueue(4);
            queue.Enqueue(2);
            queue.Enqueue(0);
            queue.Enqueue(1);
            queue.Enqueue(3);

            // Assert
            Assert.AreEqual(5, queue.Count);
            Assert.AreEqual(4, queue.Peek());
            Assert.AreEqual(4, queue.Dequeue());
            Assert.AreEqual(3, queue.Peek());
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Peek());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Peek());
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(0, queue.Peek());
            Assert.AreEqual(0, queue.Dequeue());
        }

        [TestMethod]
        public void OverlappedEnqueueAndDequeue_ReturnsInOrder()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            queue.Enqueue(4);
            queue.Enqueue(2);

            // Assert
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(4, queue.Peek());
            Assert.AreEqual(4, queue.Dequeue());
            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual(2, queue.Peek());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);

            // Act
            queue.Enqueue(0);
            queue.Enqueue(1);
            queue.Enqueue(-2);
            queue.Enqueue(3);
            queue.Enqueue(-1);

            // Assert
            Assert.AreEqual(5, queue.Count);
            Assert.AreEqual(3, queue.Peek());
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(1, queue.Peek());
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(0, queue.Peek());
            Assert.AreEqual(0, queue.Dequeue());
            Assert.AreEqual(-1, queue.Peek());
            Assert.AreEqual(-1, queue.Dequeue());
            Assert.AreEqual(-2, queue.Peek());
            Assert.AreEqual(-2, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Allows_Duplicates()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            queue.Enqueue(4);
            queue.Enqueue(2);
            queue.Enqueue(4);
            queue.Enqueue(3);
            queue.Enqueue(2);

            // Assert
            Assert.AreEqual(5, queue.Count);

            Assert.IsFalse(queue.Contains(1));
            Assert.IsTrue(queue.Contains(2));
            Assert.IsTrue(queue.Contains(3));
            Assert.IsTrue(queue.Contains(4));
            Assert.IsFalse(queue.Contains(5));

            Assert.AreEqual(4, queue.Peek());
            Assert.AreEqual(4, queue.Dequeue());
            Assert.AreEqual(4, queue.Count);

            Assert.AreEqual(4, queue.Peek());
            Assert.AreEqual(4, queue.Dequeue());
            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(3, queue.Peek());
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(2, queue.Count);

            Assert.AreEqual(2, queue.Peek());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(1, queue.Count);

            Assert.AreEqual(2, queue.Peek());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Allows_Nulls()
        {
            // Arrange
            MaxPriorityQueue<int?> queue = new MaxPriorityQueue<int?>((a, b) => (a ?? 0).CompareTo(b ?? 0));

            // Act
            queue.Enqueue(2);
            queue.Enqueue(null);
            queue.Enqueue(-1);
            queue.Enqueue(1);
            queue.Enqueue(null);

            // Assert
            Assert.AreEqual(5, queue.Count);

            Assert.IsFalse(queue.Contains(-2));
            Assert.IsTrue(queue.Contains(-1));
            Assert.IsTrue(queue.Contains(null));
            Assert.IsTrue(queue.Contains(1));
            Assert.IsTrue(queue.Contains(2));
            Assert.IsFalse(queue.Contains(3));

            Assert.AreEqual(2, queue.Peek());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(4, queue.Count);

            Assert.AreEqual(1, queue.Peek());
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(null, queue.Peek());
            Assert.AreEqual(null, queue.Dequeue());
            Assert.AreEqual(2, queue.Count);

            Assert.AreEqual(null, queue.Peek());
            Assert.AreEqual(null, queue.Dequeue());
            Assert.AreEqual(1, queue.Count);

            Assert.AreEqual(-1, queue.Peek());
            Assert.AreEqual(-1, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Add_EquivalentToEnqueue()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>
            {
                4,
                2,
            };

            // Assert
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(4, queue.Peek());
            Assert.AreEqual(4, queue.Dequeue());
            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual(2, queue.Peek());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);

            // Act
            queue.Add(0);
            queue.Enqueue(1);
            queue.Enqueue(-2);
            queue.Add(3);
            queue.Add(-1);

            // Assert
            Assert.AreEqual(5, queue.Count);
            Assert.AreEqual(3, queue.Peek());
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(1, queue.Peek());
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(0, queue.Peek());
            Assert.AreEqual(0, queue.Dequeue());
            Assert.AreEqual(-1, queue.Peek());
            Assert.AreEqual(-1, queue.Dequeue());
            Assert.AreEqual(-2, queue.Peek());
            Assert.AreEqual(-2, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Remove_RemovesSingleOccurrence()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>
            {
                1,
                2,
                1,
                0,
                3,
            };

            // Remove() returns false and doesn't change the queue if the item is not in the queue
            Assert.AreEqual(5, queue.Count);
            Assert.AreEqual(3, queue.Peek());

            Assert.IsFalse(queue.Remove(5));

            Assert.AreEqual(5, queue.Count);
            Assert.AreEqual(3, queue.Peek());

            // Remove() removes a single item at a time if it is present more than once
            Assert.IsTrue(queue.Contains(1));

            Assert.IsTrue(queue.Remove(1));

            Assert.IsTrue(queue.Contains(1));
            Assert.AreEqual(4, queue.Count);
            Assert.AreEqual(3, queue.Peek());

            // Remove() removes the last occurrence of the item
            Assert.IsTrue(queue.Remove(1));

            Assert.IsFalse(queue.Contains(1));
            Assert.AreEqual(3, queue.Count);
            Assert.AreEqual(3, queue.Peek());

            // Remove() returns false and doesn't change the queue if the item is no longer in the queue
            Assert.IsFalse(queue.Remove(1));

            Assert.IsFalse(queue.Contains(1));
            Assert.AreEqual(3, queue.Count);
            Assert.AreEqual(3, queue.Peek());

            // Remove() removes remaining items
            while (queue.Count > 0)
            {
                Assert.IsTrue(queue.Remove(queue.Peek()));
            }

            for (int i = 0; i < 5; ++i)
            {
                Assert.IsFalse(queue.Contains(i));
            }
        }

        [TestMethod]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            queue.Enqueue(4);
            queue.Enqueue(2);
            queue.Enqueue(0);
            queue.Enqueue(1);
            queue.Enqueue(-2);
            queue.Enqueue(3);
            queue.Enqueue(-1);

            Assert.AreEqual(7, queue.Count);

            // Act
            queue.Clear();

            // Assert
            Assert.AreEqual(0, queue.Count);

            // Act
            queue.Clear();
            queue.Clear();

            // Assert
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void CopyTo_Generic_CopiesAllItems()
        {
            // Arrange
            int count = 5;
            List<int> items = Enumerable.Range(0, 5).ToList();
            int[] destination = new int[count];
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(items);

            // Act
            queue.CopyTo(destination, 0);

            // Assert
            CollectionAssert.AreEquivalent(items, destination);
        }

        [TestMethod]
        public void CopyTo_NonGeneric_CopiesAllItems()
        {
            // Arrange
            int count = 5;
            List<int> items = Enumerable.Range(0, 5).ToList();
            object[] destination = new object[count];
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(items);

            // Act
            queue.CopyTo(destination, 0);

            // Assert
            CollectionAssert.AreEquivalent(items, destination);
        }

        [TestMethod]
        public void GetEnumerator_EmptyQueue_MoveNext_ReturnsFalse()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>();

            // Act
            using (IEnumerator<int> enumerator = queue.GetEnumerator())
            {
                // Assert
                Assert.IsFalse(enumerator.MoveNext());
                enumerator.Reset();
                Assert.IsFalse(enumerator.MoveNext());
            }
        }

        [TestMethod]
        public void GetEnumerator_NonEmptyQueue_IteratesThroughAllElements()
        {
            // Arrange
            int[] collection = Enumerable.Range(0, 100).Shuffle();
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(collection);

            // Act
            List<int> enumerationResult = new List<int>();
            foreach (int item in queue)
            {
                enumerationResult.Add(item);
            }

            // Assert
            CollectionAssert.AreEquivalent(collection, enumerationResult);
        }

        [TestMethod]
        public void GetWeakEnumerator_NonEmptyQueue_IteratesThroughAllElements()
        {
            // Arrange
            int[] collection = Enumerable.Range(0, 100).Shuffle();
            IEnumerable queue = new MaxPriorityQueue<int>(collection);

            // Act
            List<int> enumerationResult = new List<int>();
            foreach (object item in queue)
            {
                enumerationResult.Add((int)item);
            }

            // Assert
            CollectionAssert.AreEquivalent(collection, enumerationResult);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetEnumerator_AfterEnumeratingAllElements_Current_Throws()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            using (IEnumerator<int> enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 10; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }

                Assert.IsFalse(enumerator.MoveNext());

                // Act
                _ = enumerator.Current;
            }
        }

        [TestMethod]
        public void GetEnumerator_AfterEnumeratingAllElements_MoveNext_ReturnsFalse()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            using (IEnumerator<int> enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 10; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }

                // Assert
                for (int i = 0; i < 10; ++i)
                {
                    Assert.IsFalse(enumerator.MoveNext());
                }
            }
        }

        [TestMethod]
        public void GetEnumerator_AfterEnumeratingAllElements_Reset_ResetsEnumerator()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            using (IEnumerator<int> enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 10; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }

                Assert.IsFalse(enumerator.MoveNext());

                // Act
                enumerator.Reset();

                // Assert
                for (int i = 0; i < 10; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }

                Assert.IsFalse(enumerator.MoveNext());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetEnumerator_QueueModifiedDuringEnumeration_Current_Throws()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            using (IEnumerator<int> enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 5; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }

                // Act
                queue.Enqueue(11);
                _ = enumerator.Current;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetEnumerator_QueueModifiedDuringEnumeration_MoveNext_Throws()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            using (IEnumerator<int> enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 5; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }

                // Act
                queue.Enqueue(11);
                _ = enumerator.MoveNext();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetEnumerator_QueueModifiedDuringEnumeration_Reset_Throws()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            using (IEnumerator<int> enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 5; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }

                // Act
                queue.Enqueue(11);
                enumerator.Reset();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void GetEnumerator_AfterDisposing_Current_Throws()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            IEnumerator<int> enumerator;
            using (enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 5; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }
            }

            // Act
            _ = enumerator.Current;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void GetEnumerator_AfterDisposing_MoveNext_Throws()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            IEnumerator<int> enumerator;
            using (enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 5; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }
            }

            // Act
            _ = enumerator.MoveNext();
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void GetEnumerator_AfterDisposing_Reset_Throws()
        {
            // Arrange
            MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(Enumerable.Range(0, 10).Shuffle());

            IEnumerator<int> enumerator;
            using (enumerator = queue.GetEnumerator())
            {
                for (int i = 0; i < 5; ++i)
                {
                    Assert.IsTrue(enumerator.MoveNext());
                }
            }

            // Act
            enumerator.Reset();
        }

        private class WrapperClass
        {
            public int Value { get; set; }
        }

        private class WrapperClassComparer : IComparer<WrapperClass>
        {
            public int Compare(WrapperClass x, WrapperClass y)
            {
                return x.Value.CompareTo(y.Value);
            }
        }

        private struct WrapperStruct
        {
            public int Value { get; set; }
        }

        private class WrapperStructComparer : IComparer<WrapperStruct>
        {
            public int Compare(WrapperStruct x, WrapperStruct y)
            {
                return x.Value.CompareTo(y.Value);
            }
        }
    }
}
