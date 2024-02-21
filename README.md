# LibSharp

## Introduction

A library of C# core components that enhance the standard library. Supports .NET Standard 2.0, .NET Standard 2.1, .NET 5.0, .NET 6.0, .NET 7.0, .NET 8.0.

* Source code: <https://github.com/danylofitel/LibSharp>.
* NuGet package: <https://www.nuget.org/packages/LibSharp>.

LibSharp consists of the following namespaces:

* Common - contains extension methods for standard .NET types, as well commonly used utilities.
* Collections - contains extension methods for standard .NET library collections, as well as additional collection types.
* Caching - contains classes that enable in-memory value caching with custom time-to-live. Both synchronous and asynchronous versions are available.

## Components and Usage

### Common

`Common` namespace contains the static class `Argument` for convenient validation of public function arguments, plus extension methods and utilities for built-in types like `string` and `DateTime`.

```csharp
    using LibSharp.Common;

    public static void CommonExamples(string stringParam, long longParam, object objectParam)
    {
        // Argument validation
        Argument.EqualTo(stringParam, "Hello world", nameof(stringParam));
        Argument.NotEqualTo(stringParam, "Hello", nameof(stringParam));

        Argument.GreaterThan(longParam, -1L, nameof(longParam));
        Argument.GreaterThanOrEqualTo(longParam, 0L, nameof(longParam));
        Argument.LessThan(longParam, 100L, nameof(longParam));
        Argument.LessThanOrEqualTo(longParam, 99L, nameof(longParam));

        Argument.NotNull(stringParam, nameof(stringParam));
        Argument.NotNullOrEmpty(stringParam, nameof(stringParam));
        Argument.IsNullOrWhiteSpace(stringParam, nameof(stringParam));

        Argument.OfType(objectParam, typeof(List<string>), nameof(objectParam));

        // DateTime extensions
        DateTime fromEpochMilliseconds = longParam.FromEpochMilliseconds();
        DateTime fromEpochSeconds = longParam.FromEpochSeconds();
        long epochMilliseconds = DateTime.UtcNow.ToEpochMilliseconds();
        long epochSeconds = DateTime.UtcNow.ToEpochSeconds();

        // String extensions
        string base64Encoded = stringParam.Base64Encode();
        string base64Decoded = base64Encoded.Base64Decode();

        string reversed = stringParam.Reverse();
        string truncated = stringParam.Truncate(10);

        // Type extensions
        IComparer<int> intComparer = TypeExtensions.GetDefaultComparer<int>();

        // XML serialization extensions
        string serializedToXml = objectParam.SerializeToXml();
        List<string> deserializedFromXml = serializedToXml.DeserializeFromXml<List<string>>();
    }
```

### Collections

`Collections` namespace contains various extension methods for `ICollection`, `IDictionary`, `IEnumerable` interfaces plus `MinPriorityQueue` and `MaxPriorityQueue` collections.

