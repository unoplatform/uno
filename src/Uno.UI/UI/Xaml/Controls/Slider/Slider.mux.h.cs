// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference Slider_Partial.h

using Uno.Disposables;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls;

public partial class Slider
{
	private const double SLIDER_DEFAULT_MAXIMUM = 100;
	private const double SLIDER_DEFAULT_SMALL_CHANGE = 1;
	private const double SLIDER_DEFAULT_LARGE_CHANGE = 10;
	private const int SLIDER_TOOLTIP_DEFAULT_FONT_SIZE = 15;
	private const float SLIDER_TOOLTIP_PADDING_LEFT = 8.0f;
	private const float SLIDER_TOOLTIP_PADDING_TOP = 3.0f;
	private const float SLIDER_TOOLTIP_PADDING_RIGHT = 8.0f;
	private const float SLIDER_TOOLTIP_PADDING_BOTTOM = 5.0f;

	// Header template part
	private UIElement _tpHeaderPresenter;

	// Horizontal template root
	private FrameworkElement _tpElementHorizontalTemplate;

	// Top TickBar for Horizontal Slider
	private TickBar _tpElementTopTickBar;

	// Inline TickBar for Horizontal Slider
	private TickBar _tpElementHorizontalInlineTickBar;

	// Bottom TickBar for Horizontal Slider
	private TickBar _tpElementBottomTickBar;

	// Left TickBar for Vertical Slider
	private TickBar _tpElementLeftTickBar;

	// Inline TickBar for Vertical Slider
	private TickBar _tpElementVerticalInlineTickBar;

	// Right TickBar for Vertical Slider
	private TickBar _tpElementRightTickBar;

	// Horizontal decrease Rectangle
	private Rectangle _tpElementHorizontalDecreaseRect;

	// Thumb for dragging track
	private Thumb _tpElementHorizontalThumb;

	// Vertical template root
	private FrameworkElement _tpElementVerticalTemplate;

	// Vertical decrease Rectangle
	private Rectangle _tpElementVerticalDecreaseRect;

	// Thumb for dragging track
	private Thumb _tpElementVerticalThumb;

	// Container for the horizontal and vertical slider template parts
	private FrameworkElement _tpSliderContainer;

	// Event registration tokens for events attached to template parts
	private readonly SerialDisposable _elementHorizontalThumbDragStartedToken = new SerialDisposable();
	private readonly SerialDisposable _elementHorizontalThumbDragDeltaToken = new SerialDisposable();
	private readonly SerialDisposable _elementHorizontalThumbDragCompletedToken = new SerialDisposable();
	private readonly SerialDisposable _elementHorizontalThumbSizeChangedToken = new SerialDisposable();
	private readonly SerialDisposable _elementVerticalThumbDragStartedToken = new SerialDisposable();
	private readonly SerialDisposable _elementVerticalThumbDragDeltaToken = new SerialDisposable();
	private readonly SerialDisposable _elementVerticalThumbDragCompletedToken = new SerialDisposable();
	private readonly SerialDisposable _elementVerticalThumbSizeChangedToken = new SerialDisposable();

	// Whether the pointer is currently over the control
	private bool _isPointerOver;

	// A flag indicating whether we are in the midst of processing an input event.  Input events on Slider
	// like DragDelta and MouseLeftButtonDown cause IntermediateValue to change, and sometimes also causes
	// Value to change.  When value changes, it normally updates IntermediateValue as well.  This flag prevents
	// IntermediateValue from being stomped on when Value is being updated while processing a DragDelta event.
	private bool _processingInputEvent;

	// Accumulates drag offsets in case the pointer drags off the end of
	// the track.
	private double _dragValue;

	// Flag indicating whether we are currently pressed.
	//
	// Note that Thumb handles PointerPressed and takes care of the case where the user drags the
	// thumb. However, Slider is responsible for handling the case where the user clicks and drags on some
	// part of the track other than the Thumb.  In this case, the Thumb should jump to the click location
	// and follow the pointer until the user releases it.
	//
	// Also note that Thumb handles drag a bit differently than the Slider track. The Thumb remembers the click
	// offset and keeps itself oriented with this offset from the mouse position while dragging.  E.G. if you
	// click the left side of the Thumb and start dragging, the point of click on the left side of the Thumb
	// will always remain directly under the mouse position.
	// For the Slider track, on the other hand, this behavior is undesirable - we want the Thumb to always line
	// up centered beneath the current pointer position while dragging.
	private bool _isPressed;

	// Flag indicating whether we have created and are using a default Disambiguation UI ToolTip for the Horizontal Thumb.
	private bool _usingDefaultToolTipForHorizontalThumb;

	// Flag indicating whether we have created and are using a default Disambiguation UI ToolTip for the Vertical Thumb.
	private bool _usingDefaultToolTipForVerticalThumb;

	// Tracks the PointerId we have currently captured.  A value of 0 means "none".  We only capture one PointerId at a time;
	// we do not support multitouch.
	private uint _capturedPointerId;

	// Holds the value of the slider at the moment of engagement, used to handle cancel-disengagements where we reset the value.
	private double _preEngagementValue;

	private bool _disengagedWithA;

}
