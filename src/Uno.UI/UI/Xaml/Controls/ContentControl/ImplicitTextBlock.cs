using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Automation;

namespace Windows.UI.Xaml.Controls
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
		public ImplicitTextBlock(DependencyObject parent)
		{
			// Propagate the parent's (ContentPresenter or ContentControl) AutomationProperties.AccessibilityView to ImplicitTextBlock once.
			var accessibilityView = AutomationProperties.GetAccessibilityView(parent);
			AutomationProperties.SetAccessibilityView(this, accessibilityView);
		}
	}
}
