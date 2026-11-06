#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the ChoosingItemContainer event.
/// </summary>
public partial class ChoosingItemContainerEventArgs
{
	/// <summary>
	/// Initializes a new instance of the ChoosingItemContainerEventArgs class.
	/// </summary>
	public ChoosingItemContainerEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the container is already prepared to display the data item.
	/// </summary>
	public bool IsContainerPrepared { get; set; }

	/// <summary>
	/// Gets the data item associated with this ChoosingItemContainerEventArgs.
	/// </summary>
	public object? Item { get; internal set; }

	/// <summary>
	/// Gets or sets the UI container that will be used to display the current data item.
	/// </summary>
	public SelectorItem? ItemContainer { get; set; }

	/// <summary>
	/// Gets the index in the ItemsSource of the data item for which a container is being selected.
	/// </summary>
	public int ItemIndex { get; internal set; }
}
