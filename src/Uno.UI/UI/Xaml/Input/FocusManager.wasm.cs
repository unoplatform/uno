using System;
using Uno.UI;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Foundation;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		internal static void ProcessControlFocused(Control control)
		{
			if (
				// Don't change the focus if the control already has the focus
				!ReferenceEquals(control, _focusedElement))
			{
				// Make sure that the managed UI knowns that the element was unfocused, no matter if the new focused element can be focused or not
				(_focusedElement as Control)?.SetFocused(false);

				// Then set the new control as focused
				control.SetFocused(true);
			}
		}

		internal static void ProcessElementFocused(UIElement element)
		{
			if (
				// Don't change the focus if the element already has the focus
				!ReferenceEquals(element, _focusedElement))
			{
				// Try to find the first focusable parent and set it as focused, otherwise just keep it for reference (GetFocusedElement())
				var ownerControl = element.GetParents().OfType<Control>().Where(control => control.IsFocusable).FirstOrDefault();
				if (ownerControl == null)
				{
					// Make sure that the managed UI knows that the element was unfocused, no matter if the new focused element can be focused or not
					(_focusedElement as Control)?.SetFocused(false);

					_focusedElement = element;
				}
				else if (!ReferenceEquals(ownerControl, _focusedElement))
				{
					// Make sure that the managed UI knows that the element was unfocused, no matter if the new focused element can be focused or not
					(_focusedElement as Control)?.SetFocused(false);

					if (ownerControl.SetFocused(true))
					{
						_fallbackFocusedElement = element;
					}
				}
			}
		}

		internal static bool Focus(UIElement element)
		{
			if (element == null)
			{
				return false;
			}

			var command = $"Uno.UI.WindowManager.current.focusView({element.HtmlId});";
			WebAssemblyRuntime.InvokeJS(command);

			return true;
		}

		private static bool InnerTryMoveFocus(FocusNavigationDirection focusNavigationDirection)
		{
			return false;
		}

		private static UIElement InnerFindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
		{
			return null;
		}
	}
}
