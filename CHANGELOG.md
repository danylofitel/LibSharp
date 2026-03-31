# Changelog

- 3.0.0: Added AsyncLock. Renamed Box to Optional, allowed null values. ProactiveAsyncCache constructor now accepts ProactiveAsyncCacheOptions with more controls.
- 2.0.4: More robust disposal of async caches in edge cases.
- 2.0.3: ProactiveAsyncCache now calculates retry delay based on refresh interval and pre-fetch offset. Minor bug fixes and improvements.
- 2.0.2: ProactiveAsyncCache now supports an optional refreshTimeout parameter. Minor bug fixes and improvements.
- 2.0.1: ProactiveAsyncCache now supports stale reads mode and an action for failed background refreshes.
- 2.0.0: Dropped support for .NET Standard 2.0, .NET Standard 2.1, .NET 5.0, .NET 6.0, and .NET 7.0. Added support for .NET 10.0. Removed DateTimeExtensions.UnixEpoch. Added ProactiveAsyncCache. Added TryConvertToEnum extension method for string type. Bug fixes and thread safety improvements.
- 1.1.6: Added TryConvertToEnum extension method for int type.
- 1.1.5: Added Regex extension methods that handle regex timeouts. Added Func extension methods that run asynchronous operations with a timeout.
- 1.1.4: Added support for .NET 9.0.
- 1.1.3: Updated tags and description.
- 1.1.2: Added more constructors to KeyValueCache and KeyValueCacheAsync that allow to provide separate factories for creates and updates, and to specify custom expiration function.
- 1.1.1: Changed return type of Shuffle extension method to return an array instead of an IEnumerable. Fixed the signature of SerializeToXml extension method so it can be called as an extension. Updated all IDisposable types to throw ObjectDisposedException if a property is accessed on a disposed instance.
- 1.1.0: Added support for .NET Standard 2.0, .NET Standard 2.1, .NET 5.0, .NET 6.0, .NET 7.0.
- 1.0.0: Initial release for .NET 8.0.
