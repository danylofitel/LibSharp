// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.Caching.UnitTests
{
    [TestClass]
    public class LazyAsyncExecutionAndPublicationUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void HasValue_ThrowsWhenDisposed()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
            LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory);
            lazy.Dispose();

            // Act
            _ = lazy.HasValue;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task GetValueAsync_ThrowsWhenDisposed()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
            LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory);
            lazy.Dispose();

            // Act
            _ = await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FromValue()
        {
            // Arrange
            using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(5))
            {
                // Assert
                Assert.IsTrue(lazy.HasValue);
                Assert.AreEqual(5, await lazy.GetValueAsync().ConfigureAwait(false));
            }
        }

        [TestMethod]
        public async Task FromValue_CanceledToken_Succeeds()
        {
            // Arrange
            using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(5))
            {
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.Cancel();

                    // Assert
                    Assert.IsTrue(lazy.HasValue);
                    Assert.AreEqual(5, await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false));
                }
            }
        }

        [TestMethod]
        public async Task FromValueFactory()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

            using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory))
            {
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancellationToken = cancellationTokenSource.Token;

                    // Assert
                    Assert.IsFalse(lazy.HasValue);
                    _ = factory.DidNotReceive()(cancellationToken);

                    Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(lazy.HasValue);
                    _ = factory.Received(1)(cancellationToken);

                    Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(lazy.HasValue);
                    _ = factory.Received(1)(cancellationToken);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task FromValueFactory_TokenCanceled_Throws()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory))
            {
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.Cancel();

                    // Act
                    _ = await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }
    }
}
