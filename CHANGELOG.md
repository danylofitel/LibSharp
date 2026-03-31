# Changelog

- 3.0.0
  - Added `AsyncLock`, `DebouncedAction`, `ThrottledAction`, `ConcurrentHashSet`, and `Result` classes
  - Renamed `Box` to `Optional`; null values are now allowed
  - `ProactiveAsyncCache` constructor now accepts a `ProactiveAsyncCacheOptions` object with additional controls
  - Regex extensions now return a `bool` indicating whether the regex match timed out

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
