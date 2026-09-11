#nullable enable

using System;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Uno.Helpers.Theming;

/// <summary>
/// Provides access to current OS theme
/// and notifications about its changes.
/// </summary>
internal static partial class SystemThemeHelper
{
	private static EventHandler? _systemThemeChanged;
	private static EventHandler? _highContrastChanged;
	private static int _changesObserved;
	private static SystemTheme? _lastSystemTheme;
	private static SystemTheme? _systemThemeOverride;
	private static bool? _lastHighContrast;
	private static string? _lastHighContrastScheme;
	private static HighContrastSystemColors? _lastHighContrastSystemColors;
	private static bool _lastHighContrastSystemColorsInitialized;

#if __ANDROID__ || __APPLE_UIKIT__
	private static readonly HighContrastSystemColors _mobileHighContrastBlackSystemColors = new(
		ButtonFaceColor: Colors.Black,
		ButtonTextColor: Colors.White,
		GrayTextColor: Color.FromArgb(255, 63, 242, 63),
		HighlightColor: Color.FromArgb(255, 26, 235, 255),
		HighlightTextColor: Colors.Black,
		HotlightColor: Colors.Yellow,
		WindowColor: Colors.Black,
		WindowTextColor: Colors.White,
		ActiveCaptionColor: Colors.Black,
		BackgroundColor: Colors.Black,
		CaptionTextColor: Colors.White,
		InactiveCaptionColor: Colors.Black,
		InactiveCaptionTextColor: Color.FromArgb(255, 63, 242, 63),
		DisabledTextColor: Color.FromArgb(255, 63, 242, 63));

	private static readonly HighContrastSystemColors _mobileHighContrastWhiteSystemColors = new(
		ButtonFaceColor: Colors.White,
		ButtonTextColor: Colors.Black,
		GrayTextColor: Color.FromArgb(255, 109, 109, 109),
		HighlightColor: Colors.Black,
		HighlightTextColor: Colors.White,
		HotlightColor: Colors.Blue,
		WindowColor: Colors.White,
		WindowTextColor: Colors.Black,
		ActiveCaptionColor: Colors.White,
		BackgroundColor: Colors.White,
		CaptionTextColor: Colors.Black,
		InactiveCaptionColor: Colors.White,
		InactiveCaptionTextColor: Color.FromArgb(255, 109, 109, 109),
		DisabledTextColor: Color.FromArgb(255, 109, 109, 109));
#endif

	/// <summary>
	/// Gets the current system theme. When <see cref="SystemThemeOverride"/> is set (for testing),
	/// that value is reported instead of the real OS theme.
	/// </summary>
	/// <remarks>
	/// <see cref="_lastSystemTheme"/> only ever caches the real OS theme; the override is applied
	/// on top of it here, so it never poisons the cache.
	/// </remarks>
	internal static SystemTheme SystemTheme => _systemThemeOverride ?? (_lastSystemTheme ??= GetSystemTheme());

	/// <summary>
	/// Gets whether the OS high-contrast accessibility feature is currently active. High contrast is an
	/// OS/app-global dimension OR-ed onto the Light/Dark base theme (matching WinUI's
	/// <c>FrameworkTheming::GetTheme</c>). The effective value comes from the platform and is cached until
	/// the platform reports a change. <see cref="Uno.WinRTFeatureConfiguration.Accessibility.HighContrast"/>
	/// overrides that value when explicitly set, including for runtime tests.
	/// </summary>
	internal static bool IsHighContrast =>
		Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride
		?? (_lastHighContrast ??= GetIsHighContrastEnabled());

	internal static string HighContrastSchemeName
	{
		get
		{
			if (Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride is { } overrideScheme)
			{
				return overrideScheme;
			}

			if (IsHighContrast && HighContrastSystemColors is null)
			{
				return SystemTheme == SystemTheme.Dark ? "High Contrast Black" : "High Contrast White";
			}

			return _lastHighContrastScheme ??= GetHighContrastSchemeName();
		}
	}

	internal static HighContrastSystemColors? HighContrastSystemColors
	{
		get
		{
			if (!IsHighContrast)
			{
				return null;
			}

			if (Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride is { } overrideColors)
			{
				return overrideColors;
			}

			if (!_lastHighContrastSystemColorsInitialized)
			{
				_lastHighContrastSystemColors = GetHighContrastSystemColors();
				_lastHighContrastSystemColorsInitialized = true;
			}

			return _lastHighContrastSystemColors;
		}
	}

