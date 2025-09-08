namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the TabView.TabTearOutRequested event.
/// </summary>
public sealed partial class TabViewTabTearOutRequestedEventArgs
{
	internal TabViewTabTearOutRequestedEventArgs(object item, UIElement tab)
	{
		Items = [item];
		Tabs = [tab];
	}

	internal TabViewTabTearOutRequestedEventArgs()
	{
	}

	/// <summary>
	/// Gets the data items held by the selected tabs that are being dragged.
	/// </summary>
	public object[] Items { get; }

	/// <summary>
	/// Gets the WindowId for the new window that will host the torn-out tabs.
	/// </summary>
	public WindowId NewWindowId { get; }

	/// <summary>
	/// Gets the selected tabs that are being dragged.
	/// </summary>
	public UIElement[] Tabs { get; }
}
