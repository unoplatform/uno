using System;

namespace Microsoft.UI.Xaml.Controls;

[Obsolete(
		"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
		"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.ColorPicker instead.")]
public partial class ColorPicker
{
	public ColorPicker()
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.ColorPicker instead.");
	}
}
