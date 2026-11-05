using Microsoft.UI.Xaml.Markup;
using DirectUI;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Displays content on top of existing content, within the bounds of the app window.
/// </summary>
[ContentProperty(Name = nameof(Child))]
public partial class Popup : FrameworkElement, IBackButtonListener
{
}
