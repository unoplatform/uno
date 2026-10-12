#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Helper;

/// <summary>The trailing search suggestion that opens every result in the browser pane.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed partial class SearchSeeAllItem
{
	public SearchSeeAllItem(int count, string? term, string shortcut = "")
	{
		Count = count;
		ControlName = term ?? string.Empty;
		Shortcut = shortcut;
	}

	public int Count { get; }

	/// <summary>The keyboard route to the same results; empty on touch.</summary>
	public string Shortcut { get; }

	public bool HasShortcut => Shortcut.Length > 0;

	/// <summary>Matches the suggestions' TextMemberPath, so choosing this item keeps the typed text.</summary>
	public string ControlName { get; }

	public string Text => $"See all {Count:N0} results";
}

/// <summary>A category heading row inside the flat search results list.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed partial class SearchResultsHeader
{
	public SearchResultsHeader(string title) => Title = title;

	public string Title { get; }

	public override string ToString() => Title;
}

/// <summary>Picks the row template for sample rows, the "See all" suggestion and search headings.</summary>
public sealed partial class ShellTemplateSelector : DataTemplateSelector
{
	public DataTemplate? ItemTemplate { get; set; }

	public DataTemplate? SeeAllTemplate { get; set; }

	public DataTemplate? HeaderTemplate { get; set; }

	protected override DataTemplate? SelectTemplateCore(object item) => item switch
	{
		SearchSeeAllItem => SeeAllTemplate,
		SearchResultsHeader => HeaderTemplate,
		_ => ItemTemplate,
	};

	protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}
