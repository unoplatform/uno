using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Extensions.System;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.X11;

public class X11ApplicationHost : ISkiaApplicationHost
{
	[ThreadStatic] private static bool _isDispatcherThread;
	private readonly EventLoop _eventLoop;

	private readonly Func<Application> _appBuilder;

	static X11ApplicationHost()
	{
		// This seems to be necessary to run on WSL, but not necessary on the X.org implementation.
		// We therefore wrap every x11 call with XLockDisplay and XUnlockDisplay
		var _ = X11Helper.XInitThreads();

		[DllImport("libc")]
		static extern void setlocale(int type, string s);

		// keyboard input fails without this, not sure why this works but Avalonia and xev make similar calls.
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
		ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new X11DisplayInformationExtension(o, null));

		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource), o => new X11PointerInputSource(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoKeyboardInputSource), o => new X11KeyboardInputSource(o));

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), _ => new X11NativeWindowFactoryExtension());

		ApiExtensibility.Register(typeof(ILauncherExtension), o => new LinuxLauncherExtension(o));

		ApiExtensibility.Register(typeof(IClipboardExtension), _ => X11ClipboardExtension.Instance);

		ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), o => new LinuxFilePickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new LinuxFilePickerExtension(o));
	}

	public X11ApplicationHost(Func<Application> appBuilder)
	{
		_appBuilder = appBuilder;

		_eventLoop = new EventLoop();

		_eventLoop.Schedule(() => _isDispatcherThread = true);
		CoreDispatcher.DispatchOverride = _eventLoop.Schedule;
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
	}

	public void Run()
	{
		_eventLoop.Schedule(StartApp);

		while (!X11XamlRootHost.AllWindowsDone())
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"{nameof(X11ApplicationHost)} is testing for all windows closed.");
			}
			Thread.Sleep(100);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{nameof(X11ApplicationHost)} is exiting");
		}
	}

	private void StartApp()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		Application.StartWithArguments(CreateApp);
	}
}
