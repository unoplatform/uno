using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.UI.Xaml;
using Uno.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSCoreApplicationExtension : ICoreApplicationExtension
{
	private static readonly MacOSCoreApplicationExtension _instance = new();

	private MacOSCoreApplicationExtension()
	{
	}

	public static unsafe void Register()
	{
		NativeUno.uno_set_application_can_exit_callback(&AppCanExit);
		NativeUno.uno_set_application_should_terminate_after_last_window_closed_callback(&AppShouldTerminateAfterLastWindowClosed);
		ApiExtensibility.Register(typeof(ICoreApplicationExtension), _ => _instance);
	}

	public bool CanExit => true;

	// As WinUI's FrameworkApplication::ExitImpl: every window closes as with Window.Close(), raising Window.Closed but not
	// AppWindow.Closing; the application then exits, unless a window stayed open (Closed handled) with OnLastWindowClose.
	public void Exit()
	{
		foreach (var window in ApplicationHelper.WindowsInternal.ToArray())
		{
			try
			{
				window.Close();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Closing a window for Application.Exit() failed; the other windows still close.", e);
				}
			}
		}

		if (ApplicationHelper.WindowsInternal.Count == 0 || Application.Current?.DispatcherShutdownMode == DispatcherShutdownMode.OnExplicitShutdown)
		{
			NativeUno.uno_application_quit();
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	// System.Boolean is not blittable / https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
	internal static int AppCanExit() => _instance.CanExit ? 1 : 0;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static int AppShouldTerminateAfterLastWindowClosed()
	{
		if (Application.Current?.DispatcherShutdownMode != DispatcherShutdownMode.OnExplicitShutdown)
		{
			return 1;
		}

		if (typeof(MacOSCoreApplicationExtension).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(MacOSCoreApplicationExtension).Log().Debug("The last window closed; with OnExplicitShutdown, the application keeps running until Application.Exit().");
		}
		return 0;
	}
}
