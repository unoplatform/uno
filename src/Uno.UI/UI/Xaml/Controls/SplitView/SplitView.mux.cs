// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference SplitView_Partial.cpp, tag winui3/release/1.4.2

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Content))]
public partial class SplitView : Control
{
	// Index table as follows: [DisplayMode][Placement][IsOpen]
	private static readonly string[][][] g_visualStateTable =
	{
		// Overlay
		new[] {
			new[] { "Closed", "OpenOverlayLeft" },
			new[] { "Closed", "OpenOverlayRight" }
		},

		// Inline
		new[] {
			new[] { "Closed", "OpenInlineLeft" },
			new[] { "Closed", "OpenInlineRight" }
		},

		// CompactOverlay
		new[] {
			new[] { "ClosedCompactLeft", "OpenCompactOverlayLeft" },
			new[] { "ClosedCompactRight", "OpenCompactOverlayRight" }
		},

		// CompactInline
		new[] {
			new[] { "ClosedCompactLeft", "OpenInlineLeft" },
			new[] { "ClosedCompactRight", "OpenInlineRight" }
		}
	};

	private IDisposable m_loadedEventHandler;
	private IDisposable m_unloadedEventHandler;
	private IDisposable m_sizeChangedEventHandler;
	private IDisposable m_xamlRootChangedEventHandler;

	private IDisposable m_displayModeStateChangedEventHandler;

	// These elements are used to form the outer dismiss layer that detects user interactions
	// outside of the SplitView bounds.  There are 4 polygonal elements arranged around the SplitView.
	// They are split up into individual elements rather than being a single path element because of
	// limitations in DComps input handling which would not correctly pass input through an element
	// on top of another DComp element (such as a WebView).
	//
	// We defer creation of these elements when the SplitView takes up the full window,
	// which is the common case.  Some apps, such as Spartan, have the SplitView nested.
	private Popup m_outerDismissLayerPopup;
	private Grid m_dismissHostElement;
	private Path m_topDismissElement;
	private Path m_bottomDismissElement;
	private Path m_leftDismissElement;
	private Path m_rightDismissElement;

	private IDisposable m_dismissLayerPointerPressedEventHandler;

	// Template Parts
	private FrameworkElement m_tpPaneRoot;
	private FrameworkElement m_tpContentRoot;
	private VisualStateGroup m_tpDisplayModeStates;

	private bool m_isPaneOpeningOrClosing;

	// SplitView::~SplitView()
	// {
	// 	auto xamlRoot = XamlRoot::GetForElementStatic(this);
	// 	if (m_xamlRootChangedEventHandler && xamlRoot)
	// 	{
	// 		VERIFYHR(m_xamlRootChangedEventHandler.DetachEventHandler(xamlRoot.Get()));
	// 	}
	//
	// 	VERIFYHR(BackButtonIntegration_UnregisterListener(this));
	// }

	public event TypedEventHandler<SplitView, object> PaneClosed;
	public event TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs> PaneClosing;
	public event TypedEventHandler<SplitView, object> PaneOpened;
	public event TypedEventHandler<SplitView, object> PaneOpening;

	public SplitView()
	{
		DefaultStyleKey = typeof(SplitView);
		// Uno Doc: original in CSplitView's constructor
		TemplateSettings = new SplitViewTemplateSettings();

		PrepareState(); // Uno Specific: should have been called by DXamlCore.GetPeerPrivate
	}

