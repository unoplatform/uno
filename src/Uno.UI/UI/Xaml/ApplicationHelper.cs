using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Uno.UI;

public class ApplicationHelper
{
	private static readonly List<Window> _windows = new();
	private static string _requestedCustomTheme;

	/// <summary>
	/// This property is obsolete. The app-level custom-theme axis has been removed to align with WinUI,
	/// which has no custom-theme-name concept — the application theme is strictly Light or Dark. Setting
	/// this property is now a no-op and no longer selects a custom <c>ThemeDictionaries</c> entry.
	/// </summary>
	/// <remarks>
	/// Migration: provide a custom/brand palette via a merged <see cref="ResourceDictionary"/> that
	/// overrides specific brush/color keys on top of the standard Light/Dark theme dictionaries, rather
	/// than inventing a new theme name. To switch the application between the standard themes, set
	/// <see cref="Application.RequestedTheme"/> (before the application resources are loaded). A
	/// <c>"Light"</c>/<c>"Dark"</c> custom name continues to resolve as the corresponding standard theme.
	/// See <c>specs/theming-winui-alignment/custom-theme.md</c> (Phase 6, Option B).
	/// </remarks>
	[Obsolete("The app-level custom-theme axis has been removed (it has no WinUI equivalent and cannot compose with element-level theming). Setting RequestedCustomTheme is now a no-op. Provide a custom palette via merged ResourceDictionaries that override specific brush/color keys on top of the Light/Dark theme dictionaries, and use Application.RequestedTheme to switch between the standard themes.")]
	public static string RequestedCustomTheme
	{
		get => _requestedCustomTheme;
		// No-op: the value is retained for source compatibility (it round-trips), but it no longer keys a
		// custom ThemeDictionaries entry — Themes.Active is strictly Light/Dark (+ high contrast, composed
		// at the resolution leaf). See Application.UpdateRequestedThemesForResources.
		set => _requestedCustomTheme = value;
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
