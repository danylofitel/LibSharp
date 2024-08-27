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
    public class LazyAsyncPublicationOnlyUnitTests
    {
        [TestMethod]
        public async Task FromValue()
        {
            // Arrange
            LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(5);

            // Assert
            Assert.IsTrue(lazy.HasValue);
            Assert.AreEqual(5, await lazy.GetValueAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task FromValue_CanceledToken_Succeeds()
        {
            // Arrange
            LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(5);

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // Assert
                Assert.IsTrue(lazy.HasValue);
                Assert.AreEqual(5, await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false));
            }
        }

        [TestMethod]
        public async Task FromValueFactory()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

            LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(factory);

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
}
