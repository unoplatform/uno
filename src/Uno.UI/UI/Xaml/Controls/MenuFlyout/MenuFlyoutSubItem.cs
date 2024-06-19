using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a menu item that displays a sub-menu in a MenuFlyout control.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class MenuFlyoutSubItem : MenuFlyoutItemBase, ISubMenuOwner
{
	/// <summary>
	/// Initializes a new instance of the MenuFlyoutSubItem class.
	/// </summary>
	public MenuFlyoutSubItem()
	{
		DefaultStyleKey = typeof(MenuFlyoutSubItem);

		PrepareState();
	}
}
