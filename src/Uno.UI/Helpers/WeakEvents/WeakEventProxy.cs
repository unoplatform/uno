#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Helpers;

/// <summary>
/// An abstract base class for subscribing to an event via WeakReference.
/// See WeakNotifyCollectionChangedProxy below for sublcass usage.
/// </summary>
/// <typeparam name="TSource">The object type that has the event</typeparam>
/// <typeparam name="TEventHandler">The event handler type of the event</typeparam>
internal abstract class WeakEventProxy<TSource, TEventHandler>
	where TSource : class
	where TEventHandler : Delegate
{
	WeakReference<TSource>? _source;
	WeakReference<TEventHandler>? _handler;

	public bool TryGetSource([MaybeNullWhen(false)] out TSource source)
	{
		if (_source is not null && _source.TryGetTarget(out source))
		{
			return source is not null;
		}

		source = default;
		return false;
	}

	public bool TryGetHandler([MaybeNullWhen(false)] out TEventHandler handler)
	{
		if (_handler is not null && _handler.TryGetTarget(out handler))
		{
			return handler is not null;
		}

		handler = default;
		return false;
	}

	public virtual void Subscribe(TSource source, TEventHandler handler)
	{
		_source = new WeakReference<TSource>(source);
		_handler = new WeakReference<TEventHandler>(handler);
	}

	public virtual void Unsubscribe()
	{
		_source = null;
		_handler = null;
	}
}

