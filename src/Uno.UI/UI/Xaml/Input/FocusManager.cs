#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;


namespace Microsoft.UI.Xaml.Input
{
	public sealed partial class FocusManager
	{
		private static readonly Lazy<Logger> _log = new Lazy<Logger>(() => typeof(FocusManager).Log());

		/// <summary>
		/// Occurs when an element within a container element (a focus scope) receives focus.
		/// This event is raised asynchronously, so focus might move before bubbling is complete.
		/// </summary>		
		public static event EventHandler<FocusManagerGotFocusEventArgs>? GotFocus;

		/// <summary>
		/// Occurs when an element within a container element (a focus scope) loses focus.
		/// This event is raised asynchronously, so focus might move again before bubbling is complete.
		/// </summary>
		public static event EventHandler<FocusManagerLostFocusEventArgs>? LostFocus;

		/// <summary>
		/// Occurs before an element actually receives focus.
		/// This event is raised synchronously to ensure focus isn't moved while the event is bubbling.
		/// </summary>
		public static event EventHandler<GettingFocusEventArgs>? GettingFocus;

		/// <summary>
		/// Occurs before focus moves from the current element with focus to the target element.
		/// This event is raised synchronously to ensure focus isn't moved while the event is bubbling.
		/// </summary>
		public static event EventHandler<LosingFocusEventArgs>? LosingFocus;

		/// <summary>
		/// Retrieves the last element that can receive focus based on the specified scope.
		/// </summary>
		/// <remarks>
		/// The <paramref name="searchScope"/> parameter should always be set to a non-null value,
		/// otherwise null is returned.
		/// </remarks>
		/// <param name="searchScope">The root object from which to search. If null, the search scope is the current window.</param>
		/// <returns>The first focusable object.</returns>
		public static DependencyObject? FindFirstFocusableElement(DependencyObject? searchScope) =>
			FindFirstFocusableElementImpl(searchScope);

		/// <summary>
		/// Retrieves the last element that can receive focus based on the specified scope.
		/// </summary>
		/// <remarks>
		/// The <paramref name="searchScope"/> parameter should always be set to a non-null value,
		/// otherwise null is returned.
		/// </remarks>
		/// <param name="searchScope">The root object from which to search. If null, the search scope is the current window.</param>
		/// <returns>The first focusable object.</returns>
		public static DependencyObject? FindLastFocusableElement(DependencyObject? searchScope) =>
			FindLastFocusableElementImpl(searchScope);

		/// <summary>
		/// Retrieves the element that should receive focus based on the specified navigation direction.
		/// </summary>
		/// <param name="focusNavigationDirection">The direction that focus moves from element
		/// to element within the app UI.</param>
		/// <returns>The next object to receive focus.</returns>
		/// <remarks>We recommend using this method instead of <see cref="FindNextFocusableElement(FocusNavigationDirection)" />.
		/// FindNextFocusableElement retrieves a UIElement, which returns null if the next focusable element
		/// is not a UIElement (such as a Hyperlink object).</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use FindNextElement overload with FindNextElementOptions and set its SearchRoot property to any element in the visual tree.", false)]
		public static DependencyObject? FindNextElement(FocusNavigationDirection focusNavigationDirection)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Undefined focus navigation direction was used.");
			}

