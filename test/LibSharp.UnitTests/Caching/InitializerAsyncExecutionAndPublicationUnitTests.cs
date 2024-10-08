﻿// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching
{
    [TestClass]
    public class InitializerAsyncExecutionAndPublicationUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void HasValue_ThrowsWhenDisposed()
        {
            // Arrange
            InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
            initializer.Dispose();

            // Act
            _ = initializer.HasValue;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task GetValueAsync_ThrowsWhenDisposed()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
            InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
            initializer.Dispose();

            // Act
            _ = await initializer.GetValueAsync(factory, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FromValueFactory()
        {
            // Arrange
            Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

            _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

            using (InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>())
            {
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancellationToken = cancellationTokenSource.Token;

                    // Assert
                    Assert.IsFalse(initializer.HasValue);
                    _ = factory.DidNotReceive()(cancellationToken);

                    Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(initializer.HasValue);
                    _ = factory.Received(1)(cancellationToken);

                    Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
                    Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

                    Assert.IsTrue(initializer.HasValue);
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

            using (InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>())
            {
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.Cancel();

                    // Act
                    _ = await initializer.GetValueAsync(factory, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }
    }
}