	/// <summary>
	/// Test hook (analogous to <c>ScaleOverride</c>): when set, <see cref="SystemTheme"/> reports this
	/// value instead of the real OS theme; set to <c>null</c> to restore real OS detection. Assigning
	/// raises <see cref="SystemThemeChanged"/> when the effective theme changes, mirroring a real OS
	/// theme change so observers (e.g. <c>Application.OnSystemThemeChanged</c>) react exactly as in
	/// production. Enables runtime tests to validate behavior under a deterministic ambient OS theme,
	/// and under runtime OS-theme changes, independently of the real machine theme.
	/// </summary>
	internal static SystemTheme? SystemThemeOverride
	{
		get => _systemThemeOverride;
		set
		{
			if (_systemThemeOverride == value)
			{
				return;
			}

			var previousScheme = HighContrastSchemeName;
			var previousColors = HighContrastSystemColors;
			_systemThemeOverride = value;

			if (value is null)
			{
				// Restoring real OS detection: re-poll so the cache reflects any OS change
				// that occurred while the override was active.
				_lastSystemTheme = GetSystemTheme();
			}

			// Setting the override forces the system theme, so notify observers unconditionally — unlike
			// RefreshSystemTheme (which gates on the cached theme for OS polling). The app's current theme
			// can legitimately desync from the effective theme (e.g. after a test sets then restores an
			// explicit app theme), so a conditional raise could skip and leave a stale theme (test flakiness).
			RaiseSystemThemeChanged();

			if (!string.Equals(previousScheme, HighContrastSchemeName, StringComparison.Ordinal)
				|| previousColors != HighContrastSystemColors)
			{
				NotifyHighContrastSettingsChanged();
			}
		}
	}

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
	/// Removes event handlers whose target belongs to a non-default ALC.
	/// Called during ALC teardown to release references to inner-app objects.
	/// </summary>
	internal static void ClearNonDefaultAlcHandlers()
	{
		ClearNonDefaultAlcHandlers(ref _systemThemeChanged);
		ClearNonDefaultAlcHandlers(ref _highContrastChanged);
	}

	private static void ClearNonDefaultAlcHandlers(ref EventHandler? handler)
	{
		if (handler is null)
		{
			return;
		}

		foreach (var d in handler.GetInvocationList())
		{
			// Check both the target instance and the method's declaring type.
			// Static handlers (Target == null) can still reference a non-default ALC
			// via Method.DeclaringType.
			var handlerAssembly = d.Target?.GetType().Assembly
				?? d.Method.DeclaringType?.Assembly;

			if (handlerAssembly is not null)
			{
				var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(handlerAssembly);
				if (alc is not null && alc != System.Runtime.Loader.AssemblyLoadContext.Default)
				{
					handler -= (EventHandler)d;
				}
			}
		}
	}

	/// <summary>
	/// Starts observing system theme changes.
	/// </summary>
	internal static void ObserveThemeChanges()
	{
		if (Interlocked.Exchange(ref _changesObserved, 1) != 0)
		{
			return;
		}

		// Seed the OS-theme cache; an active SystemThemeOverride stays authoritative because the
		// SystemTheme getter checks it before the cache.
		_lastSystemTheme ??= GetSystemTheme();
		_lastHighContrast ??= GetIsHighContrastEnabled();
		_lastHighContrastScheme ??= GetHighContrastSchemeName();
		if (!_lastHighContrastSystemColorsInitialized)
		{
			_lastHighContrastSystemColors = GetHighContrastSystemColors();
			_lastHighContrastSystemColorsInitialized = true;
		}

		ObserveThemeChangesPlatform();

		// Ensure to check for theme changes when app leaves background
		// as we might miss a theme change if the app gets suspended.
		CoreApplication.LeavingBackground += (s, e) => RefreshSystemTheme();
		CoreApplication.Resuming += (s, e) => RefreshSystemTheme();
	}

	/// <summary>
	/// Raises SystemThemeChanged if the theme
	/// has changed since we last checked it.
	/// </summary>
	internal static void RefreshSystemTheme()
	{
		RefreshThemeState(refreshSystemTheme: true);
	}

