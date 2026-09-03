# Changelog

- 5.0.0
  - `Caching`
    - **Breaking:** `GetValueAsync` now returns `ValueTask<T>` instead of `Task<T>` on `IValueCacheAsync<T>`, `IKeyValueCacheAsync<TKey, TValue>`, `IInitializerAsync<T>` and all their implementations. A cache read usually completes synchronously, and that path no longer allocates. Await the result at most once, never concurrently, and call `AsTask()` before storing it or handing it to `Task.WhenAll`. The factory delegates deliberately still take and return `Task<T>`: they perform the real work and never complete synchronously, so a value task there would be caller friction for no gain
    - **Breaking:** `ProactiveAsyncCache<T>` is now configured through `ProactiveAsyncCacheOptions` instead of constructor parameters. The old positional constructor is gone; a three-argument convenience overload `(valueFactory, refreshInterval, preFetchOffset)` remains for the common case and will never gain further parameters. Settings live on the options object so that adding one later is not a binary breaking change, which an optional constructor parameter always is
    - **Breaking:** `allowStaleReads` is replaced by `StaleReadPolicy`, a closed set of cases rather than a flag: `Wait` (the default), `ServeStale`, and `ServeStaleUpTo(maxStaleness)`. The bound caps how long past expiration a value may still be served; beyond it readers wait, so a persistent failure reaches them as an exception instead of hiding behind an ever-older value. A `TimeSpan` on one case is why this is a type and not an enum: an enum plus a separate maximum-age setting would permit combinations that mean nothing
    - Added `FetchTimeout` to `ProactiveAsyncCacheOptions`, bounding a single value factory invocation. An overrun is reported as `TimeoutException` and recorded as a failure. It also bounds `DisposeAsync`, which otherwise waits as long as the factory takes, and only helps if the factory honours its token
    - **Breaking:** `ProactiveAsyncCache<T>` now accepts an optional `idleTimeout`; when set, the background refresh loop suspends itself once `GetValueAsync` has not been called for that long, holding no timer and consuming no CPU, and the next read resumes it immediately. Only `GetValueAsync` counts as activity, not `HasValue` or `Expiration`
    - **Breaking:** a caller's `CancellationToken` no longer cancels the shared refresh in `ValueCacheAsync<T>` (and therefore `KeyValueCacheAsync<TKey, TValue>`). Previously the value factory ran on whichever caller's token happened to trigger it, so one caller giving up cancelled a refresh other callers were waiting on and discarded work the cache was about to publish. The factory now runs on a token scoped to the cache instance, cancelled only by `Dispose()`, and a caller's token cancels that caller's wait alone. Concurrent callers now share a single factory invocation instead of serialising through a lock
    - **Breaking:** `LazyAsyncExecutionAndPublication<T>` and `InitializerAsyncExecutionAndPublication<T>` are no longer `IDisposable`. They held an `AsyncLock` purely to serialise initialization; they now publish a shared initialization task instead, so they own nothing that needs releasing. Remove any `using` around them. As with the caches, the factory no longer receives the caller's token — it runs with `CancellationToken.None`, and a caller's token cancels that caller's wait alone. Concurrent callers still share exactly one factory execution, and faulted or cancelled attempts are still not cached
    - `ValueCacheAsync<T>` no longer holds a lock across the value factory: it publishes a shared refresh task the way `ProactiveAsyncCache<T>` does. A factory that synchronously re-enters the cache now joins that refresh instead of deadlocking; one that awaits the nested read still deadlocks
    - Fixed an already-cancelled `CancellationToken` being ignored by `GetValueAsync` on `ValueCacheAsync<T>` and `ProactiveAsyncCache<T>` when the shared fetch had already completed. A cache hit is still served, since it does no waiting
    - Argument validation and disposal checks in `GetValueAsync` now throw synchronously rather than returning a faulted task, following the convention for `ValueTask`-returning members
    - Added `Count` to `KeyValueCache<TKey, TValue>` and `KeyValueCacheAsync<TKey, TValue>`, reporting the number of entries held. Deliberately on the concrete types rather than the interfaces, following `MemoryCache`/`IMemoryCache`: the number means different things for evicting and non-evicting implementations, may be expensive or unavailable for a remote one, and reading it takes every bucket lock of the underlying `ConcurrentDictionary`. Since nothing is evicted, it counts entries whose value has expired, which makes it the measure to watch when confirming a key space is bounded
    - Added `LastRefreshException`, `ConsecutiveRefreshFailures` and `LastSuccessfulRefresh` to `ProactiveAsyncCache<T>`. A failing background refresh was previously invisible: with `allowStaleReads` enabled callers keep receiving an arbitrarily old value and never see an error, so nothing could tell a health check that the value had stopped being updated. The failure state is cleared by the next successful refresh
    - `ProactiveAsyncCache<T>` no longer retries a failing value factory on every read. A faulted fetch is a completed task, so previously each subsequent read started a fresh factory call with no delay at all: a dependency failing fast turned a multi-minute refresh interval into one call per read, against a service already under strain. The last failure is now recorded, and reads within the retry window replay the stored exception instead of calling the factory. With `allowStaleReads` the stale value is served instead. A successful fetch clears the record. The background loop is exempt, since it already paces its own retries
    - `ValueCache<T>` and `KeyValueCache<TKey, TValue>` now throw `InvalidOperationException` when the value factory reads the cache it is refreshing, the way `Lazy<T>` reports recursive initialization. Previously the re-entrant read found no published value and called the factory again, recursing until the stack overflowed and took the process with it. Re-entering `KeyValueCache` for a different key is still allowed
    - `KeyValueCache<TKey, TValue>` and `KeyValueCacheAsync<TKey, TValue>` no longer allocate a delegate on every read. The `GetOrAdd` factory captured `this`, and Roslyn only caches closure-free lambdas, so one was allocated per call rather than per insert
  - `Threading`
    - **Breaking:** `AsyncLock.AcquireAsync` now returns `ValueTask<Handle>` instead of `Task<Handle>`; an uncontended acquisition no longer allocates

