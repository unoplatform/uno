namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the TabView.ExternalTornOutTabsDropping event.
/// </summary>
public partial class TabViewExternalTornOutTabsDroppingEventArgs
{
	internal TabViewExternalTornOutTabsDroppingEventArgs(object item, UIElement tab, int dropIndex)
	{
		Items = [item];
		Tabs = [tab];
		DropIndex = dropIndex;
	}

	internal TabViewExternalTornOutTabsDroppingEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that indicates whether or not to accept the tabs that are being dropped.
	/// </summary>
	public bool AllowDrop { get; set; }

	/// <summary>
	/// Gets the index at which the dropped tabs should be inserted into the TabView.
	/// </summary>
	public int DropIndex { get; }

	/// <summary>
	/// Gets the data items held by the tabs that are being dropped.
	/// </summary>
	public object[] Items { get; }

	/// <summary>
	/// Gets the tabs that are being dropped.
	/// </summary>
	public UIElement[] Tabs { get; }
}