```csharp
    using LibSharp.Collections;

    public static void CollectionsExamples()
    {
        // ICollection extensions
        ICollection<int> collection = new List<int>();  // []
        collection.AddRange(Enumerable.Range(0, 10));   // [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]

        // IDictionary extensions
        IDictionary<string, string> dictionary = new Dictionary<string, string>();

        _ = dictionary.AddOrUpdate(
            "key",
            "addedValue",
            (key, existingValue) => "updatedValue");

        _ = dictionary.AddOrUpdate(
            "key",
            key => "addedValue",
            (key, existingValue) => "updatedValue");

        _ = dictionary.AddOrUpdate(
            "key",
            (key, argument) => "addedValue" + argument,
            (key, existingValue, argument) => "updatedValue" + argument,
            "argument");

        _ = dictionary.GetOrAdd(
            "key",
            "addedValue");

        _ = dictionary.GetOrAdd(
            "key",
            keyValue => "addedValue");

        _ = dictionary.GetOrAdd(
            "key",
            (keyValue, argument) => "addedValue" + argument,
            "argument");
        
        IDictionary<string, string> newCopy = dictionary.Copy();

        IDictionary<string, string> destination = new Dictionary<string, string>();
        IDictionary<string, string> result = dictionary.CopyTo(destination);

        // IEnumerable extensions
        IEnumerable<List<int>> chunks = Enumerable.Range(0, 10).Chunk(10, item => item).ToList();   // [ [0, 1, 2, 3, 4, 5], [6, 7], [8, 9] ]

        IEnumerable<int> enumerable = Enumerable.Range(100).Concat(Enumerable.Range(100)).ToList();
        int firstIndex = enumerable.FirstIndexOf(51);
        int lastIndex = enumerable.LastIndexOf(51);

        IEnumerable<int> shuffled = enumerable.Shuffle();

        // Min priority queue
        MinPriorityQueue<int> minPq = new MinPriorityQueue<int>();
        minPq.Enqueue(2);
        minPq.Enqueue(1);
        minPq.Enqueue(3);

        _ = minPq.Dequeue();    // 1
        _ = minPq.Dequeue();    // 2
        _ = minPq.Dequeue();    // 3

        // Max priority queue
        MinPriorityQueue<int> maxPq = new MinPriorityQueue<int>();
        maxPq.Enqueue(2);
        maxPq.Enqueue(1);
        maxPq.Enqueue(3);

        _ = maxPq.Dequeue();    // 3
        _ = maxPq.Dequeue();    // 2
        _ = maxPq.Dequeue();    // 1
    }
```

### Caching

`Caching` namespace contains a number of classes for thread-safe lazy initialization and caching of in-memory values.

Notes:

* Some of the classes implement `IDisposable` interface and should be correctly disposed.
* Be cautious when caching types that implement `IDisposable` interface as the values will not be automatically disposed by the caches.
* Be cautious when using classes with `LazyThreadSafetyMode.PublicationOnly` behavior together with `IDisposable` types as discarded instances will not be disposed.

#### Lazy

Two different implementations of async lazy values are available - `LazyAsyncPublicationOnly` and `LazyAsyncExecutionAndPublication`. Those are async versions of `System.Lazy` class with `LazyThreadSafetyMode.PublicationOnly` and `LazyThreadSafetyMode.ExecutionAndPublication` modes respectively. The reason that async lazy implementations are separate classes is that `LazyAsyncExecutionAndPublication` implements `IDisposable` due to its usage of an instance of `SemaphoreSlim` whereas `LazyAsyncPublicationOnly` does not need to implement `IDisposable`.

```csharp
    using LibSharp.Caching;

    public static async Task LazyAsyncPublicationOnlyExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(factory);

        bool hasValue = lazy.HasValue;                              // false
        int value = await lazy.GetValueAsync(cancellationToken);    // factory invoked
        hasValue = lazy.HasValue;                                   // true
        value = await lazy.GetValueAsync(cancellationToken);        // factory not invoked
        hasValue = lazy.HasValue;                                   // true
    }

    public static async Task LazyAsyncExecutionAndPublicationExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        using LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory);

        bool hasValue = lazy.HasValue;                              // false
        int value = await lazy.GetValueAsync(cancellationToken);    // factory invoked
        hasValue = lazy.HasValue;                                   // true
        value = await lazy.GetValueAsync(cancellationToken);        // factory not invoked
        hasValue = lazy.HasValue;                                   // true
    }
```

#### Initializers

Initializers in LibSharp are equivalents of lazy types, with the only difference being that the value factory is provided at lazy initialization time instead of creation time. They also enable cases where different factories can be used to initialize the value, where only one will succeed at setting the value.

