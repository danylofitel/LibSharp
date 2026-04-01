# LibSharp

## Introduction

A library of C# core components that enhance the standard library. Supports .NET 8.0, .NET 9.0, .NET 10.0.

* Source code: <https://github.com/danylofitel/LibSharp>.
* NuGet package: <https://www.nuget.org/packages/LibSharp>.

LibSharp consists of the following namespaces:

* Common - contains extension methods for standard .NET types, as well as commonly used utilities and value types.
* Collections - contains extension methods for standard .NET library collections, as well as additional collection types.
* Caching - contains classes that enable in-memory value caching with custom time-to-live. Both synchronous and asynchronous versions are available.
* Threading - contains an async-compatible lock and utilities for controlling action invocation frequency.

## Performance Benchmarks

BenchmarkDotNet setup and benchmark scripts are available in [`benchmarks/`](benchmarks/README.md).

## Components and Usage

### Common

`Common` namespace contains:

* The static class `Argument` for convenient validation of public function arguments.
* Extension methods for built-in types such as `string`, `int`, `DateTime`, `Func`, and `Regex`.
* `Optional<T>` — a value type that wraps an optional value.
* `Result<T, TError>` — a discriminated union value type for success/error outcomes.

```csharp
    using LibSharp.Common;

    public static async Task CommonExamples(string stringParam, long longParam, object objectParam, CancellationToken cancellationToken)
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
        Argument.NotNullOrWhiteSpace(stringParam, nameof(stringParam));

        Argument.OfType(objectParam, typeof(List<string>), nameof(objectParam));

        // Optional<T> — wraps a value that may or may not be present
        Optional<int> empty = default;
        bool hasValue = empty.HasValue;             // false
        int fallback = empty.GetValueOrDefault(-1); // -1

        Optional<int> present = new Optional<int>(42);
        hasValue = present.HasValue;                // true
        int optValue = present.Value;               // 42
        bool got = present.TryGetValue(out int v);  // true, v == 42

        // Result<T, TError> — discriminated union for success/error outcomes
        Result<int, string> success = Result<int, string>.Ok(42);
        bool isSuccess = success.IsSuccess;                     // true
        int successValue = success.Value;                       // 42

        Result<int, string> failure = Result<int, string>.Fail("not found");
        bool isError = failure.IsError;                         // true
        string errorMessage = failure.Error;                    // "not found"
        int valueOrDefault = failure.GetValueOrDefault(-1);     // -1

        // DateTime extensions
        DateTime fromEpochMilliseconds = longParam.FromEpochMilliseconds();
        DateTime fromEpochSeconds = longParam.FromEpochSeconds();
        long epochMilliseconds = DateTime.UtcNow.ToEpochMilliseconds();
        long epochSeconds = DateTime.UtcNow.ToEpochSeconds();

        // Func extensions — run an async operation with a cooperative timeout
        Func<CancellationToken, Task<int>> task = async ct =>
        {
            // Example operation that observes cancellation
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return 99;
        };

        int taskResult = await task.RunWithTimeout(TimeSpan.FromSeconds(1), cancellationToken);

        // Int extensions
        bool convertedFromInt = 200.TryConvertToEnum<HttpStatusCode>(out HttpStatusCode statusCode);

        // String extensions
        bool convertedFromString = "OK".TryConvertToEnum<HttpStatusCode>(out HttpStatusCode statusCode2);

        string base64Encoded = stringParam.Base64Encode();
        string base64Decoded = base64Encoded.Base64Decode();

        string reversed = stringParam.Reverse();
        string truncated = stringParam.Truncate(10);
        string textElementTruncated = stringParam.TruncateTextElements(10);

        // Regex extensions — safe wrappers that catch RegexMatchTimeoutException
        Regex regex = new Regex(pattern: "\\s+brown\\s+", options: RegexOptions.None, matchTimeout: TimeSpan.FromSeconds(1));

        bool isMatch = regex.TryIsMatch("the quick brown fox", out bool isMatchTimedOut);
        Match match = regex.TryMatch("the quick brown fox", out bool matchTimedOut);
        string replaced = regex.TryReplace("the quick brown fox", " red ", out bool replaceTimedOut);

        // Type extensions
        IComparer<int> intComparer = TypeExtensions.GetDefaultComparer<int>();

        // XML serialization extensions
        string serializedToXml = objectParam.SerializeToXml();
        List<string> deserializedFromXml = serializedToXml.DeserializeFromXml<List<string>>();
    }
```

### Collections

`Collections` namespace contains extension methods for `ICollection`, `IDictionary`, `IEnumerable`, and `IAsyncEnumerable` interfaces, plus `ConcurrentHashSet<T>`, `MinPriorityQueue<T>`, and `MaxPriorityQueue<T>` collections.

