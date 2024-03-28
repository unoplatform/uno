using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Uno.ApplicationModel.Core;
using Uno.Foundation.Extensibility;

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
		ApiExtensibility.Register(typeof(ICoreApplicationExtension), _ => _instance);
	}

	public bool CanExit => true;

	public void Exit() => NativeUno.uno_application_quit();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	// System.Boolean is not blittable / https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
	internal static int AppCanExit() => _instance.CanExit ? 1 : 0;
}
