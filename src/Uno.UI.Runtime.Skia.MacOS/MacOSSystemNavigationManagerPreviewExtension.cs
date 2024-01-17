#nullable enable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.UI.Core.Preview;
using Microsoft.UI.Xaml;

using Uno.Foundation.Extensibility;
using Uno.UI.Core.Preview;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSSystemNavigationManagerPreviewExtension : ISystemNavigationManagerPreviewExtension
{
	public static MacOSSystemNavigationManagerPreviewExtension Instance = new();

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(ISystemNavigationManagerPreviewExtension), o => Instance);
		NativeUno.uno_set_window_should_close_callback(&WindowShouldClose);
	}

	private MacOSSystemNavigationManagerPreviewExtension()
	{
	}

	public void RequestNativeAppClose() => Window.Current.Close();

	[UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
	// System.Boolean is not blittable / https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
	internal static int WindowShouldClose()
	{
		var manager = SystemNavigationManagerPreview.GetForCurrentView();
		if (!manager.HasConfirmedClose)
		{
			if (!manager.RequestAppClose())
			{
				return 0;
			}
		}

		// Closing should continue, perform suspension.
		if (!Application.Current.IsSuspended)
		{
			Application.Current.RaiseSuspending();
			return Application.Current.IsSuspended ? 1 : 0;
		}

		// All prerequisites passed, can safely close.
		return 1;
	}
}
