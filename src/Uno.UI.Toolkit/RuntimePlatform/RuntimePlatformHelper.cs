namespace Uno.UI.Toolkit;

public static class RuntimePlatformHelper
{
	public static UnoRuntimePlatform Current =>
#if !HAS_UNO
		Uno.UI.Toolkit.UnoRuntimePlatform.NativeWinUI;
#else
		(Uno.UI.Toolkit.UnoRuntimePlatform)Uno.Helpers.RuntimePlatformHelper.Current;
#endif
}
