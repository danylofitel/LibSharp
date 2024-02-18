// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching
{
    /// <summary>
    /// Async initializer with LazyThreadSafetyMode.PublicationOnly.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <remarks>Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.</remarks>
    public class InitializerAsyncPublicationOnly<T> : IInitializerAsync<T>
    {
        /// <inheritdoc/>
        public bool HasValue => m_value != null;

        /// <inheritdoc/>
        public async Task<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
        {
            Argument.NotNull(factory, nameof(factory));

            if (!HasValue)
            {
                T value = await factory(cancellationToken).ConfigureAwait(false);
                _ = Interlocked.CompareExchange(ref m_value, new ValueReference<T>(value), null);
            }

            return m_value.Value;
        }

        private ValueReference<T> m_value;
    }
}
