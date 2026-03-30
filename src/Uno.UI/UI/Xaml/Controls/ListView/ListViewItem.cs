using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents the container for an item in a ListView control.
/// </summary>
public partial class ListViewItem : SelectorItem
{
	/// <summary>
	/// Initializes a new instance of the ListViewItem class.
	/// </summary>
	public ListViewItem()
	{
		DefaultStyleKey = typeof(ListViewItem);
		TemplateSettings = new ListViewItemTemplateSettings();
	}

	/// <summary>
	/// Gets the calculated settings for the ListViewItem template.
	/// </summary>
	public ListViewItemTemplateSettings TemplateSettings { get; }
}
