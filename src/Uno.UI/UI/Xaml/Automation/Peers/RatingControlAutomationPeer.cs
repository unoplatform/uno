using System;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

[Obsolete(
	"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.RatingControl instead.")]
public partial class RatingControlAutomationPeer : FrameworkElementAutomationPeer
{
	public RatingControlAutomationPeer(RatingControl owner) : base(owner)
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.RatingControl instead.");
	}
}