- 4.0.0
  - Enabled nullable reference type annotations across the entire public API; `TryGet*` methods and out parameters are now annotated (e.g. `[MaybeNullWhen(false)]`), and nullable inputs such as optional `Encoding`/`XmlReaderSettings` arguments are marked accordingly
  - `Caching`
    - Updated `ProactiveAsyncCache<T>` to never throw exceptions from `DisposeAsync()`
    - `ValueCache<T>`, `ValueCacheAsync<T>`, `KeyValueCache<TKey, TValue>`, `KeyValueCacheAsync<TKey, TValue>`, and `ProactiveAsyncCache<T>` now accept an optional `TimeProvider` (defaulting to `TimeProvider.System`) so expiration and background refresh can be driven deterministically in tests
  - `Collections`
    - Renamed extension classes to drop the `I` prefix: `IEnumerableExtensions` → `EnumerableExtensions`, `ICollectionExtensions` → `CollectionExtensions`, `IDictionaryExtensions` → `DictionaryExtensions`, `IAsyncEnumerableExtensions` → `AsyncEnumerableExtensions` (extension methods called via instance syntax are unaffected; static-style calls must use the new names)
    - `MinPriorityQueue<T>` and `MaxPriorityQueue<T>`: `Contains` and `Remove` now use element equality (`EqualityComparer<T>.Default`) instead of the ordering comparer, so they honour the `ICollection<T>` contract (reverses the 3.0.0 change; ordering still uses the comparer)
    - `ConcurrentHashSet<T>` now constrains `T` to `notnull` (it is backed by `ConcurrentDictionary`, which never permitted null elements); `IDictionaryExtensions.Copy` likewise constrains its key to `notnull`
    - `DictionaryExtensions` and the key-value caches now validate keys without boxing value-type keys
  - `Common`
    - `Optional<T>` no longer implements `IEquatable<T>`; it now implements only `IEquatable<Optional<T>>`, so equality is defined between two optionals. A bare value still compares equal via the new implicit conversion, but a value typed as `object` never does
    - Added an implicit conversion from `T` to `Optional<T>` (always produces a present optional, even for `null`)
    - Added `Match`, `Map`, and `Bind` to `Optional<T>`
    - Added `Match`, `Map`, `MapError`, and `Bind` to `Result<T, TError>`
    - Removed the `Argument.NotNull(object, string)` overload; calling `NotNull` on a non-nullable value type is now a compile error instead of a silent no-op (the reference-type generic overload is retained)
    - `Argument` methods now capture the argument name automatically via `[CallerArgumentExpression]`, so the `name` parameter is optional; existing calls that pass it explicitly still compile
    - `XmlSerializationExtensions.SerializeToXml` / `DeserializeFromXml` are now annotated with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` to reflect that `XmlSerializer` is incompatible with trimming and Native AOT
  - `Threading`
    - `ThrottledAction` and `DebouncedAction` now accept an optional `TimeProvider` (defaulting to `TimeProvider.System`) so the throttle interval and debounce timer can be driven deterministically in tests
    - `ThrottledAction` now clamps its interval-to-ticks conversion so an extreme interval near `TimeSpan.MaxValue` cannot overflow into a negative value and defeat throttling

- 3.0.0
  - `Caching`
    - Async cache/lazy/initializer factories now reject null task returns with a deliberate exception instead of failing with `NullReferenceException`
    - `ProactiveAsyncCache<T>` no longer implements `IDisposable`; use `await using` / `DisposeAsync()` instead
    - `ProactiveAsyncCache` no longer supports `refreshTimeout` and `onBackgroundRefreshError` parameters, and now always auto-starts in constructor
  - `Collections`
    - Added `ConcurrentHashSet`
    - Added weighted `Chunk` extension method for `IAsyncEnumerable<T>`
    - Added `TryPeek` and `TryDequeue` to `IPriorityQueue<T>`, `MinPriorityQueue<T>`, and `MaxPriorityQueue<T>`
    - `MinPriorityQueue<T>` and `MaxPriorityQueue<T>`: `Contains` and `Remove` now use the queue's comparer instead of `object.Equals`, making them consistent with the ordering relation
  - `Common`
    - Added `Result`
    - Renamed `Box` to `Optional`; null values are now allowed
    - `Optional<T>.GetHashCode` now differentiates between an empty optional and an optional wrapping `null`
    - Added `StringExtensions.TruncateTextElements` for text-element-aware truncation
    - `DateTimeExtensions` epoch conversions now use Unix-time floor semantics instead of rounding fractional units
    - `TypeExtensions.GetDefaultComparer` now supports types implementing non-generic `IComparable`
    - Regex extensions now return a `bool` indicating whether the regex match timed out
    - `FuncExtensions.RunWithTimeout`: timeout must now be strictly greater than zero
    - `XmlSerializationExtensions`: `XmlSerializer` instances are now cached per type to avoid repeated dynamic assembly generation
  - `Threading`
    - Added `AsyncLock`, `DebouncedAction`, and `ThrottledAction`

- 2.0.4
  - Improved disposal of async caches in edge cases

- 2.0.3
  - `ProactiveAsyncCache` now calculates retry delay based on the refresh interval and pre-fetch offset
  - Minor bug fixes and improvements

- 2.0.2
  - `ProactiveAsyncCache` now supports an optional `refreshTimeout` parameter
  - Minor bug fixes and improvements

- 2.0.1
  - `ProactiveAsyncCache` now supports stale reads mode
  - `ProactiveAsyncCache` now accepts an action to handle failed background refreshes

- 2.0.0
  - Dropped support for .NET Standard 2.0, .NET Standard 2.1, .NET 5.0, .NET 6.0, and .NET 7.0
  - Added support for .NET 10.0
  - Removed `DateTimeExtensions.UnixEpoch`
  - Added `ProactiveAsyncCache`
  - Added `TryConvertToEnum` extension method for `string`
  - Bug fixes and thread safety improvements

- 1.1.6
  - Added `TryConvertToEnum` extension method for `int`

- 1.1.5
  - Added Regex extension methods that handle regex timeouts gracefully
  - Added `Func` extension methods that run asynchronous operations with a timeout

- 1.1.4
  - Added support for .NET 9.0

- 1.1.3
  - Updated NuGet package tags and description

- 1.1.2
  - Added constructors to `KeyValueCache` and `KeyValueCacheAsync` that accept separate factories for creates and updates
  - Added the ability to specify a custom expiration function

- 1.1.1
  - Changed the return type of the `Shuffle` extension method from `IEnumerable<T>` to `T[]`
  - Fixed the signature of `SerializeToXml` so it can be invoked as an extension method
  - All `IDisposable` types now throw `ObjectDisposedException` when a member is accessed after disposal

- 1.1.0
  - Added support for .NET Standard 2.0, .NET Standard 2.1, .NET 5.0, .NET 6.0, and .NET 7.0

- 1.0.0
  - Initial release targeting .NET 8.0
