// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Thumb_Partial.cpp, Thumb_Partial.h

using DirectUI;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml.Controls.Primitives;

public sealed partial class Thumb
{
	// Set to a value other than ManipulationModes_None at the beginning of a pen-driven drag within a ScrollBar.
	// That mode is then restored at the end of the drag operation, when the pointer is released or otherwise canceled.
	private ManipulationModes m_prePenDragManipulationMode = ManipulationModes.None;

	// Origin of the thumb's drag operation.
	private Point m_origin;

	// Last position of the thumb while during a drag operation.
	private Point m_previousPosition;

	// Transform to the original position of the thumb
	// Used so that we don't inadvertently change coordinate systems during a drag
	private GeneralTransform m_tpTransformToOriginal;

	/// <summary>
	/// Initializes a new instance of the Thumb class.
	/// </summary>
	public Thumb()
	{
		// TODO Uno specific:
		DefaultStyleKey = typeof(Thumb);

		IgnoreTouchInput = false;
		m_origin = Point.Zero;
		m_previousPosition = Point.Zero;
	}

	/// <summary>
	/// Cancels a drag operation for the Thumb.
	/// </summary>
	public void CancelDrag()
	{
		var isDragging = IsDragging;
		if (isDragging)
		{
			IsDragging = false;
			RaiseDragCompleted(true);
		}
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == IsDraggingProperty)
		{
			OnDraggingChanged();
		}
		else if (args.Property == VisibilityProperty)
		{
			OnVisibilityChanged();
		}
		else if (args.Property == ManipulationModeProperty)
		{
			if (m_prePenDragManipulationMode != ManipulationModes.None)
			{
				// This Thumb's ManipulationMode property is being changed after it was temporarily cached and altered to support a pen-driven drag.
				// Reset the cached value so this new ManipulationMode value does not get overridden at the end of the drag.
				m_prePenDragManipulationMode = ManipulationModes.None;
			}
		}
	}

	private void OnDraggingChanged() => UpdateVisualState();

	private void OnVisibilityChanged()
	{
		var visibility = Visibility;
		if (Visibility.Visible != visibility)
		{
			IsPointerOver = false;
		}

		UpdateVisualState();
	}

	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
	{
		var isEnabled = IsEnabled;
		if (!isEnabled)
		{
			IsPointerOver = false;

			bool isDragging = IsDragging;
			if (isDragging)
			{
				IsDragging = false;
				RaiseDragCompleted(isCanceled: false);
			}
		}

		UpdateVisualState();
	}

	protected override void OnGotFocus(RoutedEventArgs e)
	{
		// MUX TODO: We should expose FrameworkElement.HasFocus method
		bool hasFocus = true;
		FocusChanged(hasFocus);
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		// MUX TODO: We should expose FrameworkElement.HasFocus method
		var hasFocus = false;
		FocusChanged(hasFocus);
	}

	private void FocusChanged(bool hasFocus)
	{
		UpdateVisualState();
	}

	internal bool IgnoreTouchInput { get; set; }

	private bool ShouldIgnoreInput(PointerRoutedEventArgs args)
	{
		if (IgnoreTouchInput)
		{
			var pointerPoint = args.GetCurrentPoint(null);
			var pointerDeviceType = pointerPoint.PointerDevice.PointerDeviceType;
			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				return true;
			}
		}

		return false;
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		var ignoreInput = ShouldIgnoreInput(args);
		if (ignoreInput)
		{
			return;
		}

		var isEnabled = IsEnabled;

		if (isEnabled)
		{
			IsPointerOver = true;
			UpdateVisualState();
		}
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		var ignoreInput = ShouldIgnoreInput(args);
		if (ignoreInput)
		{
			return;
		}

		var isEnabled = IsEnabled;

		if (isEnabled)
		{
			IsPointerOver = false;
			UpdateVisualState();
		}
	}

	protected override void OnPointerMoved(PointerRoutedEventArgs args)
	{
		var ignoreInput = ShouldIgnoreInput(args);
		if (ignoreInput)
		{
			return;
		}

		var isDragging = IsDragging;

		if (isDragging)
		{
			var pointerPoint = args.GetCurrentPoint(null);
			var position = pointerPoint.RawPosition;
			var transformedPosition = m_tpTransformToOriginal.TransformPoint(position);

			if (transformedPosition.X != m_previousPosition.X || transformedPosition.Y != m_previousPosition.Y)
			{
				RaiseDragDelta(transformedPosition.X - m_previousPosition.X, transformedPosition.Y - m_previousPosition.Y);
				m_previousPosition = transformedPosition;
			}
		}
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		AutomaticToolTipInputMode mode = AutomaticToolTipInputMode.None;

		var handled = args.Handled;
		if (handled)
		{
			return;
		}

		var ignoreInput = ShouldIgnoreInput(args);
		if (ignoreInput)
		{
			return;
		}

		var isDragging = IsDragging;
		var isEnabled = IsEnabled;
		var parent = Parent;

		var pointerPoint = args.GetCurrentPoint(null);
		var pointerProperties = pointerPoint.Properties;
		var isLeftButtonPressed = pointerProperties.IsLeftButtonPressed;
		if (isLeftButtonPressed)
		{
			if (!isDragging && isEnabled && parent != null)
			{
				GeneralTransform invertedTransform;

				var pointerDeviceType = pointerPoint.PointerDevice.PointerDeviceType;

				var templatedParentAsSlider = TemplatedParent as Slider;
				if (templatedParentAsSlider != null)
				{
					// Since we are marking the event as handled, Slider never sees it because of this bug:
					//      33598 - RoutedEvent Handled flag shouldn't be modified by templated parts
					// Therefore, as long as Thumb continues to handle this event, we need to report to Slider
					// that it must open its disambiguation UI.

					if (pointerDeviceType == PointerDeviceType.Touch)
					{
						mode = AutomaticToolTipInputMode.Touch;
					}
					else
					{
						mode = AutomaticToolTipInputMode.Mouse;
					}

					templatedParentAsSlider.UpdateThumbToolTipVisibility(true, mode);
					templatedParentAsSlider.SetIsPressed(true);
				}

				var templatedParentAsScrollViewer = TemplatedParent as ScrollBar;
				if (templatedParentAsScrollViewer != null)
				{
					var isIgnoringUserInput = templatedParentAsScrollViewer.IsIgnoringUserInput;
					if (isIgnoringUserInput)
					{
						// Do not start a thumb drag operation when the owning ScrollBar was told to ignore user input.
						return;
					}
					CacheAndReplacePrePenDragManipulationMode(pointerDeviceType);
				}

				args.Handled = true;

				var pointer = args.Pointer;
				var pointerCaptured = CapturePointer(pointer, /* UNO only */ options: PointerCaptureOptions.PreventOSSteal);
				IsDragging = true;

				// If logical parent is a popup, TransformToVisual can fail because a popup's child
				// can be in the visual tree without the popup being in the tree, if the popup is
				// created in code and has IsOpen set to true. Therefore, if we're in a popup, we
				// use coordinates relative to the thumb instead of relative to Thumb.Parent.

				// MUX TODO: Uncomment following code when(if) we bring Popup support
				//if (false)//is<xaml_primitives.IPopup>(parent.Get()))
				//{
				//	var transform = TransformToVisual(null);
				//	invertedTransform = transform.Inverse;
				//}
				//else
				//{
				UIElement parentAsUIE = (UIElement)parent;

				var spTransform = parentAsUIE.TransformToVisual(null);
				invertedTransform = spTransform.Inverse;
				//}

				m_tpTransformToOriginal = invertedTransform;

				var position = pointerPoint.RawPosition;
				var transformedPosition = m_tpTransformToOriginal.TransformPoint(position);

				m_origin = m_previousPosition = transformedPosition;

				try
				{
					RaiseDragStarted();
				}
				catch
				{
					CancelDrag();
				}
			}
		}
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		RestorePrePenDragManipulationMode(args);

		var ignoreInput = ShouldIgnoreInput(args);
		if (ignoreInput)
		{
			return;
		}

		var pointer = args.Pointer;
		var isDragging = IsDragging;
		var isEnabled = IsEnabled;
		if (isDragging && isEnabled)
		{
			IsDragging = false;
			ReleasePointerCapture(pointer);
			RaiseDragCompleted(false);
		}
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		base.OnPointerCaptureLost(args);

		RestorePrePenDragManipulationMode(args);

		var ignoreInput = ShouldIgnoreInput(args);
		if (ignoreInput)
		{
			return;
		}

		var pointer = args.Pointer;
		var isDragging = IsDragging;
		var isEnabled = IsEnabled;
		if (isDragging && isEnabled)
		{
			IsDragging = false;
			ReleasePointerCapture(pointer);
			RaiseDragCompleted(false);
		}
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		base.OnPointerCanceled(args);
		RestorePrePenDragManipulationMode(args);
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ThumbAutomationPeer(this);

	// Caches and replaces the Thumb's ManipulationMode used prior to a pen-driven drag, if needed.
	private void CacheAndReplacePrePenDragManipulationMode(PointerDeviceType nPointerDeviceType)
	{
		if (nPointerDeviceType == PointerDeviceType.Pen)
		{
			ManipulationModes prePenDragManipulationMode = ManipulationMode;
			if (prePenDragManipulationMode.HasFlag(ManipulationModes.System))
			{
				ManipulationMode = prePenDragManipulationMode & ~ManipulationModes.System;
				m_prePenDragManipulationMode = prePenDragManipulationMode;
			}
		}

	}

	// Restores the Thumb's ManipulationMode used prior to a pen-driven drag, if needed.
	private void RestorePrePenDragManipulationMode(PointerRoutedEventArgs args)
	{
		if (m_prePenDragManipulationMode != ManipulationModes.None)
		{
			var pointerPoint = args.GetCurrentPoint(null);
			var pointerDeviceType = pointerPoint.PointerDevice.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Pen)
			{
				ManipulationModes prePenDragManipulationMode = m_prePenDragManipulationMode;

				m_prePenDragManipulationMode = ManipulationModes.None;
				ManipulationMode = prePenDragManipulationMode;
			}
		}
	}

	private void RaiseDragStarted()
	{
		// Create the args
		var args = new DragStartedEventArgs(
			horizontalOffset: m_origin.X,
			verticalOffset: m_origin.Y);

		// Raise the event
		DragStarted?.Invoke(this, args);
	}

	private void RaiseDragDelta(double dDeltaX, double dDeltaY)
	{
		var args = new DragDeltaEventArgs(
			horizontalChange: dDeltaX,
			verticalChange: dDeltaY);

		// Raise the event
		DragDelta?.Invoke(this, args);
	}

	private void RaiseDragCompleted(bool isCanceled)
	{
		// Create the args
		var args = new DragCompletedEventArgs(
			horizontalChange: m_previousPosition.X - m_origin.X,
			verticalChange: m_previousPosition.Y - m_origin.Y,
			canceled: isCanceled);

		// Raise the event
		DragCompleted?.Invoke(this, args);
	}

	/// <summary>
	/// Change to the correct visual state for the thumb.
	/// </summary>
	/// <param name="useTransitions">Indicates whether transitions between visual states should be used</param>
	internal override void UpdateVisualState(
		// true to use transitions when updating the visual state, false
		// to snap directly to the new visual state.
		bool useTransitions = true)
	{
		var isEnabled = IsEnabled;
		var isDragging = IsDragging;
		var isPointerOver = IsPointerOver;
		var focusState = FocusState;

		if (!isEnabled)
		{
			GoToState(useTransitions, "Disabled");
		}
		else if (isDragging)
		{
			GoToState(useTransitions, "Pressed");
		}
		else if (isPointerOver)
		{
			GoToState(useTransitions, "PointerOver");
		}
		else
		{
			GoToState(useTransitions, "Normal");
		}

		if (FocusState.Unfocused != focusState && isEnabled)
		{
			GoToState(useTransitions, "Focused");
		}
		else
		{
			GoToState(useTransitions, "Unfocused");
		}
	}

	/// <summary>
	/// Apply a template to the Thumb.
	/// </summary>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// Sync the logical and visual states of the control
		UpdateVisualState(false);
	}
}
