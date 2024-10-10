using System;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
		"The Windows.UI.Xaml.Controls version of this control is not supported. " +
		"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.RatingControl instead.")]
public partial class RatingControl
{
	public RatingControl()
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.RatingControl instead.");
	}
}
