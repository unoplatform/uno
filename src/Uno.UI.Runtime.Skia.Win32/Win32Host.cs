using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.Win32.Foundation;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Extensions.System;
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
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoKeyboardInputSource),
			host => host as Win32WindowWrapper ?? throw new ArgumentException($"{nameof(host)} must be a {nameof(Win32WindowWrapper)} instance"));
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource),
			host => host as Win32WindowWrapper ?? throw new ArgumentException($"{nameof(host)} must be a {nameof(Win32WindowWrapper)} instance"));
		ApiExtensibility.Register<ApplicationView>(typeof(IApplicationViewExtension), o => new Win32ApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), _ => Win32SystemThemeHelperExtension.Instance);

		ApiExtensibility.Register<DisplayInformation>(typeof(IDisplayInformationExtension), displayInformation =>
		{
			var appWindow = AppWindow.GetFromWindowId(displayInformation.WindowId);
			var window = Window.GetFromAppWindow(appWindow);
			var rootElement = window.RootElement ?? throw new NullReferenceException($"The window's {nameof(window.RootElement)} is not initialized.");
			var xamlRoot = rootElement.XamlRoot ?? throw new NullReferenceException($"The window's {nameof(window.RootElement)} doesn't have a {nameof(XamlRoot)}.");
			var wrapper = Win32WindowWrapper.XamlRootMap.GetHostForRoot(xamlRoot) ?? throw new NullReferenceException($"The {nameof(XamlRoot)} is not associated with a {nameof(Win32WindowWrapper)} instance.");
			wrapper.SetDisplayInformation(displayInformation);
			return wrapper;
		});

		ApiExtensibility.Register<ConnectionProfile>(typeof(IConnectionProfileExtension), _ => Win32ConnectionProfileExtension.Instance);
		ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), _ => Win32AnalyticsInfoExtension.Instance);
		ApiExtensibility.Register(typeof(IClipboardExtension), _ => Win32ClipboardExtension.Instance);
		ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), o => new Win32FileFolderPickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new Win32FileFolderPickerExtension(o));
		ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), o => new Win32FileSaverExtension(o));
		ApiExtensibility.Register(typeof(ILauncherExtension), o => new WindowsLauncherExtension(o));
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

		_exitTcs.Task.GetAwaiter().GetResult();
		return Task.CompletedTask;
	}

	internal static void RunOnce() => _eventLoop.RunOnce();

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
