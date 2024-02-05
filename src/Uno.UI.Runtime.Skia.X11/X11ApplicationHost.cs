using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.UI.Core;
using Avalonia.X11;
using Microsoft.UI.Xaml;
using Uno.ApplicationModel.DataTransfer;
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
		X11Helper.XInitThreads();

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

		// This probably doesn't need a lock, since it doesn't modify anything and reading outdated values is fine
		SpinWait.SpinUntil(X11XamlRootHost.AllWindowsDone);

		this.Log().Debug($"{nameof(X11ApplicationHost)} is exiting");
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
