// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching
{
    /// <summary>
    /// Async lazy with LazyThreadSafetyMode.PublicationOnly.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <remarks>Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.</remarks>
    public class LazyAsyncPublicationOnly<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LazyAsyncPublicationOnly{T}"/> class from a value.
        /// </summary>
        /// <param name="value">The value to hold.</param>
        public LazyAsyncPublicationOnly(T value)
        {
            Argument.NotNull(value, nameof(value));

            m_value = new ValueReference<T>(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyAsyncPublicationOnly{T}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">The value factory.</param>
        public LazyAsyncPublicationOnly(Func<CancellationToken, Task<T>> factory)
        {
            Argument.NotNull(factory, nameof(factory));

            m_factory = factory;
        }

        /// <summary>
        /// Gets a value indicating whether the value has been initialized.
        /// </summary>
        public bool HasValue => m_value is not null;

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The value.</returns>
        public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        {
            if (!HasValue)
            {
                T value = await m_factory(cancellationToken).ConfigureAwait(false);
                _ = Interlocked.CompareExchange(ref m_value, new ValueReference<T>(value), null);
            }

            return m_value.Value;
        }

        private readonly Func<CancellationToken, Task<T>> m_factory;
        private ValueReference<T> m_value;
    }
}
