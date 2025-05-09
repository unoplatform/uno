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

#if HAS_UNO // Uno specific: Simulate enter/leave lifecycle events
		this.Loaded += (s, e) => EnterImpl(this, new Uno.UI.Xaml.EnterParams());
		this.Unloaded += (s, e) => LeaveImpl(this, new Uno.UI.Xaml.LeaveParams());
#endif
	}
}
