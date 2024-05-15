// MUX Reference Button_Partial.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Button : ButtonBase
	{
		// TODO Uno: Uncomment this code and all commented out code related to this field once we know where
		// to call SuppressFlyoutOpening method.
		// private bool _suppressFlyoutOpening;

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

		//protected override void OnPointerCanceled(PointerRoutedEventArgs e)
		//{
		//	_suppressFlyoutOpening = false;
		//	base.OnPointerCanceled(e);
		//}

		//protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		//{
		//	_suppressFlyoutOpening = false;
		//	base.OnPointerCaptureLost(args);
		//}

		//protected override void OnPointerExited(PointerRoutedEventArgs e)
		//{
		//	_suppressFlyoutOpening = false;
		//	base.OnPointerExited(e);
		//}

		/// <summary>
		/// In case if Button has set Flyout property, get associated Flyout and open it next to this Button.
		/// </summary>
		private protected virtual void OpenAssociatedFlyout()
		{
			//using var guard = Disposable.Create(() => _suppressFlyoutOpening = false);

			//if (!_suppressFlyoutOpening)
			{
				Flyout?.ShowAt(this);
			}
		}

		// TODO Uno: Keyboard accelerators not supported yet.
		//private void OnProcessKeyboardAcceleratorsImplLocal()

		//internal void SuppressFlyoutOpening()
		//{
		//	_suppressFlyoutOpening = true;
		//}
	}
}
