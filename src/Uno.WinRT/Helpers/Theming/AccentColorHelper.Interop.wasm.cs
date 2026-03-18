using System.Runtime.InteropServices.JavaScript;

namespace __Uno.Helpers.Theming;

internal partial class AccentColorHelper
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.Helpers.Theming.AccentColorHelper.observeAccentColor")]
		internal static partial void ObserveAccentColor();

		[JSImport("globalThis.Uno.Helpers.Theming.AccentColorHelper.getAccentColor")]
		internal static partial string GetAccentColor();
	}
}
