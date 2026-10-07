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
		=> Visible(HasDescription(description, isHomeVisible) && !isCollapsed);

	public static bool HasDescription(string? description, bool isHomeVisible) => NotEmpty(description) && !isHomeVisible;

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

	internal const int MaxSuggestions = 8;

	/// <summary>The header search shows the top matches, then a "See all" entry that opens the results view.</summary>
	public static IList TopSuggestions(IEnumerable? items, string? term, bool showShortcutHints)
	{
		var all = items?.Cast<object>().ToList() ?? new List<object>();
		if (all.Count <= MaxSuggestions)
		{
			return all;
		}

		var top = all.Take(MaxSuggestions).ToList();
		top.Add(new SearchSeeAllItem(all.Count, term, showShortcutHints ? "Ctrl+Enter" : ""));
		return top;
	}

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

	/// <summary>
	/// Search results as one list with a heading row per category. Uno's Skia ListView does not render
	/// GroupStyle headers (unoplatform/uno#21019), so the groups are flattened rather than shown through a grouped view.
	/// </summary>
	public static IList FlattenSearchResults(IEnumerable<IGrouping<string, SampleChooserContent>>? groups)
	{
		List<object> rows = new();
		foreach (var group in groups ?? Enumerable.Empty<IGrouping<string, SampleChooserContent>>())
		{
			rows.Add(new SearchResultsHeader(group.Key.StartsWith('_') ? "Tools" : group.Key));
			rows.AddRange(group);
		}

		return rows;
	}

	// The pane box needs its own function: the WinAppSDK XAML compiler fails on a function binding repeated with the same arguments.
	public static IList PaneSuggestions(IEnumerable? items, string? term, bool showShortcutHints) => TopSuggestions(items, term, showShortcutHints);

	public static string SearchResultsTitle(string? term) => $"Results for “{term?.Trim()}”";

	public static string SearchResultCountName(int count) => count == 1 ? "1 result" : $"{count:N0} results";

	public static string SearchEmptyTitle(string? term) => $"No samples match “{term?.Trim()}”";

	public static Visibility SearchEmptyVisibility(int count, string? term) => Visible(count == 0 && NotEmpty(term));

	/// <summary>The results view lists the best matches only; the footer says so when there are more.</summary>
	public static Visibility SearchCapVisibility(bool isSearchView, int count) => Visible(isSearchView && count > SampleSearch.MaxGroupedResults);

	public static string SearchCapText(int count) => $"Showing the first {SampleSearch.MaxGroupedResults:N0} of {count:N0}";

	public static Visibility LibraryLoadingVisibility(bool isCategoriesView, bool isIndexLoaded) => Visible(isCategoriesView && !isIndexLoaded);

	public static Visibility SavedSamplesLoadingVisibility(bool isFavoritesView, bool isRecentsView, bool isLoaded)
		=> Visible((isFavoritesView || isRecentsView) && !isLoaded);

	public static Visibility EmptyListVisibility(bool hasItems, bool isLoaded) => Visible(isLoaded && !hasItems);

	public static string RecentsEmptyHint(bool isEnabled)
		=> isEnabled ? "Samples you open show up here." : "History is disabled in this build configuration.";
}
