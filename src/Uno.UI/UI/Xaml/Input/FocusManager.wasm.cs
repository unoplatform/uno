using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Uno;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		/// <summary>
		/// True during a call to native focusView().
		/// </summary>
		private static bool _isCallingFocusNative;

		private static bool _skipNativeFocus;

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

			foreach (var candidate in element.GetAllParents())
			{
				// Try to find the first focusable parent and set it as focused, otherwise just keep it for reference (GetFocusedElement())

				// Special handling for RootVisual - which is not focusable on managed side
				// but is focusable on native side. The purpose of this trick is to allow
				// us to recognize, that the page was focused by tabbing from the address bar
				// and focusing the first focusable element on the page instead.
				if (candidate is RootVisual rootVisual)
				{
					var firstFocusable = FocusManager.FindFirstFocusableElement(rootVisual);
					if (firstFocusable is FrameworkElement frameworkElement)
					{
						if (_log.Value.IsEnabled(LogLevel.Debug))
						{
							_log.Value.LogDebug(
								$"Root visual focused - caused by browser keyboard navigation to the page, " +
								$"moving focus to actual first focusable element - {frameworkElement?.ToString() ?? "[null]"}.");
						}
						frameworkElement.Focus(FocusState.Keyboard);
					}
					break;
				}
				else if (candidate is TextBlock textBlock && textBlock.IsFocusable)
				{
					// Focusable TextBlock parent, we can move focus to it.
					var focusManager = VisualTree.GetFocusManagerForElement(textBlock);

					// We cannot call native focus here, as it would fail and would then blur focus immediately.
					_skipNativeFocus = true;
					focusManager?.UpdateFocus(new FocusMovement(textBlock, FocusNavigationDirection.None, FocusState.Pointer));
					_skipNativeFocus = false;
					break;
				}
				else if (candidate is TextBoxView textBoxView)
				{
					if (textBoxView.FindFirstParent<TextBox>() is { IsFocusable: true } tb)
					{
						var focusManager = VisualTree.GetFocusManagerForElement(tb);
						focusManager?.UpdateFocus(new FocusMovement(tb, FocusNavigationDirection.None, FocusState.Pointer));
						break;
					}
				}
				else if (
					candidate is FrameworkElement fe &&
					(!fe.AllowFocusOnInteraction || !fe.IsTabStop))
				{
					// Stop propagating, this element does not want to receive focus.
					break;
				}
				else if (candidate is Control control && control.IsFocusable)
				{
					ProcessControlFocused(control);
					break;
				}
			}
		}

		// The way focus management works natively (HTML live standard) is fundamentally different from
		// WinUI focus management. In WinUI, elements gets focus when an explicit call is made (e.g.
		// when a button is pressed, it focuses itself, etc.). However, in a browser, clicking on an
		// html element will simply focus that element (assuming it's focusable).
		internal static bool FocusNative(UIElement element)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(FocusNative)}(element: {element})");
			}

			if (_skipNativeFocus)
			{
				_log.Value.LogDebug($"{nameof(FocusNative)} skipping native focus");
				return false;
			}

			if (element == null)
			{
				return false;
			}

			var focusManager = VisualTree.GetFocusManagerForElement(element);

			if (focusManager?.InitialFocus == true)
			{
				// Do not focus natively on initial focus so the soft keyboard is not opened
				return false;
			}

			if (element is TextBox textBox)
			{
				return textBox.FocusTextView();
			}

			_isCallingFocusNative = true;
			WindowManagerInterop.FocusView(element.HtmlId);
			_isCallingFocusNative = false;

			return true;
		}

		[JSExport]
		internal static void ReceiveFocusNative(int handle)
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
				// TODO: Adjust for multiwindow #8978
				var focusManager = VisualTree.GetFocusManagerForElement(Window.CurrentSafe?.RootElement);

				// The focus manager may be null if JS raises focusin/blur before the app is initialized.
				focusManager?.ClearFocus();
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
