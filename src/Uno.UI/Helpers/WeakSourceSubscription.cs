#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Uno.UI.Helpers;

/// <summary>
/// An event subscription held by its subscriber only, so a subscriber dropped without being disposed doesn't leave its
/// handler on the source: the finalizer removes it. The source is held by a raw weak handle (no finalizer of its own,
/// unlike a pooled ManagedWeakReference), so a source that dies with the subscriber is collected at once and needs no removal.
/// </summary>
/// <remarks>
/// Pass static lambdas: they get everything they need through <typeparamref name="TState"/>, and a capture of the
/// source would keep it alive.
/// </remarks>
internal sealed class WeakSourceSubscription<TSource, TState> : IDisposable, IQueuedRemoval
	where TSource : class
{
	private readonly TState _state;
	private readonly Action<TSource, TState> _unsubscribe;
	private readonly Action<TState>? _onDisposed;
	private readonly Type _logOwner;
	private readonly string _description;
	private IntPtr _source;

	/// <param name="state">Must not reference <paramref name="source"/>.</param>
	/// <param name="onDisposed">Runs on an explicit Dispose only, not from the finalizer.</param>
	public WeakSourceSubscription(
		TSource source,
		TState state,
		Action<TSource, TState> unsubscribe,
		Action<TState>? onDisposed,
		Type logOwner,
		string description)
	{
		_state = state;
		_unsubscribe = unsubscribe;
		_onDisposed = onDisposed;
		_logOwner = logOwner;
		_description = description;
		_source = GCHandle.ToIntPtr(GCHandle.Alloc(source, GCHandleType.Weak));
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);

		if (TakeSource(out var source))
		{
			if (source is not null)
			{
				SubscriptionCleanup.Run(_unsubscribe, source, _state, _logOwner, _description);
			}

			_onDisposed?.Invoke(_state);
		}
	}

	~WeakSourceSubscription()
	{
		if (TakeSource(out var source) && source is not null)
		{
			SubscriptionCleanup.EnqueueFromFinalizer(this, source);
		}
	}

	Type IQueuedRemoval.LogOwner => _logOwner;

	string IQueuedRemoval.Description => _description;

	void IQueuedRemoval.Remove(object source)
		=> SubscriptionCleanup.Run(_unsubscribe, (TSource)source, _state, _logOwner, _description);

	// Frees the handle exactly once: returns false if it was already taken.
	private bool TakeSource(out TSource? source)
	{
		var pointer = Interlocked.Exchange(ref _source, IntPtr.Zero);
		if (pointer == IntPtr.Zero)
		{
			source = null;
			return false;
		}

		var handle = GCHandle.FromIntPtr(pointer);
		source = handle.Target as TSource;
		handle.Free();
		return true;
	}
}
