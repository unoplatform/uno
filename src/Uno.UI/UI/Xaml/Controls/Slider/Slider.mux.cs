// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference Slider_Partial.cpp

using System;
using System.Globalization;
using DirectUI;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Microsoft.UI.Xaml.Controls._Tracing;
using Uno.UI.Xaml.Core.Scaling;

namespace Microsoft.UI.Xaml.Controls;

// TODO MZ: HasXamlTemplate parts may need to be integrated.

public partial class Slider
{
	/// <summary>
	/// Initializes a new instance of the Slider class.
	/// </summary>
	public Slider()
	{
		DefaultStyleKey = typeof(Slider);

		IsPointerOver = false;
		_processingInputEvent = false;
		_isPressed = false;
		_dragValue = 0.0;
		_usingDefaultToolTipForHorizontalThumb = false;
		_usingDefaultToolTipForVerticalThumb = false;
		_capturedPointerId = 0;

		// TODO Uno specific: This is called by DirectUI automatically,
		// we have to do it manually here.
		PrepareState();

#if HAS_UNO
		// Uno specific: Detach and reattach template child events.
		// Needed to avoid memory leaks on iOS.
		Loaded += Slider_Loaded;
		Unloaded += Slider_Unloaded;
#endif
	}

	private void PrepareState()
	{
		FocusEngaged += OnFocusEngaged;
		FocusDisengaged += OnFocusDisengaged;
		_prepareStateToken.Disposable = Disposable.Create(() =>
		{
			FocusDisengaged -= OnFocusDisengaged;
			FocusEngaged -= OnFocusEngaged;
		});
	}

