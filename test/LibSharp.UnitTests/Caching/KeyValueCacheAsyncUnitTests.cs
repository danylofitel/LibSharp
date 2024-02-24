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
            Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
            KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, TimeSpan.FromMinutes(1));
            cache.Dispose();

            // Act
            _ = await cache.GetValueAsync(1, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task KeyValueCache_ValueNotExpired()
        {
            // Arrange
            Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
            _ = factory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

            using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, TimeSpan.FromHours(1));

            // Assert
            _ = factory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());

            // Act
            int value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

            // Act
            value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(1)(2, Arg.Any<CancellationToken>());

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(1)(2, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task KeyValueCache_ValueExpired()
        {
            // Arrange
            Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
            _ = factory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

            using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, TimeSpan.Zero);

            // Assert
            _ = factory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());

            // Act
            int value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

            // Act
            value = await cache.GetValueAsync(1).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(1)(2, Arg.Any<CancellationToken>());

            // Act
            value = await cache.GetValueAsync(2).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
            _ = factory.Received(2)(2, Arg.Any<CancellationToken>());
        }
    }
}
