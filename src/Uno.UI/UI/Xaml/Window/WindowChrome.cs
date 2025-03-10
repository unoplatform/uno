#nullable enable

using Windows.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Controls;

internal sealed partial class WindowChrome : ContentControl
{
	public WindowChrome(Windows.UI.Xaml.Window parent)
	{
		HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
		HorizontalContentAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;

		IsTabStop = false;
	}

	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		// Fire XamlRoot.Changed
		var xamlIslandRoot = VisualTree.GetXamlIslandRootForElement(this);
		xamlIslandRoot!.ContentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.Content);
	}
}

