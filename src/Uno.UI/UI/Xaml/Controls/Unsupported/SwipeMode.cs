using System;

namespace Microsoft.UI.Xaml.Controls;

[Obsolete(
	"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.SwipeMode instead.")]
public enum SwipeMode
{
	Reveal = 0,
	Execute = 1,
}
