#if !UNO_HAS_ENHANCED_LIFECYCLE

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Dispatching;

namespace Uno.UI.Xaml.Core;

// partial implementation of EventManager
internal interface ICustomEventManager<T, TEventArgs>
	where T : class
	where TEventArgs : class
{
	void Enqueue(T sender, TEventArgs args);

	/// <remarks>
	/// Do not call directly.
	/// </remarks>
	void OnTick();
}

internal sealed class CustomKeepLastEventManager<
	T,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TEventArgs
> : ICustomEventManager<T, TEventArgs>
	where T : class
	where TEventArgs : class
{
	private readonly ConditionalWeakTable<T, TEventArgs> _buffers = new();
	private readonly NativeDispatcherPriority _priority;
	private Action<T, TEventArgs> _dispatch;
	private Action _invokeOnTick;
	private int _isAdditionalFrameRequested;

	/// <summary>
	/// </summary>
	/// <param name="onTick">indirect lambda call to <see cref="OnTick"/> without any capture</param>
	/// <param name="dispatch">dispatch callback</param>
	/// <param name="priority"></param>
	public CustomKeepLastEventManager(Action onTick, Action<T, TEventArgs> dispatch, NativeDispatcherPriority priority = NativeDispatcherPriority.Normal)
	{
		this._invokeOnTick = onTick;
		this._dispatch = dispatch;

		this._priority = priority;
	}

	public void Enqueue(T sender, TEventArgs args)
	{
		_buffers.AddOrUpdate(sender, args);

		RequestAdditionalFrame();
	}

	private void RequestAdditionalFrame()
	{
		if (Interlocked.CompareExchange(ref _isAdditionalFrameRequested, 1, 0) == 0)
		{
			NativeDispatcher.Main.Enqueue(_invokeOnTick, _priority);
		}
	}

	public void OnTick()
	{
		_isAdditionalFrameRequested = 0;

		foreach (var item in _buffers)
		{
			if (((IDependencyObjectStoreProvider)item.Key).Store.IsDisposed)
			{
				continue;
			}

			_dispatch(item.Key, item.Value);
		}
	}
}

#endif
