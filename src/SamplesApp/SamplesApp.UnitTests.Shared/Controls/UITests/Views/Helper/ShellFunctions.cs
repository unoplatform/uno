#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using SampleControl.Entities;
using SampleControl.Presentation;

namespace Uno.UI.Samples.Helper;

/// <summary>
/// Static x:Bind functions used by the shell XAML instead of converters. All of them accept nulls,
/// because x:Bind evaluates them before the DataContext (the view model) is set.
/// </summary>
public static class ShellFunctions
{
	public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility Collapsed(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

	public static bool Not(bool value) => !value;

	/// <summary>Phones, where every target is finger-sized.</summary>
	internal static bool IsTouchPlatform => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

	/// <summary>Touch-sized targets and no shortcut hints: phones, or Simulate touch.</summary>
	internal static bool IsTouchShell => IsTouchPlatform
#if HAS_UNO
		|| SampleChooserViewModel.Instance is { SimulateTouch: true }
#endif
		;

	public static bool NotEmpty(string? value) => !string.IsNullOrWhiteSpace(value);

	public static Visibility VisibleIfNotEmpty(string? value) => Visible(NotEmpty(value));

	public static Visibility ShowDescription(string? description, bool isCollapsed, bool isHomeVisible)
		=> Visible(NotEmpty(description) && !isCollapsed && !isHomeVisible);

	public static string FavoriteLabel(bool isFavorite) => isFavorite ? "Remove from favorites" : "Add to favorites";

	public static string RowName(string? controlName, bool isFavorite, bool isManualTest)
	{
		var name = controlName ?? string.Empty;

		if (isManualTest)
		{
			name += ", manual test";
		}

		if (isFavorite)
		{
			name += ", favorite";
		}

		return name;
	}

	public static IList Take(IEnumerable? items, int count)
		=> items is null || count <= 0
			? new List<object>()
			: items.Cast<object>().Take(count).ToList();

	/// <summary>The header search shows the top matches only; the results view lists the rest.</summary>
	public static IList TopSuggestions(IEnumerable? items) => Take(items, 8);

	public static string BrowserToggleGlyph(bool isOpen) => isOpen ? "\uE89F" : "\uE8A0";

	public static string FavoriteGlyph(bool isFavorite) => isFavorite ? "\uE735" : "\uE734";

	public static string HeaderTitle(string? controlName, bool isHomeVisible) => isHomeVisible ? "Home" : controlName ?? string.Empty;

	/// <summary>The header breadcrumb steps aside while the browser pane, which has its own, is open.</summary>
	public static Visibility HeaderBreadcrumbVisibility(IReadOnlyList<string>? crumbs, bool isBrowserOpen, bool isHomeVisible)
		=> Visible(crumbs is { Count: > 0 } && !isBrowserOpen && !isHomeVisible);

	public static Visibility ManualTagVisibility(bool isManualTest, bool isHomeVisible) => Visible(isManualTest && !isHomeVisible);

	// Home replaces the sample without unloading it. A separate function from Collapsed: the WinAppSDK XAML compiler
	// fails to generate the fallback of a function binding that appears twice with identical arguments.
	public static Visibility SampleHostVisibility(bool isHomeVisible) => Collapsed(isHomeVisible);

	public static Visibility EmptyHostVisibility(object? content, bool isHomeVisible) => Visible(content is null && !isHomeVisible);

	public static Visibility SamplesViewVisibility(BrowserView view) => Visible(view == BrowserView.Samples);

	public static Visibility SettingsViewVisibility(BrowserView view) => Visible(view == BrowserView.Settings);

	public static IReadOnlyList<string> CategoryCrumbs(SampleChooserCategory? category)
		=> category?.Category is { } name ? new[] { "Library", name } : new[] { "Library" };

	public static string CategoryCountText(int count) => count == 1 ? "1 category" : $"{count:N0} categories";

	public static string FavoritesCountText(int count) => count == 1 ? "1 favorite" : $"{count:N0} favorites";

	public static string RecentsCountText(IEnumerable? recents)
		=> recents?.Cast<object>().Count() is { } count ? $"{count:N0} recent" : string.Empty;
}
