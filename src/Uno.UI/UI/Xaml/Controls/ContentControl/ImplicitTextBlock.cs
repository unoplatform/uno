using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// A special textblock that will not inherit from the default style of a TextBlock. This type should not be used directly.
	/// </summary>
	/// <remarks>This is required to ensure that content controls that are given no ContentTemplate
	/// are using the proper inherited properties, and not the implicit or default style of a TextBlock.
	/// Do to this, we just ignore the default style of a TextBlock.
	/// </remarks>
	[Data.Bindable]
	public partial class ImplicitTextBlock : TextBlock
	{
		// The ContentPresenter / ContentControl that generated this implicit text for its string content.
		internal DependencyObject ContentOwner { get; }

		public ImplicitTextBlock(DependencyObject parent)
		{
			ContentOwner = parent;
		}

		// The implicit content text follows its owning ContentPresenter/ContentControl's view membership.
		// WinUI's control templates mark the ContentPresenter AccessibilityView="Raw" (e.g. Button,
		// CheckBox, RadioButton, ToggleSwitch) so the generated content text is NOT surfaced as a separate
		// UIA element — the owning control's peer already exposes the content as its Name. The owner's
		// AccessibilityView is applied by the template after this text block is constructed, so we read it
		// live from a dedicated peer (rather than copying once, which would read the stale default).
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new ImplicitTextBlockAutomationPeer(this);
	}
}
