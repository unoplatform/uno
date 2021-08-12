using Windows.UI.Xaml.Controls.Primitives;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ToggleButtonAutomationPeer : ButtonBaseAutomationPeer, Provider.IToggleProvider
	{
		public ToggleButtonAutomationPeer(ToggleButton element) : base(element)
		{
		}

		protected override string GetClassNameCore()
		{
			return "ToggleButton";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Button;
		}

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
				((ToggleButton)Owner).AutomationPeerToggle();
			}
		}

		internal void RaiseToggleStatePropertyChangedEvent(object pOldValue, object pNewValue)
		{
			var oldValue = ToggleButtonAutomationPeer.ConvertToToggleState(pOldValue);

			var newValue = ToggleButtonAutomationPeer.ConvertToToggleState(pNewValue);

			MUX_ASSERT(oldValue != ToggleState.Indeterminate);
			MUX_ASSERT(newValue != ToggleState.Indeterminate);

			if (oldValue != newValue)
			{
				RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
			}
		}

		/// <summary>
		/// Convert the Boolean in Inspectable to the ToggleState Enum, if the Inspectable is NULL that corresponds to Indeterminate state.
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
