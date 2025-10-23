namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the TabView.ExternalTornOutTabsDropped event.
/// </summary>
public partial class TabViewExternalTornOutTabsDroppedEventArgs
{
	internal TabViewExternalTornOutTabsDroppedEventArgs(object item, UIElement tab, int dropIndex)
	{
		Items = [item];
		Tabs = [tab];
		DropIndex = dropIndex;
	}

	internal TabViewExternalTornOutTabsDroppedEventArgs()
	{
	}

	/// <summary>
	/// Gets the index at which the dropped tabs should be inserted into the TabView.
	/// </summary>
	public int DropIndex { get; }

	/// <summary>
	/// Gets the data items held by the tabs that have been dropped.
	/// </summary>
	public object[] Items { get; }

	/// <summary>
	/// Gets the tabs that have been dropped.
	/// </summary>
	public UIElement[] Tabs { get; }
}
