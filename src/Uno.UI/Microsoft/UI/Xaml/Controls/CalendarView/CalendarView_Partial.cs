// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Xaml.Controls.Primitives;
using DayOfWeek = Windows.Globalization.DayOfWeek;

namespace Windows.UI.Xaml.Controls
{
	public class CalendarView
	{
		List<DateTime> m_tpSelectedDates;

		Button m_tpHeaderButton;
		Button m_tpPreviousButton;
		Button m_tpNextButton;

		Grid m_tpViewsGrid;

		Calendar m_tpCalendar;
		DateTimeFormatter m_tpMonthYearFormatter;
		DateTimeFormatter m_tpYearFormatter;

		CalendarViewGeneratorHost m_tpMonthViewItemHost;
		CalendarViewGeneratorHost m_tpYearViewItemHost;
		CalendarViewGeneratorHost m_tpDecadeViewItemHost;

		ScrollViewer m_tpMonthViewScrollViewer;
		ScrollViewer m_tpYearViewScrollViewer;
		ScrollViewer m_tpDecadeViewScrollViewer;

		// we define last displayed date by following:
		// 1. if the last displayed item is visible, the date of last displayed item
		// 2. if last focused item is not visible, we use the first_visible_inscope_date
		// 3. when an item gets focused, we use the date of the focused item.
		//
		// the default last displayed date will be determined by following:
		// 1. display Date if it is requested, if it is not requested, then
		// 2. Today, if Today is not in given min/max range, then
		// 3. the closest date to Today (i.e. the coerced date of Today)
		DateTime m_lastDisplayedDate;

		DateTime m_today;
		// m_minDate and m_maxDate are effective min/max dates, which could be different
		// than the values return from get_MinDate/get_MaxDate.
		// because developer could set a minDate or maxDate that doesn't exist in
		// current calendarsystem. (e.g. UmAlQuraCalendar doesn't have 1/1/2099).
		DateTime m_maxDate;
		DateTime m_minDate;

		// the weekday of mindate.
		DayOfWeek m_weekDayOfMinDate;

		TrackerPtr<xaml_primitives::ICalendarViewTemplateSettings> m_tpTemplateSettings;

		ctl::EventPtr<ButtonBaseClickEventCallback> m_epHeaderButtonClickHandler;
		ctl::EventPtr<ButtonBaseClickEventCallback> m_epPreviousButtonClickHandler;
		ctl::EventPtr<ButtonBaseClickEventCallback> m_epNextButtonClickHandler;

		ctl::EventPtr<UIElementKeyDownEventCallback> m_epMonthViewScrollViewerKeyDownEventHandler;
		ctl::EventPtr<UIElementKeyDownEventCallback> m_epYearViewScrollViewerKeyDownEventHandler;
		ctl::EventPtr<UIElementKeyDownEventCallback> m_epDecadeViewScrollViewerKeyDownEventHandler;

		const int s_minNumberOfWeeks = 2;
		const int s_maxNumberOfWeeks = 8;
		const int s_defaultNumberOfWeeks = 6;
		const int s_numberOfDaysInWeek = 7;

		int m_colsInYearDecadeView;     // default value is 4

		int m_rowsInYearDecadeView;     // default value is 4

		// if we decide to have a different startIndex in YearView or DecadeView, we should make a corresponding change at CalendarViewItemAutomationPeer::get_ColumnImpl
		// in MonthView, because we can set the DayOfWeek property, the first item is not always start from the first positon inside the Panel
		int m_monthViewStartIndex;

		// dayOfWeekNames stores abbreviated names of each day of the week. dayOfWeekNamesFull stores the full name to be read aloud by accessibility.
		List<string> m_dayOfWeekNames;
		List<string> m_dayOfWeekNamesFull;

		IEnumerable<string> m_tpCalendarLanguages;

		ctl::EventPtr<DateTimeVectorChangedEventCallback> m_epSelectedDatesChangedHandler;


		// the keydown event args from CalendarItem.
		ctl::WeakRefPtr m_wrKeyDownEventArgsFromCalendarItem;

		// the focus state we need to set on the calendaritem after we change the display mode.
		FocusState m_focusStateAfterDisplayModeChanged;

		DateComparer m_dateComparer;

		//
		// Automation fields
		//

		// When Narrator gives focus to a day item, we expect it to read the month header
		// (assuming the focus used to be outside or on a day item at a different month).
		// During this focus transition, Narrator expects to be able to query the previous peer (if any).
		// So we need to keep track of the current and previous month header peers.
		CalendarViewHeaderAutomationPeer m_currentHeaderPeer;
		CalendarViewHeaderAutomationPeer m_previousHeaderPeer;

		// when mindate or maxdate changed, we set this flag
		bool m_dateSourceChanged;

		// when calendar identifier changed, we set this flag
		bool m_calendarChanged;

		bool m_itemHostsConnected;

		bool m_areDirectManipulationStateChangeHandlersHooked;

		// this flag indicts the change of SelectedDates comes from internal or external.
		bool m_isSelectedDatesChanginginternally;

		// when true we need to move focus to a calendaritem after we change the display mode.
		bool m_focusItemAfterDisplayModeChanged;

		bool m_isMultipleEraCalendar;

		bool m_isSetDisplayDateRequested;

		bool m_areYearDecadeViewDimensionsSet;

		// After navigationbutton clicked, the head text doesn't change immediately. so we use this flag to tell if the update text is from navigation button
		bool m_isNavigationButtonClicked;

		public CalendarView()
		{
			m_dateSourceChanged = true;
			m_calendarChanged = false;
			m_itemHostsConnected = false;
			m_areYearDecadeViewDimensionsSet = false;
			m_colsInYearDecadeView = 4;
			m_rowsInYearDecadeView = 4;
			m_monthViewStartIndex = 0;
			m_weekDayOfMinDate = DayOfWeek.Sunday;
			m_isSelectedDatesChanginginternally = false;
			m_focusItemAfterDisplayModeChanged = false;
			m_focusStateAfterDisplayModeChanged = FocusState.Programmatic;
			m_isMultipleEraCalendar = false;
			m_areDirectManipulationStateChangeHandlersHooked = false;
			m_isSetDisplayDateRequested = true; // by default there is a displayDate request, which is m_lastDisplayedDate
			m_isNavigationButtonClicked = false;

			m_today.UniversalTime = 0;
			m_maxDate.UniversalTime = 0;
			m_minDate.UniversalTime = 0;
			m_lastDisplayedDate.UniversalTime = 0;
		}

