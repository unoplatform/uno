#nullable enable

using Android.OS;
using Windows.UI;

namespace Uno.Helpers.Theming;

internal static partial class AccentColorHelper
{
	private static partial AccentColorPalette? GetPlatformAccentColorPalette()
	{
		// Android 12 (API 31) introduced dynamic Material You colors
		if ((int)Build.VERSION.SdkInt >= 31)
		{
			var context = Android.App.Application.Context;
			var accentColorInt = context.GetColor(Android.Resource.Color.SystemAccent1500);
			var accent = Color.FromArgb(
				(byte)((accentColorInt >> 24) & 0xFF),
				(byte)((accentColorInt >> 16) & 0xFF),
				(byte)((accentColorInt >> 8) & 0xFF),
				(byte)(accentColorInt & 0xFF));
			return AccentColorPalette.FromAccentColor(accent);
		}

		return null;
	}
}
