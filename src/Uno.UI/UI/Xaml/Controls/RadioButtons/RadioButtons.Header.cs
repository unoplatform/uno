namespace Microsoft.UI.Xaml.Controls
{
	public partial class RadioButtons
	{
		// This is an object that RadioButtons intends to attach to its child RadioButton elements.
		// It contains the revokers for the events on RadioButton that RadioButttons listens to
		// in order to manage selection.  Attaching the revokers to the object allows the parent
		// RadioButtons the ability to "Set it and forget it" since the lifetime of the RadioButton
		// and these event registrations are now intrically linked.
		// This is not needed in Uno, as we can directly unsubscribe the events
		//private class ChildHandlers
		//{
		//	ToggleButton Checked_revoker checkedRevoker;
		//	winrt::ToggleButton::Unchecked_revoker uncheckedRevoker;
		//}

		private int m_selectedIndex = -1;

		// This is used to guard against reentrency when calling select, since select changes
		// the Selected Index/Item which in turn calls select.
		private bool m_currentlySelecting = false;

		// We block selection before the control has loaded.
		// This is to ensure that we do not overwrite a provided Selected Index/Item value.
		private bool m_blockSelecting = true;

		private ItemsRepeater m_repeater = null;

		private RadioButtonsElementFactory m_radioButtonsElementFactory = null;

		private bool m_testHooksEnabled = false;

		private const string s_repeaterName = "InnerRepeater";
		//private const string s_childHandlersPropertyName = "ChildHandlers";
	}
}