		~CalendarView()
		{
			VERIFYHR(DetachButtonClickedEvents();
			VERIFYHR(DetachHandler(m_epSelectedDatesChangedHandler, m_tpSelectedDates);
			VERIFYHR(DetachScrollViewerKeyDownEvents();

			IList<DateTime> selectedDates;
			if (m_tpSelectedDates.TryGetSafeReference(&selectedDates))
			{
				((TrackableDateCollection)selectedDates).SetCollectionChangingCallback(null);
			}
		}

		private void PrepareState()
		{
			HRESULT hr = S_OK;

			CalendarViewGenerated.PrepareState();

			{
				m_dateComparer.reset(new DateComparer();

				ctl.ComPtr<TrackableDateCollection> spSelectedDates;

				ctl.make(&spSelectedDates);

				m_epSelectedDatesChangedHandler.AttachEventHandler(spSelectedDates, 
					[this](wfc.IObservableVector < wf.DateTime > *pSender, wfc.IVectorChangedEventArgs * pArgs)
				{
					return OnSelectedDatesChanged(pSender, pArgs);
				});

				spSelectedDates.SetCollectionChangingCallback(
					[this](TrackableDateCollection_CollectionChanging action, wf.DateTime addingDate)
				{
					return OnSelectedDatesChanging(action, addingDate);
				});

				m_tpSelectedDates = spSelectedDates;
				put_SelectedDates(spSelectedDates);
			}

			{
				ctl.ComPtr<CalendarViewGeneratorMonthViewHost> spMonthViewItemHost;
				ctl.ComPtr<CalendarViewGeneratorYearViewHost> spYearViewItemHost;
				ctl.ComPtr<CalendarViewGeneratorDecadeViewHost> spDecadeViewItemHost;

				ctl.make(&spMonthViewItemHost);
				m_tpMonthViewItemHost = spMonthViewItemHost;
				m_tpMonthViewItemHost.SetOwner(this);

				ctl.make(&spYearViewItemHost);
				m_tpYearViewItemHost = spYearViewItemHost;
				m_tpYearViewItemHost.SetOwner(this);

				ctl.make(&spDecadeViewItemHost);
				m_tpDecadeViewItemHost = spDecadeViewItemHost;
				m_tpDecadeViewItemHost.SetOwner(this);
			}

			{
				CreateCalendarLanguages();
				CreateCalendarAndMonthYearFormatter();
			}

			{
				ctl.ComPtr<CalendarViewTemplateSettings> spTemplateSettings;

				ctl.make(&spTemplateSettings);

				spTemplateSettings.HasMoreViews = TRUE;
				put_TemplateSettings(spTemplateSettings);
				m_tpTemplateSettings = spTemplateSettings;
			}

			Cleanup:
			return hr;
		}

		// Override the GetDefaultValue method to return the default values
		// for Hub dependency properties.
		private void GetDefaultValue2(
			DependencyProperty pDP, 
			out CValue* pValue)
		{
			HRESULT hr = S_OK;

			IFCPTR(pDP);
			IFCPTR(pValue);

			switch (pDP.GetIndex())
			{
				case KnownPropertyIndex.CalendarView_CalendarIdentifier:
					pValue.SetString(wrl_wrappers.string(STR_LEN_PAIR("GregorianCalendar")));
					break;
				case KnownPropertyIndex.CalendarView_NumberOfWeeksInView:
					pValue.SetSigned(s_defaultNumberOfWeeks);
					break;
				default:
					CalendarViewGenerated.GetDefaultValue2(pDP, pValue);
					break;
			}
		}

		// Basically these Alignment properties only affect Arrange, but in CalendarView
		// the item size and Panel size are also affected when we change the property from
		// stretch to unstretch, or vice versa. In these cases we need to invalidate panels' measure.
		private void OnAlignmentChanged(DependencyPropertyChangedEventArgs args)
		{
			uint oldAlignment = 0;
			uint newAlignment = 0;
			bool isOldStretched = false;
			bool isNewStretched = false;

			oldAlignment = (uint)args.OldValue;
			newAlignment = (uint)args.NewValue;

			switch (args.m_pDP.GetIndex())
			{
				case KnownPropertyIndex.Control_HorizontalContentAlignment:
				case KnownPropertyIndex.FrameworkElement_HorizontalAlignment:
					isOldStretched = (xaml.HorizontalAlignment)(oldAlignment) == xaml.HorizontalAlignment_Stretch;
					isNewStretched = (xaml.HorizontalAlignment)(newAlignment) == xaml.HorizontalAlignment_Stretch;
					break;
				case KnownPropertyIndex.Control_VerticalContentAlignment:
				case KnownPropertyIndex.FrameworkElement_VerticalAlignment:
					isOldStretched = (xaml.VerticalAlignment)(oldAlignment) == xaml.VerticalAlignment_Stretch;
					isNewStretched = (xaml.VerticalAlignment)(newAlignment) == xaml.VerticalAlignment_Stretch;
					break;
				default:
					ASSERT(false);
					break;
			}

			if (isOldStretched != isNewStretched)
			{
				ForeachHost([](CalendarViewGeneratorHost * pHost)
				{
					var pPanel = pHost.Panel;
					if (pPanel)
					{
						pPanel.InvalidateMeasure();
					}

					return S_OK;
				});
			}

			return S_OK;
		}

		// Handle the custom property changed event and call the OnPropertyChanged methods.
		private void OnPropertyChanged2(
			const DependencyPropertyChangedEventArgs args)
		{

			CalendarViewGenerated.OnPropertyChanged2(args);

			switch (args.m_pDP.GetIndex())
			{
				case KnownPropertyIndex.Control_HorizontalContentAlignment:
				case KnownPropertyIndex.Control_VerticalContentAlignment:
				case KnownPropertyIndex.FrameworkElement_HorizontalAlignment:
				case KnownPropertyIndex.FrameworkElement_VerticalAlignment:
					OnAlignmentChanged(args);
					break;
				case KnownPropertyIndex.CalendarView_MinDate:
				case KnownPropertyIndex.CalendarView_MaxDate:
					m_dateSourceChanged = true;
					InvalidateMeasure();
					break;
				case KnownPropertyIndex.FrameworkElement_Language:
					// Globlization.Calendar doesn't support changing languages, so when languages changed,
					// we have to create a new Globalization.Calendar, and also we'll update the date source so
					// the change of languages can take effect on the existing items.
					CreateCalendarLanguages();
				// fall through
				case KnownPropertyIndex.CalendarView_CalendarIdentifier:
					m_calendarChanged = true;
					m_dateSourceChanged = true; //calendarid changed, even if the mindate or maxdate is not changed we still need to regenerate all calendar items.
					InvalidateMeasure();
					break;
				case KnownPropertyIndex.CalendarView_NumberOfWeeksInView:
				{
					int rows = 0;
					args.NewValue.GetSigned(rows);

					if (rows < s_minNumberOfWeeks || rows > s_maxNumberOfWeeks)
					{
						ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_NUMBER_OF_WEEKS_OUTOFRANGE);
					}

					if (m_tpMonthViewItemHost.Panel)
					{
						m_tpMonthViewItemHost.Panel.SetSuggestedDimension(s_numberOfDaysInWeek, rows);
					}
				}
					break;
				case KnownPropertyIndex.CalendarView_DayOfWeekFormat:
					FormatWeekDayNames();
				// fall through
				case KnownPropertyIndex.CalendarView_FirstDayOfWeek:
					UpdateWeekDayNames();
					break;
				case KnownPropertyIndex.CalendarView_SelectionMode:
					OnSelectionModeChanged();
					break;
				case KnownPropertyIndex.CalendarView_IsOutOfScopeEnabled:
					OnIsOutOfScopePropertyChanged();
					break;
				case KnownPropertyIndex.CalendarView_DisplayMode:
				{
					uint oldDisplayMode = 0;
					uint newDisplayMode = 0;

					oldDisplayMode = (uint)args.OldValue;
					newDisplayMode = (uint)args.NewValue;

					OnDisplayModeChanged(
						(CalendarViewDisplayMode)(oldDisplayMode),
						(CalendarViewDisplayMode)(newDisplayMode)
					);
				}
					break;
				case KnownPropertyIndex.CalendarView_IsTodayHighlighted:
					OnIsTodayHighlightedPropertyChanged();
					break;
				case KnownPropertyIndex.CalendarView_IsGroupLabelVisible:
					OnIsLabelVisibleChanged();
					break;

				// To reduce memory usage, we move lots font/brush properties from CalendarViewItem to CalendarView,
				// the cost is we can't benefit from property system to invalidate measure/render automatically.
				// However changing these font/brush properties is not a frequent scenario. So once they are changed
				// we'll manually update the items.
				// Basically we should only update those affected items (e.g. when PressedBackground changed, we should only update
				// the item which is being pressed) but to make the code simple we'll update all realized item, unless
				// we see performance issue here.

				// Border brushes and Background (they are chromes) will take effect in next Render walk.
				case KnownPropertyIndex.CalendarView_FocusBorderBrush:
				case KnownPropertyIndex.CalendarView_SelectedHoverBorderBrush:
				case KnownPropertyIndex.CalendarView_SelectedPressedBorderBrush:
				case KnownPropertyIndex.CalendarView_SelectedBorderBrush:
				case KnownPropertyIndex.CalendarView_HoverBorderBrush:
				case KnownPropertyIndex.CalendarView_PressedBorderBrush:
				case KnownPropertyIndex.CalendarView_CalendarItemBorderBrush:
				case KnownPropertyIndex.CalendarView_OutOfScopeBackground:
				case KnownPropertyIndex.CalendarView_CalendarItemBackground:
					ForeachHost(pHost => 
					{
						ForeachChildInPanel(
							pHost.Panel,
							pItem =>
							{
								return pItem.InvalidateRender();
							});
					});
					break;

				// Foreground will take effect immediately
				case KnownPropertyIndex.CalendarView_PressedForeground:
				case KnownPropertyIndex.CalendarView_TodayForeground:
				case KnownPropertyIndex.CalendarView_BlackoutForeground:
				case KnownPropertyIndex.CalendarView_SelectedForeground:
				case KnownPropertyIndex.CalendarView_OutOfScopeForeground:
				case KnownPropertyIndex.CalendarView_CalendarItemForeground:
					ForeachHost(pHost =>
					{
						ForeachChildInPanel(
							pHost.Panel,
							pItem =>
							{
								return pItem.UpdateTextBlockForeground();
							});
					});
					break;

				case KnownPropertyIndex.CalendarView_TodayFontWeight:
				{
					ForeachHost(pHost =>
					{
						var pPanel = pHost.Panel;

						if (pPanel)
						{
							int indexOfToday = -1;

							indexOfToday = pHost.CalculateOffsetFromMinDate(m_today);

							if (indexOfToday != -1)
							{
								DependencyObject spChildAsIDO;
								CalendarViewBaseItem spChildAsI;

								pPanel.ContainerFromIndex(indexOfToday, &spChildAsIDO);
								spChildAsI = spChildAsIDO as ICalendarViewBaseItem;
								// today item is realized already, we need to update the state here.
								// if today item is not realized yet, we'll update the state when today item is being prepared.
								if (spChildAsI is {})
								{
									CalendarViewBaseItem spChild;

									spChild = (CalendarViewBaseItem)spChildAsI;
									spChild.UpdateTextBlockFontProperties();
								}
							}
						}
					});

					break;
				}

				// Font properties for DayItem (affect measure and arrange)
				case KnownPropertyIndex.CalendarView_DayItemFontFamily:
				case KnownPropertyIndex.CalendarView_DayItemFontSize:
				case KnownPropertyIndex.CalendarView_DayItemFontStyle:
				case KnownPropertyIndex.CalendarView_DayItemFontWeight:
				{
					// if these DayItem properties changed, we need to re-determine the
					// biggest dayitem in monthPanel, which will invalidate monthpanel's measure
					var pMonthPanel = m_tpMonthViewItemHost.Panel;
					if (pMonthPanel)
					{
						pMonthPanel.SetNeedsToDetermineBiggestItemSize();
					}

				}
				// fall through

				// Font properties for MonthLabel (they won't affect measure or arrange)
				case KnownPropertyIndex.CalendarView_FirstOfMonthLabelFontFamily:
				case KnownPropertyIndex.CalendarView_FirstOfMonthLabelFontSize:
				case KnownPropertyIndex.CalendarView_FirstOfMonthLabelFontStyle:
				case KnownPropertyIndex.CalendarView_FirstOfMonthLabelFontWeight:
					ForeachChildInPanel(
						m_tpMonthViewItemHost.Panel,
						pItem =>
						{
							return pItem.UpdateTextBlockFontProperties();
						});
					break;

				// Font properties for MonthYearItem
				case KnownPropertyIndex.CalendarView_MonthYearItemFontFamily:
				case KnownPropertyIndex.CalendarView_MonthYearItemFontSize:
				case KnownPropertyIndex.CalendarView_MonthYearItemFontStyle:
				case KnownPropertyIndex.CalendarView_MonthYearItemFontWeight:
				{
					// these properties will affect MonthItem and YearItem's size, so we should
					// tell their panels to re-determine the biggest item size.
					std.array < CalendarPanel *, 2 > pPanels{
						{
							m_tpYearViewItemHost.Panel, m_tpDecadeViewItemHost.Panel
						}
					}
					;

					for (var i = 0; i < pPanels.size(); ++i)
					{
						if (pPanels[i])
						{
							pPanels[i].SetNeedsToDetermineBiggestItemSize();
						}
					}
				}
				// fall through
				case KnownPropertyIndex.CalendarView_FirstOfYearDecadeLabelFontFamily:
				case KnownPropertyIndex.CalendarView_FirstOfYearDecadeLabelFontSize:
				case KnownPropertyIndex.CalendarView_FirstOfYearDecadeLabelFontStyle:
				case KnownPropertyIndex.CalendarView_FirstOfYearDecadeLabelFontWeight:
				{
					std.array < CalendarPanel *, 2 > pPanels{
						{
							m_tpYearViewItemHost.Panel, m_tpDecadeViewItemHost.Panel
						}
					}
					;

					for (var i = 0; i < pPanels.size(); ++i)
					{
						ForeachChildInPanel(pPanels[i], 
							[this](CalendarViewBaseItem * pItem)
						{
							return pItem.UpdateTextBlockFontProperties();
						});

					}

					break;
				}
				// Alignments affect DayItem only
				case KnownPropertyIndex.CalendarView_HorizontalDayItemAlignment:
				case KnownPropertyIndex.CalendarView_VerticalDayItemAlignment:
				case KnownPropertyIndex.CalendarView_HorizontalFirstOfMonthLabelAlignment:
				case KnownPropertyIndex.CalendarView_VerticalFirstOfMonthLabelAlignment:

					ForeachChildInPanel(
						m_tpMonthViewItemHost.Panel, 
						pItem =>
						{
							pItem.UpdateTextBlockAlignments();
						});

					break;

				// border thickness affects measure (and arrange)
				case KnownPropertyIndex.CalendarView_CalendarItemBorderThickness:
					ForeachHost(pHost =>
						{
							ForeachChildInPanel(
								pHost.Panel, 
								pItem =>
								{
									pItem.InvalidateMeasure();
								});
						});
					break;

				// Dayitem style changed, update style for all existing day items.
				case KnownPropertyIndex.CalendarView_CalendarViewDayItemStyle:
				{
					Style spStyle;

					ctl.do_query_interface(spStyle, args.NewValueOuterNoRef);
					var pMonthPanel = m_tpMonthViewItemHost.Panel;

					ForeachChildInPanel(
						pMonthPanel, 
						pItem =>
						{
							return pItem.SetDayItemStyle(spStyle);
						});

					// Some properties could affect dayitem size (e.g. Dayitem font properties, dayitem size),
					// when anyone of them is changed, we need to re-determine the biggest day item.
					// This is not a frequent scenario so we can simply set below flag and invalidate measure.

					if (pMonthPanel)
					{
						pMonthPanel.SetNeedsToDetermineBiggestItemSize();
					}

					break;
				}
			}
		}

		protected override void OnApplyTemplate()
		{
			CalendarPanel spMonthViewPanel;
			CalendarPanel spYearViewPanel;
			CalendarPanel spDecadeViewPanel;
			Button spHeaderButton;
			Button spPreviousButton;
			Button spNextButton;
			Grid spViewsGrid;
			ScrollViewer spMonthViewScrollViewer;
			ScrollViewer spYearViewScrollViewer;
			ScrollViewer spDecadeViewScrollViewer;
			string strAutomationName;

			DetachVisibleIndicesUpdatedEvents();
			DetachButtonClickedEvents();
			DetachScrollViewerFocusEngagedEvents();
			DetachScrollViewerKeyDownEvents();

			// This will clean up the panels and clear the children
			DisconnectItemHosts();

			if (m_areDirectManipulationStateChangeHandlersHooked)
			{
				m_areDirectManipulationStateChangeHandlersHooked = false;
				ForeachHost(pHost =>
				{
					var pScrollViewer = pHost.ScrollViewer;
					if (pScrollViewer)
					{
						pScrollViewer.SetDirectManipulationStateChangeHandler(null);
					}
				});
			}

			ForeachHost(pHost =>
			{
				pHost.Panel = null;
				pHost.ScrollViewer = null;
			});


			m_tpHeaderButton.Clear();
			m_tpPreviousButton.Clear();
			m_tpNextButton.Clear();
			m_tpViewsGrid.Clear();

			CalendarViewGenerated.OnApplyTemplate();

			spMonthViewPanel = this.GetTemplatePart<CalendarPanel>("MonthViewPanel");
			spYearViewPanel = this.GetTemplatePart<CalendarPanel>("YearViewPanel");
			spDecadeViewPanel = this.GetTemplatePart<CalendarPanel>("DecadeViewPanel");

			m_tpMonthViewItemHost.Panel = spMonthViewPanel;
			m_tpYearViewItemHost.Panel = spYearViewPanel;
			m_tpDecadeViewItemHost.Panel = spDecadeViewPanel;

			if (spMonthViewPanel is {})
			{
				CalendarPanel pPanel = (CalendarPanel)spMonthViewPanel;
				int numberOfWeeksInView = 0;

				// MonthView panel is the only primary panel (and never changes)
				pPanel.PanelType = CalendarPanelType.Primary;

				numberOfWeeksInView = NumberOfWeeksInView;
				pPanel.SetSuggestedDimension(s_numberOfDaysInWeek, numberOfWeeksInView);
				pPanel.Orientation = Orientation.Horizontal;
			}

			if (spYearViewPanel is {})
			{
				CalendarPanel pPanel = (CalendarPanel)spYearViewPanel;

				// YearView panel is a Secondary_SelfAdaptive panel by default
				if (!m_areYearDecadeViewDimensionsSet)
				{
					pPanel.PanelType = CalendarPanelType.Secondary_SelfAdaptive;
				}

				pPanel.SetSuggestedDimension(m_colsInYearDecadeView, m_rowsInYearDecadeView);
				pPanel.Orientation = Orientation.Horizontal;
			}

			if (spDecadeViewPaneli is {})
			{
				CalendarPanel pPanel = (CalendarPanel)spDecadeViewPanel;

				// DecadeView panel is a Secondary_SelfAdaptive panel by default
				if (!m_areYearDecadeViewDimensionsSet)
				{
					pPanel.PanelType = CalendarPanelType.Secondary_SelfAdaptive;
				}

				pPanel.SetSuggestedDimension(m_colsInYearDecadeView, m_rowsInYearDecadeView);
				pPanel.Orientation = Orientation.Horizontal;
			}

			spHeaderButton = this.GetTemplatePart<Button>("HeaderButton");
			spPreviousButton = this.GetTemplatePart<Button>("PreviousButton");
			spNextButton = this.GetTemplatePart<Button>("NextButton");

			if (spPreviousButton is {})
			{
				strAutomationName = DirectUI.AutomationProperties.GetNameStatic((Button)spPreviousButton);
				if (strAutomationName == null)
				{
					// USe the same resource string as for FlipView's Previous Button.
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_PREVIOUS);
					DirectUI.AutomationProperties.SetNameStatic((Button)spPreviousButton, strAutomationName);
				}
			}

			if (spNextButton is { })
			{
				strAutomationName = DirectUI.AutomationProperties.GetNameStatic((Button)spNextButton);
				if (strAutomationName == null)
				{
					// USe the same resource string as for FlipView's Next Button.
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_NEXT);
					DirectUI.AutomationProperties.SetNameStatic((Button)spNextButton, strAutomationName);
				}
			}

			m_tpHeaderButton = spHeaderButton;
			m_tpPreviousButton = spPreviousButton;
			m_tpNextButton = spNextButton;

			spViewsGrid = this.GetTemplatePart<Grid>("Views");
			m_tpViewsGrid = spViewsGrid;

			spMonthViewScrollViewer = this.GetTemplatePart<ScrollViewer>("MonthViewScrollViewer");
			spYearViewScrollViewer = this.GetTemplatePart<ScrollViewer>("YearViewScrollViewer");
			spDecadeViewScrollViewer = this.GetTemplatePart<ScrollViewer>("DecadeViewScrollViewer");

			m_tpMonthViewItemHost.ScrollViewer = spMonthViewScrollViewer;
			m_tpYearViewItemHost.ScrollViewer = spYearViewScrollViewer;
			m_tpDecadeViewItemHost.ScrollViewer = spDecadeViewScrollViewer;

			// Setting custom CalendarScrollViewerAutomationPeer for these scrollviewers to be the default one.
			if (spMonthViewScrollViewer is {})
			{
				((ScrollViewer)spMonthViewScrollViewer).AutomationPeerFactoryIndex = (int)(KnownTypeIndex.CalendarScrollViewerAutomationPeer);

				m_tpMonthViewScrollViewer = spMonthViewScrollViewer;
			}

			if (spYearViewScrollViewer is { })
			{
				((ScrollViewer)spYearViewScrollViewer).AutomationPeerFactoryIndex = (int)(KnownTypeIndex.CalendarScrollViewerAutomationPeer);

				m_tpYearViewScrollViewer = spYearViewScrollViewer;
			}

			if (spDecadeViewScrollViewer is { })
			{
				((ScrollViewer)spDecadeViewScrollViewer).AutomationPeerFactoryIndex = (int)(KnownTypeIndex.CalendarScrollViewerAutomationPeer);

				m_tpDecadeViewScrollViewer = spDecadeViewScrollViewer;
			}

			Debug.Assert(!m_areDirectManipulationStateChangeHandlersHooked);

			ForeachHost(pHost =>
			{
				var pScrollViewer = pHost.ScrollViewer;
				if (pScrollViewer)
				{
					pScrollViewer.TemplatedParentHandlesScrolling = TRUE;
					pScrollViewer.SetDirectManipulationStateChangeHandler(pHost);
					pScrollViewer.m_templatedParentHandlesMouseButton = TRUE;
				}

				Cleanup:
				return hr;
			});

			m_areDirectManipulationStateChangeHandlersHooked = true;

			AttachVisibleIndicesUpdatedEvents();

			AttachButtonClickedEvents();

			AttachScrollViewerKeyDownEvents();

			// This will connect the new panels with ItemHosts
			RegisterItemHosts();

			AttachScrollViewerFocusEngagedEvents();

			UpdateVisualState(FALSE /*bUseTransitions*/);

			UpdateFlowDirectionForView();

			Cleanup:
			return hr;
		}

		// Change to the correct visual state for the control.
		private void ChangeVisualState(
			bool bUseTransitions)
		{
			CalendarViewDisplayMode mode = CalendarViewDisplayMode_Month;
			BOOLEAN bIgnored = FALSE;

			mode = DisplayMode;

			if (mode == CalendarViewDisplayMode_Month)
			{
				GoToState(bUseTransitions, "Month", &bIgnored);
			}
			else if (mode == CalendarViewDisplayMode_Year)
			{
				GoToState(bUseTransitions, "Year", &bIgnored);
			}
			else //if (mode == CalendarViewDisplayMode_Decade)
			{
				GoToState(bUseTransitions, "Decade", &bIgnored);
			}

			BOOLEAN isEnabled = FALSE;
			isEnabled = IsEnabled;

			// Common States Group
			if (!isEnabled)
			{
				GoToState(bUseTransitions, "Disabled", &bIgnored);
			}
			else
			{
				GoToState(bUseTransitions, "Normal", &bIgnored);
			}

			return S_OK;
		}

		// Primary panel will determine CalendarView's size, when Primary Panel's desired size changed, we need
		// to update the template settings so other template parts can update their size correspondingly.
		private void OnPrimaryPanelDesiredSizeChanged( CalendarViewGeneratorHost* pHost)
		{
			// monthpanel is the only primary panel
			ASSERT(pHost == m_tpMonthViewItemHost;

			var pMonthViewPanel = pHost.Panel;

			ASSERT(pMonthViewPanel);

			wf.Size desiredViewportSize = { };
			pMonthViewPanel.GetDesiredViewportSize(&desiredViewportSize);

			CalendarViewTemplateSettings* pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);
			pTemplateSettingsConcrete.MinViewWidth = desiredViewportSize.Width;

			return S_OK;
		}

		IFACEMETHODIMP CalendarView.MeasureOverride(
			wf.Size availableSize,
			out wf.Size* pDesired)
		{
			HRESULT hr = S_OK;

			if (m_calendarChanged)
			{
				m_calendarChanged = false;
				CreateCalendarAndMonthYearFormatter();
				FormatWeekDayNames();
				UpdateFlowDirectionForView();
			}

			if (m_dateSourceChanged)
			{
				// m_minDate or m_maxDate changed, we need to refresh all dates
				// so we should disconnect itemhosts and update the itemhosts, then register them again.
				m_dateSourceChanged = false;
				DisconnectItemHosts();
				RefreshItemHosts();
				InitializeIndexCorrectionTableIfNeeded(); // for some timezones, we need to figure out where are the gaps (missing days)
				RegisterItemHosts();
				UpdateWeekDayNames();
			}

			CalendarViewGenerated.MeasureOverride(availableSize, pDesired);

			Cleanup:
			return hr;
		}

		IFACEMETHODIMP CalendarView.ArrangeOverride(
			wf.Size finalSize,
			out wf.Size* returnValue)
		{
			CalendarViewGenerated.ArrangeOverride(finalSize, returnValue);

			if (m_tpViewsGrid)
			{
				// When switching views, the up-scaled view needs to be clipped by the original size.
				double viewsHeight = 0.0;
				double viewsWidth = 0.0;
				CalendarViewTemplateSettings* pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

				viewsHeight = ((Grid)m_tpViewsGrid).ActualHeight;
				viewsWidth = ((Grid)m_tpViewsGrid).ActualWidth;

				wf.Rect clipRect = {0., 0., (float)(viewsWidth), (float)(viewsHeight)};

				pTemplateSettingsConcrete.ClipRect = clipRect;

				// ScaleTransform.CenterX and CenterY
				pTemplateSettingsConcrete.CenterX = (viewsWidth / 2);
				pTemplateSettingsConcrete.CenterY = (viewsHeight / 2);
			}

			if (m_isSetDisplayDateRequested)
			{
				// m_lastDisplayedDate is already coerced and adjusted, time to process this request and clear the flag.
				m_isSetDisplayDateRequested = false;
				SetDisplayDateinternal(m_lastDisplayedDate);
			}
		}

		// create a list of languages to construct Globalization.Calendar and Globalization.DateTimeFormatter.
		// here we prepend CalendarView.Language to ApplicationLanguage.Languages as the new list.
		private void CreateCalendarLanguages()
		{
			wrl_wrappers.string strLanguage;
			ctl.ComPtr<wfc.IEnumerable<string>> spCalendarLanguages;

			get_Language(strLanguage);
			CreateCalendarLanguagesStatic(std.move(strLanguage), &spCalendarLanguages);
			m_tpCalendarLanguages = spCalendarLanguages;

			return S_OK;
		}

		// helper method to prepend a string into a collection of string.
		/*static */
		private void CreateCalendarLanguagesStatic(
		wrl_wrappers.string&& language,

		_Outptr_ wfc.IEnumerable<string>** ppLanguages)
		{
			ctl.ComPtr<wg.IApplicationLanguagesStatics> spApplicationLanguagesStatics;
			ctl.ComPtr<wfc.IVectorView<string>> spApplicationLanguages;
			ctl.ComPtr<internalStringCollection> spCalendarLanguages;
			unsigned size = 0;

			ctl.GetActivationFactory(
				wrl_wrappers.string(RuntimeClass_Windows_Globalization_ApplicationLanguages),
				&spApplicationLanguagesStatics);

			spApplicationLanguages = spApplicationLanguagesStatics.Languages;

			ctl.make(&spCalendarLanguages);
			spCalendarLanguages.emplace_back(std.move(language);

			size = spApplicationLanguages.Size;

			for (unsigned i = 0; i < size; ++i)
			{
				wrl_wrappers.string strApplicationLanguage;
				spApplicationLanguages.GetAt(i, strApplicationLanguage);
				spCalendarLanguages.emplace_back(std.move(strApplicationLanguage);
			}

			spCalendarLanguages.MoveTo(ppLanguages);

			return S_OK;
		}

		private void CreateCalendarAndMonthYearFormatter()
		{
			ctl.ComPtr<wg.ICalendarFactory> spCalendarFactory;
			ctl.ComPtr<wg.ICalendar> spCalendar;
			wrl_wrappers.Hconst string strClock = "24HourClock"; // it doesn't matter if it is 24 or 12 hour clock
			wrl_wrappers.string strCalendarIdentifier;

			get_CalendarIdentifier(strCalendarIdentifier);

			//Create the calendar
			ctl.GetActivationFactory(
				wrl_wrappers.string(RuntimeClass_Windows_Globalization_Calendar),
				&spCalendarFactory);

			spCalendarFactory.CreateCalendar(
				m_tpCalendarLanguages,
				strCalendarIdentifier,
				strClock,
				spCalendar);

			m_tpCalendar = spCalendar;

			// create a Calendar clone (have the same timezone, same calendarlanguages and identifier) for DateComparer and SelectedDates
			// changing the date on the Calendar in DateComparer will not affect the Calendar in CalendarView.
			m_dateComparer.SetCalendarForComparison(spCalendar);
			((TrackableDateCollection)m_tpSelectedDates).SetCalendarForComparison(spCalendar);

			// in multiple era calendar, we'll have different (slower) logic to handle the decade scope.
			{
				int firstEra = 0;
				int lastEra = 0;
				firstEra = m_tpCalendar.FirstEra;
				lastEra = m_tpCalendar.LastEra;
				m_isMultipleEraCalendar = firstEra != lastEra;
			}

			m_tpCalendar.SetToNow();
			m_tpCalendar.GetDateTime(&m_today);

			// default displaydate is today
			if (m_lastDisplayedDate.UniversalTime == 0)
			{
				m_lastDisplayedDate = m_today;
			}

			ForeachHost([this](CalendarViewGeneratorHost * pHost)
			{
				var pPanel = pHost.Panel;
				if (pPanel)
				{
					pPanel.SetNeedsToDetermineBiggestItemSize();
				}

				pHost.ResetPossibleItemStrings();
				return S_OK;
			});

			{
				ctl.ComPtr<wg.DateTimeFormatting.IDateTimeFormatter> spFormatter;

				// month year formatter needs to be updated when calendar is updated (languages or calendar identifier changed).
				CreateDateTimeFormatter(wrl_wrappers.string("month year"), &spFormatter);
				m_tpMonthYearFormatter = spFormatter;

				// year formatter also needs to be updated when the calendar is updated.
				CreateDateTimeFormatter(wrl_wrappers.string("year"), &spFormatter);
				m_tpYearFormatter = spFormatter;
			}

			return S_OK;
		}

		private void DisconnectItemHosts()
		{

			if (m_itemHostsConnected)
			{
				m_itemHostsConnected = false;

				ForeachHost([](CalendarViewGeneratorHost * pHost)
				{
					var pPanel = pHost.Panel;
					if (pPanel)
					{
						pPanel.DisconnectItemsHost();
					}

					return S_OK;
				});
			}

			return S_OK;
		}

		private void RegisterItemHosts()
		{
			ASSERT(!m_itemHostsConnected);

			m_itemHostsConnected = true;

			ForeachHost([](CalendarViewGeneratorHost * pHost)
			{
				var pPanel = pHost.Panel;
				if (pPanel)
				{
					pPanel.RegisterItemsHost(pHost);
				}

				return S_OK;
			});

			return S_OK;
		}

		private void RefreshItemHosts()
		{
			DateTime minDate;
			DateTime maxDate;

			minDate = MinDate;
			maxDate = MaxDate;

			// making sure our MinDate and MaxDate are supported by the current Calendar
			{
				wf.DateTime tempDate;

				m_tpCalendar.SetToMin();
				m_tpCalendar.GetDateTime(&tempDate);

				m_minDate.UniversalTime = MAX(minDate.UniversalTime, tempDate.UniversalTime);

				m_tpCalendar.SetToMax();
				m_tpCalendar.GetDateTime(&tempDate);

				m_maxDate.UniversalTime = MIN(maxDate.UniversalTime, tempDate.UniversalTime);
			}

			if (m_dateComparer.LessThan(m_maxDate, m_minDate))
			{
				ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_INVALID_MIN_MAX_DATE);
			}

			CoerceDate(m_lastDisplayedDate);

			m_tpCalendar.SetDateTime(m_minDate);
			m_weekDayOfMinDate = m_tpCalendar.DayOfWeek;

			ForeachHost([](CalendarViewGeneratorHost * pHost)
			{
				pHost.ResetScope(); // reset the scope data to force update the scope and header text.
				pHost.ComputeSize();
				return S_OK;
			});

			return S_OK;
		}

		private void AttachVisibleIndicesUpdatedEvents()
		{
			return ForeachHost([this](CalendarViewGeneratorHost * pHost)
			{
				return pHost.AttachVisibleIndicesUpdatedEvent();
			});
		}

		private void DetachVisibleIndicesUpdatedEvents()
		{
			return ForeachHost([](CalendarViewGeneratorHost * pHost)
			{
				return pHost.DetachVisibleIndicesUpdatedEvent();
			});
		}

		// attach FocusEngaged event for all three hosts
		private void AttachScrollViewerFocusEngagedEvents()
		{
			return ForeachHost([this](CalendarViewGeneratorHost * pHost)
			{
				return pHost.AttachScrollViewerFocusEngagedEvent();
			});
		}

		// detach FocusEngaged event for all three hosts
		private void DetachScrollViewerFocusEngagedEvents()
		{
			return ForeachHost([](CalendarViewGeneratorHost * pHost)
			{
				return pHost.DetachScrollViewerFocusEngagedEvent();
			});
		}

		private void AttachButtonClickedEvents()
		{
			HRESULT hr = S_OK;

			if (m_tpHeaderButton)
			{
				m_epHeaderButtonClickHandler.AttachEventHandler(((Button)m_tpHeaderButton), 
					[this](IInspectable * pSender, IRoutedEventArgs * pArgs)
				{
					return OnHeaderButtonClicked();
				});
			}

			if (m_tpPreviousButton)
			{
				m_epPreviousButtonClickHandler.AttachEventHandler(((Button)m_tpPreviousButton), 
					[this](IInspectable * pSender, IRoutedEventArgs * pArgs)
				{
					return OnNavigationButtonClicked(false /*forward*/);
				});
			}

			if (m_tpNextButton)
			{
				m_epNextButtonClickHandler.AttachEventHandler(((Button)m_tpNextButton), 
					[this](IInspectable * pSender, IRoutedEventArgs * pArgs)
				{
					return OnNavigationButtonClicked(true /*forward*/);
				});
			}

			Cleanup:
			return hr;
		}

		private void DetachButtonClickedEvents()
		{
			HRESULT hr = S_OK;

			DetachHandler(m_epHeaderButtonClickHandler, m_tpHeaderButton);
			DetachHandler(m_epPreviousButtonClickHandler, m_tpPreviousButton);
			DetachHandler(m_epNextButtonClickHandler, m_tpNextButton);

			Cleanup:
			return hr;
		}

		private void AttachScrollViewerKeyDownEvents()
		{
			//Engagement now prevents events from bubbling from an engaged element. Before we relied on the bubbling behavior to
			//receive the KeyDown events from the ScrollViewer in the CalendarView. Now instead we have to handle the ScrollViewer's
			//On Key Down. To prevent handling the same OnKeyDown twice we only call into OnKeyDown if the ScrollViewer is engaged,
			//if it isn't it will bubble up the event.
			if (m_tpMonthViewScrollViewer)
			{
				m_epMonthViewScrollViewerKeyDownEventHandler.AttachEventHandler(m_tpMonthViewScrollViewer.AsOrNull<IUIElement>(), 
					[this](IInspectable * pSender, xaml.Input.IKeyRoutedEventArgs * pArgs)
				{
					BOOLEAN isEngaged = FALSE;
					isEngaged = ((ScrollViewer)m_tpMonthViewScrollViewer).IsFocusEngaged;
					if (isEngaged)
					{
						return OnKeyDown(pArgs);
					}

					return S_OK;
				});
			}

			if (m_tpYearViewScrollViewer)
			{
				m_epYearViewScrollViewerKeyDownEventHandler.AttachEventHandler(m_tpYearViewScrollViewer.AsOrNull<IUIElement>(), 
					[this](IInspectable * pSender, xaml.Input.IKeyRoutedEventArgs * pArgs)
				{
					BOOLEAN isEngaged = FALSE;
					isEngaged = ((ScrollViewer)m_tpYearViewScrollViewer).IsFocusEngaged;
					if (isEngaged)
					{
						return OnKeyDown(pArgs);
					}

					return S_OK;
				});
			}

			if (m_tpDecadeViewScrollViewer)
			{
				m_epDecadeViewScrollViewerKeyDownEventHandler.AttachEventHandler(m_tpDecadeViewScrollViewer.AsOrNull<IUIElement>(), 
					[this](IInspectable * pSender, xaml.Input.IKeyRoutedEventArgs * pArgs)
				{
					BOOLEAN isEngaged = FALSE;
					isEngaged = ((ScrollViewer)m_tpDecadeViewScrollViewer).IsFocusEngaged;
					if (isEngaged)
					{
						return OnKeyDown(pArgs);
					}

					return S_OK;
				});
			}

			return S_OK;
		}

		private void DetachScrollViewerKeyDownEvents()
		{
			DetachHandler(m_epMonthViewScrollViewerKeyDownEventHandler, m_tpMonthViewScrollViewer);
			DetachHandler(m_epYearViewScrollViewerKeyDownEventHandler, m_tpYearViewScrollViewer);
			DetachHandler(m_epDecadeViewScrollViewerKeyDownEventHandler, m_tpDecadeViewScrollViewer);

			return S_OK;
		}

		private void UpdateHeaderText( bool withAnimation)
		{
			ctl.ComPtr<CalendarViewGeneratorHost> spHost;

			GetActiveGeneratorHost(&spHost);

			CalendarViewTemplateSettings* pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);
			pTemplateSettingsConcrete.HeaderText = spHost.GetHeaderTextOfCurrentScope();

			if (withAnimation)
			{
				BOOLEAN bIgnored = FALSE;
				// play animation on the HeaderText after view mode changed.
				GoToState(true, "ViewChanged", &bIgnored);
				GoToState(true, "ViewChanging", &bIgnored);
			}

			// If UpdateText is because navigation button is clicked, make narrator to say the header.
			if (m_isNavigationButtonClicked)
			{
				m_isNavigationButtonClicked = false;
				RaiseAutomationNotificationAfterNavigationButtonClicked();

			}

			return S_OK;
		}

		// disable the button if we don't have more content
		private void UpdateNavigationButtonStates()
		{
			ctl.ComPtr<CalendarViewGeneratorHost> spHost;

			GetActiveGeneratorHost(&spHost);

			var pCalendarPanel = spHost.Panel;

			if (pCalendarPanel)
			{
				int firstVisibleIndex = 0;
				int lastVisibleIndex = 0;
				unsigned size = 0;
				CalendarViewTemplateSettings* pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

				firstVisibleIndex = pCalendarPanel.FirstVisibleIndexBase;
				lastVisibleIndex = pCalendarPanel.LastVisibleIndexBase;

				size = spHost.Size;

				pTemplateSettingsConcrete.put_HasMoreContentBefore(firstVisibleIndex > 0);
				pTemplateSettingsConcrete.put_HasMoreContentAfter(lastVisibleIndex + 1 < (int)(size));
			}

			return S_OK;
		}

		private void OnHeaderButtonClicked()
		{
			HRESULT hr = S_OK;
			CalendarViewDisplayMode mode = CalendarViewDisplayMode_Month;

			mode = DisplayMode;

			if (mode != CalendarViewDisplayMode_Decade)
			{
				if (mode == CalendarViewDisplayMode_Month)
				{
					mode = CalendarViewDisplayMode_Year;
				}
				else // if (mode == CalendarViewDisplayMode_Year)
				{
					mode = CalendarViewDisplayMode_Decade;
				}

				put_DisplayMode(mode);
			}
			else
			{
				ASSERT(FALSE, "header button should be disabled already in decade view mode.");
			}

			Cleanup:
			return hr;
		}

		private void RaiseAutomationNotificationAfterNavigationButtonClicked()
		{
			if (m_tpHeaderButton)
			{
				wrl_wrappers.string automationName;
				DirectUI.AutomationProperties.GetNameStatic(((Button)m_tpHeaderButton), automationName))
				if (!automationName)
				{
					FrameworkElement.GetStringFromObject(m_tpHeaderButton, automationName);
				}

				if (automationName)
				{
					ctl.ComPtr<xaml_automation_peers.IAutomationPeer> calendarViewAutomationPeer;

					GetOrCreateAutomationPeer(&calendarViewAutomationPeer);
					if (calendarViewAutomationPeer)
					{
						// Two possible solution: RaisePropertyChangedEvent or RaiseNotificationEvent. If Raise PropertyChangedEvent each time when head is changing,
						// it would be overkilling since header is already included in other automation event. More information about RaiseNotificationEvent, please
						// refer to https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.automation.peers.automationnotificationkind
						calendarViewAutomationPeer.RaiseNotificationEvent(
							xaml_automation_peers.AutomationNotificationKind.AutomationNotificationKind_ActionCompleted,
							xaml_automation_peers.AutomationNotificationProcessing.AutomationNotificationProcessing_MostRecent,
							automationName,
							wrl_wrappers.string("CalenderViewNavigationButtonCompleted"));
					}
				}
			}

			return S_OK;
		}

