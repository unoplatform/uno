using Windows.UI.Xaml.Controls.Primitives;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ToggleButtonAutomationPeer : ButtonBaseAutomationPeer, Provider.IToggleProvider
	{
		public ToggleButtonAutomationPeer(ToggleButton owner) : base(owner)
		{
		}

		protected override string GetClassNameCore() => "ToggleButton";

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

		public ToggleState ToggleState
		{
			get
			{
				switch (((ToggleButton)Owner).IsChecked)
				{
					case (true):
						return ToggleState.On;
					case (false):
						return ToggleState.Off;
					default:
						return ToggleState.Indeterminate;
				}
			}
		}

		public void Toggle()
		{
			if (IsEnabled())
			{
				((ToggleButton)Owner).AutomationToggleButtonOnToggle();
			}
		}

		internal void RaiseToggleStatePropertyChangedEvent(object pOldValue, object pNewValue)
		{
			var oldValue = ConvertToToggleState(pOldValue);

			var newValue = ConvertToToggleState(pNewValue);

			MUX_ASSERT(oldValue != ToggleState.Indeterminate);
			MUX_ASSERT(newValue != ToggleState.Indeterminate);

			if (oldValue != newValue)
			{
				RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
			}
		}

		/// <summary>
		/// Convert the Boolean in Inspectable to the ToggleState Enum, if the Inspectable is null
		/// that corresponds to Indeterminate state.
		/// </summary>
		internal static ToggleState ConvertToToggleState(object pValue)
		{
			var pToggleState = ToggleState.Indeterminate;

			if (pValue != null)
			{
				var bValue = (bool)pValue;

				if (bValue)
				{
					pToggleState = ToggleState.On;
				}
				else
				{

					pToggleState = ToggleState.Off;
				}
			}

			return pToggleState;
		}
	}
}
