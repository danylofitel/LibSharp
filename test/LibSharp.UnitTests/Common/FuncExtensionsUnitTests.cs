// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class FuncExtensionsUnitTests
    {
        [TestMethod]
        public async Task RunWithTimeout_WithoutReturnType_Test()
        {
            // Arrange
            int result = 0;
            Func<CancellationToken, Task> task = async cancellationToken =>
            {
                await Task.CompletedTask.ConfigureAwait(false);
                result = 99;
            };

            // Act
            await task.RunWithTimeout(TimeSpan.FromSeconds(1), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(99, result);
        }

        [TestMethod]
        public async Task RunWithTimeout_WithReturnType_Test()
        {
            // Arrange
            Func<CancellationToken, Task<int>> task = async cancellationToken =>
            {
                await Task.CompletedTask.ConfigureAwait(false);
                return 99;
            };

            // Act
            int result = await task.RunWithTimeout(TimeSpan.FromSeconds(1), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(99, result);
        }
    }
}