		private void OnNavigationButtonClicked( bool forward)
		{
			ctl.ComPtr<CalendarViewGeneratorHost> spHost;

			GetActiveGeneratorHost(&spHost);

			var pCalendarPanel = spHost.Panel;

			if (pCalendarPanel)
			{
				bool canPanelShowFullScope = false;

				int firstVisibleIndex = 0;
				ctl.ComPtr<IDependencyObject> spChildAsIDO;
				ctl.ComPtr<ICalendarViewBaseItem> spChildAsI;
				ctl.ComPtr<CalendarViewBaseItem> spChild;
				wf.DateTime dateOfFirstVisibleItem = { };
				wf.DateTime targetDate = { };

				CanPanelShowFullScope(spHost, &canPanelShowFullScope);

				firstVisibleIndex = pCalendarPanel.FirstVisibleIndexBase;

				pCalendarPanel.ContainerFromIndex(firstVisibleIndex, &spChildAsIDO);

				spChildAsI = spChildAsIDO.AsOrNull<ICalendarViewBaseItem>();
				if (spChildAsI)
				{
					spChild = ((CalendarViewBaseItem)spChildAsI);
					spChild.GetDate(&dateOfFirstVisibleItem);

					HRESULT hr = S_OK;

					if (canPanelShowFullScope)
					{
						// if Panel can show a full scope, we navigate by a scope.
						hr = spHost.GetFirstDateOfNextScope(dateOfFirstVisibleItem, forward, &targetDate);
					}
					else
					{
						// if Panel can't show a full scope, we navigate by a page, so we don't skip items.
						int cols = 0;
						int rows = 0;

						rows = pCalendarPanel.Rows;
						cols = pCalendarPanel.Cols;
						int numberOfItemsPerPage = cols * rows;
						int distance = forward ? numberOfItemsPerPage : -numberOfItemsPerPage;
						targetDate = dateOfFirstVisibleItem;
						hr = spHost.AddUnits(targetDate, distance);
#ifdef DBG
						if (SUCCEEDED(hr))
						{
							// targetDate should be still in valid range.
							var temp = targetDate;
							CoerceDate(temp);
							ASSERT(temp.UniversalTime == targetDate.UniversalTime);
						}

#endif
					}

					if (FAILED(hr))
					{
						// if we crossed the boundaries when we compute the target date, we use the hard limit.
						targetDate = forward ? m_maxDate : m_minDate;
					}

					ScrollToDateWithAnimation(spHost, targetDate);

					// After navigation button is clicked, header text is not updated immediately. ScrollToDateWithAnimation is the first step,
					// OnVisibleIndicesUpdated and UpdateHeaderText would be in another UI message processing loop.
					// This flag is to identify that the HeaderText update is from navigation button.
					m_isNavigationButtonClicked = true;
				}
			}

			return S_OK;
		}


