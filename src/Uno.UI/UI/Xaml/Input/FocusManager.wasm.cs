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
		private static readonly RoutedEventHandler OnControlFocused = (control, args) =>
		{
			// Make sure that the managed UI knowns that the element was unfocused, no matter if the new focused element can be focused or not
			(_focusedElement as Control)?.SetFocused(false);

			// Then set the new control as focused
			((Control) control).SetFocused(true);
		};
		private static readonly RoutedEventHandler OnElementFocused = (element, args) =>
		{
			// Make sure that the managed UI knowns that the element was unfocused, no matter if the new focused element can be focused or not
			(_focusedElement as Control)?.SetFocused(false);

			// Try to find the first focusable parent and set it as focused, otherwise just keep it for reference (GetFocusedElement())
			var ownerControl = element.GetParents().OfType<Control>().Where(control => control.IsFocusable).FirstOrDefault();
			if (ownerControl == null)
			{
				_focusedElement = element;
			}
			else
			{
				ownerControl.SetFocused(true);
				_fallbackFocusedElement = element;
			}
		};

		internal static void Track(UIElement element)
		{
			var handler = element is Control
				? OnControlFocused
				: OnElementFocused;

			element.GotFocus += handler;
		}

		internal static bool Focus(UIElement element)
		{
			if (element == null)
			{
				return false;
			}

			var command = $"Uno.UI.WindowManager.current.focusView(\"{element.HtmlId}\");";
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
