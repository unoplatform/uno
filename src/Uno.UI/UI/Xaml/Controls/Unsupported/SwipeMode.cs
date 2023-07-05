#nullable disable

using System;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.SwipeMode instead.")]
public enum SwipeMode
{
	Reveal = 0,
	Execute = 1,
}