	private void PrepareState()
	{
		m_loadedEventHandler?.Dispose();
		m_loadedEventHandler = Disposable.Create(() => Loaded -= OnLoaded);
		Loaded += OnLoaded;
		m_unloadedEventHandler?.Dispose();
		m_unloadedEventHandler = Disposable.Create(() => Unloaded -= OnUnloaded);
		Unloaded += OnUnloaded;
		m_sizeChangedEventHandler?.Dispose();
		m_sizeChangedEventHandler = Disposable.Create(() => SizeChanged -= OnSizeChanged);
		SizeChanged += OnSizeChanged;
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_tpPaneRoot = null;
		m_tpContentRoot = null;

		if (m_displayModeStateChangedEventHandler is { })
		{
			m_displayModeStateChangedEventHandler?.Dispose();
			m_displayModeStateChangedEventHandler = null;
			m_tpDisplayModeStates = null;
		}

		// Set window pattern on 'PaneRoot' element, if the splitview set to lightdismiss
		FrameworkElement paneRootAsFE = GetTemplateChild<FrameworkElement>("PaneRoot");
		if (paneRootAsFE is { })
		{
			m_tpPaneRoot = paneRootAsFE;
			// Uno TODO
			// paneRootAsFE.Cast<FrameworkElement>()->put_AutomationPeerFactoryIndex(static_cast<INT>(KnownTypeIndex::SplitViewPaneAutomationPeer));
		}

		FrameworkElement lightDismissLayerAsFE = GetTemplateChild<FrameworkElement>("LightDismissLayer");
		if (lightDismissLayerAsFE is { })
		{
			// Uno TODO
			// lightDismissLayerAsFE.Cast<FrameworkElement>()->put_AutomationPeerFactoryIndex(static_cast<INT>(KnownTypeIndex::SplitViewLightDismissAutomationPeer));

			// Uno TODO
			// lightDismissLayerAsFE.AllowsDragAndDropPassThrough = true;
		}

		FrameworkElement contentRootAsFE = GetTemplateChild<FrameworkElement>("ContentRoot");
		if (contentRootAsFE is { })
		{
			m_tpContentRoot = contentRootAsFE;
		}

		COnApplyTemplate();
	}

	protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
	{
		COnKeyDown(pArgs);
		bool isHandled = pArgs.Handled;

		if (!isHandled)
		{
			var displayMode = DisplayMode;
			if (displayMode is SplitViewDisplayMode.CompactInline or SplitViewDisplayMode.CompactOverlay)
			{
				VirtualKey originalKey = pArgs.OriginalKey;

				if (SharedHelpers.IsGamepadNavigationDirection(originalKey))
				{
					FocusNavigationDirection gamepadDirection = FocusSelection.GetNavigationDirection(originalKey);
					SplitView spSplitView = this;

					FocusManager pFocusManager = VisualTree.GetFocusManagerForElement(this)!;

					XYFocusOptions xyFocusOptions = XYFocusOptions.Default;
					xyFocusOptions.SearchRoot = spSplitView;
					xyFocusOptions.ShouldConsiderXYFocusKeyboardNavigation = true;
					xyFocusOptions.IgnoreClipping = false;
					xyFocusOptions.IgnoreCone = true;

					DependencyObject pCandidate = pFocusManager.FindNextFocus(new FindFocusOptions(gamepadDirection), xyFocusOptions);

					if (pCandidate is { } && m_tpPaneRoot is { } && m_tpContentRoot is { })
					{
						// we got a candidate
						bool shouldHandle = false;
						bool contentHasFocus = false;
						// IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(pCandidate, &spCandidateDO));
						DependencyObject spCandidateDO = pCandidate;

						// if pane has focus and candidate is from content (or)
						// content has focus and candidate is from the pane
						// Then handle the key and move focus. If not, do not handle and let it bubble up to auto-focus
						var paneHasFocus = m_tpPaneRoot.HasFocus();
						if (paneHasFocus)
						{
							shouldHandle = m_tpContentRoot.IsAncestorOf(spCandidateDO);
						}

						if (!shouldHandle)
						{
							contentHasFocus = m_tpContentRoot.HasFocus();
							if (contentHasFocus)
							{
								shouldHandle = m_tpPaneRoot.IsAncestorOf(spCandidateDO);
							}
						}

						if (shouldHandle)
						{
							// Uno Specific: WinUI doesn't add the last argument (forceBringIntoView)
							bool focusUpdated = this.SetFocusedElementWithDirection(spCandidateDO, FocusState.Keyboard, true, gamepadDirection, false);
							pArgs.Handled = focusUpdated;
						}
					}
				}
			}
		}
	}

