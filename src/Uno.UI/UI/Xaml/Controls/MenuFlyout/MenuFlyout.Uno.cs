using Microsoft.UI.Xaml;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyout
{
	internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnDataContextChanged(e);

		SetFlyoutItemsDataContext();
	}

	private void SetFlyoutItemsDataContext()
	{
		// This is present to force the dataContext to be passed to the popup of the flyout since it is not directly a child in the visual tree of the flyout.
		Items?.ForEach(item => item?.SetValue(
			UIElement.DataContextProperty,
			this.DataContext,
			precedence: DependencyPropertyValuePrecedences.Inheritance
		));
	}
}