	internal static void RefreshHighContrast()
	{
		RefreshThemeState(refreshSystemTheme: false);
	}

	private static void RefreshThemeState(bool refreshSystemTheme)
	{
		var previousTheme = SystemTheme;
		var previousHighContrast = IsHighContrast;
		var previousScheme = HighContrastSchemeName;
		var previousColors = HighContrastSystemColors;

		if (refreshSystemTheme)
		{
			// Re-poll the real OS theme; while an override is active this refreshes the cache
			// silently (the effective theme stays pinned to the override).
			_lastSystemTheme = GetSystemTheme();
		}

		_lastHighContrast = GetIsHighContrastEnabled();
		_lastHighContrastScheme = GetHighContrastSchemeName();
		_lastHighContrastSystemColors = GetHighContrastSystemColors();
		_lastHighContrastSystemColorsInitialized = true;

		if (SystemTheme != previousTheme)
		{
			RaiseSystemThemeChanged();
		}

		if (previousHighContrast != IsHighContrast
			|| !string.Equals(previousScheme, HighContrastSchemeName, StringComparison.Ordinal)
			|| previousColors != HighContrastSystemColors)
		{
			NotifyHighContrastSettingsChanged();
		}
	}

	internal static void NotifyHighContrastSettingsChanged()
	{
		AccessibilitySettings.OnHighContrastChanged();
		UISettings.OnColorValuesChanged();
		_highContrastChanged?.Invoke(null, EventArgs.Empty);
	}

	/// <summary>
	/// This is just a partial method, as on some platforms (e.g. Android and iOS)
	/// the theme change is listened to on the Application/Window level.
	/// </summary>
	static partial void ObserveThemeChangesPlatform();

	static partial void GetIsHighContrastEnabledPlatform(ref bool result);

	static partial void GetHighContrastSchemeNamePlatform(ref string result);

	static partial void GetHighContrastSystemColorsPlatform(ref HighContrastSystemColors? result);

	private static bool GetIsHighContrastEnabled()
	{
		var result = false;
		GetIsHighContrastEnabledPlatform(ref result);
		return result;
	}

	private static string GetHighContrastSchemeName()
	{
		var result = "High Contrast Black";
		GetHighContrastSchemeNamePlatform(ref result);
		return result;
	}

	private static HighContrastSystemColors? GetHighContrastSystemColors()
	{
		HighContrastSystemColors? result = null;
		GetHighContrastSystemColorsPlatform(ref result);
		return result;
	}

	internal static string ResolveHighContrastSchemeName(
		string? configuredSchemeName,
		bool isHighContrastEnabled,
		Color window,
		Color windowText)
	{
		if (!string.IsNullOrEmpty(configuredSchemeName))
		{
			return configuredSchemeName;
		}

		if (!isHighContrastEnabled)
		{
			return string.Empty;
		}

		if (IsWhite(window) && IsBlack(windowText))
		{
			return "High Contrast White";
		}

		if (IsBlack(window) && IsWhite(windowText))
		{
			return "High Contrast Black";
		}

		return "High Contrast";
	}

	private static bool IsWhite(Color color) =>
		(color.R == 0xFF && color.G == 0xFF && color.B == 0xFF)
		|| (color.R == 0xEB && color.G == 0xEB && color.B == 0xEB);

	private static bool IsBlack(Color color) =>
		(color.R == 0x00 && color.G == 0x00 && color.B == 0x00)
		|| (color.R == 0x10 && color.G == 0x10 && color.B == 0x10);

#if __ANDROID__ || __APPLE_UIKIT__
	private static string GetMobileHighContrastSchemeName(SystemTheme systemTheme) =>
		systemTheme == SystemTheme.Dark ? "High Contrast Black" : "High Contrast White";

	private static HighContrastSystemColors GetMobileHighContrastSystemColors(SystemTheme systemTheme) =>
		systemTheme == SystemTheme.Dark
			? _mobileHighContrastBlackSystemColors
			: _mobileHighContrastWhiteSystemColors;
#endif

	private static void RaiseSystemThemeChanged() => _systemThemeChanged?.Invoke(null, EventArgs.Empty);
}
