using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Tests.Enterprise;
using static Private.Infrastructure.TestServices;

namespace Windows.UI.Xaml.Tests.Common;

internal static class FocusTestHelper
{
	public static async Task EnsureFocus(UIElement element, FocusState focusState, int Attempts = 1)
	{
		bool gotFocus = false;

		while (Attempts > 0 && !gotFocus)
		{
			Attempts--;

			// On desktop, check if there is any other window currently having the window focus
			// If so, try to bring the test app to foreground before attempting focus on the element
			//if (!Private.Infrastructure.TestServices.Utilities.IsOneCore &&
			//	!Private.Infrastructure.TestServices.Utilities.IsXBox)
			//{
			//	if (!Private.Infrastructure.TestServices.WindowHelper.IsFocusedWindow)
			//	{
			//		LOG_OUTPUT("Test app does not have window focus, bring it to foreground and try again!");
			//		Private.Infrastructure.TestServices.WindowHelper.RestoreForegroundWindow();
			//		Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();
			//	}
			//}

			var gotFocusEvent = new Event();
			// gotFocusEvent MUST be declared before gotFocusRegistration
			// Otherwise the event handler could execute after gotFocusEvent has been destroyed.
			var gotFocusRegistration = CreateSafeEventRegistration<UIElement, RoutedEventHandler>("GotFocus");

			await RunOnUIThread(() =>
					{
						gotFocusRegistration.Attach(element, (s, e) =>

								{
									LOG_OUTPUT("Element has received focus.");
									gotFocusEvent.Set();
								});

						if (element.FocusState != FocusState.Unfocused && FocusManager.GetFocusedElement(WindowHelper.WindowContent.XamlRoot).Equals(element))
						{
							// The element is already focused
							LOG_OUTPUT("Focus was already set on this element");
							gotFocusEvent.Set();
						}
						else
						{
							LOG_OUTPUT("Setting focus to the element...");
							element.Focus(focusState);
						}
					});

			await gotFocusEvent.WaitForNoThrow(4000);
			gotFocus = gotFocusEvent.HasFired();
		}

		if (!gotFocus)
		{
			//Private.Infrastructure.TestServices.Utilities.CaptureScreen("FocusTestHelper");
		}
		VERIFY_IS_TRUE(gotFocus);
	}
}
