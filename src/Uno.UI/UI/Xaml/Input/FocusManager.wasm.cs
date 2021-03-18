using System;
using Uno.UI;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Foundation;
using Microsoft.Extensions.Logging;
using Uno;

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

			UpdateFocus(control, FocusNavigationDirection.None, FocusState.Pointer);
		}

		internal static void ProcessElementFocused(UIElement element)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(ProcessElementFocused)}() focusedElement={GetFocusedElement()}, element={element}");
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
				UpdateFocus(null, FocusNavigationDirection.None, FocusState.Pointer);
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

		private static bool InnerTryMoveFocus(FocusNavigationDirection focusNavigationDirection)
		{
			return false;
		}

		private static UIElement InnerFindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
		{
			return null;
		}

		private static DependencyObject InnerFindFirstFocusableElement(DependencyObject searchScope)
		{
			return null;
		}

		private static DependencyObject InnerFindLastFocusableElement(DependencyObject searchScope)
		{
			return null;
		}
	}
}
