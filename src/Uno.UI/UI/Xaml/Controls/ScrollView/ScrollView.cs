// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using Uno;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;

#if HAS_UNO_WINUI
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

#if !__SKIA__
[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "__WASM__")]
#endif
public partial class ScrollView : Control, IScrollView
{
	// Change to 'true' to turn on debugging outputs in Output window
	//bool ScrollViewTrace::s_IsDebugOutputEnabled{ false };
	//bool ScrollViewTrace::s_IsVerboseDebugOutputEnabled{ false };

	private const int s_noOpCorrelationId = -1;

	public ScrollView()
	{
		//SCROLLVIEW_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		//__RP_Marker_ClassById(RuntimeProfiler::ProfId_ScrollView);

		//EnsureProperties();
		this.SetDefaultStyleKey();
		HookUISettingsEvent();
		HookScrollViewEvents();
	}

	~ScrollView()
	{
		//SCROLLVIEW_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		UnhookHorizontalScrollControllerEvents(true /*isForDestructor*/);
		UnhookVerticalScrollControllerEvents(true /*isForDestructor*/);
		UnhookCompositionTargetRendering();
		UnhookScrollPresenterEvents(true /*isForDestructor*/);
		UnhookScrollViewEvents();
		ResetHideIndicatorsTimer(true /*isForDestructor*/);
	}

	#region IScrollView

	public UIElement CurrentAnchor
	{
		get
		{
			if (m_scrollPresenter is IScrollAnchorProvider scrollPresenterAsAnchorProvider)
			{
				return scrollPresenterAsAnchorProvider.CurrentAnchor;
			}

			return null;
		}
	}

	public ScrollPresenter ScrollPresenter
		=> m_scrollPresenter as ScrollPresenter;

	public CompositionPropertySet ExpressionAnimationSources
		=> m_scrollPresenter?.ExpressionAnimationSources;

	public double HorizontalOffset
		=> m_scrollPresenter?.HorizontalOffset ?? 0;

	public double VerticalOffset
		=> m_scrollPresenter?.VerticalOffset ?? 0;

	public float ZoomFactor
		=> m_scrollPresenter?.ZoomFactor ?? 0;

	public double ExtentWidth
		=> m_scrollPresenter?.ExtentWidth ?? 0;

	public double ExtentHeight
		=> m_scrollPresenter?.ExtentHeight ?? 0;

	public double ViewportWidth
		=> m_scrollPresenter?.ViewportWidth ?? 0;

	public double ViewportHeight
		=> m_scrollPresenter?.ViewportHeight ?? 0;

	public double ScrollableWidth
		=> m_scrollPresenter?.ScrollableWidth ?? 0;

	public double ScrollableHeight
		=> m_scrollPresenter?.ScrollableHeight ?? 0;

	public ScrollingInteractionState State
		=> m_scrollPresenter?.State ?? ScrollingInteractionState.Idle;

	public ScrollingInputKinds IgnoredInputKinds
	{
		get
		{
			// Workaround for Bug 17377013: XamlCompiler codegen for Enum CreateFromString always returns boxed int which is wrong for [flags] enums (should be uint)
			// Check if the boxed IgnoredInputKinds is an IReference<int> first in which case we unbox as int.
			var boxedKind = GetValue(IgnoredInputKindsProperty);
			if (boxedKind is int boxedInt)
			{
				return (ScrollingInputKinds)((uint)boxedInt);
			}

			return (ScrollingInputKinds)boxedKind;
		}
		set
		{
			//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::InputKindToString(value).c_str());
			SetValue(IgnoredInputKindsProperty, value);
		}
	}

