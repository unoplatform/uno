#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the ChoosingGroupHeaderContainer event.
/// </summary>
public partial class ChoosingGroupHeaderContainerEventArgs
{
	/// <summary>
	/// Initializes a new instance of the ChoosingGroupHeaderContainerEventArgs class.
	/// </summary>
	public ChoosingGroupHeaderContainerEventArgs()
	{
	}

	/// <summary>
	/// Gets the data item associated with this group header.
	/// </summary>
	public object? Group { get; internal set; }

	/// <summary>
	/// Gets or sets the UI container that will be used to display the current group header.
	/// </summary>
	public ListViewBaseHeaderItem? GroupHeaderContainer { get; set; }

	/// <summary>
	/// Gets the index in the ItemsSource of the group for which a container is being selected.
	/// </summary>
	public int GroupIndex { get; internal set; }
}
