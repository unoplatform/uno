using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.Helpers.Theming;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Runtime.Skia.Extensions.System;
using Uno.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Uno.Media.Playback;

namespace Uno.WinUI.Runtime.Skia.X11;

public partial class X11ApplicationHost : SkiaHost, ISkiaApplicationHost, IDisposable
{
	[ThreadStatic] private static bool _isDispatcherThread;
	private readonly EventLoop _eventLoop;

	private readonly Func<Application> _appBuilder;

	static X11ApplicationHost()
	{
		// This seems to be necessary to run on WSL, but not necessary on the X.org implementation.
		// We therefore wrap every x11 call with XLockDisplay and XUnlockDisplay
		_ = X11Helper.XInitThreads();

		// keyboard input fails without this, not sure why this works but Avalonia and xev make similar calls, cf. https://stackoverflow.com/a/18288346
		// This disables IME, cf. https://tedyin.com/posts/a-brief-intro-to-linux-input-method-framework/
		setlocale(/* LC_ALL */ 6, "");
		if (XLib.XSetLocaleModifiers("@im=none") == IntPtr.Zero)
		{
			setlocale(/* LC_ALL */ 6, "en_US.UTF-8");
			if (XLib.XSetLocaleModifiers("@im=none") == IntPtr.Zero)
			{
				setlocale(/* LC_ALL */ 6, "C.UTF-8");
				XLib.XSetLocaleModifiers("@im=none");
			}
		}

		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), _ => new X11CoreApplicationExtension());
		ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new X11ApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new X11DisplayInformationExtension(o));

		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource), o => new X11PointerInputSource(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoKeyboardInputSource), o => new X11KeyboardInputSource(o));

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), _ => new X11NativeWindowFactoryExtension());

		ApiExtensibility.Register(typeof(ILauncherExtension), o => new LinuxLauncherExtension(o));

		ApiExtensibility.Register(typeof(IClipboardExtension), _ => X11ClipboardExtension.Instance);

		ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), o => new LinuxFilePickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new LinuxFilePickerExtension(o));
		ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), o => new LinuxFileSaverExtension(o));

		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new X11NativeElementHostingExtension(o));

		ApiExtensibility.Register<DragDropManager>(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), o => new X11DragDropExtension(o));

		ApiExtensibility.Register<XamlRoot>(typeof(Uno.Graphics.INativeOpenGLWrapper), xamlRoot => new X11NativeOpenGLWrapper(xamlRoot));

		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), _ => LinuxSystemThemeHelper.Instance);

		if (Type.GetType("Uno.UI.MediaPlayer.Skia.X11.X11MediaPlayerPresenterExtension, Uno.UI.MediaPlayer.Skia.X11") is { } mediaPresenterExtensionType
			&& Type.GetType("Uno.UI.MediaPlayer.Skia.X11.SharedMediaPlayerExtension, Uno.UI.MediaPlayer.Skia.X11") is { } mediaExtensionType)
		{
			var libvlcHandle = LibDl.dlopen("libvlc.so", lazy: true);
			if (libvlcHandle != IntPtr.Zero)
			{
				LibDl.dlclose(libvlcHandle);
				ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), presenter => Activator.CreateInstance(mediaPresenterExtensionType, presenter)!);
				ApiExtensibility.Register<MediaPlayer>(typeof(IMediaPlayerExtension), player => Activator.CreateInstance(mediaExtensionType, player)!);
			}
			else
			{
				if (mediaPresenterExtensionType.Log().IsEnabled(LogLevel.Error))
				{
					mediaPresenterExtensionType.Log().Error("libvlc.so was not found. Therefore, MediaPlayerElement will not be available. Visit https://platform.uno/docs/articles/controls/MediaPlayerElement.html to learn more about the needed dependencies.");
				}
			}
		}

		XamlRoot.FrameRenderingOptions = (true, true);
	}

	public X11ApplicationHost(Func<Application> appBuilder, int renderFrameRate = 60) : this(appBuilder, renderFrameRate, false)
	{
	}

	public X11ApplicationHost(Func<Application> appBuilder, int renderFrameRate = 60, bool preloadVlc = false)
	{
		if (preloadVlc && Type.GetType("Uno.UI.MediaPlayer.Skia.X11.SharedMediaPlayerExtension, Uno.UI.MediaPlayer.Skia.X11") is { } mediaExtensionType)
		{
			mediaExtensionType.GetMethod("PreloadVlc", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
		}

		_appBuilder = appBuilder;

		if (RenderFrameRate != default && renderFrameRate != RenderFrameRate)
		{
			throw new InvalidOperationException($"X11's render frame rate should only be set once.");
		}
		RenderFrameRate = renderFrameRate;

		_eventLoop = new EventLoop();
		_eventLoop.Schedule(() => { Thread.CurrentThread.Name = "Uno Event Loop"; });

		_eventLoop.Schedule(() =>
		{
			_isDispatcherThread = true;
		});
		CoreDispatcher.DispatchOverride = (a, p) => _eventLoop.Schedule(a);
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
	}

	internal static int RenderFrameRate { get; private set; }

	[LibraryImport("libc", StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
	private static partial void setlocale(int type, string s);

	protected override Task RunLoop()
	{
		Thread.CurrentThread.Name = "Main Thread (keep-alive)";
		_eventLoop.Schedule(StartApp);

		while (!X11XamlRootHost.AllWindowsDone())
		{
			Thread.Sleep(100);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{nameof(X11ApplicationHost)} is exiting");
		}

		return Task.CompletedTask;
	}

	private void StartApp()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		Application.Start(CreateApp);
	}

	protected override void Initialize()
	{
	}

	public void Dispose()
	{
	}
}
