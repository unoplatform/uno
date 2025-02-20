// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PagerControl.cpp, tag winui3/release/1.4.2

#pragma warning disable 105 // remove when moving to WinUI tree

using Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class PagerControl : Control
{
	private const string c_numberBoxVisibleVisualState = "NumberBoxVisible";
	private const string c_comboBoxVisibleVisualState = "ComboBoxVisible";
	private const string c_numberPanelVisibleVisualState = "NumberPanelVisible";

	private const string c_firstPageButtonVisibleVisualState = "FirstPageButtonVisible";
	private const string c_firstPageButtonNotVisibleVisualState = "FirstPageButtonCollapsed";
	private const string c_firstPageButtonHiddenVisualState = "FirstPageButtonHidden";
	private const string c_firstPageButtonEnabledVisualState = "FirstPageButtonEnabled";
	private const string c_firstPageButtonDisabledVisualState = "FirstPageButtonDisabled";

	private const string c_previousPageButtonVisibleVisualState = "PreviousPageButtonVisible";
	private const string c_previousPageButtonNotVisibleVisualState = "PreviousPageButtonCollapsed";
	private const string c_previousPageButtonHiddenVisualState = "PreviousPageButtonHidden";
	private const string c_previousPageButtonEnabledVisualState = "PreviousPageButtonEnabled";
	private const string c_previousPageButtonDisabledVisualState = "PreviousPageButtonDisabled";

	private const string c_nextPageButtonVisibleVisualState = "NextPageButtonVisible";
	private const string c_nextPageButtonNotVisibleVisualState = "NextPageButtonCollapsed";
	private const string c_nextPageButtonHiddenVisualState = "NextPageButtonHidden";
	private const string c_nextPageButtonEnabledVisualState = "NextPageButtonEnabled";
	private const string c_nextPageButtonDisabledVisualState = "NextPageButtonDisabled";

	private const string c_lastPageButtonVisibleVisualState = "LastPageButtonVisible";
	private const string c_lastPageButtonNotVisibleVisualState = "LastPageButtonCollapsed";
	private const string c_lastPageButtonHiddenVisualState = "LastPageButtonHidden";
	private const string c_lastPageButtonEnabledVisualState = "LastPageButtonEnabled";
	private const string c_lastPageButtonDisabledVisualState = "LastPageButtonDisabled";

	private const string c_finiteItemsModeState = "FiniteItems";
	private const string c_infiniteItemsModeState = "InfiniteItems";

	private const string c_rootGridName = "RootGrid";
	private const string c_comboBoxName = "ComboBoxDisplay";
	private const string c_numberBoxName = "NumberBoxDisplay";

	private const string c_numberPanelRepeaterName = "NumberPanelItemsRepeater";
	private const string c_numberPanelIndicatorName = "NumberPanelCurrentPageIndicator";
	private const string c_firstPageButtonName = "FirstPageButton";
	private const string c_previousPageButtonName = "PreviousPageButton";
	private const string c_nextPageButtonName = "NextPageButton";
	private const string c_lastPageButtonName = "LastPageButton";

	private const string c_numberPanelButtonStyleName = "PagerControlNumberPanelButtonStyle";
	private const int c_AutoDisplayModeNumberOfPagesThreshold = 10; // This integer determines when to switch between NumberBoxDisplayMode and ComboBoxDisplayMode 
	private const int c_infiniteModeComboBoxItemsIncrement = 100;

	/* Common functions */
	public PagerControl()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_PagerControl);

		m_comboBoxEntries = new ObservableVector<object>();
		m_numberPanelElements = new ObservableVector<object>();

		var templateSettings = new PagerControlTemplateSettings();
		templateSettings.SetValue(PagerControlTemplateSettings.PagesProperty, m_comboBoxEntries);
		templateSettings.SetValue(PagerControlTemplateSettings.NumberPanelItemsProperty, m_numberPanelElements);
		SetValue(TemplateSettingsProperty, templateSettings);

		SetDefaultStyleKey(this);
	}

	~PagerControl()
	{
		m_rootGridKeyDownRevoker?.Dispose();
		m_comboBoxSelectionChangedRevoker?.Dispose();
		m_firstPageButtonClickRevoker?.Dispose();
		m_previousPageButtonClickRevoker?.Dispose();
		m_nextPageButtonClickRevoker?.Dispose();
		m_lastPageButtonClickRevoker?.Dispose();
	}

	protected override void OnApplyTemplate()
	{
		if (string.IsNullOrEmpty(PrefixText))
		{
			PrefixText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlPrefixTextName);
		}
		if (string.IsNullOrEmpty(SuffixText))
		{
			SuffixText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlSuffixTextName);
		}
		var rootGrid = GetTemplateChild<FrameworkElement>(c_rootGridName);
		if (rootGrid != null)
		{
			rootGrid.KeyDown += OnRootGridKeyDown;
			m_rootGridKeyDownRevoker.Disposable = Disposable.Create(() => rootGrid.KeyDown -= OnRootGridKeyDown);
		}

		var firstPageButton = GetTemplateChild<Button>(c_firstPageButtonName);
		if (firstPageButton != null)
		{
			AutomationProperties.SetName(firstPageButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlFirstPageButtonTextName));
			firstPageButton.Click += FirstButtonClicked;
			m_firstPageButtonClickRevoker.Disposable = Disposable.Create(() => firstPageButton.Click -= FirstButtonClicked);
		}
		var previousPageButton = GetTemplateChild<Button>(c_previousPageButtonName);
		if (previousPageButton != null)
		{
			AutomationProperties.SetName(previousPageButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlPreviousPageButtonTextName));
			previousPageButton.Click += PreviousButtonClicked;
			m_previousPageButtonClickRevoker.Disposable = Disposable.Create(() => previousPageButton.Click -= PreviousButtonClicked);
		}
		var nextPageButton = GetTemplateChild<Button>(c_nextPageButtonName);
		if (nextPageButton != null)
		{
			AutomationProperties.SetName(nextPageButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlNextPageButtonTextName));
			nextPageButton.Click += NextButtonClicked;
			m_nextPageButtonClickRevoker.Disposable = Disposable.Create(() => nextPageButton.Click -= NextButtonClicked);
		}
		var lastPageButton = GetTemplateChild<Button>(c_lastPageButtonName);
		if (lastPageButton != null)
		{
			AutomationProperties.SetName(lastPageButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlLastPageButtonTextName));
			lastPageButton.Click += LastButtonClicked;
			m_lastPageButtonClickRevoker.Disposable = Disposable.Create(() => lastPageButton.Click -= LastButtonClicked);
		}

		m_comboBoxSelectionChangedRevoker.Disposable = null;
		void InitComboBox(ComboBox comboBox)
		{
			m_comboBox = comboBox;
			if (comboBox != null)
			{
				FillComboBoxCollectionToSize(NumberOfPages);
				comboBox.SelectedIndex = SelectedPageIndex - 1;
				AutomationProperties.SetName(comboBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlPageTextName));
				comboBox.SelectionChanged += ComboBoxSelectionChanged;
				m_comboBoxSelectionChangedRevoker.Disposable = Disposable.Create(() => comboBox.SelectionChanged -= ComboBoxSelectionChanged);
			}
		}
		InitComboBox((ComboBox)GetTemplateChild(c_comboBoxName));

		m_numberBoxValueChangedRevoker.Disposable = null;
		void InitNumberBox(NumberBox numberBox)
		{
			m_numberBox = numberBox;
			if (numberBox != null)
			{
				numberBox.Value = SelectedPageIndex + 1;
				AutomationProperties.SetName(numberBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlPageTextName));
				numberBox.ValueChanged += NumberBoxValueChanged;
				m_numberBoxValueChangedRevoker.Disposable = Disposable.Create(() => numberBox.ValueChanged -= NumberBoxValueChanged);
			}
		}
		InitNumberBox((NumberBox)GetTemplateChild(c_numberBoxName));

		m_numberPanelRepeater = (ItemsRepeater)GetTemplateChild(c_numberPanelRepeaterName);
		m_selectedPageIndicator = (FrameworkElement)GetTemplateChild(c_numberPanelIndicatorName);

		// This is for the initial loading of the control
		OnDisplayModeChanged();
		UpdateOnEdgeButtonVisualStates();
		OnNumberOfPagesChanged(0);

		// Update button visibilities
		OnButtonVisibilityChanged(FirstButtonVisibility,
			c_firstPageButtonVisibleVisualState,
			c_firstPageButtonNotVisibleVisualState,
			c_firstPageButtonHiddenVisualState,
			0);
		OnButtonVisibilityChanged(PreviousButtonVisibility,
			c_previousPageButtonVisibleVisualState,
			c_previousPageButtonNotVisibleVisualState,
			c_previousPageButtonHiddenVisualState,
			0);
		OnButtonVisibilityChanged(NextButtonVisibility,
			c_nextPageButtonVisibleVisualState,
			c_nextPageButtonNotVisibleVisualState,
			c_nextPageButtonHiddenVisualState,
			NumberOfPages - 1);
		OnButtonVisibilityChanged(LastButtonVisibility,
			c_lastPageButtonVisibleVisualState,
			c_lastPageButtonNotVisibleVisualState,
			c_lastPageButtonHiddenVisualState,
			NumberOfPages - 1);

		OnSelectedPageIndexChange(-1);
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;
		if (this.Template != null)
		{
			if (property == FirstButtonVisibilityProperty)
			{
				OnButtonVisibilityChanged(FirstButtonVisibility,
					c_firstPageButtonVisibleVisualState,
					c_firstPageButtonNotVisibleVisualState,
					c_firstPageButtonHiddenVisualState,
					0);
			}
			else if (property == PreviousButtonVisibilityProperty)
			{
				OnButtonVisibilityChanged(PreviousButtonVisibility,
					c_previousPageButtonVisibleVisualState,
					c_previousPageButtonNotVisibleVisualState,
					c_previousPageButtonHiddenVisualState,
					0);
			}
			else if (property == NextButtonVisibilityProperty)
			{
				OnButtonVisibilityChanged(NextButtonVisibility,
					c_nextPageButtonVisibleVisualState,
					c_nextPageButtonNotVisibleVisualState,
					c_nextPageButtonHiddenVisualState,
					NumberOfPages - 1);
			}
			else if (property == LastButtonVisibilityProperty)
			{
				OnButtonVisibilityChanged(LastButtonVisibility,
					c_lastPageButtonVisibleVisualState,
					c_lastPageButtonNotVisibleVisualState,
					c_lastPageButtonHiddenVisualState,
					NumberOfPages - 1);
			}
			else if (property == DisplayModeProperty)
			{
				OnDisplayModeChanged();
				// Why are we calling this you might ask.
				// The reason is that that method only updates what it currently needs to update.
				// So when we switch to ComboBox from NumberPanel, the NumberPanel element list might be out of date.
				UpdateTemplateSettingElementLists();
			}
			else if (property == NumberOfPagesProperty)
			{
				OnNumberOfPagesChanged((int)args.OldValue);
			}
			else if (property == SelectedPageIndexProperty)
			{
				OnSelectedPageIndexChange((int)args.OldValue);
			}
			else if (property == ButtonPanelAlwaysShowFirstLastPageIndexProperty)
			{
				UpdateNumberPanel(NumberOfPages);
			}
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new PagerControlAutomationPeer(this);
	}

	/* Property changed handlers */
	private void OnDisplayModeChanged()
	{
		// Cache for performance reasons
		var displayMode = DisplayMode;

		if (displayMode == PagerControlDisplayMode.ButtonPanel)
		{
			VisualStateManager.GoToState(this, c_numberPanelVisibleVisualState, false);
		}
		else if (displayMode == PagerControlDisplayMode.ComboBox)
		{
			VisualStateManager.GoToState(this, c_comboBoxVisibleVisualState, false);
		}
		else if (displayMode == PagerControlDisplayMode.NumberBox)
		{
			VisualStateManager.GoToState(this, c_numberBoxVisibleVisualState, false);
		}
		else
		{
			UpdateDisplayModeAutoState();
		}
	}

	private void UpdateDisplayModeAutoState()
	{
		int numberOfPages = NumberOfPages;
		if (numberOfPages > -1)
		{
			VisualStateManager.GoToState(this, numberOfPages < c_AutoDisplayModeNumberOfPagesThreshold ?
				c_comboBoxVisibleVisualState : c_numberBoxVisibleVisualState, false);
		}
		else
		{
			VisualStateManager.GoToState(this, c_numberBoxVisibleVisualState, false);
		}
	}

	void OnNumberOfPagesChanged(int oldValue)
	{
		m_lastNumberOfPagesCount = oldValue;
		int numberOfPages = NumberOfPages;
		if (numberOfPages < SelectedPageIndex && numberOfPages > -1)
		{
			SelectedPageIndex = numberOfPages - 1;
		}
		UpdateTemplateSettingElementLists();
		if (DisplayMode == PagerControlDisplayMode.Auto)
		{
			UpdateDisplayModeAutoState();
		}
		if (numberOfPages > -1)
		{
			VisualStateManager.GoToState(this, c_finiteItemsModeState, false);
			var numberBox = m_numberBox;
			if (numberBox != null)
			{
				numberBox.Maximum = numberOfPages;
			}
		}
		else
		{
			VisualStateManager.GoToState(this, c_infiniteItemsModeState, false);
			var numberBox = m_numberBox;
			if (numberBox != null)
			{
				numberBox.Maximum = double.PositiveInfinity;
			}
		}
		UpdateOnEdgeButtonVisualStates();
	}

	private void OnSelectedPageIndexChange(int oldValue)
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
		// Now handle the value changes
		m_lastSelectedPageIndex = oldValue;

		var comboBox = m_comboBox;
		if (comboBox != null)
		{
			if (SelectedPageIndex < (int)m_comboBoxEntries.Count)
			{
				comboBox.SelectedIndex = SelectedPageIndex;
			}
		}
		var numBox = m_numberBox;
		if (numBox != null)
		{
			numBox.Value = SelectedPageIndex + 1;
		}

		UpdateOnEdgeButtonVisualStates();
		UpdateTemplateSettingElementLists();

		if (DisplayMode == PagerControlDisplayMode.ButtonPanel)
		{
			// NumberPanel needs to update based on multiple parameters.
			// SelectedPageIndex is one of them, so let's do that now!
			UpdateNumberPanel(NumberOfPages);
		}

		// Fire value property change for UIA
		var peer = FrameworkElementAutomationPeer.FromElement(this) as PagerControlAutomationPeer;
		if (peer != null)
		{
			peer.RaiseSelectionChanged(m_lastSelectedPageIndex, SelectedPageIndex);
		}
		RaiseSelectedIndexChanged();
	}

	private void RaiseSelectedIndexChanged()
	{
		var args = new PagerControlSelectedIndexChangedEventArgs(m_lastSelectedPageIndex, SelectedPageIndex);
		SelectedIndexChanged?.Invoke(this, args);
	}

	private void OnButtonVisibilityChanged(PagerControlButtonVisibility visibility,
		 string visibleStateName,
		 string collapsedStateName,
		 string hiddenStateName,
		 int hiddenOnEdgePageCriteria)
	{
		if (visibility == PagerControlButtonVisibility.Visible)
		{
			VisualStateManager.GoToState(this, visibleStateName, false);
		}
		else if (visibility == PagerControlButtonVisibility.Hidden)
		{
			VisualStateManager.GoToState(this, collapsedStateName, false);
		}
		else
		{
			if (SelectedPageIndex != hiddenOnEdgePageCriteria)
			{
				VisualStateManager.GoToState(this, visibleStateName, false);
			}
			else
			{
				VisualStateManager.GoToState(this, hiddenStateName, false);
			}
		}
	}

	private void UpdateTemplateSettingElementLists()
	{
		// Cache values for performance :)
		var displayMode = DisplayMode;
		var numberOfPages = NumberOfPages;

		if (displayMode == PagerControlDisplayMode.ComboBox ||
			displayMode == PagerControlDisplayMode.Auto)
		{
			if (numberOfPages > -1)
			{
				FillComboBoxCollectionToSize(numberOfPages);
			}
			else
			{
				if (m_comboBoxEntries.Count < c_infiniteModeComboBoxItemsIncrement)
				{
					FillComboBoxCollectionToSize(c_infiniteModeComboBoxItemsIncrement);
				}
			}
		}
		else if (displayMode == PagerControlDisplayMode.ButtonPanel)
		{
			UpdateNumberPanel(numberOfPages);
		}
	}

	private void FillComboBoxCollectionToSize(int numberOfPages)
	{
		int currentComboBoxItemsCount = (int)m_comboBoxEntries.Count;
		if (currentComboBoxItemsCount <= numberOfPages)
		{
			// We are increasing the number of pages, so add the missing numbers.
			for (int i = currentComboBoxItemsCount; i < numberOfPages; i++)
			{
				m_comboBoxEntries.Add(i + 1);
			}
		}
		else
		{
			// We are decreasing the number of pages, so remove numbers starting at the end.
			for (int i = currentComboBoxItemsCount; i > numberOfPages; i--)
			{
				m_comboBoxEntries.RemoveAt(m_comboBoxEntries.Count - 1);
			}
		}
	}

	private void UpdateOnEdgeButtonVisualStates()
	{
		// Cache those values as we need them often and accessing a DP is (relatively) expensive
		int selectedPageIndex = SelectedPageIndex;
		int numberOfPages = NumberOfPages;

		// Handle disabled/enabled status of buttons
		if (selectedPageIndex == 0)
		{
			VisualStateManager.GoToState(this, c_firstPageButtonDisabledVisualState, false);
			VisualStateManager.GoToState(this, c_previousPageButtonDisabledVisualState, false);
			VisualStateManager.GoToState(this, c_nextPageButtonEnabledVisualState, false);
			VisualStateManager.GoToState(this, c_lastPageButtonEnabledVisualState, false);
		}
		else if (selectedPageIndex >= numberOfPages - 1)
		{
			VisualStateManager.GoToState(this, c_firstPageButtonEnabledVisualState, false);
			VisualStateManager.GoToState(this, c_previousPageButtonEnabledVisualState, false);
			if (numberOfPages > -1)
			{
				VisualStateManager.GoToState(this, c_nextPageButtonDisabledVisualState, false);
			}
			else
			{
				VisualStateManager.GoToState(this, c_nextPageButtonEnabledVisualState, false);
			}
			VisualStateManager.GoToState(this, c_lastPageButtonDisabledVisualState, false);
		}
		else
		{
			VisualStateManager.GoToState(this, c_firstPageButtonEnabledVisualState, false);
			VisualStateManager.GoToState(this, c_previousPageButtonEnabledVisualState, false);
			VisualStateManager.GoToState(this, c_nextPageButtonEnabledVisualState, false);
			VisualStateManager.GoToState(this, c_lastPageButtonEnabledVisualState, false);
		}

		// Handle HiddenOnEdge states
		if (FirstButtonVisibility == PagerControlButtonVisibility.HiddenOnEdge)
		{
			if (selectedPageIndex != 0)
			{
				VisualStateManager.GoToState(this, c_firstPageButtonVisibleVisualState, false);
			}
			else
			{
				VisualStateManager.GoToState(this, c_firstPageButtonHiddenVisualState, false);
			}
		}
		if (PreviousButtonVisibility == PagerControlButtonVisibility.HiddenOnEdge)
		{
			if (selectedPageIndex != 0)
			{
				VisualStateManager.GoToState(this, c_previousPageButtonVisibleVisualState, false);
			}
			else
			{
				VisualStateManager.GoToState(this, c_previousPageButtonHiddenVisualState, false);
			}
		}
		if (NextButtonVisibility == PagerControlButtonVisibility.HiddenOnEdge)
		{
			if (selectedPageIndex != numberOfPages - 1)
			{
				VisualStateManager.GoToState(this, c_nextPageButtonVisibleVisualState, false);
			}
			else
			{
				VisualStateManager.GoToState(this, c_nextPageButtonHiddenVisualState, false);
			}
		}
		if (LastButtonVisibility == PagerControlButtonVisibility.HiddenOnEdge)
		{
			if (selectedPageIndex != numberOfPages - 1)
			{
				VisualStateManager.GoToState(this, c_lastPageButtonVisibleVisualState, false);
			}
			else
			{
				VisualStateManager.GoToState(this, c_lastPageButtonHiddenVisualState, false);
			}
		}
	}

	/* NumberPanel logic */
	void UpdateNumberPanel(int numberOfPages)
	{
		if (numberOfPages < 0)
		{
			UpdateNumberOfPanelCollectionInfiniteItems();
		}
		else if (numberOfPages < 8)
		{
			UpdateNumberPanelCollectionAllItems(numberOfPages);
		}
		else
		{
			var selectedIndex = SelectedPageIndex;
			// Idea: Choose correct "template" based on SelectedPageIndex (x) and NumberOfPages (n)
			// 1 2 3 4 5 6 ... n <-- Items
			if (selectedIndex < 4)
			{
				// First four items selected, create following pattern:
				// 1 2 3 4 5... n
				UpdateNumberPanelCollectionStartWithEllipsis(numberOfPages, selectedIndex);
			}
			else if (selectedIndex >= numberOfPages - 4)
			{
				// Last four items selected, create following pattern:
				//1 [...] n-4 n-3 n-2 n-1 n
				UpdateNumberPanelCollectionEndWithEllipsis(numberOfPages, selectedIndex);
			}
			else
			{
				// Neither start or end, so lets do this:
				// 1 [...] x-2 x-1 x x+1 x+2 [...] n
				// where x > 4 and x < n - 4
				UpdateNumberPanelCollectionCenterWithEllipsis(numberOfPages, selectedIndex);
			}
		}
	}

	private void UpdateNumberOfPanelCollectionInfiniteItems()
	{
		int selectedIndex = SelectedPageIndex;

		m_numberPanelElements.Clear();
		if (selectedIndex < 3)
		{
			AppendButtonToNumberPanelList(1, 0);
			AppendButtonToNumberPanelList(2, 0);
			AppendButtonToNumberPanelList(3, 0);
			AppendButtonToNumberPanelList(4, 0);
			AppendButtonToNumberPanelList(5, 0);
			MoveIdentifierToElement(selectedIndex);
		}
		else
		{
			AppendButtonToNumberPanelList(1, 0);
			AppendEllipsisIconToNumberPanelList();
			AppendButtonToNumberPanelList(selectedIndex, 0);
			AppendButtonToNumberPanelList(selectedIndex + 1, 0);
			AppendButtonToNumberPanelList(selectedIndex + 2, 0);
			MoveIdentifierToElement(3);
		}
	}

	private void UpdateNumberPanelCollectionAllItems(int numberOfPages)
	{
		// Check that the NumberOfPages did not change, so we can skip recreating collection
		if (m_lastNumberOfPagesCount != numberOfPages)
		{
			m_numberPanelElements.Clear();
			for (int i = 0; i < numberOfPages && i < 7; i++)
			{
				AppendButtonToNumberPanelList(i + 1, numberOfPages);
			}
		}
		MoveIdentifierToElement(SelectedPageIndex);
	}

	private void UpdateNumberPanelCollectionStartWithEllipsis(int numberOfPages, int selectedIndex)
	{
		if (m_lastNumberOfPagesCount != numberOfPages)
		{
			// Do a recalculation as the number of pages changed.
			m_numberPanelElements.Clear();
			AppendButtonToNumberPanelList(1, numberOfPages);
			AppendButtonToNumberPanelList(2, numberOfPages);
			AppendButtonToNumberPanelList(3, numberOfPages);
			AppendButtonToNumberPanelList(4, numberOfPages);
			AppendButtonToNumberPanelList(5, numberOfPages);
			if (ButtonPanelAlwaysShowFirstLastPageIndex)
			{
				AppendEllipsisIconToNumberPanelList();
				AppendButtonToNumberPanelList(numberOfPages, numberOfPages);
			}
		}
		// SelectedIndex is definitely the correct index here as we are counting from start
		MoveIdentifierToElement(selectedIndex);
	}

	private void UpdateNumberPanelCollectionEndWithEllipsis(int numberOfPages, int selectedIndex)
	{
		if (m_lastNumberOfPagesCount != numberOfPages)
		{
			// Do a recalculation as the number of pages changed.
			m_numberPanelElements.Clear();
			if (ButtonPanelAlwaysShowFirstLastPageIndex)
			{
				AppendButtonToNumberPanelList(1, numberOfPages);
				AppendEllipsisIconToNumberPanelList();
			}
			AppendButtonToNumberPanelList(numberOfPages - 4, numberOfPages);
			AppendButtonToNumberPanelList(numberOfPages - 3, numberOfPages);
			AppendButtonToNumberPanelList(numberOfPages - 2, numberOfPages);
			AppendButtonToNumberPanelList(numberOfPages - 1, numberOfPages);
			AppendButtonToNumberPanelList(numberOfPages, numberOfPages);
		}
		// We can only be either the last, the second from last or third from last
		// => SelectedIndex = NumberOfPages - y with y in {1,2,3}
		if (ButtonPanelAlwaysShowFirstLastPageIndex)
		{
			// Last item sits at index 4.
			// SelectedPageIndex for the last page is NumberOfPages - 1.
			// => SelectedItem = SelectedIndex - NumberOfPages + 7;
			MoveIdentifierToElement(selectedIndex - numberOfPages + 7);
		}
		else
		{
			// Last item sits at index 6.
			// SelectedPageIndex for the last page is NumberOfPages - 1.
			// => SelectedItem = SelectedIndex - NumberOfPages + 5;
			MoveIdentifierToElement(selectedIndex - numberOfPages + 5);
		}
	}

	private void UpdateNumberPanelCollectionCenterWithEllipsis(int numberOfPages, int selectedIndex)
	{
		var showFirstLastPageIndex = ButtonPanelAlwaysShowFirstLastPageIndex;
		if (m_lastNumberOfPagesCount != numberOfPages)
		{
			// Do a recalculation as the number of pages changed.
			m_numberPanelElements.Clear();
			if (showFirstLastPageIndex)
			{
				AppendButtonToNumberPanelList(1, numberOfPages);
				AppendEllipsisIconToNumberPanelList();
			}
			AppendButtonToNumberPanelList(selectedIndex, numberOfPages);
			AppendButtonToNumberPanelList(selectedIndex + 1, numberOfPages);
			AppendButtonToNumberPanelList(selectedIndex + 2, numberOfPages);
			if (showFirstLastPageIndex)
			{
				AppendEllipsisIconToNumberPanelList();
				AppendButtonToNumberPanelList(numberOfPages, numberOfPages);
			}
		}
		// "selectedIndex + 1" is our representation for SelectedIndex.
		if (showFirstLastPageIndex)
		{
			// SelectedIndex + 1 is the fifth element.
			// Collections are base zero, so the index to underline is 3.
			MoveIdentifierToElement(3);
		}
		else
		{
			// SelectedIndex + 1 is the third element.
			// Collections are base zero, so the index to underline is 1.
			MoveIdentifierToElement(1);
		}
	}

	private void MoveIdentifierToElement(int index)
	{
		var indicator = m_selectedPageIndicator;
		if (indicator != null)
		{
			var repeater = m_numberPanelRepeater;
			if (repeater != null)
			{
				repeater.UpdateLayout();
				var element = repeater.TryGetElement(index) as FrameworkElement;
				if (element != null)
				{
					var boundingRect = element.TransformToVisual(repeater).TransformBounds(new Rect(0, 0, (float)repeater.ActualWidth, (float)repeater.ActualHeight));
					Thickness newMargin = new Thickness();
					newMargin.Left = boundingRect.X;
					newMargin.Top = 0;
					newMargin.Right = 0;
					newMargin.Bottom = 0;
					indicator.Margin = newMargin;
					indicator.Width = element.ActualWidth;
				}
			}
		}
	}

	private void AppendButtonToNumberPanelList(int pageNumber, int numberOfPages)
	{
		Button button = new Button();
		button.Content = pageNumber;
		button.Click += (sender, args) =>
		{
			var button = sender as Button;
			if (button != null)
			{
				int unboxedValue = (int)button.Content;
				SelectedPageIndex = unboxedValue - 1;
			}
		};
		// Set the default style of buttons
		button.Style = (Style)ResourceAccessor.ResourceLookup(this, c_numberPanelButtonStyleName);
		AutomationProperties.SetName(button, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PagerControlPageTextName) + " " + pageNumber);
		AutomationProperties.SetPositionInSet(button, pageNumber);
		AutomationProperties.SetSizeOfSet(button, numberOfPages);
		m_numberPanelElements.Add(button);
	}

	private void AppendEllipsisIconToNumberPanelList()
	{
		SymbolIcon ellipsisIcon = new SymbolIcon();
		ellipsisIcon.Symbol = Symbol.More;
		m_numberPanelElements.Add(ellipsisIcon);
	}

	/* Interaction event listeners */
	private void OnRootGridKeyDown(object sender, KeyRoutedEventArgs args)
	{
		if (args.Key == VirtualKey.Left || args.Key == VirtualKey.GamepadDPadLeft)
		{
			var options = new FindNextElementOptions();
			options.SearchRoot = this;
			FocusManager.TryMoveFocus(FocusNavigationDirection.Left, options);
		}
		else if (args.Key == VirtualKey.Right || args.Key == VirtualKey.GamepadDPadRight)
		{
			var options = new FindNextElementOptions();
			options.SearchRoot = this;
			FocusManager.TryMoveFocus(FocusNavigationDirection.Right, options);
		}
	}

	private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		var comboBox = m_comboBox;
		if (comboBox != null)
		{
			SelectedPageIndex = comboBox.SelectedIndex;
		}
	}

	private void NumberBoxValueChanged(object sender, NumberBoxValueChangedEventArgs args)
	{
		SelectedPageIndex = (int)args.NewValue - 1;
	}

	private void FirstButtonClicked(object sender, RoutedEventArgs e)
	{
		SelectedPageIndex = 0;
		var command = FirstButtonCommand;
		if (command != null)
		{
			command.Execute(null);
		}
	}

	private void PreviousButtonClicked(object sender, RoutedEventArgs e)
	{
		// In this method, SelectedPageIndex is always greater than 1.
		SelectedPageIndex = SelectedPageIndex - 1;
		var command = PreviousButtonCommand;
		if (command != null)
		{
			command.Execute(null);
		}
	}

	private void NextButtonClicked(object sender, RoutedEventArgs e)
	{
		// In this method, SelectedPageIndex is always less than maximum.
		SelectedPageIndex = SelectedPageIndex + 1;
		var command = NextButtonCommand;
		if (command != null)
		{
			command.Execute(null);
		}
	}

	private void LastButtonClicked(object sender, RoutedEventArgs e)
	{
		SelectedPageIndex = NumberOfPages - 1;
		var command = LastButtonCommand;
		if (command != null)
		{
			command.Execute(null);
		}
	}
}
