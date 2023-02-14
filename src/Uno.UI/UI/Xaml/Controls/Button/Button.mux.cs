using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Button : ButtonBase
	{
		/// <summary>
		/// Change to the correct visual state for the button.
		/// </summary>
		/// <param name="useTransitions">Use transitions.</param>
		private protected override void ChangeVisualState(bool useTransitions)
		{
			if (!IsEnabled)
			{
				GoToState(useTransitions, "Disabled");
			}
			else if (IsPressed)
			{
				GoToState(useTransitions, "Pressed");
			}
			else if (IsPointerOver)
			{
				GoToState(useTransitions, "PointerOver");
			}
			else
			{
				GoToState(useTransitions, "Normal");
			}

			if (FocusState != FocusState.Unfocused && IsEnabled)
			{
				if (FocusState == FocusState.Pointer)
				{
					GoToState(useTransitions, "PointerFocused");
				}
				else
				{
					GoToState(useTransitions, "Focused");
				}
			}
			else
			{
				GoToState(useTransitions, "Unfocused");
			}
		}

		/// <summary>
		/// Raises the Click routed event.
		/// </summary>
		private protected override void OnClick()
		{
			var hasAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.InvokePatternOnInvoked);
			if (hasAutomationListener)
			{
				var automationPeer = GetOrCreateAutomationPeer();
				if (automationPeer != null)
				{
					automationPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
				}
			}

			base.OnClick();

			// If this button has associated Flyout, open it now.
			OpenAssociatedFlyout();
		}

		/// <summary>
		/// Create ButtonAutomationPeer to represent the button.
		/// </summary>
		/// <returns>Automation peer.</returns>
		protected override AutomationPeer OnCreateAutomationPeer() => new ButtonAutomationPeer(this);

		/// <summary>
		/// In case if Button has set Flyout property, get associated Flyout and open it next to this Button.
		/// </summary>
		private protected virtual void OpenAssociatedFlyout() => Flyout?.ShowAt(this);

		// TODO Uno: Keyboard accelerators not supported yet.
		//private void OnProcessKeyboardAcceleratorsImplLocal()
	}
}
