#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Uno.UI.Samples.Helper;

/// <summary>
/// Theme-aware brush lookup for code that builds UI by hand (runner result rows, benchmark logger).
/// </summary>
/// <remarks>
/// Brushes read from a theme dictionary in code resolve their colour under the app theme, not the
/// dictionary's (WinUI does this too), which is wrong whenever the shell's RequestedTheme differs from
/// the app's. Colour keys are plain values per theme, so this resolves the colour key and wraps it.
/// UI thread only. The returned brush is shared by every caller asking for the same key and theme, so never modify it.
/// </remarks>
public static class ShellThemeBrushes
{
	internal const string ResourcesUri = "ms-appx:///Controls/UITests/Views/Styles/Shell/ShellResources.xaml";

	internal const string LightKey = "Light";
	internal const string DarkKey = "Dark";
	internal const string HighContrastKey = "HighContrast";

	// Mirrors the theme dictionaries of ShellResources.xaml (Given_ShellResources keeps them in sync).
	private static readonly Dictionary<string, (string Color, string HighContrastColor)> _shellBrushColors = new()
	{
		["ShellTestPassedBrush"] = ("SystemFillColorSuccess", "SystemColorWindowTextColor"),
		["ShellTestFailedBrush"] = ("SystemFillColorCritical", "SystemColorHighlightColor"),
		["ShellTestWarningBrush"] = ("SystemFillColorCaution", "SystemColorWindowTextColor"),
		["ShellTestNeutralBrush"] = ("TextFillColorTertiary", "SystemColorGrayTextColor"),
		["ShellTagBackgroundBrush"] = ("SubtleFillColorSecondary", "SystemColorButtonFaceColor"),
		["ShellTagForegroundBrush"] = ("TextFillColorSecondary", "SystemColorButtonTextColor"),
		["ShellLogBackgroundBrush"] = ("CardBackgroundFillColorDefault", "SystemColorWindowColor"),
	};

	private static readonly Dictionary<(string Key, string Theme), SolidColorBrush> _cache = new();

	private static AccessibilitySettings? _accessibilitySettings;
	private static UISettings? _uiSettings;
	private static volatile bool _isCacheStale;

	internal static IEnumerable<string> ShellBrushKeys => _shellBrushColors.Keys;

	/// <summary>
	/// Resolves a Shell* brush key, or a framework brush key following the Fluent "XxxBrush" / "Xxx" colour naming,
	/// for <paramref name="theme"/> (pass the element's ActualTheme). High contrast wins, as it does for {ThemeResource}.
	/// </summary>
	public static Brush? Get(string key, ElementTheme theme)
		=> Get(key, GetThemeKey(theme, AccessibilitySettings.HighContrast));

	internal static Brush? Get(string key, string themeKey)
	{
		Debug.Assert(DispatcherQueue.GetForCurrentThread() is not null, "ShellThemeBrushes must be used on the UI thread");

		ObserveColorChanges();
		if (_isCacheStale)
		{
			_isCacheStale = false;
			_cache.Clear();
		}

		if (_cache.TryGetValue((key, themeKey), out var cached))
		{
			return cached;
		}

		var colorKey = _shellBrushColors.TryGetValue(key, out var colors)
			? (themeKey == HighContrastKey ? colors.HighContrastColor : colors.Color)
			: key.EndsWith("Brush", StringComparison.Ordinal) ? key[..^"Brush".Length] : key;

#if HAS_UNO
		// TODO Uno: scope the lookup to the theme asked for (see uno-issues: uno--theme-subdictionary-lookup-uses-ambient-theme.md)
		using var themeScope = Uno.UI.Xaml.Core.CoreServices.Instance.ScopeRequestedThemeForSubTree(ToTheme(themeKey));
#endif
		var resources = Application.Current.Resources;
		if ((FindColor(resources, colorKey, themeKey) ?? (resources.TryGetValue(colorKey, out var systemColor) ? systemColor as Color? : null)) is not { } color)
		{
			return resources.TryGetValue(key, out var value) ? value as Brush : null;
		}

		SolidColorBrush brush = new(color);
		_cache[(key, themeKey)] = brush;
		return brush;
	}

	private static AccessibilitySettings AccessibilitySettings => _accessibilitySettings ??= new();

	// High contrast and accent colours change the resolved colours; the events may arrive off the UI thread.
	private static void ObserveColorChanges()
	{
		if (_uiSettings is not null)
		{
			return;
		}

		_uiSettings = new();
		_uiSettings.ColorValuesChanged += (_, _) => _isCacheStale = true;

		try
		{
			AccessibilitySettings.HighContrastChanged += (_, _) => _isCacheStale = true;
		}
		catch (Exception e)
		{
			// WinAppSDK has no CoreWindow to raise it (0x80070490); turning high contrast on or off still changes the cache key.
			SampleControl.Presentation.ShellLog.Warn("High contrast changes are not observed.", e);
		}
	}

#if HAS_UNO
	private static Theme ToTheme(string themeKey) => themeKey switch
	{
		HighContrastKey => Theme.HighContrast,
		DarkKey => Theme.Dark,
		_ => Theme.Light,
	};
#endif

	internal static string GetThemeKey(ElementTheme theme, bool isHighContrast)
	{
		if (isHighContrast)
		{
			return HighContrastKey;
		}

		if (theme == ElementTheme.Default)
		{
			theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
		}

		return theme == ElementTheme.Dark ? DarkKey : LightKey;
	}

	private static Color? FindColor(ResourceDictionary dictionary, string key, string themeKey)
	{
		if (FindColorInTheme(dictionary, key, themeKey) is { } color)
		{
			return color;
		}

		if (FindColorInTheme(dictionary, key, "Default") is { } defaultColor)
		{
			return defaultColor;
		}

		// Last merged dictionary wins, as in a XAML lookup.
		for (var i = dictionary.MergedDictionaries.Count - 1; i >= 0; i--)
		{
			if (FindColor(dictionary.MergedDictionaries[i], key, themeKey) is { } mergedColor)
			{
				return mergedColor;
			}
		}

		return null;
	}

	private static Color? FindColorInTheme(ResourceDictionary dictionary, string key, string themeKey)
		=> dictionary.ThemeDictionaries.TryGetValue(themeKey, out var theme)
			&& theme is ResourceDictionary themeDictionary
			&& themeDictionary.TryGetValue(key, out var value)
			&& value is Color color
				? color
				: (Color?)null;
}
