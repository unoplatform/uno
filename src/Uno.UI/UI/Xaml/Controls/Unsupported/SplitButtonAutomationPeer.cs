using System;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.SplitButton instead.")]
public partial class SplitButtonAutomationPeer
{
	public SplitButtonAutomationPeer(SplitButton owner) : base(owner)
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft.UI.Xaml.Controls.SplitButton instead.");
	}
}
