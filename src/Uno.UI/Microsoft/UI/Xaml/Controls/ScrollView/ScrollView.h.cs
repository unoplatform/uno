// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls;

public partial class ScrollView : Control
{
	// Properties' default values.
	//private const ScrollingScrollBarVisibility s_defaultHorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Auto;
	//private const ScrollingScrollBarVisibility s_defaultVerticalScrollBarVisibility = ScrollingScrollBarVisibility.Auto;
	//private const ScrollingChainMode s_defaultHorizontalScrollChainMode = ScrollingChainMode.Auto;
	//private const ScrollingChainMode s_defaultVerticalScrollChainMode = ScrollingChainMode.Auto;
	//private const ScrollingRailMode s_defaultHorizontalScrollRailMode = ScrollingRailMode.Enabled;
	//private const ScrollingRailMode s_defaultVerticalScrollRailMode = ScrollingRailMode.Enabled;
	//private const Visibility s_defaultComputedHorizontalScrollBarVisibility = Visibility.Collapsed;
	//private const Visibility s_defaultComputedVerticalScrollBarVisibility = Visibility.Collapsed;
	//private const ScrollingScrollMode s_defaultHorizontalScrollMode = ScrollingScrollMode.Auto;
	//private const ScrollingScrollMode s_defaultVerticalScrollMode = ScrollingScrollMode.Auto;
	//private const ScrollingScrollMode s_defaultComputedHorizontalScrollMode = ScrollingScrollMode.Disabled;
	//private const ScrollingScrollMode s_defaultComputedVerticalScrollMode = ScrollingScrollMode.Disabled;
	//private const ScrollingChainMode s_defaultZoomChainMode = ScrollingChainMode.Auto;
	//private const ScrollingZoomMode s_defaultZoomMode = ScrollingZoomMode.Disabled;
	//private const ScrollingInputKinds s_defaultIgnoredInputKinds = ScrollingInputKinds.None;
	//private const ScrollingContentOrientation s_defaultContentOrientation = ScrollingContentOrientation.Vertical;
	//private const double s_defaultMinZoomFactor = 0.1;
	//private const double s_defaultMaxZoomFactor = 10.0;
	//private const bool s_defaultAnchorAtExtent = true;
	//private const double s_defaultAnchorRatio = 0.0;


	private const string s_rootPartName = "PART_Root";
	private const string s_scrollPresenterPartName = "PART_ScrollPresenter";
	private const string s_horizontalScrollBarPartName = "PART_HorizontalScrollBar";
	private const string s_verticalScrollBarPartName = "PART_VerticalScrollBar";
	private const string s_scrollBarsSeparatorPartName = "PART_ScrollBarsSeparator";
	private const string s_IScrollAnchorProviderNotImpl = "Template part named PART_ScrollPresenter does not implement IScrollAnchorProvider.";
	private const string s_noScrollPresenterPart = "No template part named PART_ScrollPresenter was loaded.";

	private ScrollBarController m_horizontalScrollBarController;
	private ScrollBarController m_verticalScrollBarController;

	private IScrollController m_horizontalScrollController;
	private IScrollController m_verticalScrollController;
	private UIElement m_horizontalScrollControllerElement;
	private UIElement m_verticalScrollControllerElement;
	private UIElement m_scrollControllersSeparatorElement;
	private IScrollPresenter m_scrollPresenter;
	private DispatcherTimer m_hideIndicatorsTimer;

	// Event Tokens
	// event_token m_gettingFocusToken{};
	// event_token m_isEnabledChangedToken{};
	// event_token m_unloadedToken{};

	// event_token m_scrollPresenterExtentChangedToken{};
	// event_token m_scrollPresenterStateChangedToken{};
	// event_token m_scrollPresenterScrollAnimationStartingToken{};
	// event_token m_scrollPresenterZoomAnimationStartingToken{};
	// event_token m_scrollPresenterViewChangedToken{};
	// event_token m_scrollPresenterScrollCompletedToken{};
	// event_token m_scrollPresenterZoomCompletedToken{};
	// event_token m_scrollPresenterBringingIntoViewToken{};
	// event_token m_scrollPresenterAnchorRequestedToken{};
	private long m_scrollPresenterComputedHorizontalScrollModeChangedToken;
	private long m_scrollPresenterComputedVerticalScrollModeChangedToken;

