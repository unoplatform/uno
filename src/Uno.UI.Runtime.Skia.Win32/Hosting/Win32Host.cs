using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Graphics.Display;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.GdiPlus;
using Windows.Win32.UI.HiDpi;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.Media.Playback;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Extensions.System;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Graphics;
using Uno.UI.UI.Input.Internal;

namespace Uno.UI.Runtime.Skia.Win32;

public class Win32Host : SkiaHost, ISkiaApplicationHost
{
	private UIntPtr _gdiPlusToken;
	private readonly Func<Application> _appBuilder;
	[ThreadStatic] private static bool _isDispatcherThread;
	private static volatile bool _allWindowsClosed;
	private static int _openWindows;

	static Win32Host()
	{
		var hResult = PInvoke.OleInitialize();
		if (hResult.Failed)
		{
			typeof(Win32Host).LogError()?.Error($"{nameof(PInvoke.OleInitialize)} failed: {Win32Helper.GetErrorMessage(hResult)}");
		}

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), _ => new Win32NativeWindowFactoryExtension());
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoKeyboardInputSource),
			host => host as Win32WindowWrapper ?? throw new ArgumentException($"{nameof(host)} must be a {nameof(Win32WindowWrapper)} instance"));
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource),
			host => host as Win32WindowWrapper ?? throw new ArgumentException($"{nameof(host)} must be a {nameof(Win32WindowWrapper)} instance"));
		ApiExtensibility.Register<AppWindow>(typeof(INativeInputNonClientPointerSource), appWindow => (Win32WindowWrapper)appWindow.NativeAppWindow);

		ApiExtensibility.Register<ApplicationView>(typeof(IApplicationViewExtension), o => new Win32ApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), _ => Win32SystemThemeHelperExtension.Instance);

		ApiExtensibility.Register<DisplayInformation>(typeof(IDisplayInformationExtension), displayInformation =>
		{
			var appWindow = AppWindow.GetFromWindowId(displayInformation.WindowId);
			var window = Window.GetFromAppWindow(appWindow);
			var rootElement = window.RootElement ?? throw new NullReferenceException($"The window's {nameof(window.RootElement)} is not initialized.");
			var xamlRoot = rootElement.XamlRoot ?? throw new NullReferenceException($"The window's {nameof(window.RootElement)} doesn't have a {nameof(XamlRoot)}.");
			var wrapper = XamlRootMap.GetHostForRoot(xamlRoot) as Win32WindowWrapper ?? throw new NullReferenceException($"The {nameof(XamlRoot)} is not associated with a {nameof(Win32WindowWrapper)} instance.");
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
		ApiExtensibility.Register<DragDropManager>(typeof(IDragDropExtension), manager => new Win32DragDropExtension(manager));
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new Win32NativeElementHostingExtension(o));
		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new Win32NativeWebViewProvider(o));
		// We used to do this ApiExtensibility with ApiExtensionAttribute and a condition that makes it only run
		// on Windows, but this causes problem on Wpf because we're registering Win32's MPE implementation even on WPF.
		// This way, the Win32 MPE implementation is only registered when we're using
		// Win32 host and we know for sure we're running the Win32 target.
		if (Type.GetType("Uno.UI.MediaPlayer.Skia.Win32.Win32MediaPlayerPresenterExtension, Uno.UI.MediaPlayer.Skia.Win32") is { } mediaPresenterExtensionType)
		{
			ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), presenter => Activator.CreateInstance(mediaPresenterExtensionType, presenter)!);
		}

		if (Type.GetType("Uno.UI.MediaPlayer.Skia.Win32.SharedMediaPlayerExtension, Uno.UI.MediaPlayer.Skia.Win32") is { } mediaExtensionType)
		{
			ApiExtensibility.Register<MediaPlayer>(typeof(IMediaPlayerExtension), player => Activator.CreateInstance(mediaExtensionType, player)!);
		}
		ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new Win32NativeOpenGLWrapper(xamlRoot));
	}

	public Win32Host(Func<Application> appBuilder) : this(appBuilder, false)
	{
	}

	public Win32Host(Func<Application> appBuilder, bool preloadVlc)
	{
		if (preloadVlc && Type.GetType("Uno.UI.MediaPlayer.Skia.Win32.SharedMediaPlayerExtension, Uno.UI.MediaPlayer.Skia.Win32") is { } mediaExtensionType)
		{
			mediaExtensionType.GetMethod("PreloadVlc", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
		}

		_appBuilder = appBuilder;
		Win32EventLoop.Schedule(() =>
		{
			_isDispatcherThread = true;
			var hResult = PInvoke.OleInitialize();
			if (hResult.Failed)
			{
				typeof(Win32Host).LogError()?.Error($"{nameof(PInvoke.OleInitialize)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			}

			GdiplusStartupOutput o = default;
			var status = PInvoke.GdiplusStartup(ref _gdiPlusToken, new GdiplusStartupInput { GdiplusVersion = 1 }, ref o);
			if (status != Status.Ok)
			{
				typeof(Win32Host).LogError()?.Error($"{nameof(PInvoke.GdiplusStartup)} failed: {status}");
			}
		}, NativeDispatcherPriority.Normal);
	}

	/// <summary>
	/// Gets or sets the current Skia Render surface type.
	/// </summary>
	/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
	public RenderSurfaceType? RenderSurfaceType { get; set; }

	protected override void Initialize()
	{
		CoreDispatcher.DispatchOverride = Win32EventLoop.Schedule;
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
	}

	private static bool _isRunning;

	protected override Task RunLoop()
	{
		Win32EventLoop.Schedule(() => Application.Start(_ =>
		{
			var app = _appBuilder();
			app.Host = this;

			return app;
		}), NativeDispatcherPriority.Normal);

		if (!_isRunning)
		{
			_isRunning = true;

			// This will keep running until the event loop has no queued actions left and all the windows are closed
			while (true)
			{
				Win32EventLoop.RunOnce();

				if (_allWindowsClosed && !Win32EventLoop.HasMessages())
				{
					return Task.CompletedTask;
				}
			}
		}

		return Task.CompletedTask;
	}

	internal static void RegisterWindow(HWND hwnd)
	{
		Interlocked.Increment(ref _openWindows);
	}

	internal static void UnregisterWindow(HWND hwnd)
	{
		if (Interlocked.Decrement(ref _openWindows) is 0)
		{
			_allWindowsClosed = true;
		}
	}
}
