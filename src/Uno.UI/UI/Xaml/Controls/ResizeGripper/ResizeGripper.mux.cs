// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripper.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Private.Controls;

partial class ResizeGripper
{
	// Sub-pixel jitter should not reach the host. DIPs.
	private const double c_dragDeadband = 0.5;

	// Shift takes a coarser step. A multiplier rather than a second absolute size, so it holds
	// whatever KeyboardIncrement a host chooses.
	private const double c_largeIncrementMultiplier = 4.0;

	private static bool IsShiftKeyDown()
	{
		return (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) &
			CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
	}

	public ResizeGripper()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_ResizeGripper);

		this.SetTabularDefaultStyleKey();
		// ProtectedCursor is deliberately NOT set here - see UpdateResizeCursor.
		// Set here rather than in the default style: without it no manipulation is ever raised, so a
		// host whose style failed to load would have no pointer resize at all.
		UpdateManipulationMode();

		IsEnabledChanged += OnIsEnabledChanged;
		Unloaded += OnUnloaded;
	}

	// Detached mid-gesture - the host rebuilt the subtree we live in - so no manipulation event will
	// arrive to complete the drag and IsDragging would stay true for the rest of this instance's life.
	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		m_isPointerOver = false;
		EndDrag(true /* canceled */);
	}

	private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
	{
		UpdateVisualState();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		m_templateApplied = true;
		UpdateOrientationVisualState();
		UpdateVisualState();
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);
		m_isPointerOver = true;
		UpdateVisualState();
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		base.OnPointerExited(args);
		m_isPointerOver = false;
		UpdateVisualState();
	}

	// Pressed stays on for the whole drag even if the pointer leaves the gripper, because the host
	// captures the pointer and the column keeps resizing.
	private void UpdateVisualState()
	{
		var state =
			!IsEnabled ? "Disabled" :
			IsDragging ? "Pressed" :
			m_isPointerOver ? "PointerOver" : "Normal";

		VisualStateManager.GoToState(this, state, true /* useTransitions */);
	}

	// The axis the drag is measured along. One definition: the pointer and keyboard paths deriving it
	// separately, with opposite-polarity tests, is where an axis bug hides.
	private bool IsHorizontalDrag() => DragOrientation != Orientation.Vertical;

	private void UpdateOrientationVisualState()
	{
		VisualStateManager.GoToState(this,
			IsHorizontalDrag() ? "Horizontal" : "Vertical",
			true /* useTransitions */);
	}

	// PARKED - deliberately not called. The framework drops ProtectedCursor on the next pointer move,
	// so setting it made the resize shape appear as the pointer arrived and revert immediately, which
	// is a flicker rather than an affordance. Measured: dwell on a divider reads an arrow 40 samples
	// out of 40; re-asserting per pointer move changed nothing. Tracked as ADO 63771830 / related to
	// microsoft-ui-xaml#7062. The shape logic below is correct and stays put: restore the calls in
	// OnPointerEntered and in the DragOrientation branch of OnPropertyChanged once the framework holds
	// the cursor. Until then the hover separator carries the affordance.
	//
	// Owned by the primitive so every host gets the right shape, keyed off the direction of travel.
	// Two guards, both of which cause a visibly wrong or flickering cursor when missed:
	//  - nothing before OnApplyTemplate. Assigning ProtectedCursor that early does not stick
	//    (microsoft-ui-xaml#7062), which is why the shape is resolved on pointer entry instead.
	//  - never reassign the shape already in effect. Handing the input system a brand new
	//    InputSystemCursor makes the pointer visibly reset even when the shape is identical.
#pragma warning disable IDE0051 // Unused upstream as well, kept for 1:1 parity
	private void UpdateResizeCursor()
	{
		if (!m_templateApplied)
		{
			return;
		}

		var shape = IsHorizontalDrag()
			? InputSystemCursorShape.SizeWestEast
			: InputSystemCursorShape.SizeNorthSouth;

		if (ProtectedCursor is InputSystemCursor current &&
			current.CursorShape == shape)
		{
			return;
		}

		ProtectedCursor = InputSystemCursor.Create(shape);
	}