	public void RegisterAnchorCandidate(UIElement element)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, element);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			if (scrollPresenter is IScrollAnchorProvider scrollPresenterAsAnchorProvider)
			{
				scrollPresenterAsAnchorProvider.RegisterAnchorCandidate(element);
				return;
			}
			throw new InvalidOperationException(s_IScrollAnchorProviderNotImpl);
		}
		throw new InvalidOperationException(s_noScrollPresenterPart);
	}

	public void UnregisterAnchorCandidate(UIElement element)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, element);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			if (scrollPresenter is IScrollAnchorProvider scrollPresenterAsAnchorProvider)
			{
				scrollPresenterAsAnchorProvider.UnregisterAnchorCandidate(element);
				return;
			}
			throw new InvalidOperationException(s_IScrollAnchorProviderNotImpl);
		}
		throw new InvalidOperationException(s_noScrollPresenterPart);
	}


	public int ScrollTo(double horizontalOffset, double verticalOffset)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalOffset, verticalOffset);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ScrollTo(horizontalOffset, verticalOffset);
		}

		return s_noOpCorrelationId;
	}

	public int ScrollTo(double horizontalOffset, double verticalOffset, ScrollingScrollOptions options)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL_STR, METH_NAME, this,
		//	horizontalOffset, verticalOffset, TypeLogging::ScrollOptionsToString(options).c_str());

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ScrollTo(horizontalOffset, verticalOffset, options);
		}

		return s_noOpCorrelationId;
	}

	public int ScrollBy(double horizontalOffsetDelta, double verticalOffsetDelta)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalOffsetDelta, verticalOffsetDelta);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ScrollBy(horizontalOffsetDelta, verticalOffsetDelta);
		}

		return s_noOpCorrelationId;
	}

	public int ScrollBy(double horizontalOffsetDelta, double verticalOffsetDelta, ScrollingScrollOptions options)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL_STR, METH_NAME, this,
		//	horizontalOffsetDelta, verticalOffsetDelta, TypeLogging::ScrollOptionsToString(options).c_str());

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ScrollBy(horizontalOffsetDelta, verticalOffsetDelta, options);
		}

		return s_noOpCorrelationId;
	}

	public int AddScrollVelocity(Vector2 offsetsVelocity, Vector2? inertiaDecayRate)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		//	TypeLogging::Float2ToString(offsetsVelocity).c_str(), TypeLogging::NullableFloat2ToString(inertiaDecayRate).c_str());

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.AddScrollVelocity(offsetsVelocity, inertiaDecayRate);
		}

		return s_noOpCorrelationId;
	}

	public int ZoomTo(float zoomFactor, Vector2? centerPoint)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		//	TypeLogging::NullableFloat2ToString(centerPoint).c_str(), zoomFactor);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ZoomTo(zoomFactor, centerPoint);
		}

		return s_noOpCorrelationId;
	}

	public int ZoomTo(float zoomFactor, Vector2? centerPoint, ScrollingZoomOptions options)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		//	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		//	TypeLogging::ZoomOptionsToString(options).c_str(),
		//	zoomFactor);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ZoomTo(zoomFactor, centerPoint, options);
		}

		return s_noOpCorrelationId;
	}

	public int ZoomBy(float zoomFactorDelta, Vector2? centerPoint)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		//	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		//	zoomFactorDelta);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ZoomBy(zoomFactorDelta, centerPoint);
		}

		return s_noOpCorrelationId;
	}

	public int ZoomBy(float zoomFactorDelta, Vector2? centerPoint, ScrollingZoomOptions options)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		//	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		//	TypeLogging::ZoomOptionsToString(options).c_str(),
		//	zoomFactorDelta);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.ZoomBy(zoomFactorDelta, centerPoint, options);
		}

		return s_noOpCorrelationId;
	}

	public int AddZoomVelocity(float zoomFactorVelocity, Vector2? centerPoint, float? inertiaDecayRate)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		//	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		//	TypeLogging::NullableFloatToString(inertiaDecayRate).c_str(),
		//	zoomFactorVelocity);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			return scrollPresenter.AddZoomVelocity(zoomFactorVelocity, centerPoint, inertiaDecayRate);
		}

		return s_noOpCorrelationId;
	}

	#endregion

	#region IFrameworkElementOverrides

	protected override void OnApplyTemplate()
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		base.OnApplyTemplate();

		m_hasNoIndicatorStateStoryboardCompletedHandler = false;
		m_keepIndicatorsShowing = false;

		ScrollPresenter scrollPresenter = GetTemplateChild<ScrollPresenter>(s_scrollPresenterPartName);

		UpdateScrollPresenter(scrollPresenter);

		UIElement horizontalScrollControllerElement = GetTemplateChild<UIElement>(s_horizontalScrollBarPartName);
		IScrollController horizontalScrollController = horizontalScrollControllerElement as IScrollController;
		ScrollBar horizontalScrollBar = null;

		if (horizontalScrollControllerElement is not null && horizontalScrollController is null)
		{
			horizontalScrollBar = horizontalScrollControllerElement as ScrollBar;

			if (horizontalScrollBar is not null)
			{
				if (m_horizontalScrollBarController is null)
				{
					m_horizontalScrollBarController = new ScrollBarController();
				}
				horizontalScrollController = m_horizontalScrollBarController as IScrollController;
			}
		}

		if (horizontalScrollBar is not null)
		{
			m_horizontalScrollBarController.SetScrollBar(horizontalScrollBar);
		}
		else
		{
			m_horizontalScrollBarController = null;
		}

		UpdateHorizontalScrollController(horizontalScrollController, horizontalScrollControllerElement);

		UIElement verticalScrollControllerElement = GetTemplateChild<UIElement>(s_verticalScrollBarPartName);
		IScrollController verticalScrollController = verticalScrollControllerElement as IScrollController;
		ScrollBar verticalScrollBar = null;

		if (verticalScrollControllerElement is not null && verticalScrollController is null)
		{
			verticalScrollBar = verticalScrollControllerElement as ScrollBar;

			if (verticalScrollBar is not null)
			{
				if (m_verticalScrollBarController is null)
				{
					m_verticalScrollBarController = new ScrollBarController();
				}
				verticalScrollController = m_verticalScrollBarController as IScrollController;
			}
		}

		if (verticalScrollBar is not null)
		{
			m_verticalScrollBarController.SetScrollBar(verticalScrollBar);
		}
		else
		{
			m_verticalScrollBarController = null;
		}

		UpdateVerticalScrollController(verticalScrollController, verticalScrollControllerElement);

		UIElement scrollControllersSeparator = GetTemplateChild<UIElement>(s_scrollBarsSeparatorPartName);

		UpdateScrollControllersSeparator(scrollControllersSeparator);

		UpdateScrollControllersVisibility(true /*horizontalChange*/, true /*verticalChange*/);

		FrameworkElement root = GetTemplateChild<FrameworkElement>(s_rootPartName);

		if (root is not null)
		{
			IList<VisualStateGroup> rootVisualStateGroups = VisualStateManager.GetVisualStateGroups(root);

			if (rootVisualStateGroups is not null)
			{
				int groupCount = rootVisualStateGroups.Count;

				for (int groupIndex = 0; groupIndex < groupCount; ++groupIndex)
				{
					VisualStateGroup group = rootVisualStateGroups[groupIndex];

					if (group is not null)
					{
						IList<VisualState> visualStates = group.States;

						if (visualStates is not null)
						{
							int stateCount = visualStates.Count;

							for (int stateIndex = 0; stateIndex < stateCount; ++stateIndex)
							{
								VisualState state = visualStates[stateIndex];

								if (state is not null)
								{
									var stateName = state.Name;
									Storyboard stateStoryboard = state.Storyboard;

									if (stateStoryboard is not null)
									{
										if (stateName == s_noIndicatorStateName)
										{
											stateStoryboard.Completed += OnNoIndicatorStateStoryboardCompleted;
											m_hasNoIndicatorStateStoryboardCompletedHandler = true;
										}
										else if (stateName == s_touchIndicatorStateName || stateName == s_mouseIndicatorStateName)
										{
											stateStoryboard.Completed += OnIndicatorStateStoryboardCompleted;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		UpdateVisualStates(false /*useTransitions*/);
	}

	#endregion

	#region IControlOverrides

	protected override void OnGotFocus(RoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		base.OnGotFocus(args);

		m_preferMouseIndicators =
			m_focusInputDeviceKind == FocusInputDeviceKind.Mouse ||
			m_focusInputDeviceKind == FocusInputDeviceKind.Pen;

		UpdateVisualStates(
			true  /*useTransitions*/,
			true  /*showIndicators*/,
			false /*hideIndicators*/,
			false /*scrollControllersAutoHidingChanged*/,
			true  /*updateScrollControllersAutoHiding*/,
			true  /*onlyForAutoHidingScrollControllers*/);
	}

	#endregion

	private void OnScrollViewGettingFocus(
		object sender,
		GettingFocusEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_focusInputDeviceKind = args.InputDevice;
	}

	private void OnScrollViewIsEnabledChanged(
		object sender,
		DependencyPropertyChangedEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UpdateVisualStates(
			true  /*useTransitions*/,
			false /*showIndicators*/,
			false /*hideIndicators*/,
			false /*scrollControllersAutoHidingChanged*/,
			true  /*updateScrollControllersAutoHiding*/);
	}

	private void OnScrollViewUnloaded(
		object sender,
		RoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_showingMouseIndicators = false;
		m_keepIndicatorsShowing = false;
		m_bringIntoViewOperations.Clear();

		UnhookCompositionTargetRendering();
		ResetHideIndicatorsTimer();
	}

	private void OnScrollViewPointerEntered(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		if (args.Pointer.PointerDeviceType != PointerDeviceType.Touch)
		{
			// Mouse/Pen inputs dominate. If touch panning indicators are shown, switch to mouse indicators.
			m_preferMouseIndicators = true;

			UpdateVisualStates(
				true  /*useTransitions*/,
				true  /*showIndicators*/,
				false /*hideIndicators*/,
				false /*scrollControllersAutoHidingChanged*/,
				true  /*updateScrollControllersAutoHiding*/,
				true  /*onlyForAutoHidingScrollControllers*/);
		}
	}

	private void OnScrollViewPointerMoved(
		object sender,
		PointerRoutedEventArgs args)
	{
		// Don't process if this is a generated replay of the event.
		// UNO docs: Generated events are not yet supported. This condition is always false.
		if (args.IsGenerated)
		{
			return;
		}

		if (args.Pointer.PointerDeviceType != PointerDeviceType.Touch)
		{
			// Mouse/Pen inputs dominate. If touch panning indicators are shown, switch to mouse indicators.
			m_preferMouseIndicators = true;

			UpdateVisualStates(
				true  /*useTransitions*/,
				true  /*showIndicators*/,
				false /*hideIndicators*/,
				false /*scrollControllersAutoHidingChanged*/,
				false /*updateScrollControllersAutoHiding*/,
				true  /*onlyForAutoHidingScrollControllers*/);

			if (AreScrollControllersAutoHiding() &&
				!SharedHelpers.IsAnimationsEnabled() &&
				m_hideIndicatorsTimer is not null &&
				(m_isPointerOverHorizontalScrollController || m_isPointerOverVerticalScrollController))
			{
				ResetHideIndicatorsTimer();
			}
		}
	}

	private void OnScrollViewPointerExited(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		if (args.Pointer.PointerDeviceType != PointerDeviceType.Touch)
		{
			// Mouse/Pen inputs dominate. If touch panning indicators are shown, switch to mouse indicators.
			m_isPointerOverHorizontalScrollController = false;
			m_isPointerOverVerticalScrollController = false;
			m_preferMouseIndicators = true;

			UpdateVisualStates(
				true  /*useTransitions*/,
				true  /*showIndicators*/,
				false /*hideIndicators*/,
				false /*scrollControllersAutoHidingChanged*/,
				true  /*updateScrollControllersAutoHiding*/,
				true  /*onlyForAutoHidingScrollControllers*/);

			if (AreScrollControllersAutoHiding())
			{
				HideIndicatorsAfterDelay();
			}
		}
	}

	private void OnScrollViewPointerPressed(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		if (args.Handled)
		{
			return;
		}

		if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
		{
			var pointerPoint = args.GetCurrentPoint(null);
			var pointerPointProperties = pointerPoint.Properties;

			m_isLeftMouseButtonPressedForFocus = pointerPointProperties.IsLeftButtonPressed;
		}

		// Show the scroll controller indicators as soon as a pointer is pressed on the ScrollView.
		UpdateVisualStates(
			true  /*useTransitions*/,
			true  /*showIndicators*/,
			false /*hideIndicators*/,
			false /*scrollControllersAutoHidingChanged*/,
			true  /*updateScrollControllersAutoHiding*/,
			true  /*onlyForAutoHidingScrollControllers*/);
	}

	private void OnScrollViewPointerReleased(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		bool takeFocus = false;

		if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse && m_isLeftMouseButtonPressedForFocus)
		{
			m_isLeftMouseButtonPressedForFocus = false;
			takeFocus = true;
		}

		if (args.Handled)
		{
			return;
		}

		if (takeFocus)
		{
			//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_METH, METH_NAME, this, L"Focus");

			bool tookFocus = Focus(FocusState.Pointer);
			args.Handled = tookFocus;
		}
	}

	private void OnScrollViewPointerCanceled(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
		{
			m_isLeftMouseButtonPressedForFocus = false;
		}
	}

	private void OnHorizontalScrollControllerPointerEntered(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		HandleScrollControllerPointerEntered(true /*isForHorizontalScrollController*/);
	}

	private void OnHorizontalScrollControllerPointerExited(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		HandleScrollControllerPointerExited(true /*isForHorizontalScrollController*/);
	}

	private void OnVerticalScrollControllerPointerEntered(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		HandleScrollControllerPointerEntered(false /*isForHorizontalScrollController*/);
	}

	private void OnVerticalScrollControllerPointerExited(
		object sender,
		PointerRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.Handled, args.Pointer.PointerDeviceType);

		HandleScrollControllerPointerExited(false /*isForHorizontalScrollController*/);
	}

	// Handler for when the NoIndicator state's storyboard completes animating.
	private void OnNoIndicatorStateStoryboardCompleted(
		object sender,
		object args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(m_hasNoIndicatorStateStoryboardCompletedHandler);

		m_showingMouseIndicators = false;
	}

	// Handler for when a TouchIndicator or MouseIndicator state's storyboard completes animating.
	private void OnIndicatorStateStoryboardCompleted(
		object sender,
		object args)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		// If the cursor is currently directly over either scroll controller then do not automatically hide the indicators
		if (AreScrollControllersAutoHiding() &&
			!m_keepIndicatorsShowing &&
			!m_isPointerOverVerticalScrollController &&
			!m_isPointerOverHorizontalScrollController)
		{
			UpdateScrollControllersVisualState(true /*useTransitions*/, false /*showIndicators*/, true /*hideIndicators*/);
		}
	}

	// Invoked by ScrollViewTestHooks
	internal void ScrollControllersAutoHidingChanged()
	{
		UpdateScrollControllersAutoHiding(true /*forceUpdate*/);
	}

	internal ScrollPresenter GetScrollPresenterPart()
	{
		return m_scrollPresenter as ScrollPresenter;
	}

	internal void ValidateAnchorRatio(double value)
	{
		ScrollPresenter.ValidateAnchorRatio(value);
	}

	internal void ValidateZoomFactoryBoundary(double value)
	{
		ScrollPresenter.ValidateZoomFactoryBoundary(value);
	}

	// Invoked when a dependency property of this ScrollView has changed.
	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var dependencyProperty = args.Property;

#if DEBUG
		//SCROLLVIEW_TRACE_VERBOSE(null, L"%s(property: %s)\n", METH_NAME, DependencyPropertyToString(dependencyProperty).c_str());
#endif

		bool horizontalChange = dependencyProperty == HorizontalScrollBarVisibilityProperty;
		bool verticalChange = dependencyProperty == VerticalScrollBarVisibilityProperty;

		if (horizontalChange || verticalChange)
		{
			UpdateScrollControllersVisibility(horizontalChange, verticalChange);
			UpdateVisualStates(
				true  /*useTransitions*/,
				false /*showIndicators*/,
				false /*hideIndicators*/,
				false /*scrollControllersAutoHidingChanged*/,
				true  /*updateScrollControllersAutoHiding*/);
		}
	}

	private void OnScrollControllerCanScrollChanged(
		IScrollController sender,
		object args)
	{
		// IScrollController::CanScroll changed and affect the scroll controller's visibility when its visibility mode is Auto.
		if (m_horizontalScrollController is not null && m_horizontalScrollController == sender)
		{
			//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"HorizontalScrollController.CanScroll changed: ", m_horizontalScrollController.get().CanScroll());

			UpdateScrollControllersVisibility(true /*horizontalChange*/, false /*verticalChange*/);
		}
		else if (m_verticalScrollController is not null && m_verticalScrollController == sender)
		{
			//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"VerticalScrollController.CanScroll changed: ", m_verticalScrollController.get().CanScroll());

			UpdateScrollControllersVisibility(false /*horizontalChange*/, true /*verticalChange*/);
		}
	}

	private void OnScrollControllerIsScrollingWithMouseChanged(
		IScrollController sender,
		object args)
	{
		bool isScrollControllerScrollingWithMouse = sender.IsScrollingWithMouse;
		bool showIndicators = false;
		bool hideIndicators = false;

		if (m_horizontalScrollController is not null && m_horizontalScrollController == sender)
		{
			UpdateScrollControllersAutoHiding();

			if (m_isHorizontalScrollControllerScrollingWithMouse != isScrollControllerScrollingWithMouse)
			{
				//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT_INT, METH_NAME, this, L"HorizontalScrollController.IsScrollingWithMouse changed: ", m_isHorizontalScrollControllerScrollingWithMouse, isScrollControllerScrollingWithMouse);

				m_isHorizontalScrollControllerScrollingWithMouse = isScrollControllerScrollingWithMouse;

				if (isScrollControllerScrollingWithMouse)
				{
					// Prevent the vertical scroll controller from fading out while the user is scrolling with mouse with the horizontal one.
					m_keepIndicatorsShowing = true;
					showIndicators = true;
				}
				else
				{
					// Make the scroll controllers fade out, after the normal delay, if they are auto-hiding.
					m_keepIndicatorsShowing = false;
					hideIndicators = AreScrollControllersAutoHiding();
				}
			}

			// IScrollController::CanScroll might have changed and affect the scroll controller's visibility
			// when its visibility mode is Auto.
			UpdateScrollControllersVisibility(true /*horizontalChange*/, false /*verticalChange*/);
			UpdateVisualStates(true /*useTransitions*/, showIndicators, hideIndicators);
		}
		else if (m_verticalScrollController is not null && m_verticalScrollController == sender)
		{
			UpdateScrollControllersAutoHiding();

			if (m_isVerticalScrollControllerScrollingWithMouse != isScrollControllerScrollingWithMouse)
			{
				//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT_INT, METH_NAME, this, L"VerticalScrollController.IsScrollingWithMouse changed: ", m_isVerticalScrollControllerScrollingWithMouse, isScrollControllerScrollingWithMouse);

				m_isVerticalScrollControllerScrollingWithMouse = isScrollControllerScrollingWithMouse;

				if (isScrollControllerScrollingWithMouse)
				{
					// Prevent the horizontal scroll controller from fading out while the user is scrolling with mouse with the vertical one.
					m_keepIndicatorsShowing = true;
					showIndicators = true;
				}
				else
				{
					// Make the scroll controllers fade out, after the normal delay, if they are auto-hiding.
					m_keepIndicatorsShowing = false;
					hideIndicators = AreScrollControllersAutoHiding();
				}
			}

			// IScrollController::CanScroll might have changed and affect the scroll controller's visibility
			// when its visibility mode is Auto.
			UpdateScrollControllersVisibility(false /*horizontalChange*/, true /*verticalChange*/);
			UpdateVisualStates(true /*useTransitions*/, showIndicators, hideIndicators);
		}
	}

	private void OnHideIndicatorsTimerTick(
		object sender,
		object args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		ResetHideIndicatorsTimer();

		if (AreScrollControllersAutoHiding())
		{
			HideIndicators();
		}
	}

	// UNO TODO
	//private void OnAutoHideScrollBarsChanged(
	//	UISettings uiSettings,
	//	UISettingsAutoHideScrollBarsChangedEventArgs args)
	//{
	//	// OnAutoHideScrollBarsChanged is called on a non-UI thread, process notification on the UI thread using a dispatcher.
	//	DispatcherQueue.TryEnqueue(() =>
	//	{
	//		this.m_autoHideScrollControllersValid = false;
	//		this.UpdateVisualStates(
	//			true  /*useTransitions*/,
	//			false /*showIndicators*/,
	//			false /*hideIndicators*/,
	//			true  /*scrollControllersAutoHidingChanged*/);
	//	});
	//}

	private void OnScrollPresenterExtentChanged(
		object sender,
		object args)
	{
		if (ExtentChanged is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			ExtentChanged.Invoke(this, args);
		}
	}

	private void OnScrollPresenterStateChanged(
		object sender,
		object args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			if (scrollPresenter.State == ScrollingInteractionState.Interaction)
			{
				m_preferMouseIndicators = false;
			}
		}

		StateChanged?.Invoke(this, args);
	}

	private void OnScrollAnimationStarting(
		object sender,
		ScrollingScrollAnimationStartingEventArgs args)
	{
		if (ScrollAnimationStarting is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			ScrollAnimationStarting.Invoke(this, args);
		}
	}

	private void OnZoomAnimationStarting(
		object sender,
		ScrollingZoomAnimationStartingEventArgs args)
	{
		if (ZoomAnimationStarting is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			ZoomAnimationStarting.Invoke(this, args);
		}
	}

	private void OnScrollPresenterViewChanged(
		object sender,
		object args)
	{
		// Unless the control is still loading, show the scroll controller indicators when the view changes. For example,
		// when using Ctrl+/- to zoom, mouse-wheel to scroll or zoom, or any other input type. Keep the existing indicator type.
		if (IsLoaded)
		{
			UpdateVisualStates(
				true  /*useTransitions*/,
				true  /*showIndicators*/,
				false /*hideIndicators*/,
				false /*scrollControllersAutoHidingChanged*/,
				false /*updateScrollControllersAutoHiding*/,
				true  /*onlyForAutoHidingScrollControllers*/);
		}

		if (ViewChanged is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			ViewChanged.Invoke(this, args);
		}
	}

	private void OnScrollPresenterScrollCompleted(
		object sender,
		ScrollingScrollCompletedEventArgs args)
	{
		if (args.CorrelationId == m_horizontalAddScrollVelocityOffsetChangeCorrelationId)
		{
			m_horizontalAddScrollVelocityOffsetChangeCorrelationId = s_noOpCorrelationId;
		}
		else if (args.CorrelationId == m_verticalAddScrollVelocityOffsetChangeCorrelationId)
		{
			m_verticalAddScrollVelocityOffsetChangeCorrelationId = s_noOpCorrelationId;
		}

		if (ScrollCompleted is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			ScrollCompleted.Invoke(this, args);
		}
	}

	private void OnScrollPresenterZoomCompleted(
		object sender,
		ScrollingZoomCompletedEventArgs args)
	{
		if (ZoomCompleted is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			ZoomCompleted.Invoke(this, args);
		}
	}

	private void OnScrollPresenterBringingIntoView(
		object sender,
		ScrollingBringingIntoViewEventArgs args)
	{
		if (m_bringIntoViewOperations.Count > 0)
		{
			var requestEventArgs = args.RequestEventArgs;

			foreach (var bringIntoViewOperation in m_bringIntoViewOperations)
			{
				if (requestEventArgs.TargetElement == bringIntoViewOperation.TargetElement)
				{
					// This ScrollPresenter::BringingIntoView notification results from a FocusManager::TryFocusAsync call in ScrollView::HandleKeyDownForXYNavigation.
					// Its BringIntoViewRequestedEventArgs::AnimationDesired property is set to True in order to animate to the target element rather than jumping.
					//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_INT, METH_NAME, this, bringIntoViewOperation->TargetElement(), bringIntoViewOperation->TicksCount());

					// We either want to cancel this BringIntoView operation (because we are handling the scrolling ourselves) or we want to force the operation to be animated
					if (bringIntoViewOperation.ShouldCancelBringIntoView())
					{
						args.Cancel = true;
					}
					else
					{
						requestEventArgs.AnimationDesired = true;
					}

					break;
				}
			}
		}

		if (BringingIntoView is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			BringingIntoView.Invoke(this, args);
		}
	}

	private void OnScrollPresenterAnchorRequested(
		object sender,
		ScrollingAnchorRequestedEventArgs args)
	{
		if (AnchorRequested is not null)
		{
			//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			AnchorRequested.Invoke(this, args);
		}
	}

	private void OnCompositionTargetRendering(
		object sender,
		object args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (m_bringIntoViewOperations.Count > 0)
		{
			foreach (var bringIntoViewOperation in m_bringIntoViewOperations)
			{
				//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_INT, METH_NAME, this, bringIntoViewOperation->TargetElement(), bringIntoViewOperation->TicksCount());

				if (bringIntoViewOperation.HasMaxTicksCount)
				{
					// This ScrollView is no longer expected to receive BringingIntoView notifications from its ScrollPresenter,
					// resulting from a FocusManager::TryFocusAsync call in ScrollView::HandleKeyDownForXYNavigation.
					m_bringIntoViewOperations.Remove(bringIntoViewOperation);
				}
				else
				{
					// Increment the number of ticks ellapsed since the FocusManager::TryFocusAsync call, and continue to wait for BringingIntoView notifications.
					bringIntoViewOperation.TickOperation();
				}
			}
		}

		if (m_bringIntoViewOperations.Count == 0)
		{
			UnhookCompositionTargetRendering();
		}
	}

	private void OnScrollPresenterPropertyChanged(
		DependencyObject sender,
		DependencyProperty args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (args == ScrollPresenter.ComputedHorizontalScrollModeProperty)
		{
			SetValue(ComputedHorizontalScrollModeProperty, m_scrollPresenter.ComputedHorizontalScrollMode);
		}
		else if (args == ScrollPresenter.ComputedVerticalScrollModeProperty)
		{
			SetValue(ComputedVerticalScrollModeProperty, m_scrollPresenter.ComputedVerticalScrollMode);
		}
	}

	private void ResetHideIndicatorsTimer(bool isForDestructor = false, bool restart = false)
	{
		// UNO TODO
		var hideIndicatorsTimer = m_hideIndicatorsTimer; //.safe_get(isForDestructor /*useSafeGet*/);

		if (hideIndicatorsTimer is not null && hideIndicatorsTimer.IsEnabled)
		{
			hideIndicatorsTimer.Stop();
			if (restart)
			{
				hideIndicatorsTimer.Start();
			}
		}
	}

	private void HookUISettingsEvent()
	{
		// UNO TODO:
		// Introduced in 19H1, IUISettings5 exposes the AutoHideScrollBars property and AutoHideScrollBarsChanged event.
		// if (!m_uiSettings5)
		// {
		// 	winrt::UISettings uiSettings;

		// 	m_uiSettings5 = uiSettings.try_as<winrt::IUISettings5>();
		// 	if (m_uiSettings5)
		// 	{
		// 		m_autoHideScrollBarsChangedRevoker = m_uiSettings5.AutoHideScrollBarsChanged(
		// 			winrt::auto_revoke,
		// 			OnAutoHideScrollBarsChanged);
		// 	}
		// }		
	}

	private void HookCompositionTargetRendering()
	{
		if (m_renderingToken is null)
		{
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnCompositionTargetRendering;
			m_renderingToken = new SerialDisposable();
			m_renderingToken.Disposable = Disposable.Create(() => Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnCompositionTargetRendering);
		}
	}

	private void UnhookCompositionTargetRendering()
	{
		if (m_renderingToken is not null)
		{
			m_renderingToken.Disposable = null;
			m_renderingToken = null;
		}
	}

	private void HookScrollViewEvents()
	{
		MUX_ASSERT(m_onPointerEnteredEventHandler is null);
		MUX_ASSERT(m_onPointerMovedEventHandler is null);
		MUX_ASSERT(m_onPointerExitedEventHandler is null);
		MUX_ASSERT(m_onPointerPressedEventHandler is null);
		MUX_ASSERT(m_onPointerReleasedEventHandler is null);
		MUX_ASSERT(m_onPointerCanceledEventHandler is null);
		//MUX_ASSERT(m_gettingFocusToken.value == 0);
		//MUX_ASSERT(m_isEnabledChangedToken.value == 0);
		//MUX_ASSERT(m_unloadedToken.value == 0);

		//m_gettingFocusToken = GettingFocus(OnScrollViewGettingFocus);
		//m_isEnabledChangedToken = IsEnabledChanged(OnScrollViewIsEnabledChanged);
		//m_unloadedToken = Unloaded(OnScrollViewUnloaded);
		GettingFocus += OnScrollViewGettingFocus;
		IsEnabledChanged += OnScrollViewIsEnabledChanged;
		Unloaded += OnScrollViewUnloaded;

		m_onPointerEnteredEventHandler = new PointerEventHandler(OnScrollViewPointerEntered);
		AddHandler(UIElement.PointerEnteredEvent, m_onPointerEnteredEventHandler, false);

		m_onPointerMovedEventHandler = new PointerEventHandler(OnScrollViewPointerMoved);
		AddHandler(UIElement.PointerMovedEvent, m_onPointerMovedEventHandler, false);

		m_onPointerExitedEventHandler = new PointerEventHandler(OnScrollViewPointerExited);
		AddHandler(UIElement.PointerExitedEvent, m_onPointerExitedEventHandler, false);

		m_onPointerPressedEventHandler = new PointerEventHandler(OnScrollViewPointerPressed);
		AddHandler(UIElement.PointerPressedEvent, m_onPointerPressedEventHandler, false);

		m_onPointerReleasedEventHandler = new PointerEventHandler(OnScrollViewPointerReleased);
		AddHandler(UIElement.PointerReleasedEvent, m_onPointerReleasedEventHandler, true);

		m_onPointerCanceledEventHandler = new PointerEventHandler(OnScrollViewPointerCanceled);
		AddHandler(UIElement.PointerCanceledEvent, m_onPointerCanceledEventHandler, true);
	}

	private void UnhookScrollViewEvents()
	{
		//if (m_gettingFocusToken.value != 0)
		//{
		//	GettingFocus(m_gettingFocusToken);
		//	m_gettingFocusToken.value = 0;
		//}

		//if (m_isEnabledChangedToken.value != 0)
		//{
		//	IsEnabledChanged(m_isEnabledChangedToken);
		//	m_isEnabledChangedToken.value = 0;
		//}

		//if (m_unloadedToken.value != 0)
		//{
		//	Unloaded(m_unloadedToken);
		//	m_unloadedToken.value = 0;
		//}

		GettingFocus -= OnScrollViewGettingFocus;
		IsEnabledChanged -= OnScrollViewIsEnabledChanged;
		Unloaded -= OnScrollViewUnloaded;

		if (m_onPointerEnteredEventHandler is not null)
		{
			RemoveHandler(UIElement.PointerEnteredEvent, m_onPointerEnteredEventHandler);
			m_onPointerEnteredEventHandler = null;
		}

		if (m_onPointerMovedEventHandler is not null)
		{
			RemoveHandler(UIElement.PointerMovedEvent, m_onPointerMovedEventHandler);
			m_onPointerMovedEventHandler = null;
		}

		if (m_onPointerExitedEventHandler is not null)
		{
			RemoveHandler(UIElement.PointerExitedEvent, m_onPointerExitedEventHandler);
			m_onPointerExitedEventHandler = null;
		}

		if (m_onPointerPressedEventHandler is not null)
		{
			RemoveHandler(UIElement.PointerPressedEvent, m_onPointerPressedEventHandler);
			m_onPointerPressedEventHandler = null;
		}

		if (m_onPointerReleasedEventHandler is not null)
		{
			RemoveHandler(UIElement.PointerReleasedEvent, m_onPointerReleasedEventHandler);
			m_onPointerReleasedEventHandler = null;
		}

		if (m_onPointerCanceledEventHandler is not null)
		{
			RemoveHandler(UIElement.PointerCanceledEvent, m_onPointerCanceledEventHandler);
			m_onPointerCanceledEventHandler = null;
		}
	}

	private void HookScrollPresenterEvents()
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		//MUX_ASSERT(m_scrollPresenterExtentChangedToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterStateChangedToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterScrollAnimationStartingToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterZoomAnimationStartingToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterViewChangedToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterScrollCompletedToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterZoomCompletedToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterBringingIntoViewToken.value == 0);
		//MUX_ASSERT(m_scrollPresenterAnchorRequestedToken.value == 0);
		MUX_ASSERT(m_scrollPresenterComputedHorizontalScrollModeChangedToken == 0);
		MUX_ASSERT(m_scrollPresenterComputedVerticalScrollModeChangedToken == 0);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			//m_scrollPresenterExtentChangedToken = scrollPresenter.ExtentChanged(OnScrollPresenterExtentChanged);
			//m_scrollPresenterStateChangedToken = scrollPresenter.StateChanged(OnScrollPresenterStateChanged);
			//m_scrollPresenterScrollAnimationStartingToken = scrollPresenter.ScrollAnimationStarting(OnScrollAnimationStarting);
			//m_scrollPresenterZoomAnimationStartingToken = scrollPresenter.ZoomAnimationStarting(OnZoomAnimationStarting);
			//m_scrollPresenterViewChangedToken = scrollPresenter.ViewChanged(OnScrollPresenterViewChanged);
			//m_scrollPresenterScrollCompletedToken = scrollPresenter.ScrollCompleted(OnScrollPresenterScrollCompleted);
			//m_scrollPresenterZoomCompletedToken = scrollPresenter.ZoomCompleted(OnScrollPresenterZoomCompleted);
			//m_scrollPresenterBringingIntoViewToken = scrollPresenter.BringingIntoView(OnScrollPresenterBringingIntoView);
			//m_scrollPresenterAnchorRequestedToken = scrollPresenter.AnchorRequested(OnScrollPresenterAnchorRequested);

			scrollPresenter.ExtentChanged += OnScrollPresenterExtentChanged;
			scrollPresenter.StateChanged += OnScrollPresenterStateChanged;
			scrollPresenter.ScrollAnimationStarting += OnScrollAnimationStarting;
			scrollPresenter.ZoomAnimationStarting += OnZoomAnimationStarting;
			scrollPresenter.ViewChanged += OnScrollPresenterViewChanged;
			scrollPresenter.ScrollCompleted += OnScrollPresenterScrollCompleted;
			scrollPresenter.ZoomCompleted += OnScrollPresenterZoomCompleted;
			scrollPresenter.BringingIntoView += OnScrollPresenterBringingIntoView;
			scrollPresenter.AnchorRequested += OnScrollPresenterAnchorRequested;

			DependencyObject scrollPresenterAsDO = scrollPresenter as DependencyObject;

			m_scrollPresenterComputedHorizontalScrollModeChangedToken = scrollPresenterAsDO.RegisterPropertyChangedCallback(
				ScrollPresenter.ComputedHorizontalScrollModeProperty, OnScrollPresenterPropertyChanged);

			m_scrollPresenterComputedVerticalScrollModeChangedToken = scrollPresenterAsDO.RegisterPropertyChangedCallback(
				ScrollPresenter.ComputedVerticalScrollModeProperty, OnScrollPresenterPropertyChanged);
		}
	}

	private void UnhookScrollPresenterEvents(bool isForDestructor)
	{
		//if (isForDestructor)
		//{
		//	SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
		//}
		//else
		//{
		//	SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
		//}

		// UNO TODO:
		// auto scrollPresenter = isForDestructor ? m_scrollPresenter.safe_get() : m_scrollPresenter.get()
		var scrollPresenter = m_scrollPresenter;
		if (scrollPresenter is not null)
		{
			//if (m_scrollPresenterExtentChangedToken.value != 0)
			//{
			//	scrollPresenter.ExtentChanged(m_scrollPresenterExtentChangedToken);
			//	m_scrollPresenterExtentChangedToken.value = 0;
			//}

			//if (m_scrollPresenterStateChangedToken.value != 0)
			//{
			//	scrollPresenter.StateChanged(m_scrollPresenterStateChangedToken);
			//	m_scrollPresenterStateChangedToken.value = 0;
			//}

			//if (m_scrollPresenterScrollAnimationStartingToken.value != 0)
			//{
			//	scrollPresenter.ScrollAnimationStarting(m_scrollPresenterScrollAnimationStartingToken);
			//	m_scrollPresenterScrollAnimationStartingToken.value = 0;
			//}

			//if (m_scrollPresenterZoomAnimationStartingToken.value != 0)
			//{
			//	scrollPresenter.ZoomAnimationStarting(m_scrollPresenterZoomAnimationStartingToken);
			//	m_scrollPresenterZoomAnimationStartingToken.value = 0;
			//}

			//if (m_scrollPresenterViewChangedToken.value != 0)
			//{
			//	scrollPresenter.ViewChanged(m_scrollPresenterViewChangedToken);
			//	m_scrollPresenterViewChangedToken.value = 0;
			//}

			//if (m_scrollPresenterScrollCompletedToken.value != 0)
			//{
			//	scrollPresenter.ScrollCompleted(m_scrollPresenterScrollCompletedToken);
			//	m_scrollPresenterScrollCompletedToken.value = 0;
			//}

			//if (m_scrollPresenterZoomCompletedToken.value != 0)
			//{
			//	scrollPresenter.ZoomCompleted(m_scrollPresenterZoomCompletedToken);
			//	m_scrollPresenterZoomCompletedToken.value = 0;
			//}

			//if (m_scrollPresenterBringingIntoViewToken.value != 0)
			//{
			//	scrollPresenter.BringingIntoView(m_scrollPresenterBringingIntoViewToken);
			//	m_scrollPresenterBringingIntoViewToken.value = 0;
			//}

			//if (m_scrollPresenterAnchorRequestedToken.value != 0)
			//{
			//	scrollPresenter.AnchorRequested(m_scrollPresenterAnchorRequestedToken);
			//	m_scrollPresenterAnchorRequestedToken.value = 0;
			//}
			scrollPresenter.ExtentChanged -= OnScrollPresenterExtentChanged;
			scrollPresenter.StateChanged -= OnScrollPresenterStateChanged;
			scrollPresenter.ScrollAnimationStarting -= OnScrollAnimationStarting;
			scrollPresenter.ZoomAnimationStarting -= OnZoomAnimationStarting;
			scrollPresenter.ViewChanged -= OnScrollPresenterViewChanged;
			scrollPresenter.ScrollCompleted -= OnScrollPresenterScrollCompleted;
			scrollPresenter.ZoomCompleted -= OnScrollPresenterZoomCompleted;
			scrollPresenter.BringingIntoView -= OnScrollPresenterBringingIntoView;
			scrollPresenter.AnchorRequested -= OnScrollPresenterAnchorRequested;

			DependencyObject scrollPresenterAsDO = scrollPresenter as DependencyObject;

			if (m_scrollPresenterComputedHorizontalScrollModeChangedToken != 0)
			{
				scrollPresenterAsDO.UnregisterPropertyChangedCallback(ScrollPresenter.ComputedHorizontalScrollModeProperty, m_scrollPresenterComputedHorizontalScrollModeChangedToken);
				m_scrollPresenterComputedHorizontalScrollModeChangedToken = 0;
			}

			if (m_scrollPresenterComputedVerticalScrollModeChangedToken != 0)
			{
				scrollPresenterAsDO.UnregisterPropertyChangedCallback(ScrollPresenter.ComputedVerticalScrollModeProperty, m_scrollPresenterComputedVerticalScrollModeChangedToken);
				m_scrollPresenterComputedVerticalScrollModeChangedToken = 0;
			}
		}
	}

	private void HookHorizontalScrollControllerEvents()
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(m_horizontalScrollControllerCanScrollChangedToken is null);
		MUX_ASSERT(m_horizontalScrollControllerIsScrollingWithMouseChangedToken is null);
		MUX_ASSERT(m_onHorizontalScrollControllerPointerEnteredHandler is null);
		MUX_ASSERT(m_onHorizontalScrollControllerPointerExitedHandler is null);

		if (m_horizontalScrollController is IScrollController horizontalScrollController)
		{
			horizontalScrollController.CanScrollChanged += OnScrollControllerCanScrollChanged;
			m_horizontalScrollControllerCanScrollChangedToken = new();
			m_horizontalScrollControllerCanScrollChangedToken.Disposable = Disposable.Create(() => horizontalScrollController.CanScrollChanged -= OnScrollControllerCanScrollChanged);

			horizontalScrollController.IsScrollingWithMouseChanged += OnScrollControllerIsScrollingWithMouseChanged;
			m_horizontalScrollControllerIsScrollingWithMouseChangedToken = new();
			m_horizontalScrollControllerIsScrollingWithMouseChangedToken.Disposable = Disposable.Create(() => horizontalScrollController.IsScrollingWithMouseChanged -= OnScrollControllerIsScrollingWithMouseChanged);
		}

		if (m_horizontalScrollControllerElement is UIElement horizontalScrollControllerElement)
		{
			m_onHorizontalScrollControllerPointerEnteredHandler = new PointerEventHandler(OnHorizontalScrollControllerPointerEntered);
			horizontalScrollControllerElement.AddHandler(UIElement.PointerEnteredEvent, m_onHorizontalScrollControllerPointerEnteredHandler, true);

			m_onHorizontalScrollControllerPointerExitedHandler = new PointerEventHandler(OnHorizontalScrollControllerPointerExited);
			horizontalScrollControllerElement.AddHandler(UIElement.PointerExitedEvent, m_onHorizontalScrollControllerPointerExitedHandler, true);
		}
	}

	private void UnhookHorizontalScrollControllerEvents(bool isForDestructor)
	{
		//if (isForDestructor)
		//{
		//	SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
		//}
		//else
		//{
		//	SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
		//}

		// UNO TODO:
		//IScrollController horizontalScrollController = isForDestructor ? m_horizontalScrollController.safe_get() : m_horizontalScrollController.get()
		IScrollController horizontalScrollController = m_horizontalScrollController;
		if (horizontalScrollController is not null)
		{
			if (m_horizontalScrollControllerCanScrollChangedToken is not null)
			{
				m_horizontalScrollControllerCanScrollChangedToken.Disposable = null;
				m_horizontalScrollControllerCanScrollChangedToken = null;
			}

			if (m_horizontalScrollControllerIsScrollingWithMouseChangedToken is not null)
			{
				m_horizontalScrollControllerIsScrollingWithMouseChangedToken.Disposable = null;
				m_horizontalScrollControllerIsScrollingWithMouseChangedToken = null;
			}
		}

		// UNO TODO:
		//UIElement horizontalScrollControllerElement = isForDestructor ? m_horizontalScrollControllerElement.safe_get() : m_horizontalScrollControllerElement.get()
		UIElement horizontalScrollControllerElement = m_horizontalScrollControllerElement;
		if (horizontalScrollControllerElement is not null)
		{
			if (m_onHorizontalScrollControllerPointerEnteredHandler is not null)
			{
				horizontalScrollControllerElement.RemoveHandler(UIElement.PointerEnteredEvent, m_onHorizontalScrollControllerPointerEnteredHandler);
				m_onHorizontalScrollControllerPointerEnteredHandler = null;
			}

			if (m_onHorizontalScrollControllerPointerExitedHandler is not null)
			{
				horizontalScrollControllerElement.RemoveHandler(UIElement.PointerExitedEvent, m_onHorizontalScrollControllerPointerExitedHandler);
				m_onHorizontalScrollControllerPointerExitedHandler = null;
			}
		}
	}

	private void HookVerticalScrollControllerEvents()
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(m_verticalScrollControllerCanScrollChangedToken is null);
		MUX_ASSERT(m_verticalScrollControllerIsScrollingWithMouseChangedToken is null);
		MUX_ASSERT(m_onVerticalScrollControllerPointerEnteredHandler is null);
		MUX_ASSERT(m_onVerticalScrollControllerPointerExitedHandler is null);

		if (m_verticalScrollController is IScrollController { } verticalScrollController)
		{
			verticalScrollController.CanScrollChanged += OnScrollControllerCanScrollChanged;
			m_verticalScrollControllerCanScrollChangedToken = new();
			m_verticalScrollControllerCanScrollChangedToken.Disposable = Disposable.Create(() => verticalScrollController.CanScrollChanged -= OnScrollControllerCanScrollChanged);

			verticalScrollController.IsScrollingWithMouseChanged += OnScrollControllerIsScrollingWithMouseChanged;
			m_verticalScrollControllerIsScrollingWithMouseChangedToken = new();
			m_verticalScrollControllerIsScrollingWithMouseChangedToken.Disposable = Disposable.Create(() => verticalScrollController.IsScrollingWithMouseChanged -= OnScrollControllerIsScrollingWithMouseChanged);
		}

		if (m_verticalScrollControllerElement is UIElement verticalScrollControllerElement)
		{
			m_onVerticalScrollControllerPointerEnteredHandler = new PointerEventHandler(OnVerticalScrollControllerPointerEntered);
			verticalScrollControllerElement.AddHandler(UIElement.PointerEnteredEvent, m_onVerticalScrollControllerPointerEnteredHandler, true);

			m_onVerticalScrollControllerPointerExitedHandler = new PointerEventHandler(OnVerticalScrollControllerPointerExited);
			verticalScrollControllerElement.AddHandler(UIElement.PointerExitedEvent, m_onVerticalScrollControllerPointerExitedHandler, true);
		}
	}

	private void UnhookVerticalScrollControllerEvents(bool isForDestructor)
	{
		//if (isForDestructor)
		//{
		//	SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
		//}
		//else
		//{
		//	SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
		//}

		// UNO TODO:
		//IScrollController verticalScrollController = isForDestructor ? m_verticalScrollController.safe_get() : m_verticalScrollController.get()
		IScrollController verticalScrollController = m_verticalScrollController;
		if (verticalScrollController is not null)
		{
			if (m_verticalScrollControllerCanScrollChangedToken is not null)
			{
				m_verticalScrollControllerCanScrollChangedToken.Disposable = null;
				m_verticalScrollControllerCanScrollChangedToken = null;
			}

			if (m_verticalScrollControllerIsScrollingWithMouseChangedToken is not null)
			{
				m_verticalScrollControllerIsScrollingWithMouseChangedToken.Disposable = null;
				m_verticalScrollControllerIsScrollingWithMouseChangedToken = null;
			}
		}

		// UNO TODO:
		//UIElement verticalScrollControllerElement = isForDestructor ? m_verticalScrollControllerElement.safe_get() : m_verticalScrollControllerElement.get()
		UIElement verticalScrollControllerElement = m_verticalScrollControllerElement;
		if (verticalScrollControllerElement is not null)
		{
			if (m_onVerticalScrollControllerPointerEnteredHandler is not null)
			{
				verticalScrollControllerElement.RemoveHandler(UIElement.PointerEnteredEvent, m_onVerticalScrollControllerPointerEnteredHandler);
				m_onVerticalScrollControllerPointerEnteredHandler = null;
			}

			if (m_onVerticalScrollControllerPointerExitedHandler is not null)
			{
				verticalScrollControllerElement.RemoveHandler(UIElement.PointerExitedEvent, m_onVerticalScrollControllerPointerExitedHandler);
				m_onVerticalScrollControllerPointerExitedHandler = null;
			}
		}
	}

	private void UpdateScrollPresenter(ScrollPresenter scrollPresenter)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UnhookScrollPresenterEvents(false /*isForDestructor*/);
		m_scrollPresenter = null;

		SetValue(ScrollPresenterProperty, scrollPresenter);

		if (scrollPresenter is not null)
		{
			m_scrollPresenter = scrollPresenter;
			HookScrollPresenterEvents();
		}
	}

	private void UpdateHorizontalScrollController(
		IScrollController horizontalScrollController,
		UIElement horizontalScrollControllerElement)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UnhookHorizontalScrollControllerEvents(false /*isForDestructor*/);

		m_horizontalScrollController = horizontalScrollController;
		m_horizontalScrollControllerElement = horizontalScrollControllerElement;
		HookHorizontalScrollControllerEvents();
		UpdateScrollPresenterHorizontalScrollController(horizontalScrollController);
	}

	private void UpdateScrollPresenterHorizontalScrollController(IScrollController horizontalScrollController)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			scrollPresenter.HorizontalScrollController = horizontalScrollController;
		}
	}

	private void UpdateVerticalScrollController(
		IScrollController verticalScrollController,
		UIElement verticalScrollControllerElement)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UnhookVerticalScrollControllerEvents(false /*isForDestructor*/);

		m_verticalScrollController = verticalScrollController;
		m_verticalScrollControllerElement = verticalScrollControllerElement;
		HookVerticalScrollControllerEvents();
		UpdateScrollPresenterVerticalScrollController(verticalScrollController);
	}

	private void UpdateScrollPresenterVerticalScrollController(IScrollController verticalScrollController)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (m_scrollPresenter is { } scrollPresenter)
		{
			scrollPresenter.VerticalScrollController = verticalScrollController;
		}
	}

	private void UpdateScrollControllersSeparator(UIElement scrollControllersSeparator)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_scrollControllersSeparatorElement = scrollControllersSeparator;
	}

	private void UpdateScrollControllersVisibility(
		bool horizontalChange, bool verticalChange)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(horizontalChange || verticalChange);

		bool isHorizontalScrollControllerVisible = false;

		if (horizontalChange)
		{
			var scrollBarVisibility = HorizontalScrollBarVisibility;

			if (scrollBarVisibility == ScrollingScrollBarVisibility.Auto &&
				m_horizontalScrollController is not null &&
				m_horizontalScrollController.CanScroll)
			{
				isHorizontalScrollControllerVisible = true;
			}
			else
			{
				isHorizontalScrollControllerVisible = (scrollBarVisibility == ScrollingScrollBarVisibility.Visible);
			}

			SetValue(ComputedHorizontalScrollBarVisibilityProperty, isHorizontalScrollControllerVisible ? Visibility.Visible : Visibility.Collapsed);
		}
		else
		{
			isHorizontalScrollControllerVisible = ComputedHorizontalScrollBarVisibility == Visibility.Visible;
		}

		bool isVerticalScrollControllerVisible = false;

		if (verticalChange)
		{
			var scrollBarVisibility = VerticalScrollBarVisibility;

			if (scrollBarVisibility == ScrollingScrollBarVisibility.Auto &&
				m_verticalScrollController is not null &&
				m_verticalScrollController.CanScroll)
			{
				isVerticalScrollControllerVisible = true;
			}
			else
			{
				isVerticalScrollControllerVisible = (scrollBarVisibility == ScrollingScrollBarVisibility.Visible);
			}

			SetValue(ComputedVerticalScrollBarVisibilityProperty, isVerticalScrollControllerVisible ? Visibility.Visible : Visibility.Collapsed);
		}
		else
		{
			isVerticalScrollControllerVisible = ComputedVerticalScrollBarVisibility == Visibility.Visible;
		}

		if (m_scrollControllersSeparatorElement is not null)
		{
			m_scrollControllersSeparatorElement.Visibility = isHorizontalScrollControllerVisible && isVerticalScrollControllerVisible ?
				Visibility.Visible : Visibility.Collapsed;
		}
	}

	private bool IsInputKindIgnored(ScrollingInputKinds inputKind)
	{
		return (IgnoredInputKinds & inputKind) == inputKind;
	}

	private bool AreAllScrollControllersCollapsed()
	{
		return !SharedHelpers.IsAncestor(m_horizontalScrollControllerElement as DependencyObject /*child*/, this /*parent*/, true /*checkVisibility*/) &&
			!SharedHelpers.IsAncestor(m_verticalScrollControllerElement as DependencyObject /*child*/, this /*parent*/, true /*checkVisibility*/);
	}

	private bool AreBothScrollControllersVisible()
	{
		return SharedHelpers.IsAncestor(m_horizontalScrollControllerElement as DependencyObject /*child*/, this /*parent*/, true /*checkVisibility*/) &&
			SharedHelpers.IsAncestor(m_verticalScrollControllerElement as DependencyObject /*child*/, this /*parent*/, true /*checkVisibility*/);
	}

	private bool AreScrollControllersAutoHiding()
	{
		// Use the cached value unless it was invalidated.
		if (m_autoHideScrollControllersValid)
		{
			return m_autoHideScrollControllers;
		}

		m_autoHideScrollControllersValid = true;

		if (ScrollViewTestHooks.GetGlobalTestHooks() is { } globalTestHooks)
		{
			bool? autoHideScrollControllers = ScrollViewTestHooks.GetAutoHideScrollControllers(this);

			if (autoHideScrollControllers is not null)
			{
				// Test hook takes precedence over UISettings and registry key settings.
				m_autoHideScrollControllers = autoHideScrollControllers.Value;
				return m_autoHideScrollControllers;
			}
		}

		// UNO TODO:
		m_autoHideScrollControllers = true;
		//if (m_uiSettings5 is not null)
		//{
		//	m_autoHideScrollControllers = m_uiSettings5.AutoHideScrollBars();
		//}
		//else
		//{
		//	m_autoHideScrollControllers = RegUtil.UseDynamicScrollbars();
		//}

		return m_autoHideScrollControllers;
	}

	private bool IsScrollControllersSeparatorVisible()
	{
		return m_scrollControllersSeparatorElement is not null && m_scrollControllersSeparatorElement.Visibility == Visibility.Visible;
	}

	private void HideIndicators(
		bool useTransitions = true)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, useTransitions, m_keepIndicatorsShowing);

		MUX_ASSERT(AreScrollControllersAutoHiding());

		if (!AreAllScrollControllersCollapsed() && !m_keepIndicatorsShowing)
		{
			GoToState(s_noIndicatorStateName, useTransitions);

			if (!m_hasNoIndicatorStateStoryboardCompletedHandler)
			{
				m_showingMouseIndicators = false;
			}
		}
	}

	private void HideIndicatorsAfterDelay()
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, m_keepIndicatorsShowing);

		MUX_ASSERT(AreScrollControllersAutoHiding());

		if (!m_keepIndicatorsShowing && IsLoaded)
		{
			DispatcherTimer hideIndicatorsTimer = null;

			if (m_hideIndicatorsTimer is not null)
			{
				hideIndicatorsTimer = m_hideIndicatorsTimer;
				if (hideIndicatorsTimer.IsEnabled)
				{
					hideIndicatorsTimer.Stop();
				}
			}
			else
			{
				hideIndicatorsTimer = new DispatcherTimer();
				hideIndicatorsTimer.Interval = new TimeSpan(ticks: s_noIndicatorCountdown);
				hideIndicatorsTimer.Tick += OnHideIndicatorsTimerTick;
				m_hideIndicatorsTimer = hideIndicatorsTimer;
			}

			hideIndicatorsTimer.Start();
		}
	}

	// On RS4 and RS5, update m_autoHideScrollControllers based on the DynamicScrollbars registry key value
	// and update the visual states if the value changed.
	private void UpdateScrollControllersAutoHiding(
		bool forceUpdate = false)
	{
		// UNO TODO:
		if ((forceUpdate /*|| !m_uiSettings5*/) && m_autoHideScrollControllersValid)
		{
			m_autoHideScrollControllersValid = false;

			bool oldAutoHideScrollControllers = m_autoHideScrollControllers;
			bool newAutoHideScrollControllers = AreScrollControllersAutoHiding();

			if (oldAutoHideScrollControllers != newAutoHideScrollControllers)
			{
				UpdateVisualStates(
					true  /*useTransitions*/,
					false /*showIndicators*/,
					false /*hideIndicators*/,
					true  /*scrollControllersAutoHidingChanged*/);
			}
		}
	}

	private void UpdateVisualStates(
		bool useTransitions = true,
		bool showIndicators = false,
		bool hideIndicators = false,
		bool scrollControllersAutoHidingChanged = false,
		bool updateScrollControllersAutoHiding = false,
		bool onlyForAutoHidingScrollControllers = false)
	{
		if (updateScrollControllersAutoHiding)
		{
			UpdateScrollControllersAutoHiding();
		}

		if (onlyForAutoHidingScrollControllers && !AreScrollControllersAutoHiding())
		{
			return;
		}

		UpdateScrollControllersVisualState(useTransitions, showIndicators, hideIndicators);
		UpdateScrollControllersSeparatorVisualState(useTransitions, scrollControllersAutoHidingChanged);
	}

	// Updates the state for the ScrollingIndicatorStates state group.
	private void UpdateScrollControllersVisualState(
		bool useTransitions,
		bool showIndicators,
		bool hideIndicators)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, useTransitions);
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, showIndicators, hideIndicators);

		MUX_ASSERT(!(showIndicators && hideIndicators));

		bool areScrollControllersAutoHiding = AreScrollControllersAutoHiding();

		MUX_ASSERT(!(!areScrollControllersAutoHiding && hideIndicators));

		if ((!areScrollControllersAutoHiding || showIndicators) && !hideIndicators)
		{
			if (AreAllScrollControllersCollapsed())
			{
				return;
			}

			ResetHideIndicatorsTimer(false /*isForDestructor*/, true /*restart*/);

			// Mouse indicators dominate if they are already showing or if we have set the flag to prefer them.
			if (m_preferMouseIndicators || m_showingMouseIndicators || !areScrollControllersAutoHiding)
			{
				GoToState(s_mouseIndicatorStateName, useTransitions);

				m_showingMouseIndicators = true;
			}
			else
			{
				GoToState(s_touchIndicatorStateName, useTransitions);
			}
		}
		else if (!m_keepIndicatorsShowing)
		{
			if (SharedHelpers.IsAnimationsEnabled())
			{
				// By default there is a delay before the NoIndicator state actually shows.
				HideIndicators();
			}
			else
			{
				// Since OS animations are turned off, use a timer to delay the indicators' hiding.
				HideIndicatorsAfterDelay();
			}
		}
	}

	// Updates the state for the ScrollBarsSeparatorStates state group.
	private void UpdateScrollControllersSeparatorVisualState(
		bool useTransitions,
		bool scrollControllersAutoHidingChanged)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, useTransitions, scrollControllersAutoHidingChanged);

		if (!IsScrollControllersSeparatorVisible())
		{
			return;
		}

		bool isEnabled = IsEnabled;
		bool areScrollControllersAutoHiding = AreScrollControllersAutoHiding();
		bool showScrollControllersSeparator = !areScrollControllersAutoHiding;

		if (!showScrollControllersSeparator &&
			AreBothScrollControllersVisible() &&
			(m_preferMouseIndicators || m_showingMouseIndicators) &&
			(m_isPointerOverHorizontalScrollController || m_isPointerOverVerticalScrollController))
		{
			showScrollControllersSeparator = true;
		}

		// Select the proper state for the scroll controllers separator within the ScrollBarsSeparatorStates group:
		if (SharedHelpers.IsAnimationsEnabled())
		{
			// When OS animations are turned on, show the separator when a scroll controller is shown unless the ScrollView is disabled, using an animation.
			if (showScrollControllersSeparator && isEnabled)
			{
				GoToState(s_scrollBarsSeparatorExpanded, useTransitions);
			}
			else if (isEnabled)
			{
				GoToState(s_scrollBarsSeparatorCollapsed, useTransitions);
			}
			else
			{
				GoToState(s_scrollBarsSeparatorCollapsedDisabled, useTransitions);
			}
		}
		else
		{
			// OS animations are turned off. Show or hide the separator depending on the presence of scroll controllers, without an animation.
			// When the ScrollView is disabled, hide the separator in sync with the ScrollBar(s).
			if (showScrollControllersSeparator)
			{
				if (isEnabled)
				{
					GoToState((areScrollControllersAutoHiding || scrollControllersAutoHidingChanged) ? s_scrollBarsSeparatorExpandedWithoutAnimation : s_scrollBarsSeparatorDisplayedWithoutAnimation, useTransitions);
				}
				else
				{
					GoToState(s_scrollBarsSeparatorCollapsed, useTransitions);
				}
			}
			else
			{
				GoToState(isEnabled ? s_scrollBarsSeparatorCollapsedWithoutAnimation : s_scrollBarsSeparatorCollapsed, useTransitions);
			}
		}
	}

	private void GoToState(string stateName, bool useTransitions)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, stateName.data(), useTransitions);

		VisualStateManager.GoToState(this, stateName, useTransitions);
	}

	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		//SCROLLVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::KeyRoutedEventArgsToString(e).c_str());

		base.OnKeyDown(e);

		m_preferMouseIndicators = false;

		if (m_scrollPresenter is not null)
		{
			KeyRoutedEventArgs eventArgs = e;
			if (!eventArgs.Handled)
			{
				var originalKey = eventArgs.OriginalKey;
				bool isGamepadKey = FocusHelper.IsGamepadNavigationDirection(originalKey) || FocusHelper.IsGamepadPageNavigationDirection(originalKey);

				if (isGamepadKey)
				{
					if (IsInputKindIgnored(ScrollingInputKinds.Gamepad))
					{
						return;
					}
				}
				else
				{
					if (IsInputKindIgnored(ScrollingInputKinds.Keyboard))
					{
						return;
					}
				}

				bool isXYFocusEnabledForKeyboard = XYFocusKeyboardNavigation == XYFocusKeyboardNavigationMode.Enabled;
				bool doXYFocusScrolling = isGamepadKey || isXYFocusEnabledForKeyboard;

				if (doXYFocusScrolling)
				{
					HandleKeyDownForXYNavigation(eventArgs);
				}
				else
				{
					HandleKeyDownForStandardScroll(eventArgs);
				}
			}
		}
	}

	private void HandleKeyDownForStandardScroll(KeyRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::KeyRoutedEventArgsToString(args).c_str());

		// Up/Down/Left/Right will scroll by 15% the size of the viewport.
		const double smallScrollProportion = 0.15;

		MUX_ASSERT(!args.Handled);
		MUX_ASSERT(m_scrollPresenter != null);

		bool isHandled = DoScrollForKey(args.Key, smallScrollProportion);

		args.Handled = isHandled;
	}

	private void HandleKeyDownForXYNavigation(KeyRoutedEventArgs args)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::KeyRoutedEventArgsToString(args).c_str());

		MUX_ASSERT(!args.Handled);
		MUX_ASSERT(m_scrollPresenter != null);

		bool isHandled = false;
		var originalKey = args.OriginalKey;
		var scrollPresenter = m_scrollPresenter as ScrollPresenter;
		bool isPageNavigation = FocusHelper.IsGamepadPageNavigationDirection(originalKey);
		double scrollAmountProportion = isPageNavigation ? 1.0 : 0.5;
		bool shouldProcessKeyEvent = true;
		FocusNavigationDirection navigationDirection;

		if (isPageNavigation)
		{
			navigationDirection = FocusHelper.GetPageNavigationDirection(originalKey);

			// We should only handle page navigation if we can scroll in that direction.
			// Note: For non-paging navigation, we might want to move focus even if we cannot scroll.
			shouldProcessKeyEvent = CanScrollInDirection(navigationDirection);
		}
		else
		{
			navigationDirection = FocusHelper.GetNavigationDirection(originalKey);
		}

		if (shouldProcessKeyEvent)
		{
			bool shouldScroll = false;
			bool shouldMoveFocus = false;
			DependencyObject nextElement = null;

			if (navigationDirection != FocusNavigationDirection.None)
			{
				nextElement = GetNextFocusCandidate(navigationDirection, isPageNavigation);
			}

			if (nextElement is not null && nextElement != FocusManager.GetFocusedElement(XamlRoot))
			{
				UIElement nextElementAsUIE = FocusHelper.GetUIElementForFocusCandidate(nextElement);
				MUX_ASSERT(nextElementAsUIE != null);

				var nextElementAsFe = nextElementAsUIE as FrameworkElement;
				var rect = new Rect(0, 0, nextElementAsFe.ActualWidth, nextElementAsFe.ActualHeight);
				var elementBounds = nextElementAsUIE.TransformToVisual(scrollPresenter).TransformBounds(rect);
				var viewport = new Rect(0, 0, scrollPresenter.ActualWidth, scrollPresenter.ActualHeight);

				// Extend the viewport in the direction we are moving:
				Rect extendedViewport = viewport;
				switch (navigationDirection)
				{
					case FocusNavigationDirection.Down:
						extendedViewport.Height += viewport.Height;
						break;
					case FocusNavigationDirection.Up:
						extendedViewport.Y -= viewport.Height;
						extendedViewport.Height += viewport.Height;
						break;
					case FocusNavigationDirection.Left:
						extendedViewport.X -= viewport.Width;
						extendedViewport.Width += viewport.Width;
						break;
					case FocusNavigationDirection.Right:
						extendedViewport.Width += viewport.Width;
						break;
				}

				bool isElementInExtendedViewport = RectHelper.Intersect(elementBounds, extendedViewport) != RectHelper.Empty;
				bool isElementFullyInExtendedViewport = RectHelper.Union(elementBounds, extendedViewport) == extendedViewport;

				if (isElementInExtendedViewport)
				{
					if (isPageNavigation)
					{
						// Always scroll for page navigation
						shouldScroll = true;

						if (isElementFullyInExtendedViewport)
						{
							// Move focus:
							shouldMoveFocus = true;
						}
					}
					else
					{
						// Non-paging scroll allows partial candidates
						shouldMoveFocus = true;
					}
				}
				else
				{
					// Element is outside extended viewport - scroll but don't focus.
					shouldScroll = true;
				}
			}
			else
			{
				// No focus candidate: scroll
				shouldScroll = true;
			}

			if (shouldMoveFocus)
			{
				//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_METH_INT, METH_NAME, this, L"FocusManager::TryFocusAsync", SharedHelpers::IsAnimationsEnabled());

				var focusAsyncOperation = FocusManager.TryFocusAsync(nextElement, FocusState.Keyboard);

				if (SharedHelpers.IsAnimationsEnabled()) // When system animations are turned off, the bring-into-view operations are not turned into animations.
				{
					// By changing focus, we will trigger BringIntoView requests in ScrollPresenter. If we are not going to invoke a scroll below (i.e. shouldScroll = false)
					// we should allow ScrollPresenter to animate any BringIntoView requests.
					// If we ARE going to invoke a scroll below (i.e. shouldScroll = true) we want to prevent the case where both our scroll and the scroll triggered by the
					// focus change are active at once. So in this case we want to cancel any BringIntoView operations, since we are already handling the scrolling.
					bool cancelBringIntoView = shouldScroll;
					focusAsyncOperation.Completed = (asyncOperation, asyncStatus) =>
					{
						var strongThis = this;
						var targetElement = nextElement as UIElement;
						//SCROLLVIEW_TRACE_VERBOSE(*strongThis, TRACE_MSG_METH_INT, METH_NAME, strongThis, static_cast<int>(asyncStatus));

						if (asyncStatus == AsyncStatus.Completed && asyncOperation.GetResults() is not null)
						{
							// The focus change request was successful. One or a few ScrollPresenter::BringingIntoView notifications are likely to be raised in the coming ticks.
							// For those, the BringIntoViewRequestedEventArgs::AnimationDesired property will be set to True in order to animate to the target element rather than jumping.
							//SCROLLVIEW_TRACE_VERBOSE(*strongThis, TRACE_MSG_METH_PTR, METH_NAME, strongThis, targetElement);

							var bringIntoViewOperation = new ScrollViewBringIntoViewOperation(targetElement, cancelBringIntoView);

							strongThis.m_bringIntoViewOperations.Add(bringIntoViewOperation);
							strongThis.HookCompositionTargetRendering();
						}
					};
				}

				isHandled = true;
			}

			if (shouldScroll)
			{
				if (navigationDirection == FocusNavigationDirection.None)
				{
					isHandled = DoScrollForKey(args.Key, scrollAmountProportion);
				}
				else
				{
					if (navigationDirection == FocusNavigationDirection.Down && CanScrollDown())
					{
						isHandled = true;
						DoScroll(scrollPresenter.ActualHeight * scrollAmountProportion, Orientation.Vertical);
					}
					else if (navigationDirection == FocusNavigationDirection.Up && CanScrollUp())
					{
						isHandled = true;
						DoScroll(-scrollPresenter.ActualHeight * scrollAmountProportion, Orientation.Vertical);
					}
					else if (navigationDirection == FocusNavigationDirection.Right && CanScrollRight())
					{
						isHandled = true;
						DoScroll(scrollPresenter.ActualWidth * scrollAmountProportion * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1), Orientation.Horizontal);
					}
					else if (navigationDirection == FocusNavigationDirection.Left && CanScrollLeft())
					{
						isHandled = true;
						DoScroll(-scrollPresenter.ActualWidth * scrollAmountProportion * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1), Orientation.Horizontal);
					}
				}
			}
		}

		args.Handled = isHandled;
	}

	private void HandleScrollControllerPointerEntered(
		bool isForHorizontalScrollController)
	{
		if (isForHorizontalScrollController)
		{
			m_isPointerOverHorizontalScrollController = true;
		}
		else
		{
			m_isPointerOverVerticalScrollController = true;
		}

		UpdateScrollControllersAutoHiding();
		if (AreScrollControllersAutoHiding() && !SharedHelpers.IsAnimationsEnabled())
		{
			HideIndicatorsAfterDelay();
		}
	}

	private void HandleScrollControllerPointerExited(
		bool isForHorizontalScrollController)
	{
		if (isForHorizontalScrollController)
		{
			m_isPointerOverHorizontalScrollController = false;
		}
		else
		{
			m_isPointerOverVerticalScrollController = false;
		}

		UpdateScrollControllersAutoHiding();
		if (AreScrollControllersAutoHiding())
		{
			HideIndicatorsAfterDelay();
		}
	}

	private DependencyObject GetNextFocusCandidate(FocusNavigationDirection navigationDirection, bool isPageNavigation)
	{
		MUX_ASSERT(m_scrollPresenter != null);
		MUX_ASSERT(navigationDirection != FocusNavigationDirection.None);
		var scrollPresenter = m_scrollPresenter as ScrollPresenter;

		FocusNavigationDirection focusDirection = navigationDirection;

		FindNextElementOptions findNextElementOptions = new();
		findNextElementOptions.SearchRoot = scrollPresenter.Content;

		if (isPageNavigation)
		{
			var localBounds = new Rect(0, 0, scrollPresenter.ActualWidth, scrollPresenter.ActualHeight);
			var globalBounds = scrollPresenter.TransformToVisual(null).TransformBounds(localBounds);
			const int numPagesLookAhead = 2;

			var hintRect = globalBounds;
			switch (navigationDirection)
			{
				case FocusNavigationDirection.Down:
					hintRect.Y += globalBounds.Height * numPagesLookAhead;
					break;
				case FocusNavigationDirection.Up:
					hintRect.Y -= globalBounds.Height * numPagesLookAhead;
					break;
				case FocusNavigationDirection.Left:
					hintRect.X -= globalBounds.Width * numPagesLookAhead;
					break;
				case FocusNavigationDirection.Right:
					hintRect.X += globalBounds.Width * numPagesLookAhead;
					break;
				default:
					MUX_ASSERT(false);
					break;
			}

			findNextElementOptions.HintRect = hintRect;
			findNextElementOptions.ExclusionRect = hintRect;
			focusDirection = FocusHelper.GetOppositeDirection(navigationDirection);
		}

		return FocusManager.FindNextElement(focusDirection, findNextElementOptions);
	}

	private bool DoScrollForKey(VirtualKey key, double scrollProportion)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_DBL_INT, METH_NAME, this, scrollProportion, static_cast<int>(key));

		MUX_ASSERT(m_scrollPresenter != null);

		const double offsetEpsilon = 0.001;
		bool isScrollTriggered = false;
		var scrollPresenter = m_scrollPresenter as ScrollPresenter;

		if ((key == VirtualKey.PageDown || key == VirtualKey.Down) && CanScrollDown())
		{
			MUX_ASSERT(scrollPresenter.VerticalOffset < ScrollableHeight);

			// When getting close to the maximum vertical offset:
			//  - make sure the maximum is actually reached thanks for the epsilon addition.
			var maxScrollAmount = ScrollableHeight + offsetEpsilon - scrollPresenter.VerticalOffset;
			//  - do not automatically overbounce by limiting the offset change to the remaining scrollable height.
			var scrollAmount = Math.Min(maxScrollAmount, scrollPresenter.ActualHeight * (key == VirtualKey.PageDown ? 1.0 : scrollProportion));

			isScrollTriggered = true;
			DoScroll(scrollAmount, Orientation.Vertical);
		}
		else if ((key == VirtualKey.PageUp || key == VirtualKey.Up) && CanScrollUp())
		{
			MUX_ASSERT(scrollPresenter.VerticalOffset > 0);

			// When getting close to the minimum vertical offset 0.0:
			//  - make sure 0.0 is actually reached thanks for the epsilon addition.
			var maxScrollAmount = scrollPresenter.VerticalOffset + offsetEpsilon;
			//  - do not automatically overbounce by limiting the offset change to the remaining offset.
			var scrollAmount = Math.Max(-maxScrollAmount, scrollPresenter.ActualHeight * (key == VirtualKey.PageUp ? -1.0 : -scrollProportion));

			isScrollTriggered = true;
			DoScroll(scrollAmount, Orientation.Vertical);
		}
		else if (key == VirtualKey.Left || key == VirtualKey.Right)
		{
			double scrollAmount = scrollPresenter.ActualWidth * scrollProportion;
			bool isRTL = FlowDirection == FlowDirection.RightToLeft;

			if (isRTL)
			{
				scrollAmount *= -1;
			}

			if (key == VirtualKey.Right && CanScrollRight())
			{
				// When getting close to the maximum horizontal offset:
				//  - make sure the maximum is actually reached thanks for the epsilon addition.
				var maxScrollAmount = isRTL ?
					-scrollPresenter.HorizontalOffset - offsetEpsilon :
					ScrollableWidth + offsetEpsilon - scrollPresenter.HorizontalOffset;
				//  - do not automatically overbounce by limiting the offset change to the remaining scrollable width.
				scrollAmount = isRTL ?
					Math.Max(maxScrollAmount, scrollAmount) :
					Math.Min(maxScrollAmount, scrollAmount);

				MUX_ASSERT(scrollAmount != 0.0);

				isScrollTriggered = true;
				DoScroll(scrollAmount, Orientation.Horizontal);
			}
			else if (key == VirtualKey.Left && CanScrollLeft())
			{
				// When getting close to the minimum horizontal offset 0.0:
				//  - make sure 0.0 is actually reached thanks for the epsilon addition.
				var maxScrollAmount = isRTL ?
					-ScrollableWidth - offsetEpsilon + scrollPresenter.HorizontalOffset :
					scrollPresenter.HorizontalOffset + offsetEpsilon;
				//  - do not automatically overbounce by limiting the offset change to the remaining offset.
				scrollAmount =
					isRTL ?
					Math.Min(-maxScrollAmount, -scrollAmount) :
					Math.Max(-maxScrollAmount, -scrollAmount);

				MUX_ASSERT(scrollAmount != 0.0);

				isScrollTriggered = true;
				DoScroll(scrollAmount, Orientation.Horizontal);
			}
		}
		else if (key == VirtualKey.Home)
		{
			bool canScrollUp = CanScrollUp();
			var verticalScrollMode = ComputedVerticalScrollMode;

			if (canScrollUp || (verticalScrollMode == ScrollingScrollMode.Disabled && CanScrollLeft()))
			{
				isScrollTriggered = true;
				var horizontalOffset = canScrollUp ? scrollPresenter.HorizontalOffset : 0.0;
				var verticalOffset = canScrollUp ? 0.0 : scrollPresenter.VerticalOffset;

				if (!canScrollUp && FlowDirection == FlowDirection.RightToLeft)
				{
					horizontalOffset = scrollPresenter.ExtentWidth * scrollPresenter.ZoomFactor - scrollPresenter.ActualWidth;
				}

				scrollPresenter.ScrollTo(horizontalOffset, verticalOffset);
			}
		}
		else if (key == VirtualKey.End)
		{
			bool canScrollDown = CanScrollDown();
			var verticalScrollMode = ComputedVerticalScrollMode;

			if (canScrollDown || (verticalScrollMode == ScrollingScrollMode.Disabled && CanScrollRight()))
			{
				isScrollTriggered = true;
				var zoomedExtent = (canScrollDown ? scrollPresenter.ExtentHeight : scrollPresenter.ExtentWidth) * scrollPresenter.ZoomFactor;
				var horizontalOffset = canScrollDown ? scrollPresenter.HorizontalOffset : zoomedExtent - scrollPresenter.ActualWidth;
				var verticalOffset = canScrollDown ? zoomedExtent - scrollPresenter.ActualHeight : scrollPresenter.VerticalOffset;

				if (!canScrollDown && FlowDirection == FlowDirection.RightToLeft)
				{
					horizontalOffset = 0.0;
				}

				scrollPresenter.ScrollTo(horizontalOffset, verticalOffset);
			}
		}

		return isScrollTriggered;
	}

	private void DoScroll(double offset, Orientation orientation)
	{
		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_DBL_INT, METH_NAME, this, offset, static_cast<int>(orientation));

		bool isVertical = orientation == Orientation.Vertical;

		if (m_scrollPresenter is ScrollPresenter scrollPresenter)
		{
			if (SharedHelpers.IsAnimationsEnabled())
			{
				Vector2 inertiaDecayRate = new Vector2(0.9995f, 0.9995f);

				// A velocity less than or equal to this value has no effect.
				const double minVelocity = 30.0;

				// We need to add this much velocity over minVelocity per pixel we want to move:
				const double s_velocityNeededPerPixel = 7.600855902349023;

				var scrollDir = offset > 0 ? 1 : -1;

				// The minimum velocity required to move in the given direction.
				double baselineVelocity = minVelocity * scrollDir;

				// If there is already a scroll animation running for a previous key press, we want to take that into account
				// for calculating the baseline velocity. 
				var previousScrollViewChangeCorrelationId = isVertical ? m_verticalAddScrollVelocityOffsetChangeCorrelationId : m_horizontalAddScrollVelocityOffsetChangeCorrelationId;
				if (previousScrollViewChangeCorrelationId != s_noOpCorrelationId)
				{
					var directionOfPreviousScrollOperation = isVertical ? m_verticalAddScrollVelocityDirection : m_horizontalAddScrollVelocityDirection;
					if (directionOfPreviousScrollOperation == 1)
					{
						baselineVelocity -= minVelocity;
					}
					else if (directionOfPreviousScrollOperation == -1)
					{
						baselineVelocity += minVelocity;
					}
				}

				var velocity = (float)(baselineVelocity + (offset * s_velocityNeededPerPixel));

				if (isVertical)
				{
					Vector2 offsetsVelocity = new Vector2(0.0f, velocity);
					m_verticalAddScrollVelocityOffsetChangeCorrelationId = scrollPresenter.AddScrollVelocity(offsetsVelocity, inertiaDecayRate);
					m_verticalAddScrollVelocityDirection = scrollDir;
				}
				else
				{
					Vector2 offsetsVelocity = new Vector2(velocity, 0.0f);
					m_horizontalAddScrollVelocityOffsetChangeCorrelationId = scrollPresenter.AddScrollVelocity(offsetsVelocity, inertiaDecayRate);
					m_horizontalAddScrollVelocityDirection = scrollDir;
				}
			}
			else
			{
				if (isVertical)
				{
					// Any horizontal AddScrollVelocity animation recently launched should be ignored by a potential subsequent AddScrollVelocity call.
					m_verticalAddScrollVelocityOffsetChangeCorrelationId = s_noOpCorrelationId;

					scrollPresenter.ScrollBy(0.0 /*horizontalOffsetDelta*/, offset /*verticalOffsetDelta*/);
				}
				else
				{
					// Any vertical AddScrollVelocity animation recently launched should be ignored by a potential subsequent AddScrollVelocity call.
					m_horizontalAddScrollVelocityOffsetChangeCorrelationId = s_noOpCorrelationId;

					scrollPresenter.ScrollBy(offset /*horizontalOffsetDelta*/, 0.0 /*verticalOffsetDelta*/);
				}
			}
		}
	}

	private bool CanScrollInDirection(FocusNavigationDirection direction)
	{
		bool result = false;
		switch (direction)
		{
			case FocusNavigationDirection.Down:
				result = CanScrollDown();
				break;
			case FocusNavigationDirection.Up:
				result = CanScrollUp();
				break;
			case FocusNavigationDirection.Left:
				result = CanScrollLeft();
				break;
			case FocusNavigationDirection.Right:
				result = CanScrollRight();
				break;
		}

		return result;
	}

	private bool CanScrollDown()
	{
		return CanScrollVerticallyInDirection(true /*inPositiveDirection*/);
	}

	private bool CanScrollUp()
	{
		return CanScrollVerticallyInDirection(false /*inPositiveDirection*/);
	}

	private bool CanScrollRight()
	{
		return CanScrollHorizontallyInDirection(true /*inPositiveDirection*/);
	}

	private bool CanScrollLeft()
	{
		return CanScrollHorizontallyInDirection(false /*inPositiveDirection*/);
	}

	private bool CanScrollVerticallyInDirection(bool inPositiveDirection)
	{
		bool canScrollInDirection = false;
		if (m_scrollPresenter is not null)
		{
			var scrollPresenter = m_scrollPresenter as ScrollPresenter;
			var verticalScrollMode = ComputedVerticalScrollMode;

			if (verticalScrollMode == ScrollingScrollMode.Enabled)
			{
				var zoomedExtentHeight = scrollPresenter.ExtentHeight * scrollPresenter.ZoomFactor;
				var viewportHeight = scrollPresenter.ActualHeight;
				if (zoomedExtentHeight > viewportHeight)
				{
					// Ignore distance to an edge smaller than 1/1000th of a pixel to account for rounding approximations.
					// Otherwise an Up/Down arrow key may be processed and have no effect.
					const double offsetEpsilon = 0.001;

					if (inPositiveDirection)
					{
						var maxVerticalOffset = zoomedExtentHeight - viewportHeight;
						if (scrollPresenter.VerticalOffset < maxVerticalOffset - offsetEpsilon)
						{
							canScrollInDirection = true;
						}
					}
					else
					{
						if (scrollPresenter.VerticalOffset > offsetEpsilon)
						{
							canScrollInDirection = true;
						}
					}
				}
			}
		}

		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, inPositiveDirection, canScrollInDirection);

		return canScrollInDirection;
	}

	private bool CanScrollHorizontallyInDirection(bool inPositiveDirection)
	{
		bool canScrollInDirection = false;

		if (FlowDirection == FlowDirection.RightToLeft)
		{
			inPositiveDirection = !inPositiveDirection;
		}

		if (m_scrollPresenter is not null)
		{
			var scrollPresenter = m_scrollPresenter as ScrollPresenter;
			var horizontalScrollMode = ComputedHorizontalScrollMode;

			if (horizontalScrollMode == ScrollingScrollMode.Enabled)
			{
				var zoomedExtentWidth = scrollPresenter.ExtentWidth * scrollPresenter.ZoomFactor;
				var viewportWidth = scrollPresenter.ActualWidth;
				if (zoomedExtentWidth > viewportWidth)
				{
					// Ignore distance to an edge smaller than 1/1000th of a pixel to account for rounding approximations.
					// Otherwise a Left/Right arrow key may be processed and have no effect.
					const double offsetEpsilon = 0.001;

					if (inPositiveDirection)
					{
						var maxHorizontalOffset = zoomedExtentWidth - viewportWidth;
						if (scrollPresenter.HorizontalOffset < maxHorizontalOffset - offsetEpsilon)
						{
							canScrollInDirection = true;
						}
					}
					else
					{
						if (scrollPresenter.HorizontalOffset > offsetEpsilon)
						{
							canScrollInDirection = true;
						}
					}
				}
			}
		}

		//SCROLLVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, inPositiveDirection, canScrollInDirection);

		return canScrollInDirection;
	}

