using System;
using Uno.UI;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Foundation;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		internal static void ProcessControlFocused(Control control)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(ProcessControlFocused)}() _focusedElement={_focusedElement}, control={control}");
			}

			UpdateFocus(control, FocusNavigationDirection.None, FocusState.Pointer);
		}

		internal static void ProcessElementFocused(UIElement element)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(ProcessElementFocused)}() _focusedElement={_focusedElement}, element={element}");
			}

			// Try to find the first focusable parent and set it as focused, otherwise just keep it for reference (GetFocusedElement())
			var ownerControl = element.GetParents().OfType<Control>().Where(control => control.IsFocusable).FirstOrDefault();
			UpdateFocus(ownerControl, FocusNavigationDirection.None, FocusState.Pointer);
		}

		internal static bool FocusNative(UIElement element)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(FocusNative)}(element: {element})");
			}

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