```csharp
    using LibSharp.Caching;

    public static void InitializerExample(Func<int> factory)
    {
        Initializer<int> initializer = new Initializer<int>();

        bool hasValue = initializer.HasValue;       // false
        int value = initializer.GetValue(factory);  // factory invoked
        hasValue = initializer.HasValue;            // true
        value = initializer.GetValue(factory);      // factory not invoked
        hasValue = initializer.HasValue;            // true
    }

    public static async Task InitializerAsyncPublicationOnlyExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        InitializerAsyncPublicationOnly<int> initializer = new InitializerAsyncPublicationOnly<int>();

        bool hasValue = initializer.HasValue;                                       // false
        int value = await initializer.GetValueAsync(factory, cancellationToken);    // factory invoked
        hasValue = initializer.HasValue;                                            // true
        value = await initializer.GetValueAsync(factory, cancellationToken);        // factory not invoked
        hasValue = initializer.HasValue;                                            // true
    }

    public static async Task InitializerAsyncExecutionAndPublicationExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        using InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();

        bool hasValue = initializer.HasValue;                                       // false
        int value = await initializer.GetValueAsync(factory, cancellationToken);    // factory invoked
        hasValue = initializer.HasValue;                                            // true
        value = await initializer.GetValueAsync(factory, cancellationToken);        // factory not invoked
        hasValue = initializer.HasValue;                                            // true
    }
```

#### Value Caches

Value caches are lazy types that automatically refresh the value when it expires. It is possible to either provide an exact time-to-live value or a custom function to determine expiration of a value (useful, for example, for in-memory caching of tokens with known expiration time). It is also possible to provide either a factory method for creation of a new value or a factory for updating the existing value.

Note that `ValueCacheAsync` guarantees `LazyThreadSafetyMode.ExecutionAndPublication` behavior and implements `LazyThreadSafetyMode.ExecutionAndPublication`.

```csharp
    using LibSharp.Caching;

    public static void ValueCacheExample(Func<int> factory)
    {
        ValueCache<int> cache = new ValueCache<int>(factory, TimeSpan.FromMilliseconds(1));

        bool hasValue = cache.HasValue; // false
        int value = cache.GetValue();   // factory invoked
        hasValue = cache.HasValue;      // true

        Thread.Sleep(10);
        value = cache.GetValue();       // factory invoked
        hasValue = cache.HasValue;      // true
    }

    public static async Task ValueCacheAsyncExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        using ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.FromMilliseconds(1));

        bool hasValue = cache.HasValue;                                     // false
        int value = await cache.GetValueAsync(factory, cancellationToken);  // factory invoked
        hasValue = cache.HasValue;                                          // true

        await Task.Delay(10);
        value = await cache.GetValueAsync(factory, cancellationToken);      // factory invoked
        hasValue = cache.HasValue;                                          // true
    }
```

#### Key-Value Caches

Key-value caches allow to cache and automatically refresh multiple values within a single data structure.

```csharp
    using LibSharp.Caching;

    public static void KeyValueCacheExample(Func<string, int> factory)
    {
        KeyValueCache<string, int> cache = new KeyValueCache<string, int>(factory, TimeSpan.FromMinutes(1));

        int valueA = cache.GetValue("a");   // factory invoked for "a"
        int valueB = cache.GetValue("b");   // factory invoked for "b"

        valueA = cache.GetValue("a");       // factory not invoked
        valueB = cache.GetValue("b");       // factory not invoked
    }

    public static async Task KeyValueCacheAsyncExample(Func<string, CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        using KeyValueCacheAsync<string, int> cache = new KeyValueCacheAsync<string, int>(factory, TimeSpan.FromMinutes(1));

        KeyValueCache<string, int> cache = new KeyValueCache<string, int>(factory, TimeSpan.FromMinutes(1));

        int valueA = await cache.GetValueAsync("a", cancellationToken); // factory invoked for "a"
        int valueB = await cache.GetValueAsync("b", cancellationToken); // factory invoked for "b"

        valueA = await cache.GetValueAsync("a", cancellationToken);     // factory not invoked
        valueB = await cache.GetValueAsync("b", cancellationToken);     // factory not invoked
    }
```
