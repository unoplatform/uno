// MUX Reference ItemContainer.idl, tag winui3/release/1.5.0

using System;
using Windows.UI.Xaml.Markup;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

[Flags]
internal enum ItemContainerMultiSelectMode
{
	Auto = 1,
	Single = 2,
	Extended = 4,
	Multiple = 8
}

internal enum ItemContainerInteractionTrigger
{
	PointerPressed,
	PointerReleased,
	Tap,
	DoubleTap,
	EnterKey,
	SpaceKey,
	AutomationInvoke
}

[Flags]
internal enum ItemContainerUserInvokeMode
{
	Auto = 1,
	UserCanInvoke = 2,
	UserCannotInvoke = 4,
}

[Flags]
internal enum ItemContainerUserSelectMode
{
	Auto = 1,
	UserCanSelect = 2,
	UserCannotSelect = 4,
}
