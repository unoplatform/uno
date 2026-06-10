using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI;

public class ApplicationHelper
{
	private static readonly List<Window> _windows = new();
	private static string _requestedCustomTheme;

	/// <summary>
	/// Obsolete and now a no-op. The app-level custom-theme axis has been removed to align with WinUI,
	/// which has no custom-theme-name concept — the application theme is strictly Light or Dark.
	/// </summary>
	/// <remarks>
	/// To provide a custom/brand palette, merge a <see cref="ResourceDictionary"/> that overrides specific
	/// brush/color keys on top of the standard Light/Dark theme dictionaries; to switch between the standard
	/// themes, set <see cref="Application.RequestedTheme"/>.
	/// </remarks>
	[Obsolete("RequestedCustomTheme is now a no-op (the custom-theme axis has no WinUI equivalent). Provide a custom palette via merged ResourceDictionaries and use Application.RequestedTheme to switch themes.")]
	public static string RequestedCustomTheme
	{
		get => _requestedCustomTheme;
		// Retained for source compatibility (the value round-trips) but no longer selects a theme.
		set
		{
			if (!string.IsNullOrEmpty(value) && typeof(ApplicationHelper).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(ApplicationHelper).Log().LogWarning(
					$"RequestedCustomTheme is no longer supported and \"{value}\" will not select a theme. " +
					"Provide a custom palette via merged ResourceDictionaries that override specific brush/color " +
					"keys on top of the Light/Dark theme dictionaries, and use Application.RequestedTheme to switch themes.");
			}

			_requestedCustomTheme = value;
		}
	}

	/// <summary>
	/// Force all {ThemeResource} declarations to reevaluate its bindings.
	/// </summary>
	/// <remarks>
	/// This could be useful if you manually changed the bound values in global
	/// themed dictionary and you want to reapply them without having to toggle
	/// dark/light and producing annoying flickering to user.
	/// 
	/// Only applications with dynamic color schemes should use this.
	/// </remarks>
	public static void ReapplyApplicationTheme()
		=> Application.Current.OnRequestedThemeChanged();

	public static IReadOnlyList<Microsoft.UI.Xaml.Window> Windows => _windows.AsReadOnly();

	// Exposing as a List internally to avoid enumerator boxing allocations.
	internal static List<Microsoft.UI.Xaml.Window> WindowsInternal => _windows;

	public static bool IsLoadableComponent(Uri resource)
	{
		return Application.Current?.IsLoadableComponent(resource) ?? false;
	}

	internal static void AddWindow(Microsoft.UI.Xaml.Window window) => _windows.Add(window);

	internal static void RemoveWindow(Microsoft.UI.Xaml.Window window) => _windows.Remove(window);
}