```csharp
    using LibSharp.Collections;

    public static async Task CollectionsExamples(CancellationToken cancellationToken)
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
        List<List<int>> chunks = Enumerable.Range(0, 10).Chunk(20, item => item).ToList();
        // Grouped by total weight ≤ 20: [ [0, 1, 2, 3, 4, 5], [6, 7], [8, 9] ]

        IEnumerable<int> enumerable = Enumerable.Range(0, 100).Concat(Enumerable.Range(0, 100)).ToList();
        int firstIndex = enumerable.FirstIndexOf(x => x == 51);  // 51
        int lastIndex = enumerable.LastIndexOf(x => x == 51);    // 151

        int[] shuffled = enumerable.Shuffle();

        // IAsyncEnumerable extensions
        IAsyncEnumerable<int> asyncEnumerable = GetNumbersAsync();
        List<List<int>> asyncChunks = await CollectAsync(asyncEnumerable.Chunk(20, item => item), cancellationToken);
        // Grouped by total weight ≤ 20: [ [0, 1, 2, 3, 4, 5], [6, 7], [8, 9], ... ]

        int asyncFirstIndex = await asyncEnumerable.FirstIndexOfAsync(x => x == 51, cancellationToken);
        int asyncLastIndex = await asyncEnumerable.LastIndexOfAsync(x => x == 51, cancellationToken);

        // ConcurrentHashSet<T> — thread-safe hash set implementing ISet<T> and IReadOnlySet<T>
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();
        bool added = set.Add(1);         // true
        added = set.Add(1);              // false — already present
        bool contains = set.Contains(1); // true
        bool removed = set.Remove(1);    // true

        // Set algebra operations (not atomic at the collection level)
        set.UnionWith(new[] { 2, 3 });
        set.IntersectWith(new[] { 2, 4 });
        set.ExceptWith(new[] { 4 });
        bool subset = set.IsSubsetOf(new[] { 1, 2, 3 });
        bool equal = set.SetEquals(new[] { 2 });

        // Min priority queue
        MinPriorityQueue<int> minPq = new MinPriorityQueue<int>();
        minPq.Enqueue(2);
        minPq.Enqueue(1);
        minPq.Enqueue(3);

        _ = minPq.Peek();    // 1 — smallest element, not removed
        _ = minPq.Dequeue(); // 1
        _ = minPq.Dequeue(); // 2
        _ = minPq.Dequeue(); // 3

        bool minHasValue = minPq.TryPeek(out int minPeeked);
        bool minRemoved = minPq.TryDequeue(out int minDequeued);

        // Max priority queue
        MaxPriorityQueue<int> maxPq = new MaxPriorityQueue<int>();
        maxPq.Enqueue(2);
        maxPq.Enqueue(1);
        maxPq.Enqueue(3);

        _ = maxPq.Peek();    // 3 — largest element, not removed
        _ = maxPq.Dequeue(); // 3
        _ = maxPq.Dequeue(); // 2
        _ = maxPq.Dequeue(); // 1

        bool maxHasValue = maxPq.TryPeek(out int maxPeeked);
        bool maxRemoved = maxPq.TryDequeue(out int maxDequeued);
    }

    private static async IAsyncEnumerable<int> GetNumbersAsync()
    {
        for (int i = 0; i < 200; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        List<T> results = new List<T>();

        await foreach (T item in source.WithCancellation(cancellationToken))
        {
            results.Add(item);
        }

        return results;
    }
```

### Threading

`Threading` namespace contains an async-compatible mutual exclusion lock and utilities for controlling how frequently an action can fire.

```csharp
    using LibSharp.Threading;

    public static async Task ThreadingExamples(CancellationToken cancellationToken)
    {
        // AsyncLock — async-compatible mutual exclusion lock (not re-entrant)
        using AsyncLock asyncLock = new AsyncLock();

        using (AsyncLock.Handle handle = await asyncLock.AcquireAsync(cancellationToken))
        {
            // Only one caller can be inside this block at a time
        }

        // DebouncedAction — fires only after a quiet period since the last invocation
        using DebouncedAction debounced = new DebouncedAction(
            () => Console.WriteLine("Fired"),
            delay: TimeSpan.FromMilliseconds(300));

        debounced.Invoke(); // timer starts
        debounced.Invoke(); // timer resets
        debounced.Invoke(); // timer resets again — action fires 300 ms after this last call
        // Important: do not call debounced.Dispose() from inside its callback.
        // Dispose waits for callback completion and can deadlock in that pattern.

        // ThrottledAction — executes at most once per interval
        ThrottledAction throttled = new ThrottledAction(
            () => Console.WriteLine("Fired"),
            interval: TimeSpan.FromSeconds(1));

        throttled.Invoke(); // executes immediately
        throttled.Invoke(); // ignored — within the 1-second window
        await Task.Delay(TimeSpan.FromSeconds(1));
        throttled.Invoke(); // executes again — window has expired
    }
```

