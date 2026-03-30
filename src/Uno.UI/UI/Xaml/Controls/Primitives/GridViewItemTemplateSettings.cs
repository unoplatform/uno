// MUX Reference GridViewItemTemplateSettings.g.cpp, tag winui3/release/1.8.4

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class GridViewItemTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the number of items that are being dragged as part of a drag-and-drop operation.
	/// </summary>
	public int DragItemsCount
	{
		get => (int)GetValue(DragItemsCountProperty);
		internal set => SetValue(DragItemsCountProperty, value);
	}

	/// <summary>
	/// Identifies the DragItemsCount dependency property.
	/// </summary>
	public static DependencyProperty DragItemsCountProperty { get; } =
		DependencyProperty.Register(
			nameof(DragItemsCount),
			typeof(int),
			typeof(GridViewItemTemplateSettings),
			new FrameworkPropertyMetadata(0));
}
