// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// Disposal of values produced by a losing racer in a publication-only initialization.
/// </summary>
/// <remarks>
/// A publication-only type lets concurrent callers each run the factory and keeps whichever value is
/// published first. The compare-exchange that publishes the winner names the losers exactly — it
/// returns the value already in place — so a dropped value is known, with no race and no guessing,
/// never to have been handed to any caller. Nobody else can dispose it, which is why this type does.
/// </remarks>
internal static class DroppedValue
{
    /// <summary>
    /// Disposes a value that lost the publication race, if it holds resources.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="dropped">The value that was not published.</param>
    /// <param name="published">The value that was published, used to rule out a shared instance.</param>
    /// <returns>A task that completes once the dropped value has been released.</returns>
    public static ValueTask DisposeAsync<T>(T dropped, T published)
    {
        object? droppedObject = dropped;
        if (droppedObject is null)
        {
            return default;
        }

        // Rule out the factory having handed every racer the same object, which disposing would
        // destroy along with the value just published.
        //
        // This applies to reference types only, where identity is exact. A value type is copied per
        // racer, so no test can tell a shared underlying resource from two distinct ones: equality
        // would skip disposal for genuinely separate values that merely compare equal, which leaks
        // for a factory that honours the exclusive-ownership precondition in order to protect one
        // that does not. Value types are therefore disposed on the precondition alone, and a factory
        // that cannot meet it should turn disposal off.
        if (!typeof(T).IsValueType && ReferenceEquals(droppedObject, published))
        {
            return default;
        }

        if (droppedObject is not IAsyncDisposable and not IDisposable)
        {
            return default;
        }

        return DisposeCoreAsync(droppedObject);
    }

    private static async ValueTask DisposeCoreAsync(object dropped)
    {
        try
        {
            if (dropped is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                ((IDisposable)dropped).Dispose();
            }
        }
        catch
        {
            // The caller is about to receive a perfectly good value. A failure while cleaning up one
            // it never saw must not surface as the result of its own call, and there is no other
            // caller to report it to.
        }
    }
}
