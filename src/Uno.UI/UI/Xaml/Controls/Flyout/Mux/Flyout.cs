using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents a control that displays lightweight UI that is either information, or requires user interaction.
/// Unlike a dialog, a Flyout can be light dismissed by clicking or tapping outside of it, pressing the device's
/// back button, or pressing the 'Esc' key.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class Flyout : FlyoutBase
{
}