#pragma warning restore IDE0051

	// One axis only: the cross-axis translation is noise, and leaving it out lets an ancestor
	// ScrollViewer keep panning on the axis we do not resize.
	private void UpdateManipulationMode()
	{
		ManipulationMode = IsHorizontalDrag()
			? ManipulationModes.TranslateX
			: ManipulationModes.TranslateY;
	}

	protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs args)
	{
		base.OnManipulationStarting(args);

		// Default container is this element, so the two frames agree by definition.
		m_containerIsRightToLeft = FlowDirection == FlowDirection.RightToLeft;

		// A host-designated frame wins: it knows which of its ancestors stays put while the drag
		// resizes something, and it is not subject to any scale applied above it.
		var container = ManipulationContainer;
		if (container is null)
		{
			// The gripper travels with the edge it drags, so the default container - itself - measures
			// the pointer against a frame the drag is moving, and the gesture stalls. The XamlRoot
			// content does not move while a host resizes.
			if (XamlRoot is { } xamlRoot)
			{
				container = xamlRoot.Content;
			}
		}

		if (container is not null)
		{
			args.Container = container;

			// Cumulative().Translation is expressed in the CONTAINER's space, and RTL mirrors that
			// space. Record the container's direction so the delta is mirrored against the frame it
			// was actually measured in, not against this element's own direction.
			if (container is FrameworkElement containerElement)
			{
				m_containerIsRightToLeft = containerElement.FlowDirection == FlowDirection.RightToLeft;
			}
		}
	}

	protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs args)
	{
		base.OnManipulationStarted(args);

		BeginDrag();
		args.Handled = true;
	}

	protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs args)
	{
		base.OnManipulationDelta(args);

		bool isHorizontal = IsHorizontalDrag();
		var translation = args.Cumulative.Translation;
		double totalDelta = isHorizontal ? translation.X : translation.Y;

		// Report a logical delta, so positive always grows in reading order and both input paths agree.
		// Mirror only when this element and the measurement frame disagree; mirroring on the element's
		// own direction inverts the drag in a fully-RTL app, where the container is mirrored too.
		if (isHorizontal && (FlowDirection == FlowDirection.RightToLeft) != m_containerIsRightToLeft)
		{
			totalDelta = -totalDelta;
		}

		TryDrag(totalDelta);
		args.Handled = true;
	}

	protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs args)
	{
		base.OnManipulationCompleted(args);

		EndDrag(false /* canceled */);
		args.Handled = true;
	}

	// Manipulation never marks the press handled, so without this the press bubbles to whatever the
	// host put above us - for a header cell that means taking focus mid-gesture, and the resulting
	// bring-into-view scroll moves the gripper out from under a stationary pointer. Marking it handled
	// does NOT suppress the manipulation: that is driven by ManipulationMode on this element.
	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		base.OnPointerPressed(args);

		if (IsEnabled)
		{
			args.Handled = true;
		}
	}

	// A contact the system takes away (palm rejection, the contact leaving the digitizer) is not a
	// release: the user never committed, and no ManipulationCompleted follows a canceled contact.
	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		base.OnPointerCanceled(args);

		m_isPointerOver = false;
		EndDrag(true /* canceled */);
		UpdateVisualState();
	}

	// Raise the same events a pointer drag would, so a keyboard step is indistinguishable from the
	// gesture. TryDrag takes the offset from where the drag began and returns whether it moved far
	// enough to raise DragDelta. Pass canceled to EndDrag when the gesture was torn down.
	public void BeginDrag()
	{
		// A disabled control takes no pointer input from the framework, but BeginDrag is callable
		// directly (that is how the keyboard path drives it), so gate here too.
		if (!IsEnabled || IsDragging)
		{
			return;
		}
		m_lastRaisedDelta = 0.0;
		SetValue(IsDraggingProperty, BoolBoxes.True);
		UpdateVisualState();
		DragStarted?.Invoke(this, null!);
	}

	public bool TryDrag(double totalDelta)
	{
		if (!IsDragging || !double.IsFinite(totalDelta))
		{
			return false;
		}

		if (Math.Abs(totalDelta - m_lastRaisedDelta) < c_dragDeadband)
		{
			return false;
		}

		double delta = totalDelta - m_lastRaisedDelta;
		m_lastRaisedDelta = totalDelta;

		var eventArgs = new ResizeGripperDragDeltaEventArgs(delta, totalDelta);
		DragDelta?.Invoke(this, eventArgs);
		return true;
	}

	public void EndDrag(bool canceled)
	{
		if (!IsDragging)
		{
			return;
		}

		double totalDelta = m_lastRaisedDelta;

		// Clear the flag before raising: a throwing handler would otherwise leave IsDragging stuck
		// true, wedging every later BeginDrag.
		SetValue(IsDraggingProperty, BoolBoxes.False);
		UpdateVisualState();

		var eventArgs = new ResizeGripperDragCompletedEventArgs(totalDelta, canceled);
		DragCompleted?.Invoke(this, eventArgs);
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == DragOrientationProperty)
		{
			UpdateOrientationVisualState();
			UpdateManipulationMode();
		}
	}

	// A host-settable step needs sanitising before it reaches TryDrag: a negative one would resize
	// backwards, and a non-finite or sub-deadband one would be swallowed by TryDrag - leaving the
	// keyboard silently dead while still raising a start/complete pair, which a host announces to a
	// screen reader as an unchanged width on every arrow press.
	private double EffectiveKeyboardIncrement()
	{
		double raw = Math.Abs(KeyboardIncrement);
		if (!double.IsFinite(raw) || raw == 0.0)
		{
			return c_defaultKeyboardIncrement;
		}
		return Math.Max(raw, c_dragDeadband);
	}

	// One arrow key as a complete one-step drag. The gripper acts on its own keys when a host makes
	// it focusable; a host that keeps focus on its own element calls this instead, so neither path
	// re-derives the direction, the RTL mirror or the step. False when the key is not on this axis.
	//
	// One implementation of "an arrow key is a one-step drag", used by OnKeyDown when the gripper has
	// focus and callable by a host that keeps focus on its own element. Returns false when the key is
	// not one this gripper acts on.
	public bool TryKeyboardStep(VirtualKey key)
	{
		bool isHorizontal = IsHorizontalDrag();

		double direction = 0.0;
		switch (key)
		{
			case VirtualKey.Left: direction = isHorizontal ? -1.0 : 0.0; break;
			case VirtualKey.Right: direction = isHorizontal ? 1.0 : 0.0; break;
			case VirtualKey.Up: direction = isHorizontal ? 0.0 : -1.0; break;
			case VirtualKey.Down: direction = isHorizontal ? 0.0 : 1.0; break;
			default: break;
		}

		if (direction == 0.0)
		{
			return false;
		}

		// A pointer drag is already in flight: a keyboard step would retarget its anchor and end it.
		// Reported as handled so the key cannot also move focus mid-gesture.
		if (IsDragging)
		{
			return true;
		}

		// Mirror the axis under RTL so Left always shrinks visually. FlowDirection does not mirror y.
		if (isHorizontal && FlowDirection == FlowDirection.RightToLeft)
		{
			direction *= -1.0;
		}

		double step = IsShiftKeyDown()
			? EffectiveKeyboardIncrement() * c_largeIncrementMultiplier
			: EffectiveKeyboardIncrement();

		// No ManipulationCompleted follows a keyboard step, so a throw would strand IsDragging.
		try
		{
			BeginDrag();
			TryDrag(direction * step);
		}
		catch
		{
			EndDrag(true /* canceled */);
			throw;
		}

		EndDrag(false /* canceled */);
		return true;
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ResizeGripperAutomationPeer(this);

	// A host that opts into IsTabStop gets working arrow keys without wiring anything: the increment
	// lives here, so acting on the key does too.
	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		if (!args.Handled && TryKeyboardStep(args.Key))
		{
			args.Handled = true;
		}
	}
}
