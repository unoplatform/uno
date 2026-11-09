// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripper.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Private.Controls;

// A templated Control providing a resize affordance on either axis. Its default template supplies
// the hit-testable surface; the consumer positions it, handles DragStarted / DragDelta /
// DragCompleted, and decides what the reported drag distance means. Mirrors Thumb.
//
// The gesture itself is owned here: pointer input arrives as manipulation events, so touch and pen
// contacts and multi-pointer arbitration come from the framework's gesture recognizer rather than
// from each host. The gripper captures the mouse only, so the resize cursor holds off the gripper;
// touch and pen stay uncaptured so an ancestor ScrollViewer can still pan. BeginDrag / TryDrag /
// EndDrag stay callable so a host can drive the same gesture from its own keyboard handling.
partial class ResizeGripper
{
	// The registered default for KeyboardIncrement, and the fallback when a host supplies an unusable
	// one - the two must agree, so the IDL's MUX_DEFAULT_VALUE names this rather than restating 8.0.
	internal const double c_defaultKeyboardIncrement = 8.0;

	// ResizeGripper();

	// void BeginDrag();
	// bool TryDrag(double totalDelta);
	// void EndDrag(bool canceled);
	// bool TryKeyboardStep(winrt::VirtualKey key);

	// void OnPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// IControl overrides
	// void OnApplyTemplate();
	// void OnPointerEntered(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerExited(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerPressed(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerReleased(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerCaptureLost(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerCanceled(winrt::PointerRoutedEventArgs const& args);
	// void OnManipulationStarting(winrt::ManipulationStartingRoutedEventArgs const& args);
	// void OnManipulationStarted(winrt::ManipulationStartedRoutedEventArgs const& args);
	// void OnManipulationDelta(winrt::ManipulationDeltaRoutedEventArgs const& args);
	// void OnManipulationCompleted(winrt::ManipulationCompletedRoutedEventArgs const& args);
	// void OnKeyDown(winrt::KeyRoutedEventArgs const& args);

	// IUIElement overrides
	// winrt::AutomationPeer OnCreateAutomationPeer();

	// private:
	// void UpdateResizeCursor();
	// void UpdateManipulationMode();
	// void UpdateVisualState();
	// void UpdateOrientationVisualState();
	// bool IsHorizontalDrag();
	// double EffectiveKeyboardIncrement();
	// void OnIsEnabledChanged(const winrt::IInspectable& sender, const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnUnloaded(const winrt::IInspectable& sender, const winrt::RoutedEventArgs& args);
	// void AttachCancelKeyHandler();
	// void DetachCancelKeyHandler();
	// void ReleaseCapturedPointer() noexcept;
	// void OnCancelKeyDown(const winrt::IInspectable& sender, const winrt::KeyRoutedEventArgs& args);

	private double m_lastRaisedDelta = 0.0;
	private bool m_isPointerOver = false;
	// The mouse pointer this gripper captured; null when it holds no capture.
	private Pointer? m_capturedPointer = null;
	private uint m_capturedPointerId = 0;
	private bool m_isCapturedPointerReleased = false;
	private bool m_templateApplied = false;
	// Boxed handler passed to AddHandler and its target; null when not dragging.
	private object? m_cancelKeyHandler = null;
	private WeakReference<UIElement>? m_cancelKeyRoot = null;
}
