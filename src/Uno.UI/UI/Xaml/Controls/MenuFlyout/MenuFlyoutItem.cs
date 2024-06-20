using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a command in a MenuFlyout control.
/// </summary>
[ContentProperty(Name = nameof(Text))]
public partial class MenuFlyoutItem : MenuFlyoutItemBase
{
	/// <summary>
	/// Initializes a new instance of the MenuFlyoutItem class.
	/// </summary>
	public MenuFlyoutItem()
	{
		DefaultStyleKey = typeof(MenuFlyoutItem);

		Initialize();
	}
}
