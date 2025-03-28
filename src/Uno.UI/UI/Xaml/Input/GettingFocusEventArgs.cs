// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// GettingFocusEventArgs.h, GettingFocusEventArgs.cpp

#nullable enable

using System;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Provides data for the FocusManager.GettingFocus and UIElement.GettingFocus events.
	/// </summary>
	public partial class GettingFocusEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs, IChangingFocusEventArgs
	{
		private bool _canCancelOrRedirectFocus;

		internal GettingFocusEventArgs(
			DependencyObject? oldFocusedElement,
			DependencyObject? newFocusedElement,
			FocusState focusState,
			FocusNavigationDirection direction,
			FocusInputDeviceKind inputDevice,
			bool canCancelFocus,
			Guid correlationId)
		{
			OldFocusedElement = oldFocusedElement;
			NewFocusedElement = newFocusedElement;
			FocusState = focusState;
			Direction = direction;
			InputDevice = inputDevice;
			_canCancelOrRedirectFocus = canCancelFocus;
			CorrelationId = correlationId;
		}

		/// <summary>
		/// Gets the direction that focus moved from element to element within the app UI.
		/// </summary>
		public FocusNavigationDirection Direction { get; }

		/// <summary>
		/// Gets the input mode through which an element obtained focus.
		/// </summary>
		public FocusState FocusState { get; }

		/// <summary>
		/// Gets the input device type from which input events are received.
		/// </summary>
		public FocusInputDeviceKind InputDevice { get; }

		/// <summary>
		/// Gets the last focused object.
		/// </summary>
		public DependencyObject? OldFocusedElement { get; }

		/// <summary>
		/// Gets the unique ID generated when a focus movement event is initiated.
		/// </summary>
		public Guid CorrelationId { get; }

		/// <summary>
		/// Gets the most recent focused object.
		/// </summary>
		public DependencyObject? NewFocusedElement { get; set; }

		DependencyObject? IChangingFocusEventArgs.NewFocusedElement
		{
			get => NewFocusedElement;
			set => NewFocusedElement = value;
		}

		/// <summary>
		/// Gets or sets a value that marks the routed event as handled.
		/// A true value for Handled prevents most handlers along the event
		/// route from handling the same event again.
		/// </summary>
		public bool Handled { get; set; }

		bool IHandleableRoutedEventArgs.Handled
		{
			get => Handled;
			set => Handled = value;
		}

		/// <summary>
		/// Gets or sets whether focus navigation should be canceled.
		/// </summary>
		public bool Cancel { get; set; }

		bool IChangingFocusEventArgs.Cancel
		{
			get => Cancel;
		}

		/// <summary>
		/// Attempts to cancel the ongoing focus action.
		/// </summary>
		/// <returns>True, if the focus action is canceled; otherwise, false.</returns>
		public bool TryCancel()
		{
			if (_canCancelOrRedirectFocus)
			{
				Cancel = true;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Attempts to redirect focus from the targeted element to the specified element.
		/// </summary>
		/// <param name="element">The object on which to set focus.</param>
		/// <returns>True, if the focus action is redirected; otherwise, false.</returns>
		public bool TrySetNewFocusedElement(DependencyObject? element)
		{
			if (_canCancelOrRedirectFocus)
			{
				NewFocusedElement = element;
				return true;
			}

			return false;
		}
	}
}
