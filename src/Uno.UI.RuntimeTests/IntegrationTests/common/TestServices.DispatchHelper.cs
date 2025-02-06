#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


#if HAS_UNO_WINUI || WINDOWS_WINUI
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

using _Impl = Microsoft.UI.Dispatching.DispatcherQueue;
using _Handler = Microsoft.UI.Dispatching.DispatcherQueueHandler;
using _Priority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using Uno.UI.Dispatching;

using _Impl = Windows.UI.Core.CoreDispatcher;
using _Handler = Windows.UI.Core.DispatchedHandler;
using _Priority = Windows.UI.Core.CoreDispatcherPriority;
#endif

namespace Private.Infrastructure;

// the class name is prefixed to avoid potential name collision with the real `DispatcherCompat`

public partial class UnitTestDispatcherCompat
{
	public enum Priority { Low = -1, Normal = 0, High = 1 }

	private static _Priority RemapPriority(Priority priority) => priority switch
	{
		// [uwp] Windows.UI.Core.CoreDispatcherPriority::Idle doesn't have a counterpart, and is thus ignored.

		Priority.Low => _Priority.Low,
		Priority.Normal => _Priority.Normal,
		Priority.High => _Priority.High,

		_ => throw new ArgumentOutOfRangeException($"Invalid value: ({priority:d}){priority}"),
	};
}

public partial class UnitTestDispatcherCompat
{
	private readonly _Impl _impl;

#if WINDOWS_WINUI || HAS_UNO_WINUI

	private readonly Windows.UI.Core.CoreDispatcher? _dispatcher;
	public UnitTestDispatcherCompat(_Impl impl, Windows.UI.Core.CoreDispatcher? dispatcher = null)
	{
		this._impl = impl;
		this._dispatcher = dispatcher;
	}
#else
	public UnitTestDispatcherCompat(_Impl impl)
	{
		this._impl = impl;
	}
#endif

	public Windows.Foundation.IAsyncAction RunIdleAsync(Windows.UI.Core.IdleDispatchedHandler agileCallback)
	{
#if !WINDOWS_WINUI && !HAS_UNO_WINUI
		// Windows UWP and Uno UWP can use CoreDispatcher.RunIdleAsync without issues.
		return _impl.RunIdleAsync(agileCallback);
#else
		// For now, Uno WinUI Dispatcher is non-null.
		// In the future, this will break and it will be null.
		if (_dispatcher is not null)
		{
			return _dispatcher.RunIdleAsync(agileCallback);
		}

		// This code path is for Windows WinUI, and "potentially" Uno WinUI in future when the breaking change is taken.
		// This is a wrong implementation. It doesn't really wait for "Idle".
		return RunAsync(UnitTestDispatcherCompat.Priority.Low, () => { }).AsAsyncAction();
#endif
	}

	public static UnitTestDispatcherCompat From(UIElement x) =>
#if HAS_UNO_WINUI || WINDOWS_WINUI
		new UnitTestDispatcherCompat(x.DispatcherQueue, x.Dispatcher);
#else
		new UnitTestDispatcherCompat(x.Dispatcher);
#endif

	public static UnitTestDispatcherCompat From(Window x) =>
#if HAS_UNO_WINUI || WINDOWS_WINUI
		new UnitTestDispatcherCompat(x.DispatcherQueue, x.Dispatcher);
#else
		new UnitTestDispatcherCompat(x.Dispatcher);
#endif

	public static UnitTestDispatcherCompat Instance { get; } =
#if WINAPPSDK
		new UnitTestDispatcherCompat(_Impl.GetForCurrentThread());
#elif HAS_UNO_WINUI
		new UnitTestDispatcherCompat(_Impl.Main);
#else
		new UnitTestDispatcherCompat(CoreDispatcher.Main);
#endif


	public bool HasThreadAccess => _impl.HasThreadAccess;

	public void Invoke(_Handler handler) => Invoke(default, handler);
	public void Invoke(Priority priority, _Handler handler)
	{
#if HAS_UNO_WINUI || WINDOWS_WINUI
		_impl.TryEnqueue(RemapPriority(priority), handler);
#else
		_ = _impl.RunAsync(RemapPriority(priority), handler);
#endif
	}
	public void InvokeWithBypass(_Handler handler) => InvokeWithBypass(default, handler);
	public void InvokeWithBypass(Priority priority, _Handler handler)
	{
		if (_impl.HasThreadAccess)
		{
			handler();
		}
		else
		{
			Invoke(priority, handler);
		}
	}

	public Task RunAsync(_Handler handler) => RunAsync(default, handler);
	public Task RunAsync(Priority priority, _Handler handler)
	{
		var tcs = new TaskCompletionSource<object>();

#if HAS_UNO_WINUI || WINDOWS_WINUI
		_impl.TryEnqueue(RemapPriority(priority), () =>
#else
		_ = _impl.RunAsync(RemapPriority(priority), () =>
#endif
		{
			try
			{
				handler();
				tcs.TrySetResult(default!);
			}
			catch (Exception e)
			{
				tcs.TrySetException(e);
			}
		});

		return tcs.Task;
	}
	public Task RunAsyncWithBypass(_Handler handler) => RunAsyncWithBypass(default, handler);
	public Task RunAsyncWithBypass(Priority priority, _Handler handler)
	{
		if (_impl.HasThreadAccess)
		{
			handler();
			return Task.CompletedTask;
		}
		else
		{
			return RunAsync(priority, handler);
		}
	}
}