			return FindNextElementImpl(focusNavigationDirection);
		}

		/// <summary>
		/// Retrieves the element that should receive focus based on the specified navigation direction
		/// (cannot be used with tab navigation, see remarks).
		/// </summary>
		/// <param name="focusNavigationDirection">The direction that focus moves from element
		/// to element within the app UI</param>
		/// <param name="focusNavigationOptions">The options to help identify the next element
		/// to receive focus with keyboard/controller/remote navigation.</param>
		/// <returns>The next object to receive focus.</returns>
		/// <remarks>
		/// We recommend using this method instead of <see cref="FindNextFocusableElement(FocusNavigationDirection)" />.
		/// FindNextFocusableElement retrieves a UIElement, which returns null if the next focusable element
		/// is not a UIElement (such as a Hyperlink object).
		/// </remarks>
		public static DependencyObject? FindNextElement(FocusNavigationDirection focusNavigationDirection, FindNextElementOptions focusNavigationOptions)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Invalid value of focus navigation direction was used.");
			}

			if (focusNavigationOptions is null)
			{
				throw new ArgumentNullException(nameof(focusNavigationOptions));
			}

			return FindNextElementWithOptionsImpl(focusNavigationDirection, focusNavigationOptions);
		}

		/// <summary>
		/// Retrieves the element that should receive focus based on the specified navigation direction.
		/// </summary>
		/// <param name="focusNavigationDirection">The direction that focus moves from element to element within the application UI.</param>
		/// <returns>Next focusable view depending on FocusNavigationDirection, null if focus cannot be set in the specified direction.</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use FindNextElement overload with FindNextElementOptions and set its SearchRoot property to any element in the visual tree.", false)]
		public static UIElement? FindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Undefined focus navigation direction was used.");
			}

			return FindNextFocusableElementImpl(focusNavigationDirection);
		}

		/// <summary>
		/// Retrieves the element that should receive focus based on the specified navigation direction and hint rectangle.
		/// </summary>
		/// <param name="focusNavigationDirection">The direction that focus moves from element to element within the app UI.</param>
		/// <param name="hintRect">A bounding rectangle used to influence which element is most likely to be considered the next to receive focus.</param>
		/// <returns>Next focusable view depending on FocusNavigationDirection, null if focus cannot be set in the specified direction.</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use FindNextElement overload with FindNextElementOptions and set its SearchRoot property to any element in the visual tree.", false)]
		public static UIElement? FindNextFocusableElement(FocusNavigationDirection focusNavigationDirection, Rect hintRect)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Undefined focus navigation direction was used.");
			}

			return FindNextFocusableElementWithHintImpl(focusNavigationDirection, hintRect);
		}

		/// <summary>
		/// Retrieves the element in the UI that has focus, if any.
		/// </summary>
		/// <returns>The object that has focus. Typically, this is a Control class.</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use GetFocusedElement overload with XamlRoot parameter.", false)]
		public static object? GetFocusedElement() => GetFocusedElementImpl();

		/// <summary>
		/// Retrieves the focused element within the XAML island container.
		/// </summary>
		/// <param name="xamlRoot">XAML island container where to search.</param>
		/// <returns></returns>
		public static object? GetFocusedElement(XamlRoot xamlRoot) => GetFocusedElementWithRootImpl(xamlRoot);

		/// <summary>
		/// Retrieves the element being focused within the XAML island container.
		/// </summary>
		/// <param name="xamlRoot">XAML island container where to search.</param>
		/// <returns></returns>
		internal static object? GetFocusingElement(XamlRoot xamlRoot) => GetFocusingElementWithRootImpl(xamlRoot);

		/// <summary>
		/// Asynchronously attempts to set focus on an element when the application is initialized.
		/// </summary>
		/// <param name="element">The object on which to set focus.</param>
		/// <param name="value">One of the values from the FocusState enumeration that specify how an element can obtain focus.</param>
		/// <returns>The <see cref="FocusMovementResult" /> that indicates whether focus was successfully set.</returns>
		/// <remarks>Completes synchronously when called on an element running in the app process.</remarks>
		public static IAsyncOperation<FocusMovementResult> TryFocusAsync(DependencyObject element, FocusState value)
		{
			if (element is null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			if (!Enum.IsDefined(value))
			{
				throw new ArgumentOutOfRangeException(
					nameof(value),
					"Undefined focus state was used.");
			}

			return TryFocusAsyncImpl(element, value);
		}

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
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use TryMoveFocus overload with FindNextElementOptions and set its SearchRoot property to any element in the visual tree.", false)]
		public static bool TryMoveFocus(FocusNavigationDirection focusNavigationDirection)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Undefined focus navigation direction was used.");
			}

			if (focusNavigationDirection == FocusNavigationDirection.None)
			{
				throw new ArgumentOutOfRangeException(
					"Focus navigation direction None is not supported in TryMoveFocus",
					nameof(focusNavigationDirection));
			}

			return TryMoveFocusImpl(focusNavigationDirection);
		}

		/// <summary>
		/// Attempts to change focus from the element with focus to the next focusable element in the specified direction,
		/// using the specified navigation options.
		/// </summary>
		/// <param name="focusNavigationDirection">The direction to traverse (in tab order).</param>
		/// <param name="focusNavigationOptions">The options to help identify the next element to receive focus with keyboard/controller/remote navigation.</param>
		/// <returns>true if focus moved; otherwise, false.</returns>
		/// <remarks>The tab order is the order in which a user moves from one control to another by pressing the Tab key (forward) or Shift+Tab (backward).
		/// This method uses tab order sequence and behavior to traverse all focusable elements in the UI.
		/// If the focus is on the first element in the tab order and FocusNavigationDirection.Previous is specified, focus moves to the last element.
		/// If the focus is on the last element in the tab order and FocusNavigationDirection.Next is specified, focus moves to the first element.
		/// Other directions are not supported on all platforms.
		/// </remarks>
		public static bool TryMoveFocus(FocusNavigationDirection focusNavigationDirection, FindNextElementOptions focusNavigationOptions)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Invalid value of focus navigation direction was used.");
			}

			if (focusNavigationDirection == FocusNavigationDirection.None)
			{
				throw new ArgumentOutOfRangeException(
					"Focus navigation direction None is not supported in TryMoveFocus",
					nameof(focusNavigationDirection));
			}

			if (focusNavigationOptions is null)
			{
				throw new ArgumentNullException(nameof(focusNavigationOptions));
			}

			return TryMoveFocusWithOptionsImpl(focusNavigationDirection, focusNavigationOptions);
		}

		/// <summary>
		/// Asynchronously attempts to change focus from the current element
		/// with focus to the next focusable element in the specified direction.
		/// </summary>
		/// <param name="focusNavigationDirection">The direction that focus moves from element to element within the app UI.</param>
		/// <returns>The <see cref="FocusMovementResult" /> that indicates whether focus was successfully set.</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use TryMoveFocusAsync overload with FindNextElementOptions and set its SearchRoot property to any element in the visual tree.", false)]
		public static IAsyncOperation<FocusMovementResult> TryMoveFocusAsync(FocusNavigationDirection focusNavigationDirection)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Undefined focus navigation direction was used.");
			}

			if (focusNavigationDirection == FocusNavigationDirection.None)
			{
				throw new ArgumentOutOfRangeException(
					"Focus navigation direction None is not supported in TryMoveFocusAsync",
					nameof(focusNavigationDirection));
			}

			return TryMoveFocusAsyncImpl(focusNavigationDirection);
		}

		/// <summary>
		/// Asynchronously attempts to change focus from the current element
		/// with focus to the next focusable element in the specified direction.
		/// </summary>
		/// <param name="focusNavigationDirection">The direction that focus moves from element to element within the app UI.</param>
		/// <param name="focusNavigationOptions">The navigation options used to identify the focus candidate.</param>
		/// <returns>The <see cref="FocusMovementResult" /> that indicates whether focus was successfully set.</returns>
		public static IAsyncOperation<FocusMovementResult> TryMoveFocusAsync(FocusNavigationDirection focusNavigationDirection, FindNextElementOptions focusNavigationOptions)
		{
			if (!Enum.IsDefined(focusNavigationDirection))
			{
				throw new ArgumentOutOfRangeException(
					nameof(focusNavigationDirection),
					"Invalid value of focus navigation direction was used.");
			}

			if (focusNavigationDirection == FocusNavigationDirection.None)
			{
				throw new ArgumentOutOfRangeException(
					"Focus navigation direction None is not supported in TryMoveFocusAsync",
					nameof(focusNavigationDirection));
			}

			if (focusNavigationOptions is null)
			{
				throw new ArgumentNullException(nameof(focusNavigationOptions));
			}

			return TryMoveFocusWithOptionsAsyncImpl(focusNavigationDirection, focusNavigationOptions);
		}
	}
}
