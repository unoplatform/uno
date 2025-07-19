// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPager.cpp, tag winui3/release/1.4.2

using System;
using System.Collections.ObjectModel;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
#if !HAS_UNO_WINUI // Avoid duplicate using for WinUI build
using Microsoft.UI.Xaml.Automation.Peers;
#endif
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using ButtonVisibility = Microsoft.UI.Xaml.Controls.PipsPagerButtonVisibility;
using System.Globalization;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a control that enables navigation within linearly paginated content using
/// a configurable collection of glyphs, each of which represents a single "page" within a limitless range.
/// </summary>
public partial class PipsPager : Control
{
	//private const string s_pipButtonHandlersPropertyName = "PipButtonHandlers"; Not used in Uno

	private const string c_previousPageButtonVisibleVisualState = "PreviousPageButtonVisible";
	private const string c_previousPageButtonHiddenVisualState = "PreviousPageButtonHidden";
	private const string c_previousPageButtonCollapsedVisualState = "PreviousPageButtonCollapsed";

	private const string c_previousPageButtonEnabledVisualState = "PreviousPageButtonEnabled";
	private const string c_previousPageButtonDisabledVisualState = "PreviousPageButtonDisabled";

	private const string c_nextPageButtonVisibleVisualState = "NextPageButtonVisible";
	private const string c_nextPageButtonHiddenVisualState = "NextPageButtonHidden";
	private const string c_nextPageButtonCollapsedVisualState = "NextPageButtonCollapsed";

	private const string c_nextPageButtonEnabledVisualState = "NextPageButtonEnabled";
	private const string c_nextPageButtonDisabledVisualState = "NextPageButtonDisabled";

	private const string c_previousPageButtonName = "PreviousPageButton";
	private const string c_nextPageButtonName = "NextPageButton";

	private const string c_pipsPagerRepeaterName = "PipsPagerItemsRepeater";
	private const string c_pipsPagerScrollViewerName = "PipsPagerScrollViewer";

	//private const string c_pipsPagerVerticalOrientationButtonWidthPropertyName = "PipsPagerVerticalOrientationButtonWidth";
	//private const string c_pipsPagerVerticalOrientationButtonHeightPropertyName = "PipsPagerVerticalOrientationButtonHeight";

	//private const string c_pipsPagerHorizontalOrientationButtonWidthPropertyName = "PipsPagerHorizontalOrientationButtonWidth";
	//private const string c_pipsPagerHorizontalOrientationButtonHeightPropertyName = "PipsPagerHorizontalOrientationButtonHeight";

	private const string c_pipsPagerHorizontalOrientationVisualState = "HorizontalOrientationView";
	private const string c_pipsPagerVerticalOrientationVisualState = "VerticalOrientationView";

	private const string c_pipsPagerButtonVerticalOrientationVisualState = "VerticalOrientation";
	private const string c_pipsPagerButtonHorizontalOrientationVisualState = "HorizontalOrientation";