#if false

	private string DependencyPropertyToString(DependencyProperty dependencyProperty)
	{
		if (dependencyProperty == ContentProperty)
		{
			return "Content";
		}
		else if (dependencyProperty == ScrollPresenterProperty)
		{
			return "ScrollPresenter";
		}
		else if (dependencyProperty == HorizontalScrollBarVisibilityProperty)
		{
			return "HorizontalScrollBarVisibility";
		}
		else if (dependencyProperty == VerticalScrollBarVisibilityProperty)
		{
			return "VerticalScrollBarVisibility";
		}
		else if (dependencyProperty == ContentOrientationProperty)
		{
			return "ContentOrientation";
		}
		else if (dependencyProperty == VerticalScrollChainModeProperty)
		{
			return "VerticalScrollChainMode";
		}
		else if (dependencyProperty == ZoomChainModeProperty)
		{
			return "ZoomChainMode";
		}
		else if (dependencyProperty == HorizontalScrollRailModeProperty)
		{
			return "HorizontalScrollRailMode";
		}
		else if (dependencyProperty == VerticalScrollRailModeProperty)
		{
			return "VerticalScrollRailMode";
		}
		else if (dependencyProperty == HorizontalScrollModeProperty)
		{
			return "HorizontalScrollMode";
		}
		else if (dependencyProperty == VerticalScrollModeProperty)
		{
			return "VerticalScrollMode";
		}
		else if (dependencyProperty == ComputedHorizontalScrollBarVisibilityProperty)
		{
			return "ComputedHorizontalScrollBarVisibility";
		}
		else if (dependencyProperty == ComputedVerticalScrollBarVisibilityProperty)
		{
			return "ComputedVerticalScrollBarVisibility";
		}
		else if (dependencyProperty == ComputedHorizontalScrollModeProperty)
		{
			return "ComputedHorizontalScrollMode";
		}
		else if (dependencyProperty == ComputedVerticalScrollModeProperty)
		{
			return "ComputedVerticalScrollMode";
		}
		else if (dependencyProperty == ZoomModeProperty)
		{
			return "ZoomMode";
		}
		else if (dependencyProperty == IgnoredInputKindsProperty)
		{
			return "IgnoredInputKinds";
		}
		else if (dependencyProperty == MinZoomFactorProperty)
		{
			return "MinZoomFactor";
		}
		else if (dependencyProperty == MaxZoomFactorProperty)
		{
			return "MaxZoomFactor";
		}
		else if (dependencyProperty == HorizontalAnchorRatioProperty)
		{
			return "HorizontalAnchorRatio";
		}
		else if (dependencyProperty == VerticalAnchorRatioProperty)
		{
			return "VerticalAnchorRatio";
		}
		else
		{
			return "UNKNOWN";
		}
	}

#endif
}
