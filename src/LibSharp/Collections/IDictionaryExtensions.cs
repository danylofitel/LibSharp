// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using LibSharp.Common;

namespace LibSharp.Collections
{
    /// <summary>
    /// Extension methods for IDictionary.
    /// </summary>
    public static class IDictionaryExtensions
    {
        /// <summary>
        /// Adds the value to the dictionary if it does not exist, otherwise updates it.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="addValue">The value to add.</param>
        /// <param name="updateValueFactory">The factory providing an updated value from an existing value.</param>
        /// <returns>The new value in the dictionary.</returns>
        public static TValue AddOrUpdate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue addValue,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            Argument.NotNull(dictionary, nameof(dictionary));
            Argument.NotNull(key, nameof(key));
            Argument.NotNull(updateValueFactory, nameof(updateValueFactory));

            return dictionary.AddOrUpdate(
                key,
                keyValue => addValue,
                updateValueFactory);
        }

        /// <summary>
        /// Adds the value to the dictionary if it does not exist, otherwise updates it.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="addValueFactory">The factory providing a new value.</param>
        /// <param name="updateValueFactory">The factory providing an updated value from an existing value.</param>
        /// <returns>The new value in the dictionary.</returns>
        public static TValue AddOrUpdate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> addValueFactory,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            Argument.NotNull(dictionary, nameof(dictionary));
            Argument.NotNull(key, nameof(key));
            Argument.NotNull(addValueFactory, nameof(addValueFactory));
            Argument.NotNull(updateValueFactory, nameof(updateValueFactory));

            TValue newValue;

            if (dictionary.TryGetValue(key, out TValue oldValue))
            {
                newValue = updateValueFactory(key, oldValue);
            }
            else
            {
                newValue = addValueFactory(key);
            }

            dictionary[key] = newValue;
            return newValue;
        }

        /// <summary>
        /// Adds the value to the dictionary if it does not exist, otherwise updates it.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <typeparam name="TArg">Argument type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="addValueFactory">The factory providing a new value.</param>
        /// <param name="updateValueFactory">The factory providing an updated value from an existing value.</param>
        /// <param name="factoryArgument">Additional argument that should be passed to the factories.</param>
        /// <returns>The new value in the dictionary.</returns>
        public static TValue AddOrUpdate<TKey, TValue, TArg>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TArg, TValue> addValueFactory,
            Func<TKey, TValue, TArg, TValue> updateValueFactory,
            TArg factoryArgument)
        {
            Argument.NotNull(dictionary, nameof(dictionary));
            Argument.NotNull(key, nameof(key));
            Argument.NotNull(addValueFactory, nameof(addValueFactory));
            Argument.NotNull(updateValueFactory, nameof(updateValueFactory));

            return dictionary.AddOrUpdate(
                key,
                keyValue => addValueFactory(keyValue, factoryArgument),
                (keyValue, oldValue) => updateValueFactory(keyValue, oldValue, factoryArgument));
        }

        /// <summary>
        /// Gets the value from the dictionary if it exists, otherwise adds a new value.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to add if it does not exist.</param>
        /// <returns>The new value in the dictionary.</returns>
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value)
        {
            Argument.NotNull(dictionary, nameof(dictionary));
            Argument.NotNull(key, nameof(key));

            return dictionary.GetOrAdd(key, keyValue => value);
        }

        /// <summary>
        /// Gets the value from the dictionary if it exists, otherwise adds a new value.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueFactory">The factory providing a new value.</param>
        /// <returns>The new value in the dictionary.</returns>
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> valueFactory)
        {
            Argument.NotNull(dictionary, nameof(dictionary));
            Argument.NotNull(key, nameof(key));
            Argument.NotNull(valueFactory, nameof(valueFactory));

            if (dictionary.TryGetValue(key, out TValue oldValue))
            {
                return oldValue;
            }
            else
            {
                TValue newValue = valueFactory(key);
                dictionary[key] = newValue;
                return newValue;
            }
        }

        /// <summary>
        /// Gets the value from the dictionary if it exists, otherwise adds a new value.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <typeparam name="TArg">Argument type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueFactory">The factory providing a new value.</param>
        /// <param name="factoryArgument">Additional argument that should be passed to the factory.</param>
        /// <returns>The new value in the dictionary.</returns>
        public static TValue GetOrAdd<TKey, TValue, TArg>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TArg, TValue> valueFactory,
            TArg factoryArgument)
        {
            Argument.NotNull(dictionary, nameof(dictionary));
            Argument.NotNull(key, nameof(key));
            Argument.NotNull(valueFactory, nameof(valueFactory));

            return dictionary.GetOrAdd(
                key,
                keyValue => valueFactory(keyValue, factoryArgument));
        }

        /// <summary>
        /// Copies a dictionary.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="source">Source dictionary.</param>
        /// <returns>A copy of the source dictionary.</returns>
        public static IDictionary<TKey, TValue> Copy<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            Argument.NotNull(source, nameof(source));

            return source.CopyTo(new Dictionary<TKey, TValue>(source.Count));
        }

        /// <summary>
        /// Copies all entries from the source dictionary to the destination dictionary.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="source">Source dictionary.</param>
        /// <param name="destination">Destination dictionary.</param>
        /// <returns>Destination dictionary with properties copied from the source dictionary.</returns>
        public static IDictionary<TKey, TValue> CopyTo<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> destination)
        {
            Argument.NotNull(source, nameof(source));
            Argument.NotNull(destination, nameof(destination));

            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                destination[pair.Key] = pair.Value;
            }

            return destination;
        }
    }
}