	internal override bool GetDefaultValue2(DependencyProperty property, out object value)
	{
		value = null;
		if (property == MaximumProperty)
		{
			value = SLIDER_DEFAULT_MAXIMUM;
		}
		else if (property == LargeChangeProperty)
		{
			value = SLIDER_DEFAULT_LARGE_CHANGE;
		}
		else if (property == SmallChangeProperty)
		{
			value = SLIDER_DEFAULT_SMALL_CHANGE;
		}
		else
		{
			base.GetDefaultValue2(property, out value);
		}

		return value != null;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == OrientationProperty)
		{
			OnOrientationChanged();
			UpdateVisualState();
		}
		else if (
			args.Property == IsDirectionReversedProperty ||
			args.Property == IntermediateValueProperty)
		{
			UpdateTrackLayout();
		}
		else if (args.Property == TickPlacementProperty)
		{
			OnTickPlacementChanged();
		}
		else if (args.Property == IsThumbToolTipEnabledProperty)
		{
			OnIsThumbToolTipEnabledChanged();
		}
		else if (args.Property == ThumbToolTipValueConverterProperty)
		{
			OnThumbToolTipValueConverterChanged();
		}
		else if (args.Property == VisibilityProperty)
		{
			OnVisibilityChanged();
		}
		else if (args.Property == TickFrequencyProperty)
		{
			InvalidateTickBarsArrange();
		}
		else if (
			args.Property == HeaderProperty ||
			args.Property == HeaderTemplateProperty)
		{
			UpdateHeaderPresenterVisibility();
		}
		else if (args.Property == IsFocusEngagedProperty)
		{
			OnIsFocusEngagedChanged();
		}
	}

	// OnFocusEngaged property changed handler.
	private void OnFocusEngaged(
		Control sender,
		FocusEngagedEventArgs args)
	{
		_preEngagementValue = Value;
	}

	// OnFocusEngaged property changed handler.
	private void OnFocusDisengaged(
		Control sender,
		FocusDisengagedEventArgs args)
	{
		// Revert value
		if (_preEngagementValue >= 0.0)
		{
			Value = _preEngagementValue;
			_preEngagementValue = -1.0f;
		}
	}

	// IsEnabled property changed handler.
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs args)
	{
		base.OnIsEnabledChanged(args);

		var isEnabled = IsEnabled;
		if (!isEnabled)
		{
			_isPointerOver = false;
			_isPressed = false;
		}

		UpdateVisualState();
	}

	// Update the visual states when the Visibility property is changed.
	private protected override void OnVisibilityChanged()
	{
		var visibility = Visibility;
		if (Visibility.Visible != visibility)
		{
			_isPointerOver = false;
			_isPressed = false;
		}

		UpdateVisualState();
	}

	// GotFocus event handler.
	protected override void OnGotFocus(RoutedEventArgs e)
	{
		base.OnGotFocus(e);
		UpdateVisualState(true);

		var focusState = FocusState;
		if (FocusState.Keyboard == focusState)
		{
			var contentRoot = VisualTree.GetContentRootForElement(this);
			bool inputIsGamePadOrRemote = contentRoot.InputManager.LastInputDeviceType == InputDeviceType.GamepadOrRemote;

			var isFocusEngaged = IsFocusEngaged;

			var isFocusEngagementEnabled = IsFocusEngagementEnabled;

			var isThumbToolTipEnabled = IsThumbToolTipEnabled;

			bool shouldShowToolTip = isThumbToolTipEnabled && (!inputIsGamePadOrRemote || !isFocusEngagementEnabled || isFocusEngaged);
			if (shouldShowToolTip)
			{
				UpdateThumbToolTipVisibility(true, AutomaticToolTipInputMode.Keyboard);
			}
		}
	}


	// LostFocus event handler.
	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);
		UpdateVisualState(true);
		UpdateThumbToolTipVisibility(false);
	}

	// KeyDown event handler.
	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		var handled = args.Handled;
		if (handled)
		{
			return;
		}

		var enabled = IsEnabled;
		if (!enabled)
		{
			return;
		}

		var key = args.OriginalKey;

		KeyProcess.KeyDown(key, this, out var keyPressHandled);

		if (keyPressHandled)
		{
			var focusState = FocusState;
			if (FocusState.Keyboard != focusState)
			{
				Focus(FocusState.Keyboard);
				UpdateVisualState(true);
				UpdateThumbToolTipVisibility(true, AutomaticToolTipInputMode.Keyboard);
			}

			args.Handled = true;
		}
	}

	// PreviewKeyDown event handler.
	protected override void OnPreviewKeyDown(KeyRoutedEventArgs args)
	{
		var key = args.OriginalKey;

		var isFocusEngagementEnabled = IsFocusEngagementEnabled;

		bool isFocusEngaged = IsFocusEngaged;

		bool isHandled = args.Handled;

		if (isFocusEngagementEnabled && isFocusEngaged && !isHandled)
		{
			if (key == VirtualKey.GamepadA)
			{
				// "Commit" value, aka do nothing
				_preEngagementValue = -1.0f;
				RemoveFocusEngagement();
				args.Handled = true;

				_disengagedWithA = true;
			}
			else if (key == VirtualKey.GamepadB)
			{
				// Revert value
				Value = _preEngagementValue;
				_preEngagementValue = -1.0f;
				RemoveFocusEngagement();
				args.Handled = true;
			}
		}
	}

	// PreviewKeyUp event handler.
	protected override void OnPreviewKeyUp(KeyRoutedEventArgs args)
	{
		var key = args.OriginalKey;

		bool isFocusEngagementEnabled = IsFocusEngagementEnabled;

		if (isFocusEngagementEnabled)
		{
			if (key == VirtualKey.GamepadA)
			{
				if (_disengagedWithA)
				{
					// Block the re-engagement
					_disengagedWithA = false; // We want to do this regardless of handled
					args.Handled = true;
				}
			}
		}
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);

		var isDragging = false;
		var pointerPoint = args.GetCurrentPoint(null);
		var pointerDeviceType = pointerPoint.PointerDevice.PointerDeviceType;

		//We don't want to go into the PointerOver state for touch input
		_isPointerOver = pointerDeviceType != PointerDeviceType.Touch;

		var thumb = Thumb;
		if (thumb != null)
		{
			isDragging = thumb.IsDragging;
			if (!isDragging)
			{
				UpdateVisualState();
			}
		}

		// Normally, PointerEntered/PointerExited for touch input toggles the visibility of the Thumb ToolTip.
		// However, the Thumb ToolTip should stay visible as long as the Slider or its Thumb are pressed,
		// so we do nothing here if the Slider or its Thumb are pressed.
		var focusState = FocusState;
		if (!_isPressed &&
			!isDragging &&
			FocusState.Keyboard != focusState)
		{
			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				UpdateThumbToolTipVisibility(true, AutomaticToolTipInputMode.Touch);
			}
		}
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		bool isDragging = false;
		base.OnPointerExited(args);

		_isPointerOver = false;

		var thumb = Thumb;
		if (thumb != null)
		{
			isDragging = thumb.IsDragging;
			if (!isDragging)
			{
				UpdateVisualState();
			}
		}

		// Normally, PointerEntered/PointerExited for touch input toggles the visibility of the Thumb ToolTip.
		// However, the Thumb ToolTip should stay visible as long as the Slider or its Thumb are pressed,
		// so we do nothing here if the Slider or its Thumb are pressed.
		var focusState = FocusState;
		if (!_isPressed &&
			!isDragging &&
			FocusState.Keyboard != focusState)
		{
			var pointerPoint = args.GetCurrentPoint(null);
			var pointerDeviceType = pointerPoint.PointerDevice.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				UpdateThumbToolTipVisibility(false);
			}
		}
	}

	private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		// Uno specific: If tracker is disabled, ignore pointer input.
		if (!IsTrackerEnabled)
		{
			return;
		}

		var pointerPoint = args.GetCurrentPoint(null);

		var pointerProperties = pointerPoint.Properties;

		var isLeftButtonPressed = pointerProperties.IsLeftButtonPressed;

		if (isLeftButtonPressed)
		{
			var pointerDeviceType = pointerPoint.PointerDevice.PointerDeviceType;

			args.Handled = true;

			var mode = AutomaticToolTipInputMode.None;
			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				mode = AutomaticToolTipInputMode.Touch;
			}
			else
			{
				mode = AutomaticToolTipInputMode.Mouse;
			}
			UpdateThumbToolTipVisibility(true, mode);

			Focus(FocusState.Pointer);
			var pointer = args.Pointer;

			if (_capturedPointerId == 0)
			{
				bool wasCaptured = false;

				wasCaptured = (sender as UIElement)?.CapturePointer(pointer, /* UNO only */ options: PointerCaptureOptions.PreventDirectManipulation) is PointerCaptureResult.Added;
				if (wasCaptured)
				{
					_capturedPointerId = pointer.PointerId;
				}
			}

			var position = pointerPoint.Position;
			MoveThumbToPoint(position);
			SetIsPressed(true);
		}
	}

	// PointerMoved event handler.
	private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
	{
		var thumb = Thumb;
		if (thumb != null)
		{
			var isDragging = thumb.IsDragging;

			if (_isPressed && !isDragging)
			{
				args.Handled = true;

				var pointerPoint = args.GetCurrentPoint(null);

				var point = pointerPoint.Position;
				MoveThumbToPoint(point);
			}
		}
	}

	// PointerReleased event handler.
	private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
	{
		SetIsPressed(false); // Will also update visual state
		var gestureFollowing = args.GestureFollowing;
		if (gestureFollowing == GestureModes.RightTapped)
		{
			// We will get a right tapped event for every time we visit here, and
			// we will visit before each time we receive a right tapped event
			return;
		}

		// Note that we are intentionally NOT handling the args
		// if we do not fall through here because basically we are no_opting in that case.
		args.Handled = true;
		PerformPointerUpAction();

		if (_capturedPointerId != 0)
		{
			var pointer = args.Pointer;
			var pointerID = pointer.PointerId;
			if (pointerID == _capturedPointerId)
			{
				(sender as UIElement)?.ReleasePointerCapture(pointer);
				_capturedPointerId = 0;
			}
		}
	}

	private protected override void OnRightTappedUnhandled(RightTappedRoutedEventArgs args)
	{
		base.OnRightTappedUnhandled(args);
		var isHandled = args.Handled;
		if (isHandled)
		{
			return;
		}
		PerformPointerUpAction();
	}

	// Perform the primary action related to pointer up.
	private void PerformPointerUpAction()
	{
		// When we handle LMB down, Value is updated to the closest step or tick mark.
		// Therefore, it suffices here to fetch Value and put this into IntermediateValue.
		var value = Value;
		IntermediateValue = value;

		UpdateThumbToolTipVisibility(false);
	}

	// PointerCaptureLost event handler.
	private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs args)
	{
		base.OnPointerCaptureLost(args);

		bool hasStateChanged = _isPressed || _isPointerOver;

		_isPressed = false;

		// when pointer move out of the control but OnPointerExit() would not be invoked so the control might remain in PointerOver visual state.
		// change it to false here
		_isPointerOver = false;

		if (hasStateChanged)
		{
			UpdateVisualState(true);
		}

		_capturedPointerId = 0;
	}

	// Create SliderAutomationPeer to represent the Slider.
	protected override AutomationPeer OnCreateAutomationPeer() => new SliderAutomationPeer(this);

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns a plain text string to provide a default AutomationProperties.Name
	//		in the absence of an explicitly defined one
	//
	//---------------------------------------------------------------------------
	internal override string GetPlainText()
	{
		var header = Header;

		if (header != null)
		{
			return FrameworkElement.GetStringFromObject(header);
		}

		return null;
	}

	// Change to the correct visual state for the button.
	private protected override void ChangeVisualState(
		// true to use transitions when updating the visual state, false
		// to snap directly to the new visual state.
		bool useTransitions)
	{
		var isEnabled = IsEnabled;
		var isFocusEngaged = IsFocusEngaged;
		var focusState = FocusState;
		var orientation = Orientation;

		if (!isEnabled)
		{
			// TODO: VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
			GoToState(useTransitions, "Disabled");
		}
		else if (_isPressed)
		{
			// TODO: VisualStates.GoToState(this, useTransitions, VisualStates.StatePressed, VisualStates.StateNormal);
			GoToState(useTransitions, "Pressed");
		}
		else if (_isPointerOver)
		{
			// TODO: VisualStates.GoToState(this, useTransitions, VisualStates.StatePointerOver, VisualStates.StateNormal);
			GoToState(useTransitions, "PointerOver");
		}
		else
		{
			GoToState(useTransitions, "Normal");
		}

		if (FocusState.Keyboard == focusState && isEnabled)
		{
			// TODO: VisualStates.GoToState(this, useTransitions, VisualStates.StateFocused, VisualStates.StateUnfocused);
			GoToState(useTransitions, "Focused");
		}
		else
		{
			GoToState(useTransitions, "Unfocused");
		}

		if (isFocusEngaged)
		{
			if (orientation == Orientation.Horizontal)
			{
				GoToState(useTransitions, "FocusEngagedHorizontal");
			}
			else
			{
				MUX_ASSERT(orientation == Orientation.Vertical);
				GoToState(useTransitions, "FocusEngagedVertical");
			}
		}
		else
		{
			GoToState(useTransitions, "FocusDisengaged");
		}
	}

	// Apply a template to the Slider.
	protected override void OnApplyTemplate()
	{
		bool bIsThumbToolTipEnabled = false;

		// Cleanup any existing template parts
		if (_tpElementHorizontalThumb != null)
		{
			_elementHorizontalThumbDragStartedToken.Disposable = null;
			_elementHorizontalThumbDragDeltaToken.Disposable = null;
			_elementHorizontalThumbDragCompletedToken.Disposable = null;
			_elementHorizontalThumbSizeChangedToken.Disposable = null;
		}
		if (_tpElementVerticalThumb != null)
		{
			_elementVerticalThumbDragStartedToken.Disposable = null;
			_elementVerticalThumbDragDeltaToken.Disposable = null;
			_elementVerticalThumbDragCompletedToken.Disposable = null;
			_elementVerticalThumbSizeChangedToken.Disposable = null;
		}

		{
			FrameworkElement spSliderContainer = _tpSliderContainer ?? this;

			//TODO MZ: Need to use RemoveHandler?
			spSliderContainer.PointerPressed -= OnPointerPressed;
			spSliderContainer.PointerReleased -= OnPointerReleased;
			spSliderContainer.PointerMoved -= OnPointerMoved;
			spSliderContainer.PointerCaptureLost -= OnPointerCaptureLost;
			spSliderContainer.SizeChanged -= OnSizeChanged;
		}

		_tpHeaderPresenter = null;
		_tpElementHorizontalTemplate = null;
		_tpElementTopTickBar = null;
		_tpElementHorizontalInlineTickBar = null;
		_tpElementBottomTickBar = null;
		_tpElementLeftTickBar = null;
		_tpElementVerticalInlineTickBar = null;
		_tpElementRightTickBar = null;
		_tpElementHorizontalDecreaseRect = null;
		_tpElementHorizontalThumb = null;
		_tpElementVerticalTemplate = null;
		_tpElementVerticalDecreaseRect = null;
		_tpElementVerticalThumb = null;
		_tpSliderContainer = null;

		_usingDefaultToolTipForHorizontalThumb = false;
		_usingDefaultToolTipForVerticalThumb = false;

		base.OnApplyTemplate();

		// Get the parts
		var spElementHorizontalTemplateAsDO = GetTemplateChild("HorizontalTemplate");
		_tpElementHorizontalTemplate = spElementHorizontalTemplateAsDO as FrameworkElement;
		var spElementTopTickBarAsDO = GetTemplateChild("TopTickBar");
		_tpElementTopTickBar = spElementTopTickBarAsDO as TickBar;
		var spElementHorizontalInlineTickBarAsDO = GetTemplateChild("HorizontalInlineTickBar");
		_tpElementHorizontalInlineTickBar = spElementHorizontalInlineTickBarAsDO as TickBar;
		var spElementBottomTickBarAsDO = GetTemplateChild("BottomTickBar");
		_tpElementBottomTickBar = spElementBottomTickBarAsDO as TickBar;
		var spElementLeftTickBarAsDO = GetTemplateChild("LeftTickBar");
		_tpElementLeftTickBar = spElementLeftTickBarAsDO as TickBar;
		var spElementVerticalInlineTickBarAsDO = GetTemplateChild("VerticalInlineTickBar");
		_tpElementVerticalInlineTickBar = spElementVerticalInlineTickBarAsDO as TickBar;
		var spElementRightTickBarAsDO = GetTemplateChild("RightTickBar");
		_tpElementRightTickBar = spElementRightTickBarAsDO as TickBar;
		var spElementHorizontalDecreaseRectAsDO = GetTemplateChild("HorizontalDecreaseRect");
		_tpElementHorizontalDecreaseRect = spElementHorizontalDecreaseRectAsDO as Rectangle;
		var spElementHorizontalThumbAsDO = GetTemplateChild("HorizontalThumb");
		_tpElementHorizontalThumb = spElementHorizontalThumbAsDO as Thumb;
		var spElementVerticalTemplateAsDO = GetTemplateChild("VerticalTemplate");
		_tpElementVerticalTemplate = spElementVerticalTemplateAsDO as FrameworkElement;
		var spElementVerticalDecreaseRectAsDO = GetTemplateChild("VerticalDecreaseRect");
		_tpElementVerticalDecreaseRect = spElementVerticalDecreaseRectAsDO as Rectangle;
		var spElementVerticalThumbAsDO = GetTemplateChild("VerticalThumb");
		_tpElementVerticalThumb = spElementVerticalThumbAsDO as Thumb;
		var spSliderContainerAsDO = GetTemplateChild("SliderContainer");
		_tpSliderContainer = spSliderContainerAsDO as FrameworkElement;

		// TODO Uno specific: Set background of slider container to ensure touch events are captured, but allow it to be overwritten by bindings etc
		if (_tpSliderContainer.Background == null)
		{
			_tpSliderContainer.SetValue(BackgroundProperty, SolidColorBrushHelper.Transparent, DependencyPropertyValuePrecedences.DefaultStyle);
		}

		// Attach the event handlers
		if (_tpElementHorizontalThumb != null || _tpElementVerticalThumb != null)
		{
			bIsThumbToolTipEnabled = IsThumbToolTipEnabled;

			if (_tpElementHorizontalThumb != null)
			{
				AttachHorizontalThumbSubscriptions();

				if (FeatureConfiguration.ToolTip.UseToolTips)
				{
					var spHorizontalThumbToolTip = ToolTipService.GetToolTipReference(_tpElementHorizontalThumb);
					// If no disambiguation UI ToolTip exist for the Thumb, we create one.
					// TODO: Jupiter 101372 - Cannot use RelativeSource={RelativeSource TemplatedParent} in generic.xaml
					// When the bug is fixed, stop creating a ToolTip in code and move this to XAML.
					if (spHorizontalThumbToolTip == null)
					{
						SetDefaultThumbToolTip(Orientation.Horizontal);
						_usingDefaultToolTipForHorizontalThumb = true;
					}
					else
					{
						spHorizontalThumbToolTip.IsEnabled = bIsThumbToolTipEnabled;
						spHorizontalThumbToolTip.m_isSliderThumbToolTip = true;
					}
				}
			}

			if (_tpElementVerticalThumb != null)
			{
				AttachVerticalThumbSubscriptions();

				if (FeatureConfiguration.ToolTip.UseToolTips)
				{
					var spVerticalThumbToolTip = ToolTipService.GetToolTipReference(_tpElementVerticalThumb);
					// If no disambiguation UI ToolTip exist for the Thumb, we create one.
					// TODO: Jupiter 101372 - Cannot use RelativeSource={RelativeSource TemplatedParent} in generic.xaml
					// When the bug is fixed, stop creating a ToolTip in code and move this to XAML.
					if (spVerticalThumbToolTip == null)
					{
						SetDefaultThumbToolTip(Orientation.Vertical);
						_usingDefaultToolTipForVerticalThumb = true;
					}
					else
					{
						spVerticalThumbToolTip.IsEnabled = bIsThumbToolTipEnabled;
						spVerticalThumbToolTip.m_isSliderThumbToolTip = true;
					}
				}
			}
		}

		// Attach to pointer events
		{
			AttachSliderContainerEvents();
		}

		// Updating states for parts where properties might have been updated
		// through XAML before the template was loaded.
		OnOrientationChanged();
		OnTickPlacementChanged();
		ChangeVisualState(false);
		UpdateHeaderPresenterVisibility();

		// The base Control class has special handling for focus visuals that are renderered as alternate black and white
		// rectangles with stroke dash arrays, to produce the effect of alternating black and white stroke dashes.
		// However, this handling uses the default focus visual names for all controls. Slider has different names since
		// it requires unique elements for its horizontal and vertical templates. So it needs a separate version of the
		// code that rounds focus visual stroke thickness to plateau-scaled integer value.
		var spFocusVisualWhiteHorizontalDO = GetTemplateChild("FocusVisualWhiteHorizontal");
		var spFocusVisualBlackHorizontalDO = GetTemplateChild("FocusVisualBlackHorizontal");
		if (spFocusVisualWhiteHorizontalDO != null &&
			spFocusVisualBlackHorizontalDO != null &&
			(spFocusVisualWhiteHorizontalDO is Rectangle spFocusVisualWhiteHorizontalDORect) &&
			(spFocusVisualBlackHorizontalDO is Rectangle spFocusVisualBlackHorizontalDORect))
		{
			LayoutRoundRectangleStrokeThickness(spFocusVisualWhiteHorizontalDORect);
			LayoutRoundRectangleStrokeThickness(spFocusVisualBlackHorizontalDORect);
		}

		var spFocusVisualWhiteVerticalDO = GetTemplateChild("FocusVisualWhiteVertical");
		var spFocusVisualBlackVerticalDO = GetTemplateChild("FocusVisualBlackVertical");
		if (spFocusVisualWhiteVerticalDO != null &&
			spFocusVisualBlackVerticalDO != null &&
			(spFocusVisualWhiteVerticalDO is Rectangle spFocusVisualWhiteVerticalDORect) &&
			(spFocusVisualBlackVerticalDO is Rectangle spFocusVisualBlackVerticalDORect))
		{
			LayoutRoundRectangleStrokeThickness(spFocusVisualWhiteVerticalDORect);
			LayoutRoundRectangleStrokeThickness(spFocusVisualBlackVerticalDORect);
		}

		_isTemplateApplied = true;
	}

