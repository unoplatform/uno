namespace Uno
{
	internal static class DeviceHelper
	{
		internal static bool IsSimulator { get; }
#if __IOS__ || __TVOS__
			= ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR;
#endif
	}
}
