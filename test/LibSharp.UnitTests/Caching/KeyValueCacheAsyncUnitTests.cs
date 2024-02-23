// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching
{
    [TestClass]
    public class KeyValueCacheAsyncUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task GetValueAsync_ThrowsWhenDisposed()
        {
            // Arrange
            Func<string, CancellationToken, Task<int>> factory = Substitute.For<Func<string, CancellationToken, Task<int>>>();
            KeyValueCacheAsync<string, int> cache = new KeyValueCacheAsync<string, int>(factory, TimeSpan.FromMinutes(1));
            cache.Dispose();

            // Act
            _ = await cache.GetValueAsync("key", CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task KeyValueCache_ValueNotExpired()
        {
            // Arrange
            using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(MockFactory, TimeSpan.FromHours(1));

            // Act
            int value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);
        }

        [TestMethod]
        public async Task KeyValueCache_ValueExpired()
        {
            // Arrange
            using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(MockFactory, TimeSpan.Zero);

            // Act
            int value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);
        }

        private static Task<int> MockFactory(int key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(-key);
        }
    }
}
