#nullable enable

using System;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;

namespace Uno.Helpers.Theming;

/// <summary>
/// Provides access to current OS theme
/// and notifications about its changes.
/// </summary>
internal static partial class SystemThemeHelper
{
	private static EventHandler? _systemThemeChanged;
	private static bool _changesObserved;
	private static SystemTheme? _lastSystemTheme;

	private static EventHandler? _highContrastChanged;
	private static bool? _lastHighContrast;
	private static string? _lastHighContrastScheme;
	private static HighContrastSystemColors? _lastHighContrastSystemColors;

#if __ANDROID__ || __APPLE_UIKIT__
	private static readonly HighContrastSystemColors _mobileHighContrastBlackSystemColors = new(
		ButtonFaceColor: Color.FromArgb(255, 0, 0, 0),
		ButtonTextColor: Color.FromArgb(255, 255, 255, 255),
		GrayTextColor: Color.FromArgb(255, 63, 242, 63),
		HighlightColor: Color.FromArgb(255, 26, 235, 255),
		HighlightTextColor: Color.FromArgb(255, 0, 0, 0),
		HotlightColor: Color.FromArgb(255, 255, 255, 0),
		WindowColor: Color.FromArgb(255, 0, 0, 0),
		WindowTextColor: Color.FromArgb(255, 255, 255, 255));

	private static readonly HighContrastSystemColors _mobileHighContrastWhiteSystemColors = new(
		ButtonFaceColor: Color.FromArgb(255, 255, 255, 255),
		ButtonTextColor: Color.FromArgb(255, 0, 0, 0),
		GrayTextColor: Color.FromArgb(255, 109, 109, 109),
		HighlightColor: Color.FromArgb(255, 0, 0, 0),
		HighlightTextColor: Color.FromArgb(255, 255, 255, 255),
		HotlightColor: Color.FromArgb(255, 0, 0, 255),
		WindowColor: Color.FromArgb(255, 255, 255, 255),
		WindowTextColor: Color.FromArgb(255, 0, 0, 0));
#endif

	/// <summary>
	/// Gets the current system theme.
	/// </summary>
	internal static SystemTheme SystemTheme => _lastSystemTheme ??= GetSystemTheme();

	/// <summary>
	/// Gets whether the system High Contrast mode is currently enabled.
	/// </summary>
	internal static bool IsHighContrastEnabled => _lastHighContrast ??= GetIsHighContrastEnabled();

	/// <summary>
	/// Gets the name of the active High Contrast scheme.
	/// </summary>
	internal static string HighContrastSchemeName => _lastHighContrastScheme ??= GetHighContrastSchemeName();

	/// <summary>
	/// Gets the current High Contrast system colors, if available.
	/// </summary>
	internal static HighContrastSystemColors? HighContrastSystemColors => GetCurrentHighContrastSystemColors(IsHighContrastEnabled);

	/// <summary>
	/// Triggered when system theme changes.
	/// </summary>
	internal static event EventHandler? SystemThemeChanged
	{
		add
		{
			_systemThemeChanged += value;
			ObserveThemeChanges();
		}
		remove => _systemThemeChanged -= value;
	}

	/// <summary>
	/// Triggered when system High Contrast setting changes.
	/// </summary>
	internal static event EventHandler? HighContrastChanged
	{
		add
		{
			_highContrastChanged += value;
			ObserveThemeChanges();
		}
		remove => _highContrastChanged -= value;
	}

	/// <summary>
	/// Starts observing system theme changes.
	/// </summary>
	internal static void ObserveThemeChanges()
	{
		if (_changesObserved)
		{
			return;
		}

		_changesObserved = true;

		// Cache the initial values for future comparisons
		_lastSystemTheme = GetSystemTheme();
		_lastHighContrast = GetIsHighContrastEnabled();
		_lastHighContrastScheme = GetHighContrastSchemeName();
		_lastHighContrastSystemColors = GetCurrentHighContrastSystemColors(_lastHighContrast ?? false);

		ObserveThemeChangesPlatform();

		// Ensure to check for theme changes when app leaves background
		// as we might miss a theme change if the app gets suspended.
		CoreApplication.LeavingBackground += (s, e) =>
		{
			RefreshSystemTheme();
			RefreshHighContrast();
		};
		CoreApplication.Resuming += (s, e) =>
		{
			RefreshSystemTheme();
			RefreshHighContrast();
		};
	}

