using System;

namespace Microsoft.UI.Xaml.Controls;

[Obsolete(
	"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.SplitButton instead.")]
public partial class SplitButtonAutomationPeer
{
	public SplitButtonAutomationPeer(SplitButton owner) : base(owner)
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft.UI.Xaml.Controls.SplitButton instead.");
	}
}