		// change the dimensions of YearView and DecadeView.
		// API name to be reviewed.
		private void SetYearDecadeDisplayDimensionsImpl(
			int columns, int rows)
		{
			HRESULT hr = S_OK;

			IFCEXPECT_RETURN(columns > 0 && rows > 0);

			// note once this is set, developer can't unset it
			m_areYearDecadeViewDimensionsSet = true;

			m_colsInYearDecadeView = columns;
			m_rowsInYearDecadeView = rows;

			var pYearPanel = m_tpYearViewItemHost.Panel;
			if (pYearPanel)
			{
				// Panel type is no longer Secondary_SelfAdaptive
				pYearPanel.SetPanelType(CalendarPanel.CalendarPanelType.CalendarPanelType_Secondary);
				pYearPanel.SetSuggestedDimension(columns, rows);
			}

			var pDecadePanel = m_tpDecadeViewItemHost.Panel;
			if (pDecadePanel)
			{
				// Panel type is no longer Secondary_SelfAdaptive
				pDecadePanel.SetPanelType(CalendarPanel.CalendarPanelType.CalendarPanelType_Secondary);
				pDecadePanel.SetSuggestedDimension(columns, rows);
			}

			return S_OK;
		}

		// When we call SetDisplayDate, we'll check if the current view is big enough to hold a whole scope.
		// If yes then we'll bring the first date in this scope into view,
		// otherwise bring the display date into view then the display date will be on first visible line.
		//
		// note: when panel can't show a fullscope, we might be still able to show the first day and the requested date
		// in the viewport (e.g. NumberOfWeeks is 4, we request to display 1/9/2000, in this case 1/1/2000 and 1/9/2000 can
		// be visible at the same time). To consider this case we need more logic, we can fix later when needed.
		private void BringDisplayDateintoView(
			CalendarViewGeneratorHost* pHost)
		{
			bool canPanelShowFullScope = false;
			wf.DateTime dateToBringintoView;

			CanPanelShowFullScope(pHost, &canPanelShowFullScope);

			if (canPanelShowFullScope)
			{
				m_tpCalendar.SetDateTime(m_lastDisplayedDate);
				pHost.AdjustToFirstUnitinthisScope(&dateToBringintoView);
				CoerceDate(dateToBringintoView);
			}
			else
			{
				dateToBringintoView.UniversalTime = m_lastDisplayedDate.UniversalTime;
			}

			ScrollToDate(pHost, dateToBringintoView);

			return S_OK;
		}

