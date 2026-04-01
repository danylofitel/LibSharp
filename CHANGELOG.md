# Changelog

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
