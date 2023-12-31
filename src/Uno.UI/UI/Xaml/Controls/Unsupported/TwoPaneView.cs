using System;

namespace Microsoft.UI.Xaml.Controls;

[Obsolete(
		"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
		"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.TwoPaneView instead.")]
public partial class TwoPaneView
{
	public TwoPaneView()
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.TwoPaneView instead.");
	}
}