	/// <summary>
	/// Raises SystemThemeChanged if the theme
	/// has changed since we last checked it.
	/// </summary>
	internal static void RefreshSystemTheme()
	{
		var cachedTheme = _lastSystemTheme;
		var currentTheme = GetSystemTheme();
		if (cachedTheme != currentTheme)
		{
			_lastSystemTheme = currentTheme;
			RaiseSystemThemeChanged();
		}
	}

	/// <summary>
	/// Raises HighContrastChanged if the HC state
	/// has changed since we last checked it.
	/// </summary>
	internal static void RefreshHighContrast()
	{
		var cachedHC = _lastHighContrast;
		var cachedScheme = _lastHighContrastScheme;
		var cachedColors = _lastHighContrastSystemColors;
		var currentHC = GetIsHighContrastEnabled();
		var currentScheme = GetHighContrastSchemeName();
		var currentColors = GetCurrentHighContrastSystemColors(currentHC);

		if (cachedHC != currentHC
			|| !string.Equals(cachedScheme, currentScheme, StringComparison.Ordinal)
			|| cachedColors != currentColors)
		{
			_lastHighContrast = currentHC;
			_lastHighContrastScheme = currentScheme;
			_lastHighContrastSystemColors = currentColors;
			RaiseHighContrastChanged();
		}
	}

	/// <summary>
	/// This is just a partial method, as on some platforms (e.g. Android and iOS)
	/// the theme change is listened to on the Application/Window level.
	/// </summary>
	static partial void ObserveThemeChangesPlatform();

	/// <summary>
	/// Gets whether HC is enabled from the platform.
	/// </summary>
	static partial void GetIsHighContrastEnabledPlatform(ref bool result);

	/// <summary>
	/// Gets the HC scheme name from the platform.
	/// </summary>
	static partial void GetHighContrastSchemeNamePlatform(ref string result);

	/// <summary>
	/// Gets the HC system colors from the platform.
	/// </summary>
	static partial void GetHighContrastSystemColorsPlatform(ref HighContrastSystemColors? result);

	private static bool GetIsHighContrastEnabled()
	{
		bool result = false;
		GetIsHighContrastEnabledPlatform(ref result);
		return result;
	}

	private static string GetHighContrastSchemeName()
	{
		string result = "High Contrast Black";
		GetHighContrastSchemeNamePlatform(ref result);
		return result;
	}

	private static HighContrastSystemColors? GetCurrentHighContrastSystemColors(bool isHighContrastEnabled)
	{
		var isHighContrastActive = WinRTFeatureConfiguration.Accessibility.HighContrastOverride ?? isHighContrastEnabled;

		if (!isHighContrastActive)
		{
			return null;
		}

		if (WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride is { } overrideColors)
		{
			return overrideColors;
		}

		if (!isHighContrastEnabled)
		{
			return null;
		}

		HighContrastSystemColors? result = null;
		GetHighContrastSystemColorsPlatform(ref result);
		return result;
	}

#if __ANDROID__ || __APPLE_UIKIT__
	private static string GetMobileHighContrastSchemeName(SystemTheme systemTheme)
		=> systemTheme == SystemTheme.Dark ? "High Contrast Black" : "High Contrast White";

	private static HighContrastSystemColors GetMobileHighContrastSystemColors(SystemTheme systemTheme)
		=> systemTheme == SystemTheme.Dark
			? _mobileHighContrastBlackSystemColors
			: _mobileHighContrastWhiteSystemColors;
#endif

	private static void RaiseSystemThemeChanged() => _systemThemeChanged?.Invoke(null, EventArgs.Empty);
	private static void RaiseHighContrastChanged() => _highContrastChanged?.Invoke(null, EventArgs.Empty);
}

