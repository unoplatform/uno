#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	private UIElement? GetOverlayPassThroughRootVisual()
		// WinUI stores the placement target's public root visual in m_wrRootVisual.
		=> XamlRoot?.Content;

	private static void OnOverlayInputPassThroughElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		// FlyoutBase_partial.cpp:858: an application-set value no longer belongs to show-mode policy.
		=> ((FlyoutBase)sender).m_ownsOverlayInputPassThroughElement = false;
}
