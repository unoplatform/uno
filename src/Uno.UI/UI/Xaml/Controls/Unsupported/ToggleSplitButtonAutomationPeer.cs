using System;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.ToggleSplitButton instead.")]
public partial class ToggleSplitButtonAutomationPeer
{
	public ToggleSplitButtonAutomationPeer(ToggleSplitButton owner) : base(owner)
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.ToggleSplitButton instead.");
	}
}