	private SerialDisposable m_horizontalScrollControllerCanScrollChangedToken;
	private SerialDisposable m_verticalScrollControllerCanScrollChangedToken;

	private SerialDisposable m_horizontalScrollControllerIsScrollingWithMouseChangedToken;
	private SerialDisposable m_verticalScrollControllerIsScrollingWithMouseChangedToken;

	private SerialDisposable m_renderingToken;

	private object m_onPointerEnteredEventHandler;
	private object m_onPointerMovedEventHandler;
	private object m_onPointerExitedEventHandler;
	private object m_onPointerPressedEventHandler;
	private object m_onPointerReleasedEventHandler;
	private object m_onPointerCanceledEventHandler;

	private object m_onHorizontalScrollControllerPointerEnteredHandler;
	private object m_onHorizontalScrollControllerPointerExitedHandler;
	private object m_onVerticalScrollControllerPointerEnteredHandler;
	private object m_onVerticalScrollControllerPointerExitedHandler;

	private FocusInputDeviceKind m_focusInputDeviceKind = FocusInputDeviceKind.None;

	// Used to detect changes for UISettings.AutoHiScrollBars.
	//private IUISettings5 m_uiSettings5;
	//IUISettings5::AutoHideScrollBarsChanged_revoker m_autoHideScrollBarsChangedRevoker{};

	private bool m_autoHideScrollControllersValid;
	private bool m_autoHideScrollControllers;

	private bool m_isLeftMouseButtonPressedForFocus;

	// Set to True when the mouse scrolling indicators are currently showing.
	private bool m_showingMouseIndicators;

	// Set to True to prevent the normal fade-out of the scrolling indicators.
	private bool m_keepIndicatorsShowing;

	// Set to True to favor mouse indicators over panning indicators for the scroll controllers.
	private bool m_preferMouseIndicators;

	// Indicates whether the NoIndicator visual state has a Storyboard for which a completion event was hooked up.
	private bool m_hasNoIndicatorStateStoryboardCompletedHandler;

	// Set to the values of IScrollController::IsScrollingWithMouse.
	private bool m_isHorizontalScrollControllerScrollingWithMouse;
	private bool m_isVerticalScrollControllerScrollingWithMouse;

	// Set to True when the pointer is over the optional scroll controllers.
	private bool m_isPointerOverHorizontalScrollController;
	private bool m_isPointerOverVerticalScrollController;

	private int m_verticalAddScrollVelocityDirection;
	private int m_verticalAddScrollVelocityOffsetChangeCorrelationId = -1;

	private int m_horizontalAddScrollVelocityDirection;
	private int m_horizontalAddScrollVelocityOffsetChangeCorrelationId = -1;

	// List of temporary ScrollViewBringIntoViewOperation instances used to track expected
	// ScrollPresenter::BringingIntoView occurrences due to navigation.
	private List<ScrollViewBringIntoViewOperation> m_bringIntoViewOperations = new();

	// Private constants    
	// 2 seconds delay used to hide the indicators for example when OS animations are turned off.
	private const long s_noIndicatorCountdown = 2000 * 10000;

	private const string s_noIndicatorStateName = "NoIndicator";
	private const string s_touchIndicatorStateName = "TouchIndicator";
	private const string s_mouseIndicatorStateName = "MouseIndicator";

	private const string s_scrollBarsSeparatorExpanded = "ScrollBarsSeparatorExpanded";
	private const string s_scrollBarsSeparatorCollapsed = "ScrollBarsSeparatorCollapsed";
	private const string s_scrollBarsSeparatorCollapsedDisabled = "ScrollBarsSeparatorCollapsedDisabled";
	private const string s_scrollBarsSeparatorCollapsedWithoutAnimation = "ScrollBarsSeparatorCollapsedWithoutAnimation";
	private const string s_scrollBarsSeparatorDisplayedWithoutAnimation = "ScrollBarsSeparatorDisplayedWithoutAnimation";
	private const string s_scrollBarsSeparatorExpandedWithoutAnimation = "ScrollBarsSeparatorExpandedWithoutAnimation";
};
