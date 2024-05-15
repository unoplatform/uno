using System.Runtime.InteropServices.JavaScript;

namespace __Uno.Helpers.Theming;

internal partial class SystemThemeHelper
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.Helpers.Theming.SystemThemeHelper.observeSystemTheme")]
		internal static partial void ObserveSystemTheme();

		[JSImport("globalThis.Uno.Helpers.Theming.SystemThemeHelper.getSystemTheme")]
		internal static partial string GetSystemTheme();
	}
}