#if HAS_UNO
	private void AttachVerticalThumbSubscriptions()
	{
		_tpElementVerticalThumb.DragStarted += OnThumbDragStarted;
		_elementVerticalThumbDragStartedToken.Disposable = Disposable.Create(() => _tpElementVerticalThumb.DragStarted -= OnThumbDragStarted);
		_tpElementVerticalThumb.DragDelta += OnThumbDragDelta;
		_elementVerticalThumbDragDeltaToken.Disposable = Disposable.Create(() => _tpElementVerticalThumb.DragDelta -= OnThumbDragDelta);
		_tpElementVerticalThumb.DragCompleted += OnThumbDragCompleted;
		_elementVerticalThumbDragCompletedToken.Disposable = Disposable.Create(() => _tpElementVerticalThumb.DragCompleted -= OnThumbDragCompleted);
		_tpElementVerticalThumb.SizeChanged += OnThumbSizeChanged;
		_elementVerticalThumbSizeChangedToken.Disposable = Disposable.Create(() => _tpElementVerticalThumb.SizeChanged -= OnThumbSizeChanged);
	}

	private void AttachHorizontalThumbSubscriptions()
	{
		_tpElementHorizontalThumb.DragStarted += OnThumbDragStarted;
		_elementHorizontalThumbDragStartedToken.Disposable = Disposable.Create(() => _tpElementHorizontalThumb.DragStarted -= OnThumbDragStarted);
		_tpElementHorizontalThumb.DragDelta += OnThumbDragDelta;
		_elementHorizontalThumbDragDeltaToken.Disposable = Disposable.Create(() => _tpElementHorizontalThumb.DragDelta -= OnThumbDragDelta);
		_tpElementHorizontalThumb.DragCompleted += OnThumbDragCompleted;
		_elementHorizontalThumbDragCompletedToken.Disposable = Disposable.Create(() => _tpElementHorizontalThumb.DragCompleted -= OnThumbDragCompleted);
		_tpElementHorizontalThumb.SizeChanged += OnThumbSizeChanged;
		_elementHorizontalThumbSizeChangedToken.Disposable = Disposable.Create(() => _tpElementHorizontalThumb.SizeChanged -= OnThumbSizeChanged);
	}

	private void AttachSliderContainerEvents()
	{
		FrameworkElement spSliderContainer = _tpSliderContainer ?? this;

		//TODO MZ: Should include handled too?
		spSliderContainer.PointerPressed += OnPointerPressed;
		spSliderContainer.PointerReleased += OnPointerReleased;
		spSliderContainer.PointerMoved += OnPointerMoved;
		spSliderContainer.PointerCaptureLost += OnPointerCaptureLost;
		spSliderContainer.SizeChanged += OnSizeChanged;
		_sliderContainerToken.Disposable = Disposable.Create(() =>
		{
			spSliderContainer.PointerPressed -= OnPointerPressed;
			spSliderContainer.PointerReleased -= OnPointerReleased;
			spSliderContainer.PointerMoved -= OnPointerMoved;
			spSliderContainer.PointerCaptureLost -= OnPointerCaptureLost;
			spSliderContainer.SizeChanged -= OnSizeChanged;
		});
	}
