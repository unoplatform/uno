#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Helpers;

// Original source: https://github.com/dotnet/maui/blob/fe4bc5b8258f4abe037f0d3c3f54de1facfef853/src/Controls/src/Core/Internals/WeakEventProxy.cs

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
	private WeakReference<TSource>? _source;
	private WeakReference<TEventHandler>? _handler;

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

