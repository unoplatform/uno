#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;

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
}
