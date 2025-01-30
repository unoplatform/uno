#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal interface IFocusManager
	{
		DependencyObject? FindNextFocus(
			FindFocusOptions findFocusOptions,
			XYFocusOptions xyFocusOptions,
			DependencyObject? component = null,
			bool updateManifolds = true);

		FocusMovementResult SetFocusedElement(FocusMovement movement);
	}
}
