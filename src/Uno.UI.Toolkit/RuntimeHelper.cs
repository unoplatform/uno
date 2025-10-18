namespace Uno.UI.Toolkit;

public static class PlatformRuntimeHelper
{
	public static UnoRuntimePlatform Current =>
#if !HAS_UNO
		Uno.UI.Toolkit.UnoRuntimePlatform.NativeWinUI;
#else
		(Uno.UI.Toolkit.UnoRuntimePlatform)PlatformRuntimeHelper.Current;
#endif
}
