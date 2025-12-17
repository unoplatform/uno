#nullable enable

using System;
using System.Reflection;

namespace Uno.UI.Helpers;

internal class WeakDelegate
{
	public static WeakDelegate<TDelegate>? Create<TDelegate>(TDelegate? d)
		where TDelegate : Delegate
	{
		return d is { } ? new(d) : null;
	}
}

/// <summary>
/// Represents a delegate that references its <see cref="Delegate.Target"/> weakly, allowing the target to be eventually garbage collected.
/// </summary>
/// <remarks>Use <see cref="WeakDelegate{TDelegate}"/> to hold a reference to a delegate without preventing its
/// target object from being collected by the garbage collector. This is useful for scenarios where you want to
/// avoid memory leaks caused by strong references. Only delegates with a single target are supported;
/// multicast delegates are not allowed.</remarks>
/// <typeparam name="TDelegate">The type of delegate to be referenced. Must derive from <see cref="Delegate"/>.</typeparam>
internal class WeakDelegate<TDelegate> where TDelegate : Delegate
{
	public WeakReference? Instance { get; init; }
	public MethodInfo Method { get; init; }

	// static method delegate doesn't capture any target instance, so we can just reuse it
	private TDelegate? _staticDelegate;

	public WeakDelegate(TDelegate d)
	{
		if (!d.HasSingleTarget)
		{
			throw new NotImplementedException("Multi-cast delegate not supported");
		}

		if (d.Target is { } t)
		{
			Instance = new WeakReference(t);
			Method = d.Method;
		}
		else
		{
			_staticDelegate = d;
			Method = d.Method; // still used outside of this class for logging
		}
	}

	public object? Target => Instance?.Target;

	/// <summary>
	/// Gets the delegate instance that targets the referenced object and method,
	/// may be null if the target is no longer available.
	/// </summary>
	/// <remarks>
	/// DO NOT store/cache the returned delegate, as it keeps a strong reference to the target just like the original delegate.
	/// </remarks>
	public TDelegate? Delegate =>
		_staticDelegate ??
		// for instanced method delegate, only try to create if the instance reference is still alive
		(Instance!.Target is { } t
			? System.Delegate.CreateDelegate(typeof(TDelegate), t, Method) as TDelegate
			: null);
}
