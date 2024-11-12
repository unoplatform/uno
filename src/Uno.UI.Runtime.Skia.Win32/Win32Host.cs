using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Extensibility;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Win32;

public class Win32Host : SkiaHost, ISkiaApplicationHost
{
	private readonly Func<Application> _appBuilder;
	private static readonly Win32EventLoop _eventLoop = new(a => new Thread(a) { Name = "Uno Event Loop", IsBackground = true });
	[ThreadStatic] private static bool _isDispatcherThread;
	private static readonly TaskCompletionSource _exitTcs = new();
	private static int _openWindows;

	static Win32Host()
	{
		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), _ => new Win32NativeWindowFactoryExtension());
	}

	public Win32Host(Func<Application> appBuilder)
	{
		_appBuilder = appBuilder;
		_eventLoop.Schedule(() => _isDispatcherThread = true, NativeDispatcherPriority.Normal);
	}

	/// <summary>
	/// Gets or sets the current Skia Render surface type.
	/// </summary>
	/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
	public RenderSurfaceType? RenderSurfaceType { get; set; }

	protected override void Initialize()
	{
		CoreDispatcher.DispatchOverride = _eventLoop.Schedule;
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
	}

	protected override Task RunLoop()
	{
		_eventLoop.Schedule(() => Application.Start(_ =>
		{
			var app = _appBuilder();
			app.Host = this;
		}), NativeDispatcherPriority.Normal);

		return _exitTcs.Task;
	}

	internal static void RegisterWindow(HWND hwnd)
	{
		Interlocked.Increment(ref _openWindows);
		_eventLoop.AddHwnd(hwnd);
	}

	internal static void UnregisterWindow(HWND hwnd)
	{
		_eventLoop.RemoveHwnd(hwnd);
		if (Interlocked.Decrement(ref _openWindows) is 0)
		{
			_exitTcs.SetResult();
		}
	}
}
