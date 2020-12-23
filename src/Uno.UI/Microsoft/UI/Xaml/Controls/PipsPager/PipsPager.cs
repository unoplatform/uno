// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using ButtonVisibility = Microsoft.UI.Xaml.Controls.PipsPagerButtonVisibility;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PipsPager : Control
	{
		private const string s_pipButtonHandlersPropertyName = "PipButtonHandlers";

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

		private const string c_pipsPagerButtonWidthPropertyName = "PipsPagerButtonWidth";
		private const string c_pipsPagerButtonHeightPropertyName = "PipsPagerButtonHeight";

		private const string c_pipsPagerHorizontalOrientationVisualState = "HorizontalOrientationView";
		private const string c_pipsPagerVerticalOrientationVisualState = "VerticalOrientationView";

		public PipsPager()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_PipsPager);

			m_pipsPagerItems = new ObservableCollection<object>();
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
				if (button != null)
				{
					AutomationProperties.SetName(button, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PipsPagerNextPageButtonText));
					button.Click += OnNextButtonClicked;
					m_nextPageButtonClickRevoker.Disposable = Disposable.Create(() => button.Click -= OnNextButtonClicked);
				}
			}
			AssignNextPageButton(GetTemplateChild(c_nextPageButtonName) as Button);

			m_pipsPagerElementPreparedRevoker.Disposable = null;
			void AssignPipsPagerRepeater(ItemsRepeater repeater)
			{

				m_pipsPagerRepeater = repeater;
				if (repeater != null)
				{
					repeater.ElementPrepared += OnElementPrepared;
					m_pipsPagerElementPreparedRevoker.Disposable = Disposable.Create(() => repeater.ElementPrepared -= OnElementPrepared);
				}
			}
			AssignPipsPagerRepeater(GetTemplateChild(c_pipsPagerRepeaterName) as ItemsRepeater);

			m_pipsPagerScrollViewer = (ScrollViewer)GetTemplateChild(c_pipsPagerScrollViewerName);

			m_defaultPipSize = GetDesiredPipSize(DefaultIndicatorButtonStyle);
			m_selectedPipSize = GetDesiredPipSize(SelectedIndicatorButtonStyle);
			OnNavigationButtonVisibilityChanged(PreviousButtonVisibility, c_previousPageButtonCollapsedVisualState, c_previousPageButtonDisabledVisualState);
			OnNavigationButtonVisibilityChanged(NextButtonVisibility, c_nextPageButtonCollapsedVisualState, c_nextPageButtonDisabledVisualState);
			UpdatePipsItems(NumberOfPages, MaxVisualIndicators);
			OnOrientationChanged();
			OnSelectedPageIndexChanged(m_lastSelectedPageIndex);
		}

		private void RaiseSelectedIndexChanged()
		{
			var args = new PipsPagerSelectedIndexChangedEventArgs(m_lastSelectedPageIndex, SelectedPageIndex);
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
						element.Style(style);
						element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
						return element.DesiredSize;
					}
				}
			}
			/* Extract default sizes and return in case the code above fails */
			var pipHeight = (double)ResourceAccessor.ResourceLookup(this, c_pipsPagerButtonHeightPropertyName);
			var pipWidth = (double)ResourceAccessor.ResourceLookup(this, c_pipsPagerButtonWidthPropertyName);
			return new Size((float)(pipWidth), (float)(pipHeight));
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
				FocusManager.TryMoveFocus(previousPipDirection);
				args.Handled = true;
			}
			else if (args.Key == VirtualKey.Right || args.Key == VirtualKey.Down)
			{
				FocusManager.TryMoveFocus(nextPipDirection);
				args.Handled = true;
			}
			// Call for all other presses
			base.OnKeyDown(args);
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

		private void UpdateIndividualNavigationButtonVisualState(
			 bool hiddenOnEdgeCondition,
			 ButtonVisibility visibility,
			 string visibleStateName,
			 string hiddenStateName,
			 string enabledStateName,
			 string disabledStateName)
		{

			var ifGenerallyVisible = !hiddenOnEdgeCondition && NumberOfPages != 0 && MaxVisualIndicators > 0;
			if (visibility != ButtonVisibility.Collapsed)
			{
				if ((visibility == ButtonVisibility.Visible || m_isPointerOver) && ifGenerallyVisible)
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
			/* Vertical and Horizontal AligmentsRatio are not available until Win Version 1803 (sdk version 17134) */
			if (SharedHelpers.IsBringIntoViewOptionsVerticalAlignmentRatioAvailable())
			{
				var options = new BringIntoViewOptions();
				options.VerticalAlignmentRatio = 0.5;
				options.HorizontalAlignmentRatio = 0.5;
				options.AnimationDesired = true;
				sender.StartBringIntoView(options);
			}
			else if (m_pipsPagerScrollViewer is ScrollViewer scrollViewer)
			{
				double pipSize;
				Action<double> changeViewFunc;
				if (Orientation == Orientation.Horizontal)
				{
					pipSize = m_defaultPipSize.Width;
					changeViewFunc = (double offset) => { scrollViewer.ChangeView(offset, null, null); };
				}
				else
				{
					pipSize = m_defaultPipSize.Height;
					changeViewFunc = (double offset) => { scrollViewer.ChangeView(null, offset, null); };
				}
				int maxVisualIndicators = MaxVisualIndicators;
				/* This line makes sure that while having even # of indicators the scrolling will be done correctly */
				int offSetChangeForEvenSizeWindow = maxVisualIndicators % 2 == 0 && index > m_lastSelectedPageIndex ? 1 : 0;
				int offSetNumOfElements = index + offSetChangeForEvenSizeWindow - maxVisualIndicators / 2;
				double offset = Math.Max(0.0, offSetNumOfElements * pipSize);
				changeViewFunc(offset);
			}
		}

		private void UpdateSelectedPip(int index)
		{
			if (NumberOfPages != 0 && MaxVisualIndicators > 0)
			{
				var repeater = m_pipsPagerRepeater;
				if (repeater != null)
				{
					repeater.UpdateLayout();
					if (repeater.TryGetElement(m_lastSelectedPageIndex) is Button element)
					{
						element.Style = DefaultIndicatorButtonStyle;
					}
					if (repeater.GetOrCreateElement(index) is Button selectedElement)
					{
						selectedElement.Style = SelectedIndicatorButtonStyle;
						ScrollToCenterOfViewport(selectedElement, index);
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
					var scrollViewerWidth = CalculateScrollViewerSize(m_defaultPipSize.Width, m_selectedPipSize.Width, NumberOfPages, MaxVisualIndicators);
					scrollViewer.MaxWidth = scrollViewerWidth;
					scrollViewer.MaxHeight = Math.Max(m_defaultPipSize.Height, m_selectedPipSize.Height);
				}
				else
				{
					var scrollViewerHeight = CalculateScrollViewerSize(m_defaultPipSize.Height, m_selectedPipSize.Height, NumberOfPages, MaxVisualIndicators);
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
			var element = args.Element;
			if (element != null)
			{
				if (element is Button pip)
				{
					var index = args.Index;
					if (index != SelectedPageIndex)
					{
						pip.Style(DefaultIndicatorButtonStyle);
					}

					// Narrator says: Page 5, Button 5 of 30. Is it expected behavior?
					AutomationProperties.SetName(pip, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PipsPagerPageText) + " " + (index + 1));
					AutomationProperties.SetPositionInSet(pip, index + 1);
					AutomationProperties.SetSizeOfSet(pip, NumberOfPages);

					// TODO: This may leak - all buttons leave memory only with pager
					pip.Click += (sender, args) =>
					{
						var repeater = m_pipsPagerRepeater;
						if (repeater != null)
						{
							var button = sender as Button;
							if (button != null)
							{
								SelectedPageIndex = repeater.GetElementIndex(button);
							}
						}
					};
				}
			}
		}

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

		private void OnMaxVisualIndicatorsChanged()
		{
			var numberOfPages = NumberOfPages;
			if (numberOfPages < 0)
			{
				UpdatePipsItems(numberOfPages, MaxVisualIndicators);
			}
			SetScrollViewerMaxSize();
			UpdateSelectedPip(SelectedPageIndex);
			UpdateNavigationButtonVisualStates();
		}

		private void OnNumberOfPagesChanged()
		{
			int numberOfPages = NumberOfPages;
			int selectedPageIndex = SelectedPageIndex;
			UpdateSizeOfSetForElements(numberOfPages);
			UpdatePipsItems(numberOfPages, MaxVisualIndicators);
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
					UpdatePipsItems(NumberOfPages, MaxVisualIndicators);
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
			SetScrollViewerMaxSize();
			UpdateSelectedPip(SelectedPageIndex);
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
				else if (property == MaxVisualIndicatorsProperty)
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
				else if (property == DefaultIndicatorButtonStyleProperty)
				{
					m_defaultPipSize = GetDesiredPipSize(DefaultIndicatorButtonStyle);
					SetScrollViewerMaxSize();
					UpdateSelectedPip(SelectedPageIndex);
				}
				else if (property == SelectedIndicatorButtonStyleProperty)
				{
					m_selectedPipSize = GetDesiredPipSize(SelectedIndicatorButtonStyle);
					SetScrollViewerMaxSize();
					UpdateSelectedPip(SelectedPageIndex);
				}
				else if (property == OrientationProperty)
				{
					OnOrientationChanged();
				}
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer() =>
			new PipsPagerAutomationPeer(this);

		void UpdateSizeOfSetForElements(int numberOfPages)
		{
			var repeater = m_pipsPagerRepeater;
			if (repeater != null)
			{
				for (int i = 0; i < numberOfPages; i++)
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
}