### Caching

`Caching` namespace contains a number of classes for thread-safe lazy initialization and caching of in-memory values.

Notes:

* Some of the classes implement `IDisposable` interface and should be correctly disposed.
* Be cautious when caching types that implement `IDisposable` interface as the values will not be automatically disposed by the caches.
* Be cautious when using classes with `LazyThreadSafetyMode.PublicationOnly` behavior together with `IDisposable` types as discarded instances will not be disposed.
* `PublicationOnly` implementations may run multiple factories concurrently and publish the first successful result.
* Async lazy and initializer methods throw `InvalidOperationException` if a factory returns a null `Task`.

#### Lazy

Two different implementations of async lazy values are available — `LazyAsyncPublicationOnly` and `LazyAsyncExecutionAndPublication`. Those are async versions of `System.Lazy` class with `LazyThreadSafetyMode.PublicationOnly` and `LazyThreadSafetyMode.ExecutionAndPublication` modes respectively. The reason that async lazy implementations are separate classes is that `LazyAsyncExecutionAndPublication` implements `IDisposable` due to its usage of an instance of `SemaphoreSlim` whereas `LazyAsyncPublicationOnly` does not need to implement `IDisposable`.

`LazyAsyncExecutionAndPublication` runs at most one in-flight factory and retries after failed or canceled attempts. `LazyAsyncPublicationOnly` may execute multiple concurrent factories, but only the first successfully published value is retained.

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

`InitializerAsyncExecutionAndPublication` runs at most one in-flight factory and retries after failed or canceled attempts. `InitializerAsyncPublicationOnly` may execute multiple concurrent factories, but only the first successfully published value is retained.

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

Note that `ValueCacheAsync` guarantees `LazyThreadSafetyMode.ExecutionAndPublication` behavior and implements `IDisposable`.

```csharp
    using LibSharp.Caching;

    public static void ValueCacheExample(Func<int> factory)
    {
        ValueCache<int> cache = new ValueCache<int>(factory, TimeSpan.FromMilliseconds(1));

        bool hasValue = cache.HasValue; // false
        int value = cache.GetValue();   // factory invoked
        hasValue = cache.HasValue;      // true

        Thread.Sleep(10);
        value = cache.GetValue();       // factory invoked again — TTL expired
        hasValue = cache.HasValue;      // true
    }

    public static async Task ValueCacheAsyncExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        using ValueCacheAsync<int> cache = new ValueCacheAsync<int>(factory, TimeSpan.FromMilliseconds(1));

        bool hasValue = cache.HasValue;                             // false
        int value = await cache.GetValueAsync(cancellationToken);   // factory invoked
        hasValue = cache.HasValue;                                  // true

        await Task.Delay(10);
        value = await cache.GetValueAsync(cancellationToken);       // factory invoked again — TTL expired
        hasValue = cache.HasValue;                                  // true
    }
```

#### Key-Value Caches

Key-value caches allow caching and automatically refreshing multiple values within a single data structure.

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

        int valueA = await cache.GetValueAsync("a", cancellationToken); // factory invoked for "a"
        int valueB = await cache.GetValueAsync("b", cancellationToken); // factory invoked for "b"

        valueA = await cache.GetValueAsync("a", cancellationToken);     // factory not invoked
        valueB = await cache.GetValueAsync("b", cancellationToken);     // factory not invoked
    }
```

#### Proactive Async Cache

`ProactiveAsyncCache` is an async cache that proactively refreshes its value in the background before it expires. It starts a background loop that re-fetches the value at a configurable interval. A pre-fetch offset allows refresh to happen before expiration, reducing the chance that callers need to wait for the factory.

```csharp
    using LibSharp.Caching;

    public static async Task ProactiveAsyncCacheExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        // Default options: background loop starts automatically, stale reads disabled
        await using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            factory,
            refreshInterval: TimeSpan.FromMinutes(5),
            preFetchOffset: TimeSpan.FromSeconds(30));

        bool hasValue = cache.HasValue;                             // false — until first background fetch completes
        int value = await cache.GetValueAsync(cancellationToken);   // waits for background fetch if not yet complete
        hasValue = cache.HasValue;                                  // true
        value = await cache.GetValueAsync(cancellationToken);       // returns cached value
    }

    public static async Task ProactiveAsyncCacheWithOptionsExample(Func<CancellationToken, Task<int>> factory, CancellationToken cancellationToken)
    {
        await using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            factory,
            refreshInterval: TimeSpan.FromMinutes(5),
            preFetchOffset: TimeSpan.FromSeconds(30),
            allowStaleReads: true);                                 // return the previous value while a refresh is in progress

        int value = await cache.GetValueAsync(cancellationToken);
    }
```
