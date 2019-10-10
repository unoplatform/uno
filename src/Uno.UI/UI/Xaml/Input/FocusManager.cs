#if XAMARIN || __WASM__
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class FocusManager
	{
		private static object _focusedElement;

		// We keep a _fallbackFocused as backup if an element is unfocused and try move focus is called right after
		// It is currently the case in ContextualCommand.
		private static object _fallbackFocusedElement;

		/// <summary>
		/// Get the currently focused element, if any
		/// </summary>
		/// <returns>null means nothing is focused.</returns>
		public static object GetFocusedElement() => _focusedElement;

		/// <summary>
		/// Attempts to change focus from the element with focus to the next focusable element in the specified direction.
		/// </summary>
		/// <param name="focusNavigationDirection">The direction to traverse.</param>
		/// <returns>true if focus moved; otherwise, false.</returns>
		/// <remarks>The tab order is the order in which a user moves from one control to another by pressing the Tab key (forward) or Shift+Tab (backward).
		/// This method uses tab order sequence and behavior to traverse all focusable elements in the UI.
		/// If the focus is on the first element in the tab order and FocusNavigationDirection.Previous is specified, focus moves to the last element.
		/// If the focus is on the last element in the tab order and FocusNavigationDirection.Next is specified, focus moves to the first element.
		/// Other directions are not supported on all platforms.
		/// </remarks>
		public static bool TryMoveFocus(FocusNavigationDirection focusNavigationDirection)
		{
			return InnerTryMoveFocus(focusNavigationDirection);
		}

		/// <summary>
		/// Gets the next focusable UIElement depending on focusnavigationdirection, or null if no focusable elements are available.
		/// </summary>
		/// <param name="focusNavigationDirection"></param>
		/// <returns>Next focusable view depending on FocusNavigationDirection</returns>
		public static UIElement FindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
		{
			return InnerFindNextFocusableElement(focusNavigationDirection) as UIElement;
		}

		internal static void OnFocusChanged(Control control, FocusState focusState)
		{
			if (focusState == FocusState.Unfocused)
			{
				if (control == _focusedElement)
				{
					_focusedElement = null;

					if (LostFocus != null)
					{
						void OnLostFocus()
						{
							// we replay all "lost focus" events
							LostFocus?.Invoke(null, new FocusManagerLostFocusEventArgs {OldFocusedElement = control});
						}

						control.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, OnLostFocus); // event is rescheduled, as on UWP
					}
				}
			}
			else // Focused
			{
				if (_focusedElement != control)
				{
					(_focusedElement as Control)?.Unfocus();
					_focusedElement = control;

					if (GotFocus != null)
					{
						void OnGotFocus()
						{
							if (_focusedElement == control) // still focused
							{
								// we play the gotfocus event only on last/winning control
								GotFocus?.Invoke(null, new FocusManagerGotFocusEventArgs {NewFocusedElement = control});
							}
						}

						control.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, OnGotFocus); // event is rescheduled, as on UWP
					}
				}
				
				_fallbackFocusedElement = control;

#if __ANDROID__
				// Forcefully try to bring the control into view when keyboard is open to accommodate adjust nothing mode
				if (InputPane.GetForCurrentView().Visible)
				{
					control.StartBringIntoView();
				}
#endif
			}
		}

		internal static object GetFocusedElement(bool useFallback) => GetFocusedElement() ?? _fallbackFocusedElement;

		public static event EventHandler<FocusManagerGotFocusEventArgs> GotFocus;
		public static event EventHandler<FocusManagerLostFocusEventArgs> LostFocus;

	}
}
#endif