	/// <summary>
	/// Initializes a new instance of the PipsPager class.
	/// </summary>
	public PipsPager()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_PipsPager);

		m_pipsPagerItems = new ObservableCollection<int>();
		var templateSettings = new PipsPagerTemplateSettings();
		templateSettings.SetValue(PipsPagerTemplateSettings.PipsPagerItemsProperty, m_pipsPagerItems);
		SetValue(TemplateSettingsProperty, templateSettings);

		//s_pipButtonHandlersProperty =
		//	InitializeDependencyProperty(
		//		s_pipButtonHandlersPropertyName,
		//		name_of<PipsPagerViewItemRevokers>(),
		//		name_of<PipsPager>(),
		//		true,
		//		null,
		//		null);

		DefaultStyleKey = typeof(PipsPager);
	}

	protected override void OnApplyTemplate()
	{
		AutomationProperties.SetName(this, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PipsPagerNameText));

		m_previousPageButtonClickRevoker.Disposable = null;

		void AssignPreviousPageButton(Button button)
		{
			m_previousPageButton = button;
			if (button != null)
			{
				AutomationProperties.SetName(button, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PipsPagerPreviousPageButtonText));
				button.Click += OnPreviousButtonClicked;
				m_previousPageButtonClickRevoker.Disposable = Disposable.Create(() => button.Click -= OnPreviousButtonClicked);
			}
		}
		AssignPreviousPageButton(GetTemplateChild(c_previousPageButtonName) as Button);

		m_nextPageButtonClickRevoker.Disposable = null;
		void AssignNextPageButton(Button button)
		{
			m_nextPageButton = button;
			if (button != null)
			{
				AutomationProperties.SetName(button, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PipsPagerNextPageButtonText));
				button.Click += OnNextButtonClicked;
				m_nextPageButtonClickRevoker.Disposable = Disposable.Create(() => button.Click -= OnNextButtonClicked);
			}
		}
		AssignNextPageButton(GetTemplateChild(c_nextPageButtonName) as Button);

		m_pipsPagerElementPreparedRevoker.Disposable = null;
		m_pipsAreaGettingFocusRevoker.Disposable = null;
		m_pipsAreaBringIntoViewRequestedRevoker.Disposable = null;
		void AssignPipsPagerRepeater(ItemsRepeater repeater)
		{

			m_pipsPagerRepeater = repeater;
			if (repeater != null)
			{
				repeater.ElementPrepared += OnElementPrepared;
				m_pipsPagerElementPreparedRevoker.Disposable = Disposable.Create(() => repeater.ElementPrepared -= OnElementPrepared);
				repeater.GettingFocus += OnPipsAreaGettingFocus;
				m_pipsAreaGettingFocusRevoker.Disposable = Disposable.Create(() => repeater.GettingFocus -= OnPipsAreaGettingFocus);
				repeater.BringIntoViewRequested += OnPipsAreaBringIntoViewRequested;
				m_pipsAreaBringIntoViewRequestedRevoker.Disposable = Disposable.Create(() => repeater.BringIntoViewRequested -= OnPipsAreaBringIntoViewRequested);
			}
		}
		AssignPipsPagerRepeater(GetTemplateChild(c_pipsPagerRepeaterName) as ItemsRepeater);

		m_scrollViewerBringIntoViewRequestedRevoker.Disposable = null;

		void InitScrollViewer(ScrollViewer scrollViewer)
		{
			m_pipsPagerScrollViewer = scrollViewer;
			if (scrollViewer != null)
			{
				scrollViewer.BringIntoViewRequested += OnScrollViewerBringIntoViewRequested;
				m_scrollViewerBringIntoViewRequestedRevoker.Disposable =
					Disposable.Create(() => scrollViewer.BringIntoViewRequested -= OnScrollViewerBringIntoViewRequested);
			}
		}
		InitScrollViewer(GetTemplateChild<ScrollViewer>(c_pipsPagerScrollViewerName));

		m_defaultPipSize = GetDesiredPipSize(NormalPipStyle);
		m_selectedPipSize = GetDesiredPipSize(SelectedPipStyle);
		OnNavigationButtonVisibilityChanged(PreviousButtonVisibility, c_previousPageButtonCollapsedVisualState, c_previousPageButtonDisabledVisualState);
		OnNavigationButtonVisibilityChanged(NextButtonVisibility, c_nextPageButtonCollapsedVisualState, c_nextPageButtonDisabledVisualState);
		UpdatePipsItems(NumberOfPages, MaxVisiblePips);
		OnOrientationChanged();
		OnSelectedPageIndexChanged(m_lastSelectedPageIndex);
	}

	private void RaiseSelectedIndexChanged()
	{
		var args = new PipsPagerSelectedIndexChangedEventArgs();
		SelectedIndexChanged?.Invoke(this, args);
	}

	private Size GetDesiredPipSize(Style style)
	{
		var repeater = m_pipsPagerRepeater;
		if (repeater != null)
		{
			if (repeater.ItemTemplate is DataTemplate itemTemplate)
			{
				if (itemTemplate.LoadContent() is FrameworkElement element)
				{
					ApplyStyleToPipAndUpdateOrientation(element, style);
					element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					return element.DesiredSize;
				}
			}
		}

		return new Size(0.0, 0.0);
	}

	// UNO TODO: this is a workaround for the case when MaxVisiblePips is less than NumberOfPages.
	// Our current implementation of ScrollContentPresenter  doesn't calculate CanHorizontallyScroll correctly,
	// and therefore sends an incorrect availableSize to children during the layout cycle.
	// so we temporarily force it to be scrollable in order to layout correctly and then set it back so that
	// it's still not scrollable with a pointer, etc.
	protected override Size MeasureOverride(Size availableSize)
	{
		if (m_pipsPagerScrollViewer?.Presenter is { } presenter)
		{
			bool canScroll;
			if (Orientation is Orientation.Horizontal)
			{
				canScroll = presenter.CanHorizontallyScroll;
				presenter.CanHorizontallyScroll = true;
			}
			else
			{
				canScroll = presenter.CanVerticallyScroll;
				presenter.CanVerticallyScroll = true;
			}

			var result = base.MeasureOverride(availableSize);

			if (Orientation is Orientation.Horizontal)
			{
				presenter.CanHorizontallyScroll = canScroll;
			}
			else
			{
				presenter.CanVerticallyScroll = canScroll;
			}

			return result;
		}

		return base.MeasureOverride(availableSize);
	}

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		FocusNavigationDirection previousPipDirection;
		FocusNavigationDirection nextPipDirection;
		if (Orientation == Orientation.Vertical)
		{
			previousPipDirection = FocusNavigationDirection.Up;
			nextPipDirection = FocusNavigationDirection.Down;
		}
		else
		{
			previousPipDirection = FocusNavigationDirection.Left;
			nextPipDirection = FocusNavigationDirection.Right;
		}

		if (args.Key == VirtualKey.Left || args.Key == VirtualKey.Up)
		{
			var options = new FindNextElementOptions();
			options.SearchRoot = this.XamlRoot.Content;
			FocusManager.TryMoveFocus(previousPipDirection, options);
			args.Handled = true;
		}
		else if (args.Key == VirtualKey.Right || args.Key == VirtualKey.Down)
		{
			var options = new FindNextElementOptions();
			options.SearchRoot = this.XamlRoot.Content;
			FocusManager.TryMoveFocus(nextPipDirection, options);
			args.Handled = true;
		}
		// Call for all other presses
		base.OnKeyDown(args);
	}

	private void UpdateIndividualNavigationButtonVisualState(
		 bool hiddenOnEdgeCondition,
		 ButtonVisibility visibility,
		 string visibleStateName,
		 string hiddenStateName,
		 string enabledStateName,
		 string disabledStateName)
	{

		var ifGenerallyVisible = !hiddenOnEdgeCondition && NumberOfPages != 0 && MaxVisiblePips > 0;
		if (visibility != ButtonVisibility.Collapsed)
		{
			if ((visibility == ButtonVisibility.Visible || m_isPointerOver || m_isFocused) && ifGenerallyVisible)
			{
				VisualStateManager.GoToState(this, visibleStateName, false);
				VisualStateManager.GoToState(this, enabledStateName, false);
			}
			else
			{
				if (!ifGenerallyVisible)
				{
					VisualStateManager.GoToState(this, disabledStateName, false);
				}
				else
				{
					VisualStateManager.GoToState(this, enabledStateName, false);
				}
				VisualStateManager.GoToState(this, hiddenStateName, false);
			}
		}
	}

	private void UpdateNavigationButtonVisualStates()
	{
		int selectedPageIndex = SelectedPageIndex;
		int numberOfPages = NumberOfPages;

		var ifPreviousButtonHiddenOnEdge = selectedPageIndex == 0;
		UpdateIndividualNavigationButtonVisualState(ifPreviousButtonHiddenOnEdge, PreviousButtonVisibility,
			c_previousPageButtonVisibleVisualState, c_previousPageButtonHiddenVisualState,
			c_previousPageButtonEnabledVisualState, c_previousPageButtonDisabledVisualState);

		var ifNextButtonHiddenOnEdge = selectedPageIndex == numberOfPages - 1;
		UpdateIndividualNavigationButtonVisualState(ifNextButtonHiddenOnEdge, NextButtonVisibility,
			c_nextPageButtonVisibleVisualState, c_nextPageButtonHiddenVisualState,
			c_nextPageButtonEnabledVisualState, c_nextPageButtonDisabledVisualState);
	}

	private void ScrollToCenterOfViewport(UIElement sender, int index)
	{
		var options = new BringIntoViewOptions();
		if (Orientation == Orientation.Horizontal)
		{
			options.HorizontalAlignmentRatio = 0.5;
		}
		else
		{
			options.VerticalAlignmentRatio = 0.5;
		}
		options.AnimationDesired = true;
		sender.StartBringIntoView(options);
	}

	private void UpdateSelectedPip(int index)
	{
		if (NumberOfPages != 0 && MaxVisiblePips > 0)
		{
			var repeater = m_pipsPagerRepeater;
			if (repeater != null)
			{
				repeater.UpdateLayout();
				if (repeater.TryGetElement(m_lastSelectedPageIndex) is FrameworkElement pip)
				{
					ApplyStyleToPipAndUpdateOrientation(pip, NormalPipStyle);
				}
				if (repeater.GetOrCreateElement(index) is FrameworkElement pip2)
				{
					ApplyStyleToPipAndUpdateOrientation(pip2, SelectedPipStyle);
					ScrollToCenterOfViewport(pip2, index);
				}
			}
		}
	}

	private double CalculateScrollViewerSize(double defaultPipSize, double selectedPipSize, int numberOfPages, int maxVisualIndicators)
	{
		var numberOfPagesToDisplay = 0;
		maxVisualIndicators = Math.Max(0, maxVisualIndicators);
		if (maxVisualIndicators == 0 || numberOfPages == 0)
		{
			return 0;
		}
		else if (numberOfPages > 0)
		{
			numberOfPagesToDisplay = Math.Min(maxVisualIndicators, numberOfPages);
		}
		else
		{
			numberOfPagesToDisplay = maxVisualIndicators;
		}
		return defaultPipSize * (numberOfPagesToDisplay - 1) + selectedPipSize;
	}

	private void SetScrollViewerMaxSize()
	{
		var scrollViewer = m_pipsPagerScrollViewer;
		if (scrollViewer != null)
		{
			if (Orientation == Orientation.Horizontal)
			{
				var scrollViewerWidth = CalculateScrollViewerSize(m_defaultPipSize.Width, m_selectedPipSize.Width, NumberOfPages, MaxVisiblePips);
				scrollViewer.MaxWidth = scrollViewerWidth;
				scrollViewer.MaxHeight = Math.Max(m_defaultPipSize.Height, m_selectedPipSize.Height);
			}
			else
			{
				var scrollViewerHeight = CalculateScrollViewerSize(m_defaultPipSize.Height, m_selectedPipSize.Height, NumberOfPages, MaxVisiblePips);
				scrollViewer.MaxHeight = scrollViewerHeight;
				scrollViewer.MaxWidth = Math.Max(m_defaultPipSize.Width, m_selectedPipSize.Width);
			}
		}
	}

	private void UpdatePipsItems(int numberOfPages, int maxVisualIndicators)
	{
		var pipsListSize = m_pipsPagerItems.Count;

		if (numberOfPages == 0 || maxVisualIndicators == 0)
		{
			m_pipsPagerItems.Clear();
		}
		/* Inifinite number of pages case */
		else if (numberOfPages < 0)
		{
			/* Treat negative max visual indicators as 0*/
			var minNumberOfElements = Math.Max(SelectedPageIndex + 1, Math.Max(0, maxVisualIndicators));
			if (minNumberOfElements > pipsListSize)
			{
				for (int i = pipsListSize; i < minNumberOfElements; i++)
				{
					m_pipsPagerItems.Add(i + 1);
				}
			}
			else if (SelectedPageIndex == pipsListSize - 1)
			{
				m_pipsPagerItems.Add(pipsListSize + 1);
			}
		}
		else if (pipsListSize < numberOfPages)
		{
			for (int i = pipsListSize; i < numberOfPages; i++)
			{
				m_pipsPagerItems.Add(i + 1);
			}
		}
		else
		{
			for (int i = numberOfPages; i < pipsListSize; i++)
			{
				m_pipsPagerItems.Remove(m_pipsPagerItems.Count - 1);
			}
		}
	}

	private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
	{
		if (args.Element is FrameworkElement element)
		{
			var index = args.Index;
			var style = index == SelectedPageIndex ? SelectedPipStyle : NormalPipStyle;
			ApplyStyleToPipAndUpdateOrientation(element, style);

			AutomationProperties.SetName(element, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PipsPagerPageText) + " " + (index + 1).ToString(CultureInfo.InvariantCulture));
			AutomationProperties.SetPositionInSet(element, index + 1);
			AutomationProperties.SetSizeOfSet(element, NumberOfPages);

			if (element is ButtonBase pip)
			{
				void PipClickHandler(object sender, EventArgs e)
				{
					if (m_pipsPagerRepeater is { } repeater)
					{
						if (sender is Button button)
						{
							SelectedPageIndex = repeater.GetElementIndex(button);
						}
					}
				}
				pip.Click += PipClickHandler;
			}
		}
	}

