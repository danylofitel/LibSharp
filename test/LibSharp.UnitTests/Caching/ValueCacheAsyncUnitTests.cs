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
    public class ValueCacheAsyncUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void HasValue_ThrowsWhenDisposed()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
            ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.FromMinutes(1));
            cache.Dispose();

            // Act
            _ = cache.HasValue;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Expiration_ThrowsWhenDisposed()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
            ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.FromMinutes(1));
            cache.Dispose();

            // Act
            _ = cache.Expiration;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task GetValueAsync_ThrowsWhenDisposed()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
            ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.FromMinutes(1));
            cache.Dispose();

            // Act
            _ = await cache.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FromValueFactory_WhenCacheExpires_RefreshesCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0), Task.FromResult(1), Task.FromResult(2), Task.FromResult(3), Task.FromResult(4));

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.Zero))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

                    Assert.AreEqual(1, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(2, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(3, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(4, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
                }

                _ = factory.Received(5)(cancellationToken);
            }
        }

        [TestMethod]
        public async Task FromValueFactoryWithExpirationFunction_WhenCacheExpires_RefreshesCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0), Task.FromResult(1), Task.FromResult(2), Task.FromResult(3), Task.FromResult(4));

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, _ => DateTime.UtcNow.AddMinutes(-1)))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

                    Assert.AreEqual(1, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(2, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(3, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(4, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
                }

                _ = factory.Received(5)(cancellationToken);
            }
        }

        [TestMethod]
        public async Task FromUpdateFactory_WhenCacheExpires_RefreshesCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> createFactory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = createFactory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

            Func<int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();

            _ = updateFactory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => (int)x[0] + 1);

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(createFactory, updateFactory, TimeSpan.Zero))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

                    Assert.AreEqual(1, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(2, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(3, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(4, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
                }

                _ = createFactory.Received(1)(cancellationToken);
                _ = updateFactory.Received(4)(Arg.Any<int>(), cancellationToken);
            }
        }

        [TestMethod]
        public async Task FromUpdateFactoryWithExpirationFunction_WhenCacheExpires_RefreshesCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> createFactory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = createFactory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

            Func<int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();

            _ = updateFactory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => (int)x[0] + 1);

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(createFactory, updateFactory, _ => DateTime.UtcNow.AddMinutes(-1)))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

                    Assert.AreEqual(1, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(2, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(3, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(4, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
                }

                _ = createFactory.Received(1)(cancellationToken);
                _ = updateFactory.Received(4)(Arg.Any<int>(), cancellationToken);
            }
        }

        [TestMethod]
        public async Task FromValueFactory_InfiniteTimeToLive_DoesNotRefreshCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.MaxValue))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
                }

                _ = factory.Received(1)(cancellationToken);
            }
        }

        [TestMethod]
        public async Task FromValueFactoryWithExpirationFunction_CacheNotExpired_DoesNotRefreshCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, _ => DateTime.UtcNow.AddMinutes(1)))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
                }

                _ = factory.Received(1)(cancellationToken);
            }
        }

        [TestMethod]
        public async Task FromUpdateFactory_InfiniteTimeToLive_DoesNotRefreshCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> createFactory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = createFactory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

            Func<int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(createFactory, updateFactory, TimeSpan.MaxValue))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
                }

                _ = createFactory.Received(1)(cancellationToken);
                _ = updateFactory.DidNotReceive()(Arg.Any<int>(), cancellationToken);
            }
        }

        [TestMethod]
        public async Task FromUpdateFactoryWithExpirationFunction_CacheNotExpired_DoesNotRefreshCache()
        {
            // Arrange
            Func<CancellationToken, Task<int>> createFactory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = createFactory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

            Func<int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(createFactory, updateFactory, _ => DateTime.UtcNow.AddMinutes(1)))
                {
                    // Assert
                    Assert.IsFalse(cache.HasValue);
                    Assert.IsNull(cache.Expiration);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(0, await cache.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(cache.HasValue);
                    Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
                }

                _ = createFactory.Received(1)(cancellationToken);
                _ = updateFactory.DidNotReceive()(Arg.Any<int>(), cancellationToken);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task FromValueFactory_CanceledToken_Throws()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.Zero))
                {
                    // Act
                    _ = await cache.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task FromValueFactoryWithExpirationFunction_CanceledToken_Throws()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, _ => DateTime.UtcNow.AddMinutes(-1)))
                {
                    // Act
                    _ = await cache.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task FromUpdateFactory_CanceledToken_Throws()
        {
            // Arrange
            Func<CancellationToken, Task<int>> createFactory = Substitute.For<Func<CancellationToken, Task<int>>>();

            Func<int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(createFactory, updateFactory, TimeSpan.Zero))
                {
                    // Act
                    _ = await cache.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task FromUpdateFactoryWithExpirationFunction_CanceledToken_Throws()
        {
            // Arrange
            Func<CancellationToken, Task<int>> createFactory = Substitute.For<Func<CancellationToken, Task<int>>>();

            Func<int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                using (ValueCacheAsync<int> cache = new ValueCacheAsync<int>(createFactory, updateFactory, _ => DateTime.UtcNow.AddMinutes(-1)))
                {
                    // Act
                    _ = await cache.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }
    }
}
