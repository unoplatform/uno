// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusMovement.h, FocusMovement.cpp

#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	/// <summary>
	/// Stores state information about a focus movement operation.
	/// </summary>
	internal class FocusMovement
	{
		internal FocusMovement(
			XYFocusOptions xyFocusOptions,
			FocusNavigationDirection direction,
			DependencyObject? target)
		{
			XYFocusOptions = xyFocusOptions;
			Direction = direction;
			Target = target;
			ForceBringIntoView = true;
			CorrelationId = Guid.NewGuid();
			FocusState = FocusState.Programmatic;
			if (direction == FocusNavigationDirection.Down ||
				direction == FocusNavigationDirection.Left ||
				direction == FocusNavigationDirection.Right ||
				direction == FocusNavigationDirection.Up)
			{
				FocusState = FocusState.Keyboard;
			}
		}

		internal FocusMovement(
			DependencyObject? target,
			FocusNavigationDirection direction,
			FocusState focusState)
		{
			Target = target;
			Direction = direction;
			FocusState = focusState;
			CorrelationId = Guid.NewGuid();
		}

		internal FocusMovement(
			DependencyObject? target,
			FocusMovement copy)
		{
			Target = target;
			FocusState = copy.FocusState;
			Direction = copy.Direction;
			XYFocusOptions = copy.XYFocusOptions;
			ForceBringIntoView = copy.ForceBringIntoView;
			AnimateIfBringIntoView = copy.AnimateIfBringIntoView;
			IsProcessingTab = copy.IsProcessingTab;
			IsShiftPressed = copy.IsShiftPressed;
			CanCancel = copy.CanCancel;
			CanDepartFocus = copy.CanDepartFocus;
			CanNavigateFocus = copy.CanNavigateFocus;
			RaiseGettingLosingEvents = copy.RaiseGettingLosingEvents;
			ShouldCompleteAsyncOperation = copy.ShouldCompleteAsyncOperation;
			CorrelationId = copy.CorrelationId;
		}

		/// <summary>
		/// Gets the focus target.
		/// </summary>
		internal DependencyObject? Target { get; }

		/// <summary>
		/// Gets the focus state.
		/// </summary>
		internal FocusState FocusState { get; } = FocusState.Unfocused;

		/// <summary>
		/// Gets the focus navigation direction.
		/// </summary>
		internal FocusNavigationDirection Direction { get; } = FocusNavigationDirection.None;

		/// <summary>
		/// Gets the XY focus options.
		/// </summary>
		internal XYFocusOptions? XYFocusOptions { get; }

		/// <summary>
		/// Gets or sets the correlation ID.
		/// </summary>
		internal Guid CorrelationId { get; set; } = Guid.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether we force bring the target to view.
		/// </summary>
		internal bool ForceBringIntoView { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether bringing the target into view should be animated.
		/// </summary>
		internal bool AnimateIfBringIntoView { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tab key press is being processed.
		/// </summary>
		internal bool IsProcessingTab { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether shift key is pressed.
		/// </summary>
		internal bool IsShiftPressed { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the operation can be canceled.
		/// </summary>
		internal bool CanCancel { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the focus can depart.
		/// </summary>
		internal bool CanDepartFocus { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether focus can be navigated.
		/// </summary>
		internal bool CanNavigateFocus { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the getting/losing focus events should be raised.
		/// </summary>
		internal bool RaiseGettingLosingEvents { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the async operation should be completed.
		/// </summary>
		internal bool ShouldCompleteAsyncOperation { get; set; }
	}
}
