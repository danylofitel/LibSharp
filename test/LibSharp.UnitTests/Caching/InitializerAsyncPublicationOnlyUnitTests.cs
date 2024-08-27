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
    public class InitializerAsyncPublicationOnlyUnitTests
    {
        [TestMethod]
        public async Task FromValueFactory()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

            InitializerAsyncPublicationOnly<int> lazy = new InitializerAsyncPublicationOnly<int>();

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                // Assert
                Assert.IsFalse(lazy.HasValue);
                _ = factory.DidNotReceive()(cancellationToken);

                Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

                Assert.IsTrue(lazy.HasValue);
                _ = factory.Received(1)(cancellationToken);

                Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
                Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
                Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

                Assert.IsTrue(lazy.HasValue);
                _ = factory.Received(1)(cancellationToken);
            }
        }
    }
}
