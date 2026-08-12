using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private DismissalTriggerFlags m_dismissalTriggerFlags;

	private bool m_fIsLightDismiss;
	private bool m_shouldTakeFocus = true;

	internal bool IsFlyout => AssociatedFlyout is not null;

	internal void SetShouldTakeFocus(bool value) => m_shouldTakeFocus = value;

	// We don't check IsActive() because we want the focus logic to also work with unrooted popups,
	// which are never in the live tree.
	internal override bool IsFocusable =>
		m_fIsLightDismiss &&
		IsVisible() &&
		IsEnabledInternal() &&
		AreAllAncestorsVisible();
}