	private void OnLoaded(object _1, RoutedEventArgs _2)
	{
		var splitViewCore = this;

		// Creates the Outer Dismiss Layer when we're setting our initial state
		bool isPaneOpen = false;
		isPaneOpen = IsPaneOpen;

		if (isPaneOpen && splitViewCore.CanLightDismiss())
		{
			SetupOuterDismissLayer();
		}

		var xamlRoot = XamlRoot.GetForElement(this);
		if (m_xamlRootChangedEventHandler is null && xamlRoot is { })
		{
			m_xamlRootChangedEventHandler = Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged);
			xamlRoot.Changed += OnXamlRootChanged;
		}

		// Uno Specific
		SynchronizeContentTemplatedParent();
	}

	private void OnUnloaded(object _1, RoutedEventArgs _2)
	{
		TeardownOuterDismissLayer();

		// Uno Specific: done here instead of destructors
		m_xamlRootChangedEventHandler?.Dispose();
		m_xamlRootChangedEventHandler = null;
		UnregisterEventHandlers();
	}

	private void OnSizeChanged(object _, SizeChangedEventArgs pArgs)
	{
		Size prevSize = pArgs.PreviousSize;

		var splitViewCore = this;

		// Light dismiss only if we're not setting our initial size.
		if ((prevSize.Width != 0 || prevSize.Height != 0) && splitViewCore.CanLightDismiss())
		{
			splitViewCore.TryCloseLightDismissiblePane();
		}
	}

	private void OnXamlRootChanged(XamlRoot _1, XamlRootChangedEventArgs _2)
	{
		var splitViewCore = this;
		if (splitViewCore.CanLightDismiss())
		{
			splitViewCore.TryCloseLightDismissiblePane();
		}
	}

	private void OnOuterDismissElementPointerPressed(object _1, PointerRoutedEventArgs _2)
	{
		var splitViewCore = this;
		if (splitViewCore.CanLightDismiss())
		{
			splitViewCore.TryCloseLightDismissiblePane();
		}
	}

	private void OnDisplayModeStateChanged(object _1, VisualStateChangedEventArgs _2)
	{
		// Only respond to visual state changes between opened and closed states.
		// We could get state changes between opened states if display mode is changed (such as going from Compact to Overlay)
		// while the pane is open.
		if (m_isPaneOpeningOrClosing)
		{
			m_isPaneOpeningOrClosing = false;

			bool isPaneOpen = IsPaneOpen;
			OnPaneOpenedOrClosed(isPaneOpen);
		}
	}

	// Second tab stop processing pass that handles the case where a child element
	// has overridden the tab-stop candidate but that new candidate isn't a child
	// of the SplitView pane. SplitView will override that new tab-stop candidate
	// to make sure focus stays within the pane.
	// _Check_return_ HRESULT
	// SplitView::ProcessCandidateTabStopOverride(
	// _In_opt_ DependencyObject* pFocusedElement,
	// _In_ DependencyObject* /*pCandidateTabStopElement*/,
	// _In_opt_ DependencyObject* pOverriddenCandidateTabStopElement,
	// const bool isBackward,
	// 	_Outptr_ DependencyObject** ppNewTabStop,
	// 	_Out_ BOOLEAN* pIsCandidateTabStopOverridden
	// )
	// {
	// 	if (pOverriddenCandidateTabStopElement != nullptr)
	// 	{
	// 		IFC_RETURN(ProcessTabStopInternal(pFocusedElement, pOverriddenCandidateTabStopElement, !isBackward, ppNewTabStop));
	// 		*pIsCandidateTabStopOverridden = (*ppNewTabStop != nullptr);
	// 	}
	//
	// 	return S_OK;
	// }
	//
	// _Check_return_ HRESULT
	// SplitView::ProcessTabStopInternal(
	// _In_opt_ DependencyObject* pFocusedElement,
	// _In_opt_ DependencyObject* pCandidateTabStopElement,
	// bool isForward,
	// 	_Outptr_result_maybenull_ DependencyObject** ppNewTabStop
	// )
	// {
	// 	xref_ptr<CDependencyObject> spNewTabStop;
	//
	// 	*ppNewTabStop = nullptr;
	//
	// 	spNewTabStop.attach(static_cast<CSplitView*>(GetHandle())->ProcessTabStop(
	// 		isForward,
	// 		pFocusedElement ? pFocusedElement->GetHandle() : nullptr,
	// 		pCandidateTabStopElement ? pCandidateTabStopElement->GetHandle() : nullptr));
	//
	// 	// Check to see if we overrode the tab stop candidate.
	// 	if (spNewTabStop)
	// 	{
	// 		IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(spNewTabStop, ppNewTabStop));
	// 	}
	//
	// 	return S_OK;
	// }

	internal override DependencyObject GetFirstFocusableElementOverride()
	{
		// If the splitview is open and light-dismissible, then always send focus to the pane.
		var splitViewCore = this;
		if (splitViewCore.CanLightDismiss())
		{
			DependencyObject element = splitViewCore.GetFirstFocusableElementFromPane();
			if (element is { })
			{
				return element;
			}
		}
		else
		{
			// Fallback to the default behavior.
			return base.GetFirstFocusableElementOverride();
		}

		return null;
	}

	internal override DependencyObject GetLastFocusableElementOverride()
	{
		// If the splitview is open and light-dismissible, then always send focus to the pane.
		var splitViewCore = this;
		if (splitViewCore.CanLightDismiss())
		{
			DependencyObject element = splitViewCore.GetLastFocusableElementFromPane();
			if (element is { })
			{
				return element;
			}
		}
		else
		{
			// Fallback to the default behavior.
			return base.GetLastFocusableElementOverride();
		}

		return null;
	}

	// _Check_return_ HRESULT
	// SplitView::OnBackButtonPressedImpl(_Out_ BOOLEAN* pHandled)
	// {
	// 	IFCPTR_RETURN(pHandled);
	//
	// 	bool canLightDismiss = static_cast<CSplitView*>(GetHandle())->CanLightDismiss();
	// 	if (canLightDismiss)
	// 	{
	// 		IFC_RETURN(put_IsPaneOpen(FALSE));
	// 		*pHandled = TRUE;
	// 	}
	//
	// 	return S_OK;
	// }

	private protected override void ChangeVisualState(bool useTransitions)
	{
		// DisplayModeStates
		{
			var displayMode = DisplayMode;

			var placement = PanePlacement;

			bool isPaneOpen = IsPaneOpen;

			// Look up the visual state based on display mode, placement and, ispaneopen state.
			var visualStateName = g_visualStateTable[(int)displayMode][(int)placement][isPaneOpen ? 1 : 0];
			GoToState(useTransitions, visualStateName, out _);
		}

		// OverlayVisibilityStates
		{
			bool isOverlayVisible = LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl(this);
			GoToState(useTransitions, isOverlayVisible ? "OverlayVisible" : "OverlayNotVisible", out _);
		}
	}

	private void OnIsPaneOpenChanged(bool isOpen)
	{
		RegisterForDisplayModeStatesChangedEvent();

		m_isPaneOpeningOrClosing = true;

		UpdateVisualState();

		if (isOpen)
		{
			// Raise the PaneOpening event.
			PaneOpening?.Invoke(this, null);

			// Call into the core object to set focus to the pane on opening.
			this.OnPaneOpening();

			// Uno TODO
			// if (DXamlCore::GetCurrent()->GetHandle()->BackButtonSupported())
			// {
			// 	IFC_RETURN(BackButtonIntegration_RegisterListener(this));
			// }
			SetupOuterDismissLayer();
		}
		else
		{
			// Call into the core object to raise the PaneClosing event, which is raised asynchronously.
			// This will also restore focus to whichever element had focus when the pane opened.
			this.OnPaneClosing();

			// Uno TODO
			// IFC_RETURN(BackButtonIntegration_UnregisterListener(this));
			TeardownOuterDismissLayer();
		}

		// If the display modes states changing event was not registered, then
		// do the opened/closed work here instead. This could be the case if
		// the SplitView has been re-templated to remove the 'DisplayModeStates'
		// state group.
		if (m_displayModeStateChangedEventHandler is null)
		{
			OnPaneOpenedOrClosed(isOpen);
		}
	}

	private void OnDisplayModeChanged()
	{
		if (this.CanLightDismiss())
		{
			SetupOuterDismissLayer();
		}
		else
		{
			TeardownOuterDismissLayer();
		}
	}

	private void SetupOuterDismissLayer()
	{
		// If we're not in a light-dismissible state, then we can bail out of
		// setting up this dismiss layer.
		if (!CanLightDismiss())
		{
			return;
		}

		if (!IsInLiveTree)
		{
			return;
		}

		// To detect input outside of the SplitView's bounds, we create
		// a popup layer that hosts 4 polygonal elements arranged around
		// the SplitView.  The Top and Bottom elements are defined by 6
		// points, while the Left and Right elements are defined by 4 points.
		// We make them polygons rather than rectangles to account for any
		// transforms that may be applied to the SplitView (such as skew).
		//
		// Pointer handlers are attached to the elements and dismiss the
		// pane if activated.
		//
		// We try to create only the elements that are needed, for example,
		// a full screen SplitView does not need any, so we bail early.
		// Some apps that have nested SplitViews might not be full screen.
		// A notable example is Spartan where their SplitView is stacked
		// underneath the title/menu bar.  In that case, only the top
		// element should be needed.
		//
		//    X-------------------------------X
		//    |                               |
		//    |              Top              |
		//    |                               |
		//    X-------X---------------X-------X
		//    |       |               |       |
		//    | Left  |   SplitView   | Right |
		//    |       |               |       |
		//    X-------X---------------X-------X
		//    |                               |
		//    |             Bottom            |
		//    |                               |
		//    X-------------------------------X
		//
		// TODO: At better solution would likely involve some coordination
		// with the input manager, however at the time of writing this, that
		// was a riskier option.  We ultimately went with this solution because
		// it is relatively low risk, but we should revisit this in some future MQ.
		// Task 2386326 has been opened to track improvements in this area.

		Rect windowBounds = this.GetAbsoluteBoundsRect();

		double actualWidth = ActualWidth;
		double actualHeight = ActualHeight;

		GeneralTransform transformToVisual = TransformToVisual(null);

		// Transform all 4 corners of the SplitView's bounds.
		Point topLeftCorner;
		Point topRightCorner;
		Point bottomRightCorner;
		Point bottomLeftCorner;

		var flowDirection = FlowDirection;
		if (flowDirection == FlowDirection.LeftToRight)
		{
			topLeftCorner = transformToVisual.TransformPoint(new Point(0, 0));
			topRightCorner = transformToVisual.TransformPoint(new Point(actualWidth, 0));
			bottomRightCorner = transformToVisual.TransformPoint(new Point(actualWidth, actualHeight));
			bottomLeftCorner = transformToVisual.TransformPoint(new Point(0, actualHeight));
		}
		else
		{
			topLeftCorner = transformToVisual.TransformPoint(new Point(actualWidth, 0));
			topRightCorner = transformToVisual.TransformPoint(new Point(0, 0));
			bottomRightCorner = transformToVisual.TransformPoint(new Point(0, actualHeight));
			bottomLeftCorner = transformToVisual.TransformPoint(new Point(actualWidth, actualHeight));
		}

		// Determine which elements we need based on the translated corner points.
		bool needsTop = topLeftCorner.Y > 0 || topRightCorner.Y > 0;
		bool needsBottom = bottomLeftCorner.Y < windowBounds.Height || bottomRightCorner.Y < windowBounds.Height;
		bool needsLeft = topLeftCorner.X > 0 || bottomLeftCorner.X > 0;
		bool needsRight = topRightCorner.X < windowBounds.Width || bottomRightCorner.X < windowBounds.Width;

		// If the SplitView doesn't need any of the 4 elements, then it takes up the entire
		// window, so we can bail early.
		if (!needsTop && !needsBottom && !needsLeft && !needsRight)
		{
			return;
		}

		if (m_dismissHostElement is null)
		{
			Grid grid = new Grid();
			m_dismissHostElement = grid;

			m_dismissLayerPointerPressedEventHandler?.Dispose();
			m_dismissLayerPointerPressedEventHandler = Disposable.Create(() => grid.PointerPressed -= OnOuterDismissElementPointerPressed);
			grid.PointerPressed += OnOuterDismissElementPointerPressed;
		}

		if (m_outerDismissLayerPopup is null)
		{
			Popup popup = new Popup();
			popup.Child = m_dismissHostElement;
			m_outerDismissLayerPopup = popup;
		}

		// Uno Specific
		// m_outerDismissLayerPopup.SetAssociatedIsland(VisualTree.GetRootOrIslandForElement(this));
		m_outerDismissLayerPopup.XamlRoot = XamlRoot;

		m_outerDismissLayerPopup.IsOpen = true;

		if (needsTop)
		{
			if (m_topDismissElement is { })
			{
				m_topDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_topDismissElement = CreatePolygonalPath(m_dismissHostElement, 6);
			}

			Point[] points = { new Point(0, 0), new Point(windowBounds.Width, 0), new Point(windowBounds.Width, topRightCorner.Y), topRightCorner, topLeftCorner, new Point(0, topLeftCorner.Y) };
			UpdatePolygonalPath(m_topDismissElement, points);
		}
		else if (m_topDismissElement is { })
		{
			m_topDismissElement.Visibility = Visibility.Collapsed;
		}

		if (needsBottom)
		{
			if (m_bottomDismissElement is { })
			{
				m_bottomDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_bottomDismissElement = CreatePolygonalPath(m_dismissHostElement, 6);
			}

			Point[] points = { new Point(0, bottomLeftCorner.Y), bottomLeftCorner, bottomRightCorner, new Point(windowBounds.Width, bottomRightCorner.Y), new Point(windowBounds.Width, windowBounds.Height), new Point(0, windowBounds.Height) };
			UpdatePolygonalPath(m_bottomDismissElement, points);
		}
		else if (m_bottomDismissElement is { })
		{
			m_bottomDismissElement.Visibility = Visibility.Collapsed;
		}

		if (needsLeft)
		{
			if (m_leftDismissElement is { })
			{
				m_leftDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_leftDismissElement = CreatePolygonalPath(m_dismissHostElement, 4);
			}

			Point[] points = { new Point(0, topLeftCorner.Y), topLeftCorner, bottomLeftCorner, new Point(0, bottomLeftCorner.Y) };
			UpdatePolygonalPath(m_leftDismissElement, points);
		}
		else if (m_leftDismissElement is { })
		{
			m_leftDismissElement.Visibility = Visibility.Collapsed;
		}

		if (needsRight)
		{
			if (m_rightDismissElement is { })
			{
				m_rightDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_rightDismissElement = CreatePolygonalPath(m_dismissHostElement, 4);
			}

			Point[] points = { topRightCorner, new Point(windowBounds.Width, topRightCorner.Y), new Point(windowBounds.Width, bottomRightCorner.Y), bottomRightCorner };
			UpdatePolygonalPath(m_rightDismissElement, points);
		}
		else if (m_rightDismissElement is { })
		{
			m_rightDismissElement.Visibility = Visibility.Collapsed;
		}
	}

	private void TeardownOuterDismissLayer()
	{
		if (m_outerDismissLayerPopup is { })
		{
			m_outerDismissLayerPopup.IsOpen = false;
		}
	}

	private void RegisterForDisplayModeStatesChangedEvent()
	{
		if (m_displayModeStateChangedEventHandler is null)
		{
			VisualStateGroup displayModeStates = GetTemplateChild<VisualStateGroup>("DisplayModeStates");
			if (displayModeStates is { })
			{
				m_displayModeStateChangedEventHandler?.Dispose();
				m_displayModeStateChangedEventHandler = Disposable.Create(() => displayModeStates.CurrentStateChanged -= OnDisplayModeStateChanged);
				displayModeStates.CurrentStateChanged += OnDisplayModeStateChanged;
				m_tpDisplayModeStates = displayModeStates;
			}
		}
	}

	private void OnPaneOpenedOrClosed(bool isPaneOpen)
	{
		if (isPaneOpen)
		{
			// Raise the PaneOpened event.
			PaneOpened?.Invoke(this, null);

			// IFC_RETURN(DirectUI::ElementSoundPlayerService::RequestInteractionSoundForElementStatic(xaml::ElementSoundKind_Show, this));
		}
		else
		{
			// Call into the core object to raise the PaneClosed event, which is raised asynchronously.
			this.OnPaneClosed();

			// IFC_RETURN(DirectUI::ElementSoundPlayerService::RequestInteractionSoundForElementStatic(xaml::ElementSoundKind_Hide, this));
		}
	}

	private Path CreatePolygonalPath(Grid hostElement, int numPoints)
	{
		Path path = new Path();

		UIElementCollection children = hostElement.Children;
		children.Add(path);

		// Set a transparent brush to make sure it's hit-testable.
		SolidColorBrush transparentBrush = new SolidColorBrush();
		transparentBrush.Color = new Color(0, 0, 0, 0);
		path.Fill = transparentBrush;

		// Create the path geometry into which we'll add our rectangle figures.
		PathGeometry pathGeometry = new PathGeometry();
		path.Data = pathGeometry;

		PathFigureCollection figures = pathGeometry.Figures;

		PathFigure figure = new PathFigure();

		figures.Add(figure);

		figure.IsClosed = true;

		PathSegmentCollection segments = figure.Segments;

		// The number of segments we have is equal to the number of points we have minus 1
		// because this is a closed figure.
		for (int i = 0; i < (numPoints - 1); ++i)
		{
			LineSegment segment = new LineSegment();
			segments.Add(segment);
		}

		// Uno TODO
		// var uiElementCore = path;
		// uiElementCore.SetAllowsDragAndDropPassThrough(true);

		return path;
	}

	private void UpdatePolygonalPath(Path path, Point[] points)
	{
		Geometry geometry = path.Data;

		PathGeometry pathGeometry = geometry as PathGeometry;

		PathFigureCollection figures = pathGeometry.Figures;

		PathFigure figure = figures[0];

		figure.StartPoint = points[0];

		PathSegmentCollection segments = figure.Segments;

		for (int i = 1; i < points.Length; ++i)
		{
			// Subtract 1 because the first point in our array is set as the figure's
			// start point.
			PathSegment segment = segments[i - i];
			global::System.Diagnostics.Debug.Assert(segment is LineSegment);

			((LineSegment)segment).Point = points[i];
		}
	}

	// Uno Specific to the end of this file
	private void OnContentChanged(DependencyPropertyChangedEventArgs e)
	{
		SynchronizeContentTemplatedParent();
	}

	protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnTemplatedParentChanged(e);

		// This is required to ensure that FrameworkElement.FindName can dig through the tree after
		// the control has been created.
		SynchronizeContentTemplatedParent();
	}

	private void SynchronizeContentTemplatedParent()
	{
		// Manual propagation of the templated parent to the content property
		// until we get the propagation running properly
		if (Content is IFrameworkElement contentBinder)
		{
			contentBinder.TemplatedParent = this.TemplatedParent;
		}
		if (Pane is IFrameworkElement paneBinder)
		{
			paneBinder.TemplatedParent = this.TemplatedParent;
		}
	}
}
