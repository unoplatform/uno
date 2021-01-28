// MUX Reference RatingControl.h, commit de78834

namespace Microsoft.UI.Xaml.Controls
{
	internal enum RatingControlStates
	{
		Disabled = 0,
		Set = 1,
		PointerOverSet = 2,
		PointerOverPlaceholder = 3, // Also functions as the pointer over unset state at the moment
		Placeholder = 4,
		Unset = 5,
		Null = 6
	};
}
