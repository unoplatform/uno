using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a flyout that displays a menu of commands.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class MenuFlyout : FlyoutBase, IMenu
{
}
