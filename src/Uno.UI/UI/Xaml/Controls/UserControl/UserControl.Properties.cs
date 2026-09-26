#nullable enable

using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class UserControl
{
	// Content is hosted as a real visual child, so DataContext flows through the visual tree.
	// Don't propagate it again as a property (ValueDoesNotInheritDataContext avoids double-propagation).
	[GeneratedDependencyProperty(ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext)]
	public partial UIElement Content { get; set; }

	private static UIElement? GetContentDefaultValue() => null;
}
