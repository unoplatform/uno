#nullable enable

using Microsoft.UI.Xaml.Media;
using Uno.Helpers.Theming;
using Uno.UI;
using Windows.UI;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Injects accent color values into the system resource dictionaries (MasterDictionary)
/// so that ThemeResource references to accent colors resolve to the current accent color.
/// </summary>
internal static class AccentColorResourceUpdater
{
	private static readonly string[] _themeKeys = ["Default", "Light", "HighContrast"];

	internal static void UpdateAccentColorResources()
	{
		var palette = AccentColorHelper.CurrentPalette;
		var accentDictionary = FindAccentColorDictionary();
		if (accentDictionary is null)
		{
			return;
		}

		foreach (var themeKey in _themeKeys)
		{
			if (accentDictionary.ThemeDictionaries.TryGetValue(themeKey, out var themeObj) &&
				themeObj is ResourceDictionary themeDictionary)
			{
				SetAccentColors(themeDictionary, palette);
			}
		}

		accentDictionary.InvalidateNotFoundCache(true);
	}

	private static void SetAccentColors(ResourceDictionary dictionary, AccentColorPalette palette)
	{
		dictionary["SystemAccentColor"] = palette.Accent;
		dictionary["SystemAccentColorLight1"] = palette.Light1;
		dictionary["SystemAccentColorLight2"] = palette.Light2;
		dictionary["SystemAccentColorLight3"] = palette.Light3;
		dictionary["SystemAccentColorDark1"] = palette.Dark1;
		dictionary["SystemAccentColorDark2"] = palette.Dark2;
		dictionary["SystemAccentColorDark3"] = palette.Dark3;

		// SystemColorHighlightColor should match accent
		dictionary["SystemColorHighlightColor"] = palette.Accent;

		// Rebuild the accent brush
		dictionary["SystemColorControlAccentBrush"] = new SolidColorBrush(palette.Accent);

		// SystemListAccent colors (accent with varying alpha)
		dictionary["SystemListAccentLowColor"] = Color.FromArgb(0x66, palette.Accent.R, palette.Accent.G, palette.Accent.B);
		dictionary["SystemListAccentMediumColor"] = Color.FromArgb(0x99, palette.Accent.R, palette.Accent.G, palette.Accent.B);
		dictionary["SystemListAccentHighColor"] = Color.FromArgb(0xB2, palette.Accent.R, palette.Accent.G, palette.Accent.B);
	}

	/// <summary>
	/// Finds the merged dictionary in MasterDictionary that contains SystemAccentColor
	/// in its ThemeDictionaries, following the same pattern as FindSymbolFontFamilyDictionary
	/// in ResourceResolver.cs.
	/// </summary>
	private static ResourceDictionary? FindAccentColorDictionary()
	{
		var masterDictionary =
#if __NETSTD_REFERENCE__
			(ResourceDictionary?)null;
#else
			Uno.UI.GlobalStaticResources.MasterDictionary;
#endif

		if (masterDictionary is null)
		{
			return null;
		}

		for (var mergedIndex = 0; mergedIndex < masterDictionary.MergedDictionaries.Count; mergedIndex++)
		{
			var merged = masterDictionary.MergedDictionaries[mergedIndex];

			foreach (var theme in merged.ThemeDictionaries)
			{
				if (theme.Value is ResourceDictionary themeDictionary &&
					themeDictionary.ContainsKey("SystemAccentColor"))
				{
					return merged;
				}
			}
		}

		return null;
	}
}
