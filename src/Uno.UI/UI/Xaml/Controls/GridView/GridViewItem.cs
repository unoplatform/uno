using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents the container for an item in a GridView control.
/// </summary>
public partial class GridViewItem : SelectorItem
{
	/// <summary>
	/// Initializes a new instance of the GridViewItem class.
	/// </summary>
	public GridViewItem()
	{
		Initialize();

		DefaultStyleKey = typeof(GridViewItem);
		TemplateSettings = new GridViewItemTemplateSettings();
	}

	partial void Initialize();

	/// <summary>
	/// Gets the calculated settings for the GridViewItem template.
	/// </summary>
	public GridViewItemTemplateSettings TemplateSettings { get; }
}
