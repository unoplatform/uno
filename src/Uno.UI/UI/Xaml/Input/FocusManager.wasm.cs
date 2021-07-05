using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Foundation;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		/// <summary>
		/// True during a call to native focusView().
		/// </summary>
		private static bool _isCallingFocusNative;

		internal static void ProcessControlFocused(Control control)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(ProcessControlFocused)}() focusedElement={GetFocusedElement()}, control={control}");
			}

			if (FocusProperties.IsFocusable(control))
			{
				var focusManager = VisualTree.GetFocusManagerForElement(control);
				focusManager?.UpdateFocus(new FocusMovement(control, FocusNavigationDirection.None, FocusState.Pointer));
			}
		}

		internal static void ProcessElementFocused(UIElement element)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(ProcessElementFocused)}() focusedElement={GetFocusedElement()}, element={element}, searching for focusable parent control");
			}

			// Try to find the first focusable parent and set it as focused, otherwise just keep it for reference (GetFocusedElement())
			var ownerControl = element.GetParents().OfType<Control>().Where(control => control.IsFocusable).FirstOrDefault();
			ProcessControlFocused(ownerControl);
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

			if (element is TextBox textBox)
			{
				return textBox.FocusTextView();
			}

			_isCallingFocusNative = true;
			var command = $"Uno.UI.WindowManager.current.focusView({element.HtmlId});";
			WebAssemblyRuntime.InvokeJS(command);
			_isCallingFocusNative = false;

			return true;
		}

		[Preserve]
		public static void ReceiveFocusNative(int handle)
		{
			if (_isCallingFocusNative)
			{
				// We triggered this callback by calling focusView() ourselves, ignore it so we don't overwrite the FocusState
				return;
			}
			var focused = GetFocusElementFromHandle(handle);
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(ReceiveFocusNative)}({focused?.ToString() ?? "[null]"})");
			}

			if (focused is Control control)
			{
				ProcessControlFocused(control);
			}
			else if (focused != null)
			{
				ProcessElementFocused(focused);
			}
			else
			{
				// This might occur if a non-Uno element receives focus
				var focusManager = VisualTree.GetFocusManagerForElement(Window.Current.RootElement);
				focusManager.ClearFocus();
			}
		}

		private static UIElement GetFocusElementFromHandle(int handle)
		{
			if (handle == -1)
			{
				// 
				return null;
			}
			return UIElement.GetElementFromHandle(handle);
		}
	}
}
