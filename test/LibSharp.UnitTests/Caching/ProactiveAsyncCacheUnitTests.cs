// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching
{
    [TestClass]
    public class ProactiveAsyncCacheUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_ThrowsOnNullFactory()
        {
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_ThrowsOnZeroRefreshInterval()
        {
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.Zero, TimeSpan.Zero);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_ThrowsOnNegativePreFetchOffset()
        {
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(-1));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_ThrowsWhenPreFetchOffsetExceedsRefreshInterval()
        {
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
        }

        [TestMethod]
        public void HasValue_ReturnsFalseBeforeFirstFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act & Assert
            Assert.IsFalse(cache.HasValue);
        }

        [TestMethod]
        public void Expiration_ReturnsNullBeforeFirstFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act & Assert
            Assert.IsNull(cache.Expiration);
        }

        [TestMethod]
        public async Task GetValueAsync_ReturnsValueFromFactory()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            int value = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        public async Task GetValueAsync_ReturnsCachedValueOnSubsequentCalls()
        {
            // Arrange
            int callCount = 0;
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                _ =>
                {
                    int result = Interlocked.Increment(ref callCount);
                    return Task.FromResult(result);
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            // Act
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            int second = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, second);
            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public async Task HasValue_ReturnsTrueAfterFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            _ = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.IsTrue(cache.HasValue);
        }

        [TestMethod]
        public async Task Expiration_ReturnsValueAfterFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            _ = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(cache.Expiration);
            Assert.IsTrue(cache.Expiration > DateTime.UtcNow);
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void HasValue_ThrowsWhenDisposed()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Dispose();

            // Act
            _ = cache.HasValue;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Expiration_ThrowsWhenDisposed()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Dispose();

            // Act
            _ = cache.Expiration;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task GetValueAsync_ThrowsWhenDisposed()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Dispose();

            // Act
            _ = await cache.GetValueAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DisposeAsync_CanBeCalledSafely()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            await cache.DisposeAsync().ConfigureAwait(false);

            // Assert — should not throw
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task GetValueAsync_WithReferenceType_ReturnsValue()
        {
            // Arrange
            using ProactiveAsyncCache<string> cache = new ProactiveAsyncCache<string>(_ => Task.FromResult("hello"), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            string value = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("hello", value);
        }
    }
}
