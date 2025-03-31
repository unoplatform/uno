using System;

namespace Microsoft.UI.Xaml.Controls;

[Obsolete(
	"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.SwipeMode instead.")]
public enum SwipeBehaviorOnInvoked
{
	Auto = 0,
	Close = 1,
	RemainOpen = 2,
}
