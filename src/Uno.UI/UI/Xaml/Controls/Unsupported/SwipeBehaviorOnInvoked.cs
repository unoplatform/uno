#nullable disable

using System;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.SwipeMode instead.")]
public enum SwipeBehaviorOnInvoked
{
	Auto = 0,
	Close = 1,
	RemainOpen = 2,
}