#endif

	protected override void OnValueChanged(double oldValue, double newValue)
	{
		base.OnValueChanged(oldValue, newValue);

		if (!_processingInputEvent)
		{
			IntermediateValue = newValue;
		}
	}

	// Called when the Minimum value changed.
	protected override void OnMinimumChanged(
		double oldMinimum,
		double newMinimum)
	{
		base.OnMinimumChanged(oldMinimum, newMinimum);
		UpdateTrackLayout();
	}

	// Called when the Maximum value changed.
	protected override void OnMaximumChanged(
		double oldMaximum,
		double newMaximum)
	{
		base.OnMaximumChanged(oldMaximum, newMaximum);
		UpdateTrackLayout();
	}

	// Called whenever the Thumb drag operation is started.
	private void OnThumbDragStarted(object sender, DragStartedEventArgs args)
	{
		Focus(FocusState.Pointer);
		UpdateVisualState(true);
		_dragValue = Value;
	}

	// Whenever the thumb gets dragged, we handle the event through this function to
	// update the current value depending upon the thumb drag delta.
	private void OnThumbDragDelta(
		object sender,
		DragDeltaEventArgs args)
	{
		try
		{
			Grid spRootGrid;
			double nominator = 0.0;
			double denominator = 0.0;
			double offset = 0.0;
			double zoom = 1.0;
			double maximum = 0.0;
			double minimum = 0.0;
			double actualSize = 0.0;
			double thumbSize = 0.0;
			double change = 0.0;
			Orientation orientation = Orientation.Vertical;

			_processingInputEvent = true;

			spRootGrid = GetRootGrid();
			if (spRootGrid != null)
			{
				maximum = Maximum;
				minimum = Minimum;

				orientation = Orientation;
				if (orientation == Orientation.Horizontal &&
					_tpElementHorizontalThumb != null)
				{
					change = args.HorizontalChange;
					actualSize = spRootGrid.ActualWidth;
					thumbSize = _tpElementHorizontalThumb.ActualWidth;

					nominator = (zoom * change) * (maximum - minimum);
					denominator = (actualSize - thumbSize);
					offset = nominator / DoubleUtil.Max(1, denominator);
				}
				else if (orientation == Orientation.Vertical &&
					_tpElementVerticalThumb != null)
				{
					change = args.VerticalChange;
					actualSize = spRootGrid.ActualHeight;
					thumbSize = _tpElementVerticalThumb.ActualHeight;

					nominator = (zoom * -change) * (maximum - minimum);
					denominator = (actualSize - thumbSize);
					offset = nominator / DoubleUtil.Max(1, denominator);
				}

				if (!DoubleUtil.IsNaN(offset) &&
					!DoubleUtil.IsInfinity(offset))
				{
					bool reversed = false;
					double newValue = 0;
					SliderSnapsTo snapsTo = SliderSnapsTo.StepValues;
					double tickFrequency = 0;
					double stepFrequency = 0;
					double closestStep = 0;
					double value = 0;

					reversed = IsDirectionReversed;
					_dragValue += reversed ? -offset : offset;

					newValue = DoubleUtil.Min(maximum, DoubleUtil.Max(minimum, _dragValue));
					IntermediateValue = newValue;

					// Set value to the closest multiple of StepFrequency.
					value = Value;
					snapsTo = SnapsTo;
					if (snapsTo == SliderSnapsTo.Ticks)
					{
						tickFrequency = TickFrequency;
						closestStep = GetClosestStep(tickFrequency, newValue);
					}
					else
					{
						stepFrequency = this.StepFrequency;
						closestStep = GetClosestStep(stepFrequency, newValue);
					}

					if (!DoubleUtil.AreClose(value, closestStep))
					{
						Value = closestStep;
					}
				}
			}
		}
		finally
		{
			_processingInputEvent = false;
		}
	}

	// Whenever the thumb drag completes, we handle the event through this
	// function to update IntermediateValue to snap to the closest step or tick mark.
	private void OnThumbDragCompleted(object sender, DragCompletedEventArgs args)
	{

		double value = 0;

		// While we drag, Value is always updated to the closest step or tick mark.
		// Therefore, it suffices here to fetch Value and put this into IntermediateValue.
		value = Value;
		IntermediateValue = value;
	}

	// Whenever the thumb size changes, we need to recalculate track layout.
	private void OnThumbSizeChanged(
		object sender,
		SizeChangedEventArgs args)
	{
		UpdateTrackLayout();
	}

	// Handle the SizeChanged event.
	private void OnSizeChanged(
		object sender,
		SizeChangedEventArgs args)
	{
		UpdateTrackLayout();
	}

	// Change the template being used to display this control when the orientation
	// changes.
	private void OnOrientationChanged()
	{
		Orientation orientation = Orientation.Vertical;

		orientation = Orientation;

		if (_tpElementHorizontalTemplate != null)
		{
			_tpElementHorizontalTemplate.Visibility =
				orientation == Orientation.Horizontal ?
				Visibility.Visible :
				Visibility.Collapsed;
		}

		if (_tpElementVerticalTemplate != null)
		{
			_tpElementVerticalTemplate.Visibility =
				orientation == Orientation.Horizontal ?
				Visibility.Collapsed :
				Visibility.Visible;
		}

		UpdateTrackLayout();
	}

	// Updates the track layout using ActualWidth and ActualHeight of the control.
	private void UpdateTrackLayout()
	{

		double actualWidth = 0.0;
		double actualHeight = 0.0;

		// New behavior for blue: only the slider's root grid is interactive.
		if (_tpSliderContainer != null)
		{
			actualWidth = _tpSliderContainer.ActualWidth;
			actualHeight = _tpSliderContainer.ActualHeight;
		}
		// Old behavior for win8: the entire control is interactive.
		else
		{
			actualWidth = ActualWidth;
			actualHeight = ActualHeight;
		}

		// If we are layout rounding ensure that width and height are rounded
		actualWidth = LayoutRoundedDimension(actualWidth);
		actualHeight = LayoutRoundedDimension(actualHeight);

		UpdateTrackLayout(actualWidth, actualHeight);
	}

	// This method will take the current min, max, and value to calculate and layout the current control measurements.
	private void UpdateTrackLayout(
		double actualWidth,
		double actualHeight)
	{
		Grid spRoot;
		double maximum = 0.0;
		double minimum = 0.0;
		double currentValue = 0.0;
		double multiplier = 1.0;
		Orientation orientation = Orientation.Vertical;
		bool reversed;
		double thumbWidth = 0.0;
		double thumbHeight = 0.0;
		double elementNewLength = 0;
		double elementOldLength = 0;
		bool isThumbToolTipEnabled = false;
		Thickness padding = Thickness.Empty;

		// Extract padding from the actual size.
		if (_tpSliderContainer == null)
		{
			padding = Padding;
		}

		actualWidth = Math.Max(actualWidth - padding.Left - padding.Right, 0.0);
		actualHeight = Math.Max(actualHeight - padding.Top - padding.Bottom, 0.0);

		isThumbToolTipEnabled = IsThumbToolTipEnabled;
#if HAS_UNO
		isThumbToolTipEnabled &= FeatureConfiguration.ToolTip.UseToolTips;
#endif

		maximum = Maximum;
		minimum = Minimum;
		currentValue = IntermediateValue;
		orientation = Orientation;
		reversed = IsDirectionReversed;
		double range = maximum - minimum;
		multiplier = (range <= 0) ? 0 : 1 - (maximum - currentValue) / range;

		spRoot = GetRootGrid();
		if (spRoot != null)
		{
			ToolTip spToolTip;
			GeneralTransform spTransformToRoot;

			var starGridLength = new GridLength(1.0, GridUnitType.Star);
			var autoGridLength = new GridLength(1.0, GridUnitType.Auto);

			if (orientation == Orientation.Horizontal)
			{
				if (_tpElementHorizontalThumb != null)
				{
					thumbWidth = _tpElementHorizontalThumb.ActualWidth;
					thumbHeight = _tpElementHorizontalThumb.ActualHeight;
				}

				var spColumns = spRoot.ColumnDefinitions;
				if (spColumns != null)
				{
					var count = spColumns.Count;
					if (count == 3 || count == 4) // we shouldn't have this check. Let's just swap first with the last.
					{
						ColumnDefinition spFirstColumn;
						ColumnDefinition spLastColumn;

						spFirstColumn = spColumns[0];
						spLastColumn = spColumns[count - 1];
						spFirstColumn.Width = reversed ? starGridLength : autoGridLength;
						spLastColumn.Width = reversed ? autoGridLength : starGridLength;

						if (_tpElementHorizontalDecreaseRect != null)
						{
							var spDecreaseValue = PropertyValue.CreateInt32(reversed ? count - 1 : 0);
							_tpElementHorizontalDecreaseRect.SetValue(Grid.ColumnProperty, spDecreaseValue);
						}

						if (_tpElementHorizontalDecreaseRect != null)
						{
							elementNewLength = DoubleUtil.Max(0.0, multiplier * (actualWidth - thumbWidth));
							elementNewLength = LayoutRoundedDimension(elementNewLength);

							elementOldLength = _tpElementHorizontalDecreaseRect.Width;
							// If length has not changed by atleast a rounding step, no reason to redo everything.
							if (double.IsNaN(elementOldLength) || DoubleUtil.GreaterThanOrClose(Math.Abs(elementOldLength - elementNewLength), RoundingStep()))
							{
								_tpElementHorizontalDecreaseRect.Width = elementNewLength;
							}
							else
							{
								elementNewLength = elementOldLength;
							}
						}
					}
				}

				if (_tpElementHorizontalThumb != null && isThumbToolTipEnabled)
				{
					Point origin = Point.Zero;
					Point targetTopLeft = Point.Zero;
					double lengthDelta = elementNewLength - elementOldLength;
					FlowDirection targetFlowDirection = FlowDirection.LeftToRight;
					Rect rcDockTo = Rect.Empty;

					spTransformToRoot = _tpElementHorizontalThumb.TransformToVisual(null);
					targetTopLeft = spTransformToRoot.TransformPoint(origin);

					targetFlowDirection = _tpElementHorizontalThumb.FlowDirection;

					var rcDockToLeft = 0L;
					var rcDockToRight = 0L;
					var rcDockToTop = 0L;
					var rcDockToBottom = 0L;

					if (FlowDirection.RightToLeft == targetFlowDirection)
					{
						// RTL case
						rcDockToLeft = (long)(targetTopLeft.X - lengthDelta - thumbWidth);
						rcDockToRight = (long)(targetTopLeft.X - lengthDelta);
					}
					else
					{
						// Normal case
						rcDockToLeft = (long)(targetTopLeft.X + lengthDelta);
						rcDockToRight = (long)(targetTopLeft.X + lengthDelta + thumbWidth);
					}
					rcDockToTop = (long)(targetTopLeft.Y);
					rcDockToBottom = (long)(targetTopLeft.Y + thumbHeight);

					rcDockTo = new Rect(new Point(rcDockToLeft, rcDockToTop), new Point(rcDockToRight, rcDockToBottom));

					spToolTip = ToolTipService.GetToolTipReference(_tpElementHorizontalThumb);
					spToolTip?.PerformPlacement(rcDockTo);
				}
			}
			else
			{
				if (_tpElementVerticalThumb != null)
				{
					thumbWidth = _tpElementVerticalThumb.ActualWidth;
					thumbHeight = _tpElementVerticalThumb.ActualHeight;
				}

				var spRows = spRoot.RowDefinitions;
				if (spRows != null)
				{
					var count = spRows.Size;
					if (count == 3)
					{
						RowDefinition spFirstRow;
						RowDefinition spLastRow;

						spFirstRow = spRows[0];
						spLastRow = spRows[2];
						spFirstRow.Height = reversed ? autoGridLength : starGridLength;
						spLastRow.Height = reversed ? starGridLength : autoGridLength;

						if (_tpElementVerticalDecreaseRect != null)
						{
							var spDecreaseValue = PropertyValue.CreateInt32(reversed ? 0 : 2);
							_tpElementVerticalDecreaseRect.SetValue(Grid.RowProperty, spDecreaseValue);
						}

						if (_tpElementVerticalDecreaseRect != null)
						{
							elementNewLength = DoubleUtil.Max(0.0, multiplier * (actualHeight - thumbHeight));
							elementNewLength = LayoutRoundedDimension(elementNewLength);

							elementOldLength = _tpElementVerticalDecreaseRect.Height;
							// If length has not changed by atleast a rounding step, no reason to redo everything.
							if (double.IsNaN(elementOldLength) || DoubleUtil.GreaterThanOrClose(Math.Abs(elementOldLength - elementNewLength), RoundingStep()))
							{
								_tpElementVerticalDecreaseRect.Height = elementNewLength;
							}
							else
							{
								elementNewLength = elementOldLength;
							}
						}
					}
				}

				if (_tpElementVerticalThumb != null && isThumbToolTipEnabled)
				{
					Point origin = Point.Zero;
					Point targetTopLeft = Point.Zero;
					double lengthDelta = elementNewLength - elementOldLength;
					FlowDirection targetFlowDirection = FlowDirection.LeftToRight;
					Rect rcDockTo = new Rect();

					spTransformToRoot = _tpElementVerticalThumb.TransformToVisual(null);
					targetTopLeft = spTransformToRoot.TransformPoint(origin);

					targetFlowDirection = _tpElementVerticalThumb.FlowDirection;

					var rcDockToLeft = 0L;
					var rcDockToRight = 0L;
					var rcDockToTop = 0L;
					var rcDockToBottom = 0L;

					if (FlowDirection.RightToLeft == targetFlowDirection)
					{
						// RTL case
						rcDockToLeft = (long)(targetTopLeft.X - thumbWidth);
						rcDockToRight = (long)(targetTopLeft.X);
					}
					else
					{
						// Normal case
						rcDockToLeft = (long)(targetTopLeft.X);
						rcDockToRight = (long)(targetTopLeft.X + thumbWidth);
					}
					rcDockToTop = (long)(targetTopLeft.Y - lengthDelta);
					rcDockToBottom = (long)(targetTopLeft.Y - lengthDelta + thumbHeight);
					rcDockTo = new Rect(new Point(rcDockToLeft, rcDockToTop), new Point(rcDockToRight, rcDockToBottom));

					spToolTip = ToolTipService.GetToolTipReference(_tpElementVerticalThumb);
					spToolTip.PerformPlacement(rcDockTo);
				}
			}
		}

		InvalidateTickBarsArrange();
	}

	internal Thumb Thumb
	{
		get
		{
			Thumb thumb;
			var orientation = this.Orientation;
			if (orientation == Orientation.Horizontal)
			{
				thumb = _tpElementHorizontalThumb;
			}
			else
			{
				thumb = _tpElementVerticalThumb;
			}

			return thumb;
		}
	}

	internal UIElement ElementHorizontalTemplate => _tpElementHorizontalTemplate;

	internal UIElement ElementVerticalTemplate => _tpElementVerticalTemplate;

	// Returns the distance across the thumb in the direction of orientation.
	//  e.g. returns the thumb width for horizontal orientation.
	internal double GetThumbLength()
	{
		double length = 0;

		var orientation = this.Orientation;
		if (orientation == Orientation.Horizontal)
		{
			if (_tpElementHorizontalThumb != null)
			{
				length = _tpElementHorizontalThumb.ActualWidth;
			}
		}
		else
		{
			if (_tpElementVerticalThumb != null)
			{
				length = _tpElementVerticalThumb.ActualHeight;
			}
		}

		return length;
	}

	// Causes the TickBars to rearrange.
	private void InvalidateTickBarsArrange()
	{
		var orientation = Orientation;
		if (Orientation.Horizontal == orientation)
		{
			// Horizontal - Top, HorizontalInline, and Bottom TickBars

			if (_tpElementTopTickBar != null)
			{
				_tpElementTopTickBar.InvalidateArrange();
			}
			if (_tpElementHorizontalInlineTickBar != null)
			{
				_tpElementHorizontalInlineTickBar.InvalidateArrange();
			}
			if (_tpElementBottomTickBar != null)
			{
				_tpElementBottomTickBar.InvalidateArrange();
			}
		}
		else
		{
			// Vertical - Left, VerticalInline, and Right TickBars

			if (_tpElementLeftTickBar != null)
			{
				_tpElementLeftTickBar.InvalidateArrange();
			}
			if (_tpElementVerticalInlineTickBar != null)
			{
				_tpElementVerticalInlineTickBar.InvalidateArrange();
			}
			if (_tpElementRightTickBar != null)
			{
				_tpElementRightTickBar.InvalidateArrange();
			}
		}
	}

	// Set Value to the next step position in the specified direction.  Uses SmallChange
	// as the step interval if bUseSmallChange is true; uses LargeChange otherwise.
	private void Step(bool bUseSmallChange, bool bForward)
	{
		double stepDelta = 0;
		double value = 0;
		double max = 0;
		double newValue = 0;
		double closestStep = 0;

		var snapsTo = SnapsTo;
		if (snapsTo == SliderSnapsTo.Ticks)
		{
			if (bUseSmallChange)
			{
				stepDelta = TickFrequency;
			}
			else
			{
				// For SliderSnapsTo=Ticks, we ignore SmallChange and move the Thumb by TickFrequency for arrow key events.
				// However, we still want to honor LargeChange for larger increments while keeping the Thumb on tick mark intervals.
				// To achieve this, we round LargeChange to the nearest multiple of TickFrequency.
				double largeChange = 0;
				double tickFrequency = 0;
				largeChange = LargeChange;
				tickFrequency = TickFrequency;
				stepDelta = DoubleUtil.Floor(largeChange / tickFrequency + 0.5) * tickFrequency;
			}
		}
		else
		{
			if (bUseSmallChange)
			{
				stepDelta = SmallChange;
			}
			else
			{
				stepDelta = LargeChange;
			}
		}

		value = Value;

		// At the max end of the Slider, subtracting stepDelta and then rounding to the closest step may cause the
		// last stepDelta multiple to be skipped.  For example, the last tick mark may be skipped if TickFrequency
		// is not a factor of Maximum - Minimum.  To avoid this, we detect the condition where we are at the end of
		// the Slider and then go to the last stepDelta multiple before the end of the Slider.
		max = Maximum;
		if (!bForward &&
			DoubleUtil.AreClose(value, max) &&
			!DoubleUtil.AreClose(DoubleUtil.Fractional(value / stepDelta), 0))
		{
			closestStep = DoubleUtil.Floor(value / stepDelta) * stepDelta;
		}
		else
		{
			newValue = bForward ? value + stepDelta : value - stepDelta;
			closestStep = GetClosestStep(stepDelta, newValue);
		}
		Value = closestStep;
	}

	// Find the closest step position from fromValue.  stepValue lets you specify the step interval.
	private double GetClosestStep(double stepDelta, double fromValue)
	{
		double numSteps = 0;
		double nextStep = 0;
		double prevStep = 0;

		var value = 0.0;

		var min = Minimum;
		var max = Maximum;

		numSteps = fromValue / stepDelta;
		nextStep = DoubleUtil.Min(max, DoubleUtil.Ceil(numSteps) * stepDelta);
		prevStep = DoubleUtil.Max(min, DoubleUtil.Floor(numSteps) * stepDelta);
		if (DoubleUtil.LessThan(nextStep - fromValue, fromValue - prevStep))
		{
			value = nextStep;
		}
		else
		{
			value = prevStep;
		}

		return value;
	}

	internal void UpdateThumbToolTipVisibility(
		bool isVisible,
		AutomaticToolTipInputMode mode = AutomaticToolTipInputMode.None)
	{
		var currentThumbToolTip = GetCurrentThumbToolTip();

		if (currentThumbToolTip != null)
		{
			// Set IsOpen.
			currentThumbToolTip.RemoveAutomaticStatusFromOpenToolTip();
			if (isVisible)
			{
				currentThumbToolTip.m_inputMode = mode;
			}
			currentThumbToolTip.IsOpen = isVisible;
		}
	}

	internal class DefaultDisambiguationUIConverter : IValueConverter
	{
		//  Convert from source to target
		//
		//  Converts the Slider's double Value to an HSTRING we can display in the
		//  disambiguation UI tooltip's TextBlock.Text property.
		//  Uses a weak reference to the Slider as a ConverterParameter.  We get the
		//  Slider's StepFrequency and determine the number of significant digits in its
		//  mantissa.  We will display the same number of significant digits in the mantissa
		//  of the disambiguation UI's value.  We round to the final significant digit.
		//
		//  We choose to display a maximum of 4 significant digits in our formatted string.
		//
		//  E.G. If StepFrequency==0.1 and Value==0.57, the disambiguation UI shows 0.6
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			double epsilon = 0.00001;   // We cap at 4 digits.
			int numPlacesPastDecimalPoint = 0;
			string szFormat = null;
			double roundedValue = 0;

			double originalValue = (double)value;

			Slider spSlider = null;
			if ((parameter as WeakReference<Slider>)?.TryGetTarget(out spSlider) == true)
			{
				var stepFrequency = spSlider.StepFrequency;

				// Determine the number of digits after the decimal point specified in step frequency.
				// We cap at 4 digits.
				while (DoubleUtil.Fractional(stepFrequency) > epsilon ||
					DoubleUtil.Fractional(stepFrequency) < -epsilon)
				{
					++numPlacesPastDecimalPoint;
					if (numPlacesPastDecimalPoint == 4)
						break;
					stepFrequency *= 10;
				}

				// Uno specific: Using C# formatting rules here (original code uses C++)
				switch (numPlacesPastDecimalPoint)
				{
					case 0:
						szFormat = "{0:0}";
						break;
					case 1:
						szFormat = "{0:0.0}";
						break;
					case 2:
						szFormat = "{0:0.00}";
						break;
					case 3:
						szFormat = "{0:0.000}";
						break;
					default:
						szFormat = "{0:0.0000}";
						break;
				}
			}
			else
			{
				// This should not be a legit case, since we should always be able to resolve the weak reference to the Slider.
				// If it cannot resolve e.g. because we are shutting down, we'll just display the nearest INT.
				szFormat = "{0:0}";
			}

			roundedValue = DoubleUtil.Round(originalValue, numPlacesPastDecimalPoint);

			return string.Format(CultureInfo.CurrentCulture, szFormat, roundedValue);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}

	// Helper function to translate the thumb based on an input point.
	// Calculates what percentage of the track the point represents, and sets the Slider's
	// IntermediateValue and Value accordingly.
	private void MoveThumbToPoint(Point point)
	{
		try
		{
			Grid rootGrid;
			Orientation orientation = Orientation.Horizontal;
			double trackActualLength = 0;
			double thumbLength = 0;
			double trackClickableRegionLength = 0;
			double clickDelta = 0;
			double clickPercentage = 0;
			bool isDirectionReversed = false;
			double max = 0;
			double min = 0;
			double intermediateValue = 0;
			double value = 0;
			SliderSnapsTo snapsTo = SliderSnapsTo.StepValues;
			double tickFrequency = 0;
			double stepFrequency = 0;
			double closestStep = 0;

			_processingInputEvent = true;

			rootGrid = GetRootGrid();
			if (rootGrid != null)
			{
				Point transformedPoint = Point.Zero;
				GeneralTransform transformRoot;
				GeneralTransform transformFromRoot;

				// Determine the length of the track, in pixels.
				orientation = Orientation;
				if (orientation == Orientation.Horizontal)
				{
					trackActualLength = rootGrid.ActualWidth;
				}
				else
				{
					trackActualLength = rootGrid.ActualHeight;
				}
				thumbLength = GetThumbLength();

				// When the thumb is at the far left, the midpoint of the Thumb is still at thumbLength/2 offset.
				// So, the clickable region of the track is different from the actual length of the track.
				trackClickableRegionLength = DoubleUtil.Max(trackActualLength - thumbLength, 1);

				transformRoot = rootGrid.TransformToVisual(null);
				transformFromRoot = transformRoot.Inverse;
				transformedPoint = transformFromRoot.TransformPoint(point);

				// Determine what percentage of the track is contained in the input point.
				clickDelta = orientation == Orientation.Horizontal ?
					transformedPoint.X : trackActualLength - transformedPoint.Y;
				clickPercentage = (clickDelta - thumbLength / 2) / trackClickableRegionLength;
				clickPercentage = DoubleUtil.Max(clickPercentage, 0);
				clickPercentage = DoubleUtil.Min(clickPercentage, 1.0);

				isDirectionReversed = IsDirectionReversed;
				if (isDirectionReversed)
				{
					clickPercentage = 1 - clickPercentage;
				}

				// Calculate and set intermediateValue as a function of clickPercentage, min, and max.
				max = Maximum;
				min = Minimum;
				intermediateValue = min + clickPercentage * (max - min);
				IntermediateValue = intermediateValue;

				// Set value to the nearest step.
				value = Value;
				snapsTo = SnapsTo;
				if (snapsTo == SliderSnapsTo.Ticks)
				{
					tickFrequency = TickFrequency;
					closestStep = GetClosestStep(tickFrequency, intermediateValue);
				}
				else
				{
					stepFrequency = this.StepFrequency;
					closestStep = GetClosestStep(stepFrequency, intermediateValue);
				}

				if (!DoubleUtil.AreClose(value, closestStep))
				{
					Value = closestStep;
				}
			}
		}
		finally
		{
			_processingInputEvent = false;
		}
	}

	// Helper function to set a default ToolTip on the Slider Thumb.
	//
	// Slider has a "Disambiguation UI" feature that displays the Value of the Slider in a ToolTip centered
	// on the Thumb.  Currently, this ToolTip is created in code if the Thumb template part's ToolTip is null.
	private void SetDefaultThumbToolTip(Orientation orientation)
	{
#if HAS_UNO
		if (!FeatureConfiguration.ToolTip.UseToolTips)
		{
			return;
		}
#endif
		object spSliderValueWeakReference;    // to be used as ConverterParameter
		PlacementMode placementMode = PlacementMode.Top;
		bool bIsThumbToolTipEnabled = false;
		Thickness padding = new Thickness(SLIDER_TOOLTIP_PADDING_LEFT, SLIDER_TOOLTIP_PADDING_TOP, SLIDER_TOOLTIP_PADDING_RIGHT, SLIDER_TOOLTIP_PADDING_BOTTOM);

		spSliderValueWeakReference = new WeakReference<Slider>(this);

		// Use Slider.ThumbToolTipValueConverter if it is specified.  Otherwise, create and use a DefaultDisambiguationUIConverter.
		var converter = ThumbToolTipValueConverter;
		if (converter == null)
		{
			var spNewConverter = new DefaultDisambiguationUIConverter();
			converter = spNewConverter;
		}

		var textBlock = new TextBlock();

		var fontWeight = FontWeights.Normal;
		textBlock.FontWeight = fontWeight;
		textBlock.FontSize = SLIDER_TOOLTIP_DEFAULT_FONT_SIZE;

		var textBinding = new Binding();

		// TODO Uno specific: The original code magically binds to Value property of the ConverterParameter.
		// I was unable to figure out how, so this solution just explicitly binds to "Value" property here.
		textBinding.Source = this;
		textBinding.Path = new PropertyPath("Value");
		textBinding.Mode = BindingMode.OneWay;
		textBinding.Converter = converter;
		textBinding.ConverterParameter = spSliderValueWeakReference;

		textBlock.SetBinding(
			TextBlock.TextProperty,
			textBinding);

		ToolTip toolTip = new ToolTip();

		toolTip.Padding = padding;

		toolTip.Content = textBlock;

		// For horizontal Slider, we place the disambiguation UI ToolTip on the side opposite of the user's handedness.
		if (orientation == Orientation.Vertical)
		{
			placementMode = ToolTipPositioning.IsLefthandedUser() ?
				PlacementMode.Right :
				PlacementMode.Left;
		}
		toolTip.Placement = placementMode;

		bIsThumbToolTipEnabled = IsThumbToolTipEnabled;
		toolTip.IsEnabled = bIsThumbToolTipEnabled;

		toolTip.m_isSliderThumbToolTip = true;
		if (orientation == Orientation.Horizontal)
		{
			toolTip.SetAnchor(_tpElementHorizontalThumb);
			ToolTipService.SetToolTipReference(_tpElementHorizontalThumb, toolTip);
		}
		else
		{
			toolTip.SetAnchor(_tpElementVerticalThumb);
			ToolTipService.SetToolTipReference(_tpElementVerticalThumb, toolTip);
		}
	}

	// Called when IsThumbToolTipEnabled changes.
	private void OnIsThumbToolTipEnabledChanged()
	{
#if HAS_UNO
		if (!FeatureConfiguration.ToolTip.UseToolTips)
		{
			return;
		}
#endif
		bool isThumbToolTipEnabled = IsThumbToolTipEnabled;

		if (_tpElementHorizontalThumb != null)
		{
			var horizontalThumbToolTip = ToolTipService.GetToolTipReference(_tpElementHorizontalThumb);
			if (horizontalThumbToolTip != null)
			{
				horizontalThumbToolTip.IsEnabled = isThumbToolTipEnabled;
			}
		}

		if (_tpElementVerticalThumb != null)
		{
			var verticalThumbToolTip = ToolTipService.GetToolTipReference(_tpElementVerticalThumb);
			if (verticalThumbToolTip != null)
			{
				verticalThumbToolTip.IsEnabled = isThumbToolTipEnabled;
			}
		}
	}

	// Called when ThumbToolTipValueConverter changes.
	private void OnThumbToolTipValueConverterChanged()
	{
		if (_usingDefaultToolTipForHorizontalThumb)
		{
			SetDefaultThumbToolTip(Orientation.Horizontal);
		}

		if (_usingDefaultToolTipForVerticalThumb)
		{
			SetDefaultThumbToolTip(Orientation.Vertical);
		}
	}

	// Sets the _isPressed flag, and updates visual state if the flag has changed.
	internal void SetIsPressed(bool isPressed)
	{
		if (isPressed != _isPressed)
		{
			_isPressed = isPressed;
			UpdateVisualState();
		}
	}

	// Gets the Horizontal or Vertical root Grid, depending on the current Orientation.
	private Grid GetRootGrid()
	{
		Grid rootGrid = null;

		var orientation = Orientation;

		if (orientation == Orientation.Horizontal)
		{
			rootGrid = _tpElementHorizontalTemplate as Grid;
		}
		else
		{
			rootGrid = _tpElementVerticalTemplate as Grid;
		}

		return rootGrid;
	}

	// Get the disambiguation UI ToolTip for the current Thumb, depending on the orientation.
	private ToolTip GetCurrentThumbToolTip()
	{
		ToolTip thumbToolTip = null;

		var currentThumb = Thumb;
		if (currentThumb != null)
		{
			// Get the ToolTip.
			var toolTip = ToolTipService.GetToolTipReference(currentThumb);

			thumbToolTip = toolTip;
		}

		return thumbToolTip;
	}

	// Update the visibility of TickBars in the template when the TickPlacement property changes.
	private void OnTickPlacementChanged()
	{
		Visibility topAndLeftVisibility = Visibility.Collapsed;
		Visibility inlineVisibility = Visibility.Collapsed;
		Visibility bottomAndRightVisibility = Visibility.Collapsed;

		var tickPlacement = TickPlacement;
		switch (tickPlacement)
		{
			case Primitives.TickPlacement.TopLeft:
				topAndLeftVisibility = Visibility.Visible;
				break;
			case Primitives.TickPlacement.BottomRight:
				bottomAndRightVisibility = Visibility.Visible;
				break;
			case Primitives.TickPlacement.Outside:
				topAndLeftVisibility = Visibility.Visible;
				bottomAndRightVisibility = Visibility.Visible;
				break;
			case Primitives.TickPlacement.Inline:
				inlineVisibility = Visibility.Visible;
				break;
		}

		if (_tpElementTopTickBar != null)
		{
			_tpElementTopTickBar.Visibility = topAndLeftVisibility;
		}
		if (_tpElementHorizontalInlineTickBar != null)
		{
			_tpElementHorizontalInlineTickBar.Visibility = inlineVisibility;
		}
		if (_tpElementBottomTickBar != null)
		{
			_tpElementBottomTickBar.Visibility = bottomAndRightVisibility;
		}
		if (_tpElementLeftTickBar != null)
		{
			_tpElementLeftTickBar.Visibility = topAndLeftVisibility;
		}
		if (_tpElementVerticalInlineTickBar != null)
		{
			_tpElementVerticalInlineTickBar.Visibility = inlineVisibility;
		}
		if (_tpElementRightTickBar != null)
		{
			_tpElementRightTickBar.Visibility = bottomAndRightVisibility;
		}
	}

	// Updates the visibility of the Header ContentPresenter
	private void UpdateHeaderPresenterVisibility()
	{
		var headerTemplate = HeaderTemplate;
		var header = Header;

		ConditionallyGetTemplatePartAndUpdateVisibility(
			"HeaderContentPresenter",
			(header != null || headerTemplate != null),
			ref _tpHeaderPresenter);
	}

	private void OnIsFocusEngagedChanged()
	{
		UpdateVisualState();

		bool isFocusEngaged = IsFocusEngaged;

		UpdateThumbToolTipVisibility(isFocusEngaged, AutomaticToolTipInputMode.Keyboard);
	}

	private double LayoutRoundedDimension(double dimension)
	{
		var useLayoutRounding = UseLayoutRounding;
		if (useLayoutRounding)
		{
			dimension = LayoutRound(dimension);
		}

		return dimension;
	}

	private float RoundingStep()
	{
		float scale = (float)RootScale.GetRasterizationScaleForElement(this);
		float roundingStep = 1.0f / scale;
		return roundingStep;
	}
}
