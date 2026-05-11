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

		[JSImport("globalThis.Uno.Helpers.Theming.SystemThemeHelper.getHighContrast")]
		[return: JSMarshalAs<JSType.Boolean>]
		internal static partial bool GetHighContrast();

		[JSImport("globalThis.Uno.Helpers.Theming.SystemThemeHelper.observeHighContrast")]
		internal static partial void ObserveHighContrast();
	}
}