		// bring a item into view
		// This function will scroll to the target item immediately,
		// when target is far away from realized window, we'll not see unrealized area.
		private void ScrollToDate(
			CalendarViewGeneratorHost* pHost,
			wf.DateTime date)
		{
			HRESULT hr = S_OK;
			int index = 0;

			pHost.CalculateOffsetFromMinDate(date, &index);
			ASSERT(index >= 0);
			ASSERT(pHost.Panel;
			pHost.Panel.ScrollItemintoView(
				index,
				ScrollintoViewAlignment_Leading,
				0.0 /* offset */,
				TRUE /* forceSynchronous */);

			Cleanup:
			return hr;
		}

		// Bring a item into view with animation.
		// This function will scroll to the target item with DManip animation so
		// if target is not realized yet, we might see unrealized area.
		// This only gets called in NavigationButton clicked event where
		// the target should be less than one page away from visible window.
		private void ScrollToDateWithAnimation(
			CalendarViewGeneratorHost* pHost,
			wf.DateTime date)
		{
			var pScrollViewer = pHost.ScrollViewer;
			if (pScrollViewer)
			{
				int index = 0;
				int firstVisibleIndex = 0;
				int cols = 0;
				ctl.ComPtr<IDependencyObject> spFirstVisibleItemAsI;
				ctl.ComPtr<CalendarViewBaseItem> spFirstVisibleItem;
				ctl.ComPtr<IInspectable> spVerticalOffset;
				ctl.ComPtr<wf.IReference<DOUBLE>> spVerticalOffsetReference;
				boolean handled = false;

				var pCalendarPanel = pHost.Panel;

				// The target item may be not realized yet so we can't get
				// the offset from virtualization information.
				// However all items have the same size so we could deduce the target's
				// exact offset from the current realized item, e.g. the firstVisibleItem

				// 1. compute the target index.
				pHost.CalculateOffsetFromMinDate(date, &index);
				ASSERT(index >= 0);

				cols = pCalendarPanel.Cols;
				ASSERT(cols > 0);

				// cols should not be 0 at this point. If it is, perhaps
				// the calendar view has not been fully brought up yet.
				// If cols is 0, we do not want to bring the process down though.
				// Doing a no-op for the scroll to date in this case.
				if (cols > 0)
				{
					// 2. find the first visible index.
					firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;
					pCalendarPanel.ContainerFromIndex(firstVisibleIndex, &spFirstVisibleItemAsI);
					spFirstVisibleItemAsI.As(&spFirstVisibleItem);

					ASSERT(spFirstVisibleItem.GetVirtualizationInformation();

					// 3. based on the first visible item's bounds, compute the target item's offset
					var bounds = spFirstVisibleItem.GetVirtualizationInformation().GetBounds();
					var verticalDistance = (index - firstVisibleIndex) / cols;

					// if target item is before the first visible index and is not the first in that row, we should substract 1 from the distance
					// because -6 / 7 = 0 (we expect -1).
					if ((index - firstVisibleIndex) % cols != 0 && index <= firstVisibleIndex)
					{
						--verticalDistance;
					}
					// there are some corner cases in Japanese calendar where the target date and current date are in the same row.
					// e.g. Showa 64 only has 1 month, in year view, January Show64 and January Heisei are in the same row.
					// When we try to scroll down from showa64 to Heisei1 in year view, verticalDistance would be 0 since those 2 years are in the same row.
					// We do ++verticalDistance here to point to March of Heise 1 in the next row, otherwise we'll get stuck in the first row and navigate down button would stop working.
					else if (verticalDistance == 0 && index > firstVisibleIndex)
					{
						++verticalDistance;
					}

					var offset = bounds.Y + verticalDistance * bounds.Height;

					// 4. scroll to target item's offset (with animation)
					PropertyValue.CreateFromDouble(offset, &spVerticalOffset);
					spVerticalOffset.As(&spVerticalOffsetReference);

					pScrollViewer.ChangeViewWithOptionalAnimation(
						null /*horizontalOffset*/,
						spVerticalOffsetReference,
						null /*zoomFactor*/,
						false /*disableAnimation*/,
						&handled);
					ASSERT(handled);
				}
			}

			return S_OK;
		}

		private void SetDisplayDateImpl( wf.DateTime date)
		{
			// if m_dateSourceChanged is true, this means we might changed m_minDate or m_maxDate
			// so we should not call CoerceDate until next measure pass, by then the m_minDate and
			// m_maxDate are updated.
			if (!m_dateSourceChanged)
			{
				CoerceDate(date);

				SetDisplayDateinternal(date);
			}
			else
			{
				// given that m_dateSourceChanged is true, we'll have a new layout pass soon.
				// we're going to honer the display date request in that layout pass.
				// note: there is an issue to call ScrollItemintoView in MCBP's measure pass
				// the workaround is call it in Arrange pass or later. here we'll call it
				// in the arrange pass.
				m_isSetDisplayDateRequested = true;
				m_lastDisplayedDate = date;
			}

			return S_OK;
		}

		private void SetDisplayDateinternal( wf.DateTime date)
		{
			HRESULT hr = S_OK;
			ctl.ComPtr<CalendarViewGeneratorHost> spHost;

			GetActiveGeneratorHost(&spHost);

			m_lastDisplayedDate = date;

			if (spHost.Panel)
			{
				// note if panel is not loaded yet (i.e. we call SetDisplayDate before Panel is loaded,
				// below call will fail silently. This is not a problem because
				// we'll call this again in panel loaded event.

				BringDisplayDateintoView(spHost);
			}

			Cleanup:
			return hr;
		}

		void CalendarView.CoerceDate(wf.DateTime& date)
		{
			// we should not call CoerceDate when m_dateSourceChanged is true, because
			// m_dateSourceChanged being true means the current m_minDate or m_maxDate is
			// out of dated.

			ASSERT(!m_dateSourceChanged);
			if (m_dateComparer.LessThan(date, m_minDate))
			{
				date = m_minDate;
			}

			if (m_dateComparer.LessThan(m_maxDate, date))
			{
				date = m_maxDate;
			}
		}

		private void OnVisibleIndicesUpdated(
			CalendarViewGeneratorHost* pHost)
		{
			HRESULT hr = S_OK;
			int firstVisibleIndex = 0;
			int lastVisibleIndex = 0;
			ctl.ComPtr<IDependencyObject> spTempChildAsIDO;
			ctl.ComPtr<ICalendarViewBaseItem> spTempChildAsI;
			wf.DateTime firstDate = { };
			wf.DateTime lastDate = { };
			bool isScopeChanged = false;
			int startIndex = 0;
			int numberOfItemsInCol;

			var pCalendarPanel = pHost.Panel;

			ASSERT(pCalendarPanel);

			// We explicitly call UpdateLayout in OnDisplayModeChanged, this will ocassionaly make CalendarPanelType invalid,
			// which causes CalendarPanel to skip the row&col calculations.
			// If CalendarPanelType is invalid, just skip the current update
			// since this method will be called again in later layout passes.
			pCalendarPanel.GetPanelType() != CalendarPanel.CalendarPanelType.CalendarPanelType_Invalid)
			{
				startIndex = pCalendarPanel.StartIndex;
				numberOfItemsInCol = pCalendarPanel.Cols;

				ASSERT(startIndex < numberOfItemsInCol);

				firstVisibleIndex = pCalendarPanel.FirstVisibleIndexBase;
				lastVisibleIndex = pCalendarPanel.LastVisibleIndexBase;

				pCalendarPanel.ContainerFromIndex(firstVisibleIndex, &spTempChildAsIDO);

				spTempChildAsIDO.As(&spTempChildAsI);

				((CalendarViewBaseItem)spTempChildAsI).GetDate(&firstDate);

				pCalendarPanel.ContainerFromIndex(lastVisibleIndex, spTempChildAsIDO);

				spTempChildAsIDO.As(&spTempChildAsI);

				((CalendarViewBaseItem)spTempChildAsI).GetDate(&lastDate);

				//now determine the current scope based on this date.
				pHost.UpdateScope(firstDate, lastDate, &isScopeChanged);

				if (isScopeChanged)
				{
					UpdateHeaderText(false /*withAnimation*/);
				}

				// everytime visible indices changed, we need to update
				// navigationButtons' states.
				UpdateNavigationButtonStates();

				UpdateItemsScopeState(
					pHost,
					true, /*ignoreWhenIsOutOfScopeDisabled*/
					true /*ignoreInDirectManipulation*/);
			}

			Cleanup:
			return hr;
		}

		// to achieve best visual effect we define that items are in OutOfScope state only when:
		// 1. IsOutOfScopeEnabled is true, and
		// 2. item is in Visible window and it is not in current scope.
		// 3. Not in manipulation.
		// for all other cases, item is in InScope state.
		//
		// this function updates the ScopeState for
		// 1. all visible items, and
		// 2. the items that are not visible but was marked as OutOfScope (because viewport changed)
		//
		// so we'll call this function when
		// 1. IsOutOfScopeEnabled property changed, or
		// 2. Visible Indices changed
		// 3. Manipulation state changed.
		private void UpdateItemsScopeState(
			CalendarViewGeneratorHost* pHost,
			bool ignoreWhenIsOutOfScopeDisabled,
			bool ignoreInDirectManipulation)
		{
			var pCalendarPanel = pHost.Panel;
			if (!pCalendarPanel)
			{
				// it is possible that we change IsOutOfScopeEnabled property before CalendarView enters visual tree.
				return S_OK;
			}

			BOOLEAN isOutOfScopeEnabled = FALSE;
			isOutOfScopeEnabled = IsOutOfScopeEnabled;

			if (ignoreWhenIsOutOfScopeDisabled && !isOutOfScopeEnabled)
			{
				return S_OK;
			}

			bool isInDirectManipulation = pHost.ScrollViewer && pHost.ScrollViewer.IsInDirectManipulation();
			if (ignoreInDirectManipulation && isInDirectManipulation)
			{
				return S_OK;
			}

			bool canHaveOutOfScopeState = isOutOfScopeEnabled && !isInDirectManipulation;
			int firstIndex = -1;
			int lastIndex = -1;
			ctl.ComPtr<IDependencyObject> spChildAsIDO;
			ctl.ComPtr<ICalendarViewBaseItem> spChildAsI;
			ctl.ComPtr<CalendarViewBaseItem> spChild;
			wf.DateTime date;

			firstIndex = pCalendarPanel.FirstVisibleIndex;
			lastIndex = pCalendarPanel.LastVisibleIndex;

			// given that all items not in visible window have InScope state, so we only want
			// to check the visible window, plus the items in last visible window. this way
			// we don't need to check against virtualization window.
			auto & lastVisibleIndicesPair = pHost.GetLastVisibleIndicesPairRef();

			if (firstIndex != -1 && lastIndex != -1)
			{
				for (int index = firstIndex; index <= lastIndex; ++index)
				{
					pCalendarPanel.ContainerFromIndex(index, &spChildAsIDO);
					spChildAsIDO.As(&spChildAsI);

					spChild = ((CalendarViewBaseItem)spChildAsI);
					spChild.GetDate(&date);

					bool isOutOfScope = m_dateComparer.LessThan(date, pHost.GetMinDateOfCurrentScope()) || m_dateComparer.LessThan(pHost.GetMaxDateOfCurrentScope(), date);


					spChild.SetIsOutOfScope(canHaveOutOfScopeState && isOutOfScope);
				}
			}

			// now let's check the items were marked as OutOfScope but now not in Visible window (so they should be marked as InScope)


			if (lastVisibleIndicesPair[0] != -1 && lastVisibleIndicesPair[1] != -1)
			{
				if (lastVisibleIndicesPair[0] < firstIndex)
				{
					for (int index = lastVisibleIndicesPair[0]; index <= Math.Min(lastVisibleIndicesPair[1], firstIndex - 1); ++index)
					{
						pCalendarPanel.ContainerFromIndex(index, &spChildAsIDO);
						spChildAsI = spChildAsIDO.AsOrNull<ICalendarViewBaseItem>();

						if (spChildAsI)
						{
							// this item is not in visible window but was marked as OutOfScope before, set it to "InScope" now.
							((CalendarViewBaseItem)spChildAsI).SetIsOutOfScope(false);
						}
					}
				}

				if (lastVisibleIndicesPair[1] > lastIndex)
				{
					for (int index = lastVisibleIndicesPair[1]; index >= Math.Max(lastVisibleIndicesPair[0], lastIndex + 1); --index)
					{
						pCalendarPanel.ContainerFromIndex(index, &spChildAsIDO);
						spChildAsI = spChildAsIDO.AsOrNull<ICalendarViewBaseItem>();

						if (spChildAsI)
						{
							// this item is not in visible window but was marked as OutOfScope before, set it to "InScope" now.
							((CalendarViewBaseItem)spChildAsI).SetIsOutOfScope(false);
						}
					}
				}
			}

			// store the visible indices pair
			lastVisibleIndicesPair[0] = firstIndex;
			lastVisibleIndicesPair[1] = lastIndex;
			return S_OK;
		}

		// this property affects Today in MonthView, ThisMonth in YearView and ThisYear in DecadeView.
		private void OnIsTodayHighlightedPropertyChanged()
		{
			ForeachHost([this](CalendarViewGeneratorHost * pHost)
			{
				var pPanel = pHost.Panel;
				if (pPanel)
				{
					int indexOfToday = -1;

					pHost.CalculateOffsetFromMinDate(m_today, &indexOfToday);

					if (indexOfToday != -1)
					{
						ctl.ComPtr<IDependencyObject> spChildAsIDO;
						ctl.ComPtr<ICalendarViewBaseItem> spChildAsI;

						pPanel.ContainerFromIndex(indexOfToday, &spChildAsIDO);
						spChildAsI = spChildAsIDO.AsOrNull<ICalendarViewBaseItem>();
						// today item is realized already, we need to update the state here.
						// if today item is not realized yet, we'll update the state when today item is being prepared.
						if (spChildAsI)
						{
							BOOLEAN isTodayHighlighted = FALSE;

							isTodayHighlighted = IsTodayHighlighted;
							((CalendarViewBaseItem)spChildAsI).SetIsToday(!!isTodayHighlighted);
						}
					}
				}

				return S_OK;
			});

			return S_OK;
		}

		private void OnIsOutOfScopePropertyChanged()
		{
			ctl.ComPtr<CalendarViewGeneratorHost> spHost;
			BOOLEAN isOutOfScopeEnabled = FALSE;

			isOutOfScopeEnabled = IsOutOfScopeEnabled;

			// when IsOutOfScopeEnabled property is false, we don't care about scope state (all are inScope),
			// so we don't need to hook to ScrollViewer's state change handler.
			// when IsOutOfScopeEnabled property is true, we need to do so.
			if (m_areDirectManipulationStateChangeHandlersHooked != !!isOutOfScopeEnabled)
			{
				m_areDirectManipulationStateChangeHandlersHooked = !m_areDirectManipulationStateChangeHandlersHooked;

				ForeachHost([isOutOfScopeEnabled](CalendarViewGeneratorHost * pHost)
				{
					var pScrollViewer = pHost.ScrollViewer;
					if (pScrollViewer)
					{
						pScrollViewer.SetDirectManipulationStateChangeHandler(
							isOutOfScopeEnabled ? pHost : null
						);
					}

					return S_OK;
				});
			}

			GetActiveGeneratorHost(&spHost);
			UpdateItemsScopeState(
				spHost,
				false, /*ignoreWhenIsOutOfScopeDisabled*/
				true /*ignoreInDirectManipulation*/);

			return S_OK;
		}

		private void OnScrollViewerFocusEngaged(
			IFocusEngagedEventArgs* pArgs)
		{
			ctl.ComPtr<CalendarViewGeneratorHost> spHost;

			GetActiveGeneratorHost(&spHost);

			if (spHost)
			{
				bool focused = false;
				m_focusItemAfterDisplayModeChanged = false;
				ctl.ComPtr<IFocusEngagedEventArgs>
				spArgs(pArgs);

				FocusItemByDate(spHost, m_lastDisplayedDate, m_focusStateAfterDisplayModeChanged, &focused);

				spArgs.Handled = focused;
			}

			return S_OK;
		}

		private void OnDisplayModeChanged(
			CalendarViewDisplayMode oldDisplayMode,
			CalendarViewDisplayMode newDisplayMode)
		{
			ctl.ComPtr<CalendarViewGeneratorHost> spCurrentHost;
			ctl.ComPtr<CalendarViewGeneratorHost> spOldHost;
			BOOLEAN isEngaged = FALSE;

			GetGeneratorHost(oldDisplayMode, &spOldHost);
			if (spOldHost)
			{
				var pScrollViewer = spOldHost.ScrollViewer;

				if (pScrollViewer)
				{
					// if old host is engaged, disengage
					isEngaged = pScrollViewer.IsFocusEngaged;
					if (isEngaged)
					{
						pScrollViewer.RemoveFocusEngagement();
					}
				}
			}

			UpdateLastDisplayedDate(oldDisplayMode);

			UpdateVisualState();

			GetGeneratorHost(newDisplayMode, &spCurrentHost);
			var pCurrentPanel = spCurrentHost.Panel;
			if (pCurrentPanel)
			{
				// if panel is not loaded yet (e.g. the first time we switch to the YearView or DecadeView),
				// ScrollItemintoView (called by FocusItemByDate) will not work because ScrollViewer is not
				// hooked up yet. Give the panel an extra layout pass to hook up the ScrollViewer.
				if (newDisplayMode != CalendarViewDisplayMode_Month)
				{
					pCurrentPanel.UpdateLayout();
				}

				// If Engaged, make sure that the new scroll viewer is engaged. Note that
				// you want to Engage before focusing ItemByDate to land on the correct item.
				if (isEngaged)
				{
					var spScrollViewer = spCurrentHost.ScrollViewer;
					if (spScrollViewer)
					{
						// The old ScrollViewer was engaged, engage the new ScrollViewer
						ctl.ComPtr<xaml_input.IFocusManagerStaticsPrivate> spFocusManager;

						ctl.GetActivationFactory(
							wrl_wrappers.string(RuntimeClass_Microsoft_UI_Xaml_Input_FocusManager),
							&spFocusManager);

						//A control must be focused before we can set Engagement on it, attempt to set focus first
						BOOLEAN focused = FALSE;
						DependencyObject.SetFocusedElement(spScrollViewer, xaml.FocusState_Keyboard, FALSE /*animateIfBringintoView*/, &focused);
						if (focused)
						{
							spFocusManager.SetEngagedControl(ctl.as_iinspectable(spScrollViewer));
						}
					}
				}

				// If we requested to move focus to item, let's do it.
				if (m_focusItemAfterDisplayModeChanged)
				{
					bool focused = false;
					m_focusItemAfterDisplayModeChanged = false;

					FocusItemByDate(spCurrentHost, m_lastDisplayedDate, m_focusStateAfterDisplayModeChanged, &focused);
				}
				else // we only scroll to the focusedDate without moving focus to it
				{
					BringDisplayDateintoView(spCurrentHost);
				}
			}

			CalendarViewTemplateSettings* pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

			pTemplateSettingsConcrete.put_HasMoreViews(newDisplayMode != CalendarViewDisplayMode_Decade);
			UpdateHeaderText(true /*withAnimation*/);

			UpdateNavigationButtonStates();

			return S_OK;
		}


		private void UpdateLastDisplayedDate( CalendarViewDisplayMode lastDisplayMode)
		{
			ctl.ComPtr<CalendarViewGeneratorHost> spPreviousHost;
			GetGeneratorHost(lastDisplayMode, &spPreviousHost);

			var pPreviousPanel = spPreviousHost.Panel;
			if (pPreviousPanel)
			{
				int firstVisibleIndex = 0;
				int lastVisibleIndex = 0;
				wf.DateTime firstVisibleDate = { };
				wf.DateTime lastVisibleDate = { };
				ctl.ComPtr<IDependencyObject> spChildAsIDO;
				ctl.ComPtr<ICalendarViewBaseItem> spChildAsI;

				firstVisibleIndex = pPreviousPanel.FirstVisibleIndexBase;
				lastVisibleIndex = pPreviousPanel.LastVisibleIndexBase;

				ASSERT(firstVisibleIndex != -1 && lastVisibleIndex != -1);

				pPreviousPanel.ContainerFromIndex(firstVisibleIndex, &spChildAsIDO);
				spChildAsIDO.As(&spChildAsI);
				((CalendarViewBaseItem)spChildAsI).GetDate(&firstVisibleDate);

				pPreviousPanel.ContainerFromIndex(lastVisibleIndex, &spChildAsIDO);
				spChildAsIDO.As(&spChildAsI);
				((CalendarViewBaseItem)spChildAsI).GetDate(&lastVisibleDate);

				// check if last displayed Date is visible or not
				bool isLastDisplayedDateVisible = false;
				int result = 0;
				spPreviousHost.CompareDate(m_lastDisplayedDate, firstVisibleDate, &result);
				if (result >= 0)
				{
					spPreviousHost.CompareDate(m_lastDisplayedDate, lastVisibleDate, &result);
					if (result <= 0)
					{
						isLastDisplayedDateVisible = true;
					}
				}

				if (!isLastDisplayedDateVisible)
				{
					// if last displayed date is not visible, we use the first_visible_inscope_date as the last displayed date

					// first try to use the first_inscope_date
					wf.DateTime firstVisibleInscopeDate = spPreviousHost.GetMinDateOfCurrentScope();
					// check if first_inscope_date is visible or not
					spPreviousHost.CompareDate(firstVisibleInscopeDate, firstVisibleDate, &result);
					if (result < 0)
					{
						// the firstInscopeDate is not visible, then we use the firstVisibleDate.
#ifdef DBG
						{
							// in this case firstVisibleDate must be in scope (i.e. it must be less than or equals to the maxDateOfCurrentScope).
							int temp = 0;
							spPreviousHost.CompareDate(firstVisibleDate, spPreviousHost.GetMaxDateOfCurrentScope(), &temp);
							ASSERT(temp <= 0);
						}
#endif
						firstVisibleInscopeDate = firstVisibleDate;
					}

					// based on the display mode, partially copy the firstVisibleInscopeDate to m_lastDisplayedDate.
					CopyDate(
						lastDisplayMode,
						firstVisibleInscopeDate,
						m_lastDisplayedDate);
				}
			}

			return S_OK;
		}


		private void OnIsLabelVisibleChanged()
		{
			HRESULT hr = S_OK;

			// we don't have label text in decade view.
			std.array < CalendarViewGeneratorHost *, 2 > hosts{
				{
					m_tpMonthViewItemHost, m_tpYearViewItemHost
				}
			}
			;

			BOOLEAN isLabelVisible = FALSE;

			isLabelVisible = IsGroupLabelVisible;

			for (unsigned i = 0; i < hosts.size(); ++i)
			{
				var pHost = hosts[i];
				var pPanel = pHost.Panel;

				if (pPanel)
				{
					ForeachChildInPanel(pPanel, 
						[pHost, isLabelVisible](CalendarViewBaseItem * pItem)
					{
						return pHost.UpdateLabel(pItem, !!isLabelVisible);
					});
				}
			}

			Cleanup:
			return hr;
		}

		private void CreateDateTimeFormatter(
		string format,
		_Outptr_ wg.DateTimeFormatting.IDateTimeFormatter** ppDateTimeFormatter)
		{
			HRESULT hr = S_OK;
			ctl.ComPtr<wg.DateTimeFormatting.IDateTimeFormatterFactory> spFormatterFactory;
			ctl.ComPtr<wg.DateTimeFormatting.IDateTimeFormatter> spFormatter;
			wrl_wrappers.Hconst string strClock = "24HourClock"; // it doesn't matter if it is 24 or 12 hour clock
			wrl_wrappers.Hconst string strGeographicRegion = "ZZ"; // geographicRegion doesn't really matter as we have no decimal separator or grouping
			wrl_wrappers.string strCalendarIdentifier;

			get_CalendarIdentifier(strCalendarIdentifier);

			ctl.GetActivationFactory(
				wrl_wrappers.string(RuntimeClass_Windows_Globalization_DateTimeFormatting_DateTimeFormatter),
				&spFormatterFactory);

			IFCPTR(spFormatterFactory);

			spFormatterFactory.CreateDateTimeFormatterContext(
				format,
				m_tpCalendarLanguages,
				strGeographicRegion,
				strCalendarIdentifier,
				strClock,
				spFormatter);

			spFormatter.MoveTo(ppDateTimeFormatter);

			Cleanup:
			return hr;
		}

		private void FormatWeekDayNames()
		{
			HRESULT hr = S_OK;

			if (m_tpMonthViewItemHost.Panel)
			{
				ctl.ComPtr<IInspectable> spDayOfWeekFormat;
				BOOLEAN isUnsetValue = FALSE;
				wg.DayOfWeek dayOfWeek = wg.DayOfWeek_Sunday;
				ctl.ComPtr<wg.DateTimeFormatting.IDateTimeFormatter> spFormatter;

				ReadLocalValue(
					MetadataAPI.GetDependencyPropertyByIndex(KnownPropertyIndex.CalendarView_DayOfWeekFormat),
					&spDayOfWeekFormat);
				DependencyPropertyFactory.IsUnsetValue(spDayOfWeekFormat, isUnsetValue);

				m_tpCalendar.SetToNow();
				dayOfWeek = m_tpCalendar.DayOfWeek;

				// adjust to next sunday.
				m_tpCalendar.AddDays((s_numberOfDaysInWeek - dayOfWeek) % s_numberOfDaysInWeek);
				m_dayOfWeekNames.clear();
				m_dayOfWeekNames.reserve(s_numberOfDaysInWeek);

				// Fill m_dayOfWeekNamesFull. This will always be the full name of the day regardless of abbreviation used for m_dayOfWeekNames.
				m_dayOfWeekNamesFull.clear();
				m_dayOfWeekNamesFull.reserve(s_numberOfDaysInWeek);

				if (!isUnsetValue) // format is set, use this format.
				{
					wrl_wrappers.string dayOfWeekFormat;

					get_DayOfWeekFormat(dayOfWeekFormat);

					// Workaround: we can't bind an unset value to a property.
					// Here we'll check if the format is empty or not - because in CalendarDatePicker this property
					// is bound to CalendarDatePicker.DayOfWeekFormat which will cause this value is always set.
					if (!dayOfWeekFormat.IsEmpty())
					{
						CreateDateTimeFormatter(dayOfWeekFormat, &spFormatter);

					}
				}

				for (int i = 0; i < s_numberOfDaysInWeek; ++i)
				{
					wrl_wrappers.string string;

					if (spFormatter) // there is a valid datetimeformatter specified by user, use it
					{
						wf.DateTime date;
						m_tpCalendar.GetDateTime(&date);
						spFormatter.Format(date, string);
					}
					else // otherwise use the shortest string formatted by calendar.
					{
						m_tpCalendar.DayOfWeekAsString(
							1, /*shortest length*/
							string);
					}

					m_dayOfWeekNames.emplace_back(std.move(string);

					// for automation peer name, we always use the full string.
					m_tpCalendar.DayOfWeekAsFullString(string);
					m_dayOfWeekNamesFull.emplace_back(std.move(string);

					m_tpCalendar.AddDays(1);
				}
			}

			Cleanup:
			return hr;
		}

		private void UpdateWeekDayNameAPName(reads_(count) wchar_t* str, int count, const wrl_wrappers.string  & name)
		{
			ctl.ComPtr<ITextBlock> spWeekDay;
			GetTemplatePart<ITextBlock>(str, count, spWeekDay);
			AutomationProperties.SetNameStatic(((TextBlock)spWeekDay), name);
			return S_OK;
		}

		private void UpdateWeekDayNames()
		{
			HRESULT hr = S_OK;

			var pMonthPanel = m_tpMonthViewItemHost.Panel;
			if (pMonthPanel)
			{
				wg.DayOfWeek firstDayOfWeek = wg.DayOfWeek_Sunday;
				int index = 0;
				CalendarViewTemplateSettings* pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

				firstDayOfWeek = FirstDayOfWeek;

				if (m_dayOfWeekNames.empty())
				{
					FormatWeekDayNames();
				}

				index = (int)(firstDayOfWeek - wg.DayOfWeek_Sunday);

				pTemplateSettingsConcrete.put_WeekDay1(m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(STR_LEN_PAIR("WeekDay1"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.put_WeekDay2(m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(STR_LEN_PAIR("WeekDay2"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.put_WeekDay3(m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(STR_LEN_PAIR("WeekDay3"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.put_WeekDay4(m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(STR_LEN_PAIR("WeekDay4"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.put_WeekDay5(m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(STR_LEN_PAIR("WeekDay5"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.put_WeekDay6(m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(STR_LEN_PAIR("WeekDay6"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.put_WeekDay7(m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(STR_LEN_PAIR("WeekDay7"), m_dayOfWeekNamesFull[index]);

				m_monthViewStartIndex = (m_weekDayOfMinDate - firstDayOfWeek + s_numberOfDaysInWeek) % s_numberOfDaysInWeek;

				ASSERT(m_monthViewStartIndex >= 0 && m_monthViewStartIndex < s_numberOfDaysInWeek);

				pMonthPanel.StartIndex = m_monthViewStartIndex;
			}

			Cleanup:
			return hr;
		}

		private void GetActiveGeneratorHost(_Outptr_ CalendarViewGeneratorHost** ppHost)
		{
			CalendarViewDisplayMode mode = CalendarViewDisplayMode_Month;
			*ppHost = null;

			mode = DisplayMode;

			return GetGeneratorHost(mode, ppHost);
		}

		private void GetGeneratorHost(
		CalendarViewDisplayMode mode,
		_Outptr_ CalendarViewGeneratorHost** ppHost)
		{
			if (mode == CalendarViewDisplayMode_Month)
			{
				m_tpMonthViewItemHost.CopyTo(ppHost);
			}
			else if (mode == CalendarViewDisplayMode_Year)
			{
				m_tpYearViewItemHost.CopyTo(ppHost);
			}
			else if (mode == CalendarViewDisplayMode_Decade)
			{
				m_tpDecadeViewItemHost.CopyTo(ppHost);
			}
			else
			{
				ASSERT(false);
			}

			return S_OK;
		}

		private void FormatYearName( wf.DateTime date, out string* pName)
		{
			return m_tpYearFormatter.Format(date, pName);
		}

		private void FormatMonthYearName( wf.DateTime date, out string* pName)
		{
			return m_tpMonthYearFormatter.Format(date, pName);
		}

		// Partially copy date from source to target.
		// Only copy the parts we want and keep the remaining part.
		// Once the remaining part becomes invalid with the new copied parts,
		// we need to adjust the remaining part to the most reasonable value.
		// e.g. target: 3/31/2014, source 2/1/2013 and we want to copy month part,
		// the target will become 2/31/2013 and we'll adjust the day to 2/28/2013.

		private void CopyDate(
		CalendarViewDisplayMode displayMode,
		wf.DateTime source,
		wf.DateTime& target)
		{
			HRESULT hr = S_OK;

			bool copyEra = true;
			bool copyYear = true;
			bool copyMonth = displayMode == CalendarViewDisplayMode_Month ||
				displayMode == CalendarViewDisplayMode_Year;
			bool copyDay = displayMode == CalendarViewDisplayMode_Month;

			if (copyEra && copyYear && copyMonth && copyDay)
			{
				// copy everything.
				target = source;
			}
			else
			{
				int era = 0;
				int year = 0;
				int month = 0;
				int day = 0;

				m_tpCalendar.SetDateTime(source);
				if (copyEra)
				{
					era = m_tpCalendar.Era;
				}

				if (copyYear)
				{
					year = m_tpCalendar.Year;
				}

				if (copyMonth)
				{
					month = m_tpCalendar.Month;
				}

				if (copyDay)
				{
					day = m_tpCalendar.Day;
				}

				m_tpCalendar.SetDateTime(target);

				if (copyEra)
				{
					// era is always valid.
					m_tpCalendar.Era = era;
				}

				if (copyYear)
				{
					// year might not be valid.
					int first = 0;
					int last = 0;
					first = m_tpCalendar.FirstYearinthisEra;
					last = m_tpCalendar.LastYearinthisEra;
					year = Math.Min(last, Math.Max(first, year);
					m_tpCalendar.Year = year;
				}

				if (copyMonth)
				{
					// month might not be valid.
					int first = 0;
					int last = 0;
					first = m_tpCalendar.FirstMonthinthisYear;
					last = m_tpCalendar.LastMonthinthisYear;
					month = Math.Min(last, Math.Max(first, month);
					m_tpCalendar.Month = month;
				}

				if (copyDay)
				{
					// day might not be valid.
					int first = 0;
					int last = 0;
					first = m_tpCalendar.FirstDayinthisMonth;
					last = m_tpCalendar.LastDayinthisMonth;
					day = Math.Min(last, Math.Max(first, day);
					m_tpCalendar.Day = day;
				}

				m_tpCalendar.GetDateTime(&target);
				// make sure the target is still in range.
				CoerceDate(target);
			}

			Cleanup:
			return hr;
		}

		/*static*/
		private void CanPanelShowFullScope(
		CalendarViewGeneratorHost* pHost,
		out bool* pCanPanelShowFullScope)
		{
			var pCalendarPanel = pHost.Panel;
			int row = 0;
			int col = 0;
			*pCanPanelShowFullScope = false;

			ASSERT(pCalendarPanel);

			row = pCalendarPanel.Rows;
			col = pCalendarPanel.Cols;

			// Consider about the corner case: the first item in this scope
			// is laid on the last col in first row, so according dimension
			// row x col, we could arrange up to (row - 1) x col + 1 items

			*pCanPanelShowFullScope = (row - 1) * col + 1 >= pHost.GetMaximumScopeSize();

			return S_OK;
		}

		private void ForeachChildInPanel(
		CalendarPanel pCalendarPanel,
		std.function<HRESULT( CalendarViewBaseItem*)> func)
		{
			if (pCalendarPanel)
			{
				if (pCalendarPanel.IsInLiveTree())
				{
					int firstCacheIndex = 0;
					int lastCacheIndex = 0;

					firstCacheIndex = pCalendarPanel.FirstCacheIndex;
					lastCacheIndex = pCalendarPanel.LastCacheIndex;

					for (int i = firstCacheIndex; i <= lastCacheIndex; ++i)
					{
						ctl.ComPtr<IDependencyObject> spChildAsIDO;
						ctl.ComPtr<ICalendarViewBaseItem> spChildAsI;

						pCalendarPanel.ContainerFromIndex(i, &spChildAsIDO);
						spChildAsIDO.As(&spChildAsI);

						func(((CalendarViewBaseItem)spChildAsI));
					}
				}
			}

			return S_OK;
		}

		private void ForeachHost( std.function<HRESULT( CalendarViewGeneratorHost* pHost)> func)
		{
			func(m_tpMonthViewItemHost);
			func(m_tpYearViewItemHost);
			func(m_tpDecadeViewItemHost);
			return S_OK;
		}

		/*static*/
		private void SetDayItemStyle(
			CalendarViewBaseItem* pItem,
			xaml.IStyle* pStyle)
		{
			ASSERT(ctl. is <ICalendarViewDayItem > (pItem);
			if (pStyle)
			{
				pItem.Style = pStyle;
			}
			else
			{
				pItem.ClearValue(
					MetadataAPI.GetDependencyPropertyByIndex(KnownPropertyIndex.FrameworkElement_Style));
			}

			return S_OK;
		}

		IFACEMETHODIMP CalendarView.OnCreateAutomationPeer(_Outptr_result_maybenull_ xaml_automation_peers.IAutomationPeer** ppAutomationPeer)
		{
			IFCPTR_RETURN(ppAutomationPeer);
			*ppAutomationPeer = null;

			ctl.ComPtr<CalendarViewAutomationPeer> spAutomationPeer;
			ActivationAPI.ActivateAutomationInstance(KnownTypeIndex.CalendarViewAutomationPeer, GetHandle(), spAutomationPeer);
			spAutomationPeer.Owner = this;
			*ppAutomationPeer = spAutomationPeer.Detach();
			return S_OK;
		}

		// Called when the IsEnabled property changes.
		private void OnIsEnabledChanged( IsEnabledChangedEventArgs* pArgs)
		{
			return UpdateVisualState();
		}

		private void GetRowHeaderForItemAutomationPeer(
		wf.DateTime itemDate,
		CalendarViewDisplayMode displayMode,
		out Uint* pReturnValueCount,
			_Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
		{
			*pReturnValueCount = 0;
			*ppReturnValue = null;

			CalendarViewDisplayMode mode = CalendarViewDisplayMode_Month;
			mode = DisplayMode;

			//Ensure we only read out this header when in Month mode or Year mode. Decade mode reading the header isn't helpful.
			if (displayMode == mode)
			{
				int month, year;
				m_tpCalendar.SetDateTime(itemDate);
				month = m_tpCalendar.Month;
				year = m_tpCalendar.Year;

				const bool useCurrentHeaderPeer =
					m_currentHeaderPeer &&
					(m_currentHeaderPeer.GetMonth() == month || mode == CalendarViewDisplayMode_Year) &&
					m_currentHeaderPeer.GetYear() == year &&
					m_currentHeaderPeer.GetMode() == mode;

				const bool usePreviousHeaderPeer =
					m_previousHeaderPeer &&
					(m_previousHeaderPeer.GetMonth() == month || mode == CalendarViewDisplayMode_Year) &&
					m_previousHeaderPeer.GetYear() == year &&
					m_previousHeaderPeer.GetMode() == mode;

				const bool createNewHeaderPeer = !useCurrentHeaderPeer && !usePreviousHeaderPeer;

				if (createNewHeaderPeer)
				{
					ctl.ComPtr<CalendarViewHeaderAutomationPeer> peer;
					ActivationAPI.ActivateAutomationInstance(
						KnownTypeIndex.CalendarViewHeaderAutomationPeer,
						GetHandle(),
						peer);

					wrl_wrappers.string headerName;

					if (mode == CalendarViewDisplayMode_Month)
					{
						FormatMonthYearName(itemDate, headerName);
					}
					else
					{
						ASSERT(mode == CalendarViewDisplayMode_Year);
						FormatYearName(itemDate, headerName);
					}

					peer.Initialize(std.move(headerName), month, year, mode);

					m_previousHeaderPeer = m_currentHeaderPeer;
					m_currentHeaderPeer = peer;
				}

				ASSERT(m_currentHeaderPeer || m_previousHeaderPeer);

				const var peerToUse =
					usePreviousHeaderPeer ? m_previousHeaderPeer : m_currentHeaderPeer;

				ctl.ComPtr<xaml_automation.Provider.IIRawElementProviderSimple> provider;
				peerToUse.ProviderFromPeer(peerToUse, &provider);

				unsigned allocSize = sizeof(IIRawElementProviderSimple*);
				*ppReturnValue = (IIRawElementProviderSimple**)(CoTaskMemAlloc(allocSize);
				IFCOOMFAILFAST(*ppReturnValue);
				ZeroMemory(*ppReturnValue, allocSize);

				(*ppReturnValue)[0] = provider.Detach();
				*pReturnValueCount = 1;
			}

			return S_OK;
		}

		private void UpdateFlowDirectionForView()
		{
			if (m_tpViewsGrid && m_tpMonthYearFormatter)
			{
				bool isRTL = false;
				{
					ctl.ComPtr<__FIVectorView_1_string> spPatterns;
					spPatterns = m_tpMonthYearFormatter.Patterns;

					wrl_wrappers.string strFormatPattern;
					spPatterns.GetAt(0, strFormatPattern);
					if (strFormatPattern)
					{
						uint length = 0;
						var buffer = strFormatPattern.GetRawBuffer(&length);
						isRTL = buffer[0] == RTL_CHARACTER_CODE;
					}
				}

				var flowDirection = isRTL ? xaml.FlowDirection_RightToLeft : xaml.FlowDirection_LeftToRight;
				((Grid)m_tpViewsGrid).FlowDirection = flowDirection;
			}

			return S_OK;
		}
	}
}
