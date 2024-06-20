using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Text))]
public partial class MenuFlyoutItem : MenuFlyoutItemBase
{
	public MenuFlyoutItem()
	{
		DefaultStyleKey = typeof(MenuFlyoutItem);

		Initialize();
	}
}
