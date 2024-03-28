using System;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
		"The Windows.UI.Xaml.Controls version of this control is not supported. " +
		"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.ColorPicker instead.")]
public partial class ColorPicker
{
	public ColorPicker()
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.ColorPicker instead.");
	}
}
