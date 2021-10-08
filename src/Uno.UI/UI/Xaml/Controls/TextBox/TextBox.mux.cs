using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private bool _forceFocusedVisualState = false;
		private bool _isPointerOver = false;

		internal override void UpdateVisualState(bool useTransitions = true)
		{
			var focusManager = VisualTree.GetFocusManagerForElement(this);
			// CommonStates & FocusStates are combined
			//
			// NOTES: Pressed state is the same as Focused
			//        PointerFocused state is the same as Focused
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", true);
			}
			else if (_forceFocusedVisualState || (FocusState != FocusState.Unfocused && focusManager.IsPluginFocused()))
			{
				VisualStateManager.GoToState(this, "Focused", true);
			}
			else if (_isPointerOver)
			{
				VisualStateManager.GoToState(this, "PointerOver", true);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", true);
			}
		}
	}
}