#if false
	private void OnElementIndexChanged(ItemsRepeater itemsRepeater, ItemsRepeaterElementIndexChangedEventArgs args)
	{
		var pip = args.Element;
		if (pip != null)
		{
			var newIndex = args.NewIndex;
			AutomationProperties.SetName(pip, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PipsPagerPageText) + " " + (newIndex + 1));
			AutomationProperties.SetPositionInSet(pip, newIndex + 1);
		}
	}
#endif

	private void OnMaxVisualIndicatorsChanged()
	{
		var numberOfPages = NumberOfPages;
		if (numberOfPages < 0)
		{
			UpdatePipsItems(numberOfPages, MaxVisiblePips);
		}
		SetScrollViewerMaxSize();
		UpdateSelectedPip(SelectedPageIndex);
		UpdateNavigationButtonVisualStates();
	}

	private void OnNumberOfPagesChanged()
	{
		int numberOfPages = NumberOfPages;
		int selectedPageIndex = SelectedPageIndex;
		UpdateSizeOfSetForElements(numberOfPages, m_pipsPagerItems.Count);
		UpdatePipsItems(numberOfPages, MaxVisiblePips);
		SetScrollViewerMaxSize();
		if (SelectedPageIndex > numberOfPages - 1 && numberOfPages > -1)
		{
			SelectedPageIndex = numberOfPages - 1;
		}
		else
		{
			UpdateSelectedPip(selectedPageIndex);
			UpdateNavigationButtonVisualStates();
		}
	}

	private void OnSelectedPageIndexChanged(int oldValue)
	{
		// If we don't have any pages, there is nothing we should do.
		// Ensure that SelectedPageIndex will end up in the valid range of values
		// Special case is NumberOfPages being 0, in that case, don't verify upperbound restrictions
		if (SelectedPageIndex > NumberOfPages - 1 && NumberOfPages > 0)
		{
			SelectedPageIndex = NumberOfPages - 1;
		}
		else if (SelectedPageIndex < 0)
		{
			SelectedPageIndex = 0;
		}
		else
		{
			// Now handle the value changes
			m_lastSelectedPageIndex = oldValue;

			// Fire value property change for UIA
			if (FrameworkElementAutomationPeer.FromElement(this) is PipsPagerAutomationPeer peer)
			{
				peer.RaiseSelectionChanged(m_lastSelectedPageIndex, SelectedPageIndex);
			}
			if (NumberOfPages < 0)
			{
				UpdatePipsItems(NumberOfPages, MaxVisiblePips);
			}
			UpdateSelectedPip(SelectedPageIndex);
			UpdateNavigationButtonVisualStates();
			RaiseSelectedIndexChanged();
		}
	}

	private void OnOrientationChanged()
	{
		if (Orientation == Orientation.Horizontal)
		{
			VisualStateManager.GoToState(this, c_pipsPagerHorizontalOrientationVisualState, false);
		}
		else
		{
			VisualStateManager.GoToState(this, c_pipsPagerVerticalOrientationVisualState, false);
		}
		if (m_pipsPagerRepeater is { } repeater)
		{
			if (repeater.ItemsSourceView is { } itemsSourceView)
			{
				var itemCount = itemsSourceView.Count;
				for (int i = 0; i < itemCount; i++)
				{
					if (repeater.TryGetElement(i) is Control pip)
					{
						UpdatePipOrientation(pip);
					}
				}
			}
		}
		m_defaultPipSize = GetDesiredPipSize(NormalPipStyle);
		m_selectedPipSize = GetDesiredPipSize(SelectedPipStyle);
		SetScrollViewerMaxSize();
		if (GetSelectedItem() is { } selectedPip)
		{
			ScrollToCenterOfViewport(selectedPip, SelectedPageIndex);
		}
	}

	private void ApplyStyleToPipAndUpdateOrientation(FrameworkElement pip, Style style)
	{
		pip.Style = style;
		if (pip is Control control)
		{
			control.ApplyTemplate();
			UpdatePipOrientation(control);
		}
	}

	private void UpdatePipOrientation(Control pip)
	{
		if (Orientation == Orientation.Horizontal)
		{
			VisualStateManager.GoToState(pip, c_pipsPagerButtonHorizontalOrientationVisualState, false);
		}
		else
		{
			VisualStateManager.GoToState(pip, c_pipsPagerButtonVerticalOrientationVisualState, false);
		}
	}

	private void OnNavigationButtonVisibilityChanged(ButtonVisibility visibility, string collapsedStateName, string disabledStateName)
	{
		if (visibility == ButtonVisibility.Collapsed)
		{
			VisualStateManager.GoToState(this, collapsedStateName, false);
			VisualStateManager.GoToState(this, disabledStateName, false);
		}
		else
		{
			UpdateNavigationButtonVisualStates();
		}
	}

	private void OnPreviousButtonClicked(object sender, RoutedEventArgs e)
	{
		// In this method, SelectedPageIndex is always greater than 0.
		SelectedPageIndex = SelectedPageIndex - 1;
	}

	private void OnNextButtonClicked(object sender, RoutedEventArgs e)
	{
		// In this method, SelectedPageIndex is always less than maximum.
		SelectedPageIndex = SelectedPageIndex + 1;
	}

	protected override void OnGotFocus(RoutedEventArgs args)
	{
		if (args.OriginalSource is Button btn)
		{
			// If the element inside the Pager is already keyboard focused
			// and the user will use the mouse to focus on something else
			// the LostFocus will not be triggered on keyboard focused element
			// while GotFocus will be triggered on the new mouse focused element.
			// We account for this scenario and update m_isFocused in case
			// user will use mouse while being in keyboard focus.
			if (btn.FocusState != FocusState.Pointer)
			{
				m_isFocused = true;
				UpdateNavigationButtonVisualStates();
			}
			else
			{
				m_isFocused = false;
			}
		}
	}

	private void OnPipsAreaGettingFocus(object sender, GettingFocusEventArgs args)
	{
		if (m_pipsPagerRepeater is { } repeater)
		{
			// Easiest way to check if focus change came from within:
			// Check if element is child of repeater by getting index and checking for -1
			// If it is -1, focus came from outside and we want to get to selected element.
			if (args.OldFocusedElement is UIElement oldFocusedElement)
			{
				if (repeater.GetElementIndex(oldFocusedElement) == -1)
				{
					if (repeater.GetOrCreateElement(SelectedPageIndex) is UIElement realizedElement)
					{
						if (args.TrySetNewFocusedElement(realizedElement))
						{
							args.Handled = true;
						}
					}
				}
			}
		}
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		m_isFocused = false;
		UpdateNavigationButtonVisualStates();
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);
		m_isPointerOver = true;
		UpdateNavigationButtonVisualStates();
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		// We can get a spurious Exited and then Entered if the button
		// that is being clicked on hides itself. In order to avoid switching
		// visual states in this case, we check if the pointer is over the
		// control bounds when we get the exited event.
		if (IsOutOfControlBounds(args.GetCurrentPoint(this).Position))
		{
			m_isPointerOver = false;
			UpdateNavigationButtonVisualStates();
		}
		else
		{
			args.Handled = true;
		}
		base.OnPointerExited(args);
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		base.OnPointerCanceled(args);
		m_isPointerOver = false;
		UpdateNavigationButtonVisualStates();
	}

	private bool IsOutOfControlBounds(Point point)
	{
		// This is a conservative check. It is okay to say we are
		// out of the bounds when close to the edge to account for rounding.
		var tolerance = 1.0;
		var actualWidth = ActualWidth;
		var actualHeight = ActualHeight;
		return point.X < tolerance ||
			point.X > actualWidth - tolerance ||
			point.Y < tolerance ||
			point.Y > actualHeight - tolerance;
	}

	// In order to handle undesired scrolling when a user
	// tabs into the pipspager/focuses a pip using keyboard
	// we'll check for offsets and if they're NAN -
	// meaning it was not scroll initiated by us, we handle it.
	private void OnPipsAreaBringIntoViewRequested(object sender, BringIntoViewRequestedEventArgs args)
	{
		if ((Orientation == Orientation.Vertical && double.IsNaN(args.VerticalAlignmentRatio)) ||
			(Orientation == Orientation.Horizontal && double.IsNaN(args.HorizontalAlignmentRatio)))
		{
			args.Handled = true;
		}
	}

	// Inner scrollviewer will bubble BringIntoView event to
	// parent scrollviewers (if they exist) if the scrolling was
	// not complete (could not scroll to specified offset because
	// the beginning/end of the scrollable area was already reached).
	// To avoid that, we handle BringIntoViewRequested on inner scrollviewer.
	private void OnScrollViewerBringIntoViewRequested(object sender, BringIntoViewRequestedEventArgs args)
	{
		args.Handled = true;
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;
		if (Template != null)
		{
			if (property == NumberOfPagesProperty)
			{
				OnNumberOfPagesChanged();
			}
			else if (property == SelectedPageIndexProperty)
			{
				OnSelectedPageIndexChanged((int)args.OldValue);
			}
			else if (property == MaxVisiblePipsProperty)
			{
				OnMaxVisualIndicatorsChanged();
			}
			else if (property == PreviousButtonVisibilityProperty)
			{
				OnNavigationButtonVisibilityChanged(PreviousButtonVisibility, c_previousPageButtonCollapsedVisualState, c_previousPageButtonDisabledVisualState);
			}
			else if (property == NextButtonVisibilityProperty)
			{
				OnNavigationButtonVisibilityChanged(NextButtonVisibility, c_nextPageButtonCollapsedVisualState, c_nextPageButtonDisabledVisualState);
			}
			else if (property == NormalPipStyleProperty)
			{
				m_defaultPipSize = GetDesiredPipSize(NormalPipStyle);
				SetScrollViewerMaxSize();
				UpdateSelectedPip(SelectedPageIndex);
			}
			else if (property == SelectedPipStyleProperty)
			{
				m_selectedPipSize = GetDesiredPipSize(SelectedPipStyle);
				SetScrollViewerMaxSize();
				UpdateSelectedPip(SelectedPageIndex);
			}
			else if (property == OrientationProperty)
			{
				OnOrientationChanged();
			}
		}
	}

	internal UIElement GetSelectedItem()
	{
		if (m_pipsPagerRepeater is { } repeater)
		{
			return repeater.TryGetElement(SelectedPageIndex);
		}
		return null;
	}

	protected override AutomationPeer OnCreateAutomationPeer() =>
		new PipsPagerAutomationPeer(this);

	void UpdateSizeOfSetForElements(int numberOfPages, int numberOfItems)
	{
		var repeater = m_pipsPagerRepeater;
		if (repeater != null)
		{
			for (int i = 0; i < numberOfItems; i++)
			{
				var pip = repeater.TryGetElement(i);
				if (pip != null)
				{
					AutomationProperties.SetSizeOfSet(pip, numberOfPages);
				}
			}
		}
	}
}






