// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DirectUI;
using Uno.Extensions;
using DayOfWeek = Windows.Globalization.DayOfWeek;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	public partial class CalendarView : Control
	{
		private const string UIA_FLIPVIEW_PREVIOUS = "UIA_FLIPVIEW_PREVIOUS";
		private const string UIA_FLIPVIEW_NEXT = "UIA_FLIPVIEW_NEXT";

		private const char RTL_CHARACTER_CODE = '\x8207';

		TrackableDateCollection m_tpSelectedDates;

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

		CalendarViewTemplateSettings m_tpTemplateSettings;

		RoutedEventHandler m_epHeaderButtonClickHandler;
		RoutedEventHandler m_epPreviousButtonClickHandler;
		RoutedEventHandler m_epNextButtonClickHandler;

		KeyEventHandler m_epMonthViewScrollViewerKeyDownEventHandler;
		KeyEventHandler m_epYearViewScrollViewerKeyDownEventHandler;
		KeyEventHandler m_epDecadeViewScrollViewerKeyDownEventHandler;

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
		List<string> m_dayOfWeekNames = new List<string>();
		List<string> m_dayOfWeekNamesFull = new List<string>();

		IEnumerable<string> m_tpCalendarLanguages;

		VectorChangedEventHandler<DateTimeOffset> m_epSelectedDatesChangedHandler;


		// the keydown event args from CalendarItem.
		WeakReference<KeyRoutedEventArgs> m_wrKeyDownEventArgsFromCalendarItem = new WeakReference<KeyRoutedEventArgs>(default);

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
		bool m_isSelectedDatesChangingInternally;

		// when true we need to move focus to a calendaritem after we change the display mode.
		bool m_focusItemAfterDisplayModeChanged;

		bool m_isMultipleEraCalendar;

		bool m_isSetDisplayDateRequested;

		bool m_areYearDecadeViewDimensionsSet;

		// After navigationbutton clicked, the head text doesn't change immediately. so we use this flag to tell if the update text is from navigation button
		bool m_isNavigationButtonClicked;

		public CalendarView()
		{
			// Ctor from core\elements\CalendarView.cpp
			//m_pFocusBorderBrush = null;
			//m_pSelectedHoverBorderBrush = null;
			//m_pSelectedPressedBorderBrush = null;
			//m_pSelectedBorderBrush = null;
			//m_pHoverBorderBrush = null;
			//m_pPressedBorderBrush = null;
			//m_pCalendarItemBorderBrush = null;
			//m_pOutOfScopeBackground = null;
			//m_pCalendarItemBackground = null;
			//m_pPressedForeground = null;
			//m_pTodayForeground = null;
			//m_pBlackoutForeground = null;
			//m_pSelectedForeground = null;
			//m_pOutOfScopeForeground = null;
			//m_pCalendarItemForeground = null;
			//m_pDayItemFontFamily = /*null;*/ "XamlAutoFontFamily";
			m_pDisabledForeground = Resources[c_strDisabledForegroundStorage] as Brush;
			m_pTodaySelectedInnerBorderBrush = Resources[c_strTodaySelectedInnerBorderBrushStorage] as Brush;
			m_pTodayHoverBorderBrush = Resources[c_strTodayHoverBorderBrushStorage] as Brush;
			m_pTodayPressedBorderBrush = Resources[c_strTodayPressedBorderBrushStorage] as Brush;
			//m_pTodayBackground = Resources[c_strTodayBackgroundStorage] as Brush;
			m_pTodayBlackoutBackground = Resources[c_strTodayBlackoutBackgroundStorage] as Brush;
			//m_dayItemFontSize = 20.0f;
			//m_dayItemFontStyle = FontStyle.Normal;
			//m_dayItemFontWeight = FontWeights.Normal;
			//m_todayFontWeight = FontWeights.SemiBold;
			//m_pFirstOfMonthLabelFontFamily = /*null;*/ "XamlAutoFontFamily";
			//m_firstOfMonthLabelFontSize = 12.0f;
			//m_firstOfMonthLabelFontStyle = FontStyle.Normal;
			//m_firstOfMonthLabelFontWeight = FontWeights.Normal;
			//m_pMonthYearItemFontFamily = /*null;*/ "XamlAutoFontFamily";
			//m_monthYearItemFontSize = 20.0f;
			//m_monthYearItemFontStyle = FontStyle.Normal;
			//m_monthYearItemFontWeight = FontWeights.Normal;
			//m_pFirstOfYearDecadeLabelFontFamily = /*null;*/ "XamlAutoFontFamily";
			//m_firstOfYearDecadeLabelFontSize = 12.0f;
			//m_firstOfYearDecadeLabelFontStyle = FontStyle.Normal;
			//m_firstOfYearDecadeLabelFontWeight = FontWeights.Normal;
			//m_horizontalDayItemAlignment = HorizontalAlignment.Center;
			//m_verticalDayItemAlignment = VerticalAlignment.Center;
			//m_horizontalFirstOfMonthLabelAlignment = HorizontalAlignment.Center;
			//m_verticalFirstOfMonthLabelAlignment = VerticalAlignment.Top;
			//m_calendarItemBorderThickness = default;

			// Ctor from lib\CalendarView_Partial.cpp
			m_dateSourceChanged = true;
			m_calendarChanged = false;
			m_itemHostsConnected = false;
			m_areYearDecadeViewDimensionsSet = false;
			m_colsInYearDecadeView = 4;
			m_rowsInYearDecadeView = 4;
			m_monthViewStartIndex = 0;
			m_weekDayOfMinDate = DayOfWeek.Sunday;
			m_isSelectedDatesChangingInternally = false;
			m_focusItemAfterDisplayModeChanged = false;
			m_focusStateAfterDisplayModeChanged = FocusState.Programmatic;
			m_isMultipleEraCalendar = false;
			m_areDirectManipulationStateChangeHandlersHooked = false;
			m_isSetDisplayDateRequested = true; // by default there is a displayDate request, which is m_lastDisplayedDate
			m_isNavigationButtonClicked = false;

			m_today = default;
			m_maxDate = default;
			m_minDate = default;
			m_lastDisplayedDate = default;

			PrepareState();
			DefaultStyleKey = typeof(CalendarView);

#if __WASM__
			IsMeasureDirtyPathDisabled = true;
#endif
		}

		~CalendarView()
		{
			//DetachButtonClickedEvents();
			//m_tpSelectedDates.VectorChanged -= m_epSelectedDatesChangedHandler;
			//DetachScrollViewerKeyDownEvents();

			if (m_tpSelectedDates is { } selectedDates)
			{
				((TrackableDateCollection)selectedDates).SetCollectionChangingCallback(null);
			}
		}

		// UNO SPECIFIC
		private protected override void OnLoaded()
		{
			base.OnLoaded();

			AttachButtonClickedEvents();
			AttachScrollViewerKeyDownEvents();
		}

		// UNO SPECIFIC
		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			DetachButtonClickedEvents();
			DetachScrollViewerKeyDownEvents();
		}

		private void PrepareState()
		{
			//base.PrepareState();

			{
				m_dateComparer = new DateComparer();

				TrackableDateCollection spSelectedDates;

				spSelectedDates = new TrackableDateCollection();

				m_epSelectedDatesChangedHandler ??= new VectorChangedEventHandler<DateTimeOffset>((pSender, pArgs) => OnSelectedDatesChanged(pSender, pArgs));
				spSelectedDates.VectorChanged += m_epSelectedDatesChangedHandler;

				spSelectedDates.SetCollectionChangingCallback(
					(TrackableDateCollection.CollectionChanging action, DateTimeOffset addingDate) =>
				{
					OnSelectedDatesChanging(action, addingDate);
				});

				m_tpSelectedDates = spSelectedDates;
				SelectedDates = spSelectedDates;
			}

			{
				CalendarViewGeneratorMonthViewHost spMonthViewItemHost;
				CalendarViewGeneratorYearViewHost spYearViewItemHost;
				CalendarViewGeneratorDecadeViewHost spDecadeViewItemHost;

				spMonthViewItemHost = new CalendarViewGeneratorMonthViewHost();
				m_tpMonthViewItemHost = spMonthViewItemHost;
				m_tpMonthViewItemHost.Owner = this;

				spYearViewItemHost = new CalendarViewGeneratorYearViewHost();
				m_tpYearViewItemHost = spYearViewItemHost;
				m_tpYearViewItemHost.Owner = this;

				spDecadeViewItemHost = new CalendarViewGeneratorDecadeViewHost();
				m_tpDecadeViewItemHost = spDecadeViewItemHost;
				m_tpDecadeViewItemHost.Owner = this;
			}

			{
				CreateCalendarLanguages();
				CreateCalendarAndMonthYearFormatter();
			}

			{
				CalendarViewTemplateSettings spTemplateSettings;

				spTemplateSettings = new CalendarViewTemplateSettings();

				spTemplateSettings.HasMoreViews = true;
				TemplateSettings = spTemplateSettings;
				m_tpTemplateSettings = spTemplateSettings;
			}

		}

		// UNO Specific: Default values are set in DP declaration
		//// Override the GetDefaultValue method to return the default values
		//// for Hub dependency properties.
		//private static void GetDefaultValue2(
		//	DependencyProperty pDP,
		//	out object pValue)
		//{
		//	if (pDP == CalendarView.CalendarIdentifierProperty)
		//	{
		//		pValue = "GregorianCalendar";
		//	}
		//	else if (pDP == CalendarView.NumberOfWeeksInViewProperty)
		//	{
		//		pValue = s_defaultNumberOfWeeks;
		//	}
		//	else
		//	{
		//		base.GetDefaultValue2(pDP, out pValue);
		//	}
		//}

		// Basically these Alignment properties only affect Arrange, but in CalendarView
		// the item size and Panel size are also affected when we change the property from
		// stretch to unstretch, or vice versa. In these cases we need to invalidate panels' measure.
		private void OnAlignmentChanged(DependencyPropertyChangedEventArgs args)
		{
			//uint oldAlignment = 0;
			//uint newAlignment = 0;
			bool isOldStretched = false;
			bool isNewStretched = false;

			//oldAlignment = (uint)args.OldValue;
			//newAlignment = (uint)args.NewValue;

			switch (args.Property)
			{
				case DependencyProperty Control_HorizontalContentAlignment when Control_HorizontalContentAlignment == Control.HorizontalContentAlignmentProperty:
				case DependencyProperty FrameworkElement_HorizontalAlignment when FrameworkElement_HorizontalAlignment == FrameworkElement.HorizontalAlignmentProperty:
					isOldStretched = (HorizontalAlignment)(args.OldValue) == HorizontalAlignment.Stretch;
					isNewStretched = (HorizontalAlignment)(args.NewValue) == HorizontalAlignment.Stretch;
					break;
				case DependencyProperty Control_VerticalContentAlignment when Control_VerticalContentAlignment == Control.VerticalContentAlignmentProperty:
				case DependencyProperty FrameworkElement_VerticalAlignment when FrameworkElement_VerticalAlignment == FrameworkElement.VerticalAlignmentProperty:
					isOldStretched = (VerticalAlignment)(args.OldValue) == VerticalAlignment.Stretch;
					isNewStretched = (VerticalAlignment)(args.NewValue) == VerticalAlignment.Stretch;
					break;
				default:
					global::System.Diagnostics.Debug.Assert(false);
					break;
			}

			if (isOldStretched != isNewStretched)
			{
				ForeachHost((CalendarViewGeneratorHost pHost) =>
				{
					var pPanel = pHost.Panel;
					if (pPanel is { })
					{
						pPanel.InvalidateMeasure();
					}

					return;
				});
			}

			return;
		}

		// Handle the custom property changed event and call the OnPropertyChanged methods.
		internal override void OnPropertyChanged2(
			DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			switch (args.Property)
			{
				case DependencyProperty Control_HorizontalContentAlignmentProperty when Control_HorizontalContentAlignmentProperty == Control.HorizontalContentAlignmentProperty:
				case DependencyProperty Control_VerticalContentAlignmentProperty when Control_VerticalContentAlignmentProperty == Control.VerticalContentAlignmentProperty:
				case DependencyProperty FrameworkElement_HorizontalAlignmentProperty when FrameworkElement_HorizontalAlignmentProperty == FrameworkElement.HorizontalAlignmentProperty:
				case DependencyProperty FrameworkElement_VerticalAlignmentProperty when FrameworkElement_VerticalAlignmentProperty == FrameworkElement.VerticalAlignmentProperty:
					OnAlignmentChanged(args);
					break;
				case DependencyProperty CalendarView_MinDateProperty when CalendarView_MinDateProperty == CalendarView.MinDateProperty:
				case DependencyProperty CalendarView_MaxDateProperty when CalendarView_MaxDateProperty == CalendarView.MaxDateProperty:
					m_dateSourceChanged = true;
					InvalidateMeasure();
					break;
				case DependencyProperty FrameworkElement_LanguageProperty when FrameworkElement_LanguageProperty == FrameworkElement.LanguageProperty:
					// Globlization.Calendar doesn't support changing languages, so when languages changed,
					// we have to create a new Globalization.Calendar, and also we'll update the date source so
					// the change of languages can take effect on the existing items.
					CreateCalendarLanguages();
					// fall through
					goto fall_through_1;
				case DependencyProperty CalendarView_CalendarIdentifierProperty when CalendarView_CalendarIdentifierProperty == CalendarView.CalendarIdentifierProperty:
				fall_through_1:
					m_calendarChanged = true;
					m_dateSourceChanged = true; //calendarid changed, even if the mindate or maxdate is not changed we still need to regenerate all calendar items.
					InvalidateMeasure();
					break;
				case DependencyProperty CalendarView_NumberOfWeeksInViewProperty when CalendarView_NumberOfWeeksInViewProperty == CalendarView.NumberOfWeeksInViewProperty:
					{
						int rows = 0;
						rows = (int)args.NewValue;

						if (rows < s_minNumberOfWeeks || rows > s_maxNumberOfWeeks)
						{
							throw new ArgumentOutOfRangeException("ERROR_CALENDAR_NUMBER_OF_WEEKS_OUTOFRANGE");
						}

						if (m_tpMonthViewItemHost.Panel is { })
						{
							m_tpMonthViewItemHost.Panel.SetSuggestedDimension(s_numberOfDaysInWeek, rows);
						}
					}
					break;
				case DependencyProperty CalendarView_DayOfWeekFormatProperty when CalendarView_DayOfWeekFormatProperty == CalendarView.DayOfWeekFormatProperty:
					FormatWeekDayNames();
					// fall through
					goto fall_through_2;
				case DependencyProperty CalendarView_FirstDayOfWeekProperty when CalendarView_FirstDayOfWeekProperty == CalendarView.FirstDayOfWeekProperty:
				fall_through_2:
					UpdateWeekDayNames();
					break;
				case DependencyProperty CalendarView_SelectionModeProperty when CalendarView_SelectionModeProperty == CalendarView.SelectionModeProperty:
					OnSelectionModeChanged();
					break;
				case DependencyProperty CalendarView_IsOutOfScopeEnabledProperty when CalendarView_IsOutOfScopeEnabledProperty == CalendarView.IsOutOfScopeEnabledProperty:
					OnIsOutOfScopePropertyChanged();
					break;
				case DependencyProperty CalendarView_DisplayModeProperty when CalendarView_DisplayModeProperty == CalendarView.DisplayModeProperty:
					{
						CalendarViewDisplayMode oldDisplayMode = 0;
						CalendarViewDisplayMode newDisplayMode = 0;

						oldDisplayMode = (CalendarViewDisplayMode)args.OldValue;
						newDisplayMode = (CalendarViewDisplayMode)args.NewValue;

						OnDisplayModeChanged(
							(CalendarViewDisplayMode)(oldDisplayMode),
							(CalendarViewDisplayMode)(newDisplayMode)
						);
					}
					break;
				case DependencyProperty CalendarView_IsTodayHighlightedProperty when CalendarView_IsTodayHighlightedProperty == CalendarView.IsTodayHighlightedProperty:
					OnIsTodayHighlightedPropertyChanged();
					break;
				case DependencyProperty CalendarView_IsGroupLabelVisibleProperty when CalendarView_IsGroupLabelVisibleProperty == CalendarView.IsGroupLabelVisibleProperty:
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
				case DependencyProperty CalendarView_FocusBorderBrushProperty when CalendarView_FocusBorderBrushProperty == CalendarView.FocusBorderBrushProperty:
				case DependencyProperty CalendarView_SelectedHoverBorderBrushProperty when CalendarView_SelectedHoverBorderBrushProperty == CalendarView.SelectedHoverBorderBrushProperty:
				case DependencyProperty CalendarView_SelectedPressedBorderBrushProperty when CalendarView_SelectedPressedBorderBrushProperty == CalendarView.SelectedPressedBorderBrushProperty:
				case DependencyProperty CalendarView_SelectedBorderBrushProperty when CalendarView_SelectedBorderBrushProperty == CalendarView.SelectedBorderBrushProperty:
				case DependencyProperty CalendarView_HoverBorderBrushProperty when CalendarView_HoverBorderBrushProperty == CalendarView.HoverBorderBrushProperty:
				case DependencyProperty CalendarView_PressedBorderBrushProperty when CalendarView_PressedBorderBrushProperty == CalendarView.PressedBorderBrushProperty:
				case DependencyProperty CalendarView_CalendarItemBorderBrushProperty when CalendarView_CalendarItemBorderBrushProperty == CalendarView.CalendarItemBorderBrushProperty:
				case DependencyProperty CalendarView_OutOfScopeBackgroundProperty when CalendarView_OutOfScopeBackgroundProperty == CalendarView.OutOfScopeBackgroundProperty:
				case DependencyProperty CalendarView_CalendarItemBackgroundProperty when CalendarView_CalendarItemBackgroundProperty == CalendarView.CalendarItemBackgroundProperty:
					ForeachHost(pHost =>
					{
						ForeachChildInPanel(
							pHost.Panel,
							pItem =>
							{
								pItem.InvalidateRender();
							});
					});
					break;

				// Foreground will take effect immediately
				case DependencyProperty CalendarView_PressedForegroundProperty when CalendarView_PressedForegroundProperty == CalendarView.PressedForegroundProperty:
				case DependencyProperty CalendarView_TodayForegroundProperty when CalendarView_TodayForegroundProperty == CalendarView.TodayForegroundProperty:
				case DependencyProperty CalendarView_BlackoutForegroundProperty when CalendarView_BlackoutForegroundProperty == CalendarView.BlackoutForegroundProperty:
				case DependencyProperty CalendarView_SelectedForegroundProperty when CalendarView_SelectedForegroundProperty == CalendarView.SelectedForegroundProperty:
				case DependencyProperty CalendarView_OutOfScopeForegroundProperty when CalendarView_OutOfScopeForegroundProperty == CalendarView.OutOfScopeForegroundProperty:
				case DependencyProperty CalendarView_CalendarItemForegroundProperty when CalendarView_CalendarItemForegroundProperty == CalendarView.CalendarItemForegroundProperty:
					ForeachHost(pHost =>
					{
						ForeachChildInPanel(
							pHost.Panel,
							pItem =>
							{
								pItem.UpdateTextBlockForeground();
							});
					});
					break;

				case DependencyProperty CalendarView_TodayFontWeightProperty when CalendarView_TodayFontWeightProperty == CalendarView.TodayFontWeightProperty:
					{
						ForeachHost(pHost =>
						{
							var pPanel = pHost.Panel;

							if (pPanel is { })
							{
								int indexOfToday = -1;

								indexOfToday = pHost.CalculateOffsetFromMinDate(m_today);

								if (indexOfToday != -1)
								{
									DependencyObject spChildAsIDO;
									CalendarViewBaseItem spChildAsI;

									spChildAsIDO = pPanel.ContainerFromIndex(indexOfToday);
									spChildAsI = spChildAsIDO as CalendarViewBaseItem;
									// today item is realized already, we need to update the state here.
									// if today item is not realized yet, we'll update the state when today item is being prepared.
									if (spChildAsI is { })
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
				case DependencyProperty CalendarView_DayItemFontFamilyProperty when CalendarView_DayItemFontFamilyProperty == CalendarView.DayItemFontFamilyProperty:
				case DependencyProperty CalendarView_DayItemFontSizeProperty when CalendarView_DayItemFontSizeProperty == CalendarView.DayItemFontSizeProperty:
				case DependencyProperty CalendarView_DayItemFontStyleProperty when CalendarView_DayItemFontStyleProperty == CalendarView.DayItemFontStyleProperty:
				case DependencyProperty CalendarView_DayItemFontWeightProperty when CalendarView_DayItemFontWeightProperty == CalendarView.DayItemFontWeightProperty:
					{
						// if these DayItem properties changed, we need to re-determine the
						// biggest dayitem in monthPanel, which will invalidate monthpanel's measure
						var pMonthPanel = m_tpMonthViewItemHost.Panel;
						if (pMonthPanel is { })
						{
							pMonthPanel.SetNeedsToDetermineBiggestItemSize();
						}

					}
					goto fall_through_3;

				// Font properties for MonthLabel (they won't affect measure or arrange)
				case DependencyProperty CalendarView_FirstOfMonthLabelFontFamilyProperty when CalendarView_FirstOfMonthLabelFontFamilyProperty == CalendarView.FirstOfMonthLabelFontFamilyProperty:
				case DependencyProperty CalendarView_FirstOfMonthLabelFontSizeProperty when CalendarView_FirstOfMonthLabelFontSizeProperty == CalendarView.FirstOfMonthLabelFontSizeProperty:
				case DependencyProperty CalendarView_FirstOfMonthLabelFontStyleProperty when CalendarView_FirstOfMonthLabelFontStyleProperty == CalendarView.FirstOfMonthLabelFontStyleProperty:
				case DependencyProperty CalendarView_FirstOfMonthLabelFontWeightProperty when CalendarView_FirstOfMonthLabelFontWeightProperty == CalendarView.FirstOfMonthLabelFontWeightProperty:
				fall_through_3:
					ForeachChildInPanel(
						m_tpMonthViewItemHost.Panel,
						pItem =>
						{
							pItem.UpdateTextBlockFontProperties();
						});
					break;

				// Font properties for MonthYearItem
				case DependencyProperty CalendarView_MonthYearItemFontFamilyProperty when CalendarView_MonthYearItemFontFamilyProperty == CalendarView.MonthYearItemFontFamilyProperty:
				case DependencyProperty CalendarView_MonthYearItemFontSizeProperty when CalendarView_MonthYearItemFontSizeProperty == CalendarView.MonthYearItemFontSizeProperty:
				case DependencyProperty CalendarView_MonthYearItemFontStyleProperty when CalendarView_MonthYearItemFontStyleProperty == CalendarView.MonthYearItemFontStyleProperty:
				case DependencyProperty CalendarView_MonthYearItemFontWeightProperty when CalendarView_MonthYearItemFontWeightProperty == CalendarView.MonthYearItemFontWeightProperty:
					{
						// these properties will affect MonthItem and YearItem's size, so we should
						// tell their panels to re-determine the biggest item size.
						CalendarPanel[] pPanels = new[]{
							m_tpYearViewItemHost.Panel, m_tpDecadeViewItemHost.Panel
						};
						;

						for (var i = 0; i < pPanels.Length; ++i)
						{
							if (pPanels[i] is { })
							{
								pPanels[i].SetNeedsToDetermineBiggestItemSize();
							}
						}
					}
					// fall through
					goto fall_through_4;

				case DependencyProperty CalendarView_FirstOfYearDecadeLabelFontFamilyProperty when CalendarView_FirstOfYearDecadeLabelFontFamilyProperty == CalendarView.FirstOfYearDecadeLabelFontFamilyProperty:
				case DependencyProperty CalendarView_FirstOfYearDecadeLabelFontSizeProperty when CalendarView_FirstOfYearDecadeLabelFontSizeProperty == CalendarView.FirstOfYearDecadeLabelFontSizeProperty:
				case DependencyProperty CalendarView_FirstOfYearDecadeLabelFontStyleProperty when CalendarView_FirstOfYearDecadeLabelFontStyleProperty == CalendarView.FirstOfYearDecadeLabelFontStyleProperty:
				case DependencyProperty CalendarView_FirstOfYearDecadeLabelFontWeightProperty when CalendarView_FirstOfYearDecadeLabelFontWeightProperty == CalendarView.FirstOfYearDecadeLabelFontWeightProperty:
				fall_through_4:
					{
						CalendarPanel[] pPanels = new[]
							{
							m_tpYearViewItemHost.Panel, m_tpDecadeViewItemHost.Panel
						}
						;

						for (var i = 0; i < pPanels.Length; ++i)
						{
							ForeachChildInPanel(pPanels[i],
								(CalendarViewBaseItem pItem) =>
							{
								pItem.UpdateTextBlockFontProperties();
							});

						}

						break;
					}
				// Alignments affect DayItem only
				case DependencyProperty CalendarView_HorizontalDayItemAlignmentProperty when CalendarView_HorizontalDayItemAlignmentProperty == CalendarView.HorizontalDayItemAlignmentProperty:
				case DependencyProperty CalendarView_VerticalDayItemAlignmentProperty when CalendarView_VerticalDayItemAlignmentProperty == CalendarView.VerticalDayItemAlignmentProperty:
				case DependencyProperty CalendarView_HorizontalFirstOfMonthLabelAlignmentProperty when CalendarView_HorizontalFirstOfMonthLabelAlignmentProperty == CalendarView.HorizontalFirstOfMonthLabelAlignmentProperty:
				case DependencyProperty CalendarView_VerticalFirstOfMonthLabelAlignmentProperty when CalendarView_VerticalFirstOfMonthLabelAlignmentProperty == CalendarView.VerticalFirstOfMonthLabelAlignmentProperty:

					ForeachChildInPanel(
						m_tpMonthViewItemHost.Panel,
						pItem =>
						{
							pItem.UpdateTextBlockAlignments();
						});

					break;

				// border thickness affects measure (and arrange)
				case DependencyProperty CalendarView_CalendarItemBorderThicknessProperty when CalendarView_CalendarItemBorderThicknessProperty == CalendarView.CalendarItemBorderThicknessProperty:
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
				case DependencyProperty CalendarView_CalendarViewDayItemStyleProperty when CalendarView_CalendarViewDayItemStyleProperty == CalendarView.CalendarViewDayItemStyleProperty:
					{
						Style spStyle;

						spStyle = args.NewValue as Style;
						var pMonthPanel = m_tpMonthViewItemHost.Panel;

						ForeachChildInPanel(
							pMonthPanel,
							pItem =>
							{
								SetDayItemStyle(pItem, spStyle);
							});

						// Some properties could affect dayitem size (e.g. Dayitem font properties, dayitem size),
						// when anyone of them is changed, we need to re-determine the biggest day item.
						// This is not a frequent scenario so we can simply set below flag and invalidate measure.

						if (pMonthPanel is { })
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
			//DetachButtonClickedEvents();
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
					if (pScrollViewer is { })
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


			m_tpHeaderButton = null;
			m_tpPreviousButton = null;
			m_tpNextButton = null;
			m_tpViewsGrid = null;

			base.OnApplyTemplate();

			spMonthViewPanel = this.GetTemplateChild<CalendarPanel>("MonthViewPanel");
			spYearViewPanel = this.GetTemplateChild<CalendarPanel>("YearViewPanel");
			spDecadeViewPanel = this.GetTemplateChild<CalendarPanel>("DecadeViewPanel");

			m_tpMonthViewItemHost.Panel = spMonthViewPanel;
			m_tpYearViewItemHost.Panel = spYearViewPanel;
			m_tpDecadeViewItemHost.Panel = spDecadeViewPanel;

			if (spMonthViewPanel is { })
			{
				CalendarPanel pPanel = (CalendarPanel)spMonthViewPanel;
				int numberOfWeeksInView = 0;

				// MonthView panel is the only primary panel (and never changes)
				pPanel.PanelType = CalendarPanelType.Primary;

				numberOfWeeksInView = NumberOfWeeksInView;
				pPanel.SetSuggestedDimension(s_numberOfDaysInWeek, numberOfWeeksInView);
				pPanel.Orientation = Orientation.Horizontal;
			}

			if (spYearViewPanel is { })
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

			if (spDecadeViewPanel is { })
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

			spHeaderButton = this.GetTemplateChild<Button>("HeaderButton");
			spPreviousButton = this.GetTemplateChild<Button>("PreviousButton");
			spNextButton = this.GetTemplateChild<Button>("NextButton");

			if (spPreviousButton is { })
			{
				strAutomationName = AutomationProperties.GetName((Button)spPreviousButton);
				if (strAutomationName == null)
				{
					// USe the same resource string as for FlipView's Previous Button.
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_PREVIOUS);
					AutomationProperties.SetName((Button)spPreviousButton, strAutomationName);
				}
			}

			if (spNextButton is { })
			{
				strAutomationName = AutomationProperties.GetName((Button)spNextButton);
				if (strAutomationName == null)
				{
					// USe the same resource string as for FlipView's Next Button.
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_NEXT);
					AutomationProperties.SetName((Button)spNextButton, strAutomationName);
				}
			}

			m_tpHeaderButton = spHeaderButton;
			m_tpPreviousButton = spPreviousButton;
			m_tpNextButton = spNextButton;

			spViewsGrid = this.GetTemplateChild<Grid>("Views");
			m_tpViewsGrid = spViewsGrid;

			spMonthViewScrollViewer = this.GetTemplateChild<ScrollViewer>("MonthViewScrollViewer");
			spYearViewScrollViewer = this.GetTemplateChild<ScrollViewer>("YearViewScrollViewer");
			spDecadeViewScrollViewer = this.GetTemplateChild<ScrollViewer>("DecadeViewScrollViewer");

			m_tpMonthViewItemHost.ScrollViewer = spMonthViewScrollViewer;
			m_tpYearViewItemHost.ScrollViewer = spYearViewScrollViewer;
			m_tpDecadeViewItemHost.ScrollViewer = spDecadeViewScrollViewer;

			// Setting custom CalendarScrollViewerAutomationPeer for these scrollviewers to be the default one.
			if (spMonthViewScrollViewer is { })
			{
				((ScrollViewer)spMonthViewScrollViewer).AutomationPeerFactoryIndex = () => new CalendarScrollViewerAutomationPeer(spMonthViewScrollViewer);

				m_tpMonthViewScrollViewer = spMonthViewScrollViewer;
			}

			if (spYearViewScrollViewer is { })
			{
				((ScrollViewer)spYearViewScrollViewer).AutomationPeerFactoryIndex = () => new CalendarScrollViewerAutomationPeer(spYearViewScrollViewer);

				m_tpYearViewScrollViewer = spYearViewScrollViewer;
			}

			if (spDecadeViewScrollViewer is { })
			{
				((ScrollViewer)spDecadeViewScrollViewer).AutomationPeerFactoryIndex = () => new CalendarScrollViewerAutomationPeer(spDecadeViewScrollViewer);

				m_tpDecadeViewScrollViewer = spDecadeViewScrollViewer;
			}

			global::System.Diagnostics.Debug.Assert(!m_areDirectManipulationStateChangeHandlersHooked);

			ForeachHost(pHost =>
			{
				var pScrollViewer = pHost.ScrollViewer;
				if (pScrollViewer is { })
				{
					pScrollViewer.TemplatedParentHandlesScrolling = true;
					pScrollViewer.SetDirectManipulationStateChangeHandler(pHost);
					pScrollViewer.m_templatedParentHandlesMouseButton = true;
				}

			});

			m_areDirectManipulationStateChangeHandlersHooked = true;

			AttachVisibleIndicesUpdatedEvents();

			AttachButtonClickedEvents();

			AttachScrollViewerKeyDownEvents();

			// This will connect the new panels with ItemHosts
			RegisterItemHosts();

			AttachScrollViewerFocusEngagedEvents();

			UpdateVisualState(false /*bUseTransitions*/);

			UpdateFlowDirectionForView();

		}

		// Change to the correct visual state for the control.
		private protected override void ChangeVisualState(
			bool bUseTransitions)
		{
			CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;
			//bool bIgnored = false;

			mode = DisplayMode;

			if (mode == CalendarViewDisplayMode.Month)
			{
				GoToState(bUseTransitions, "Month");
			}
			else if (mode == CalendarViewDisplayMode.Year)
			{
				GoToState(bUseTransitions, "Year");
			}
			else //if (mode == CalendarViewDisplayMode.Decade)
			{
				GoToState(bUseTransitions, "Decade");
			}

			bool isEnabled = false;
			isEnabled = IsEnabled;

			// Common States Group
			if (!isEnabled)
			{
				GoToState(bUseTransitions, "Disabled");
			}
			else
			{
				GoToState(bUseTransitions, "Normal");
			}

			return;
		}

		// Primary panel will determine CalendarView's size, when Primary Panel's desired size changed, we need
		// to update the template settings so other template parts can update their size correspondingly.
		internal void OnPrimaryPanelDesiredSizeChanged(CalendarViewGeneratorHost pHost)
		{
			// monthpanel is the only primary panel
			global::System.Diagnostics.Debug.Assert(pHost == m_tpMonthViewItemHost);

			var pMonthViewPanel = pHost.Panel;

			global::System.Diagnostics.Debug.Assert(pMonthViewPanel is { });

			Size desiredViewportSize = default;
			desiredViewportSize = pMonthViewPanel.GetDesiredViewportSize();

			CalendarViewTemplateSettings pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);
			pTemplateSettingsConcrete.MinViewWidth = desiredViewportSize.Width;

			return;
		}

		protected override Size MeasureOverride(
			Size availableSize)
		{
			Size pDesired = default;
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

			pDesired = base.MeasureOverride(availableSize);

			return pDesired;
		}

		protected override Size ArrangeOverride(
			Size finalSize)
		{
			Size returnValue = default;
			returnValue = base.ArrangeOverride(finalSize);

			if (m_tpViewsGrid is { })
			{
				// When switching views, the up-scaled view needs to be clipped by the original size.
				double viewsHeight = 0.0;
				double viewsWidth = 0.0;
				CalendarViewTemplateSettings pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

				viewsHeight = ((Grid)m_tpViewsGrid).ActualHeight;
				viewsWidth = ((Grid)m_tpViewsGrid).ActualWidth;

				Rect clipRect = new Rect(0.0, 0.0, (float)(viewsWidth), (float)(viewsHeight));

				pTemplateSettingsConcrete.ClipRect = clipRect;

				// ScaleTransform.CenterX and CenterY
				pTemplateSettingsConcrete.CenterX = (viewsWidth / 2);
				pTemplateSettingsConcrete.CenterY = (viewsHeight / 2);
			}

			if (m_isSetDisplayDateRequested)
			{
				// m_lastDisplayedDate is already coerced and adjusted, time to process this request and clear the flag.
				m_isSetDisplayDateRequested = false;
				SetDisplayDateInternal(m_lastDisplayedDate);
			}

			return returnValue;
		}

		// create a list of languages to construct Globalization.Calendar and Globalization.DateTimeFormatter.
		// here we prepend CalendarView.Language to ApplicationLanguage.Languages as the new list.
		private void CreateCalendarLanguages()
		{
			string strLanguage;
			IEnumerable<string> spCalendarLanguages;

			strLanguage = Language;
			spCalendarLanguages = CreateCalendarLanguagesStatic(strLanguage);
			m_tpCalendarLanguages = spCalendarLanguages;

			return;
		}

		// helper method to prepend a string into a collection of string.
		/*static */
		internal static IEnumerable<string> CreateCalendarLanguagesStatic(
			string language)
		{
			IEnumerable<string> ppLanguages = default;
			IReadOnlyList<string> spApplicationLanguages;
			IList<string> spCalendarLanguages;
			int size = 0;

			spApplicationLanguages = ApplicationLanguages.Languages;

			spCalendarLanguages = new List<string>();

			if (language is { }) // UNO
			{
				spCalendarLanguages.Add(language);
			}

			size = spApplicationLanguages.Count;

			for (uint i = 0; i < size; ++i)
			{
				string strApplicationLanguage;
				strApplicationLanguage = spApplicationLanguages[(int)i];
				spCalendarLanguages.Add(strApplicationLanguage);
			}

			ppLanguages = spCalendarLanguages;

			return ppLanguages;
		}

		private void CreateCalendarAndMonthYearFormatter()
		{
			//CalendarFactory spCalendarFactory;
			Calendar spCalendar;
			string strClock = "24HourClock"; // it doesn't matter if it is 24 or 12 hour clock
			string strCalendarIdentifier;

			strCalendarIdentifier = CalendarIdentifier;

			//Create the calendar
			//ctl.GetActivationFactory(
			//	RuntimeClass_Windows_Globalization_Calendar,
			//	&spCalendarFactory);

			//spCalendarFactory.CreateCalendar(
			//	m_tpCalendarLanguages,
			//	strCalendarIdentifier,
			//	strClock,
			//	spCalendar);

			spCalendar = new Calendar(m_tpCalendarLanguages, strCalendarIdentifier, strClock);

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
			m_today = m_tpCalendar.GetDateTime();

			// default displaydate is today
			if (m_lastDisplayedDate.UniversalTime == 0)
			{
				m_lastDisplayedDate = m_today;
			}

			ForeachHost(pHost =>
			{
				var pPanel = pHost.Panel;
				if (pPanel is { })
				{
					pPanel.SetNeedsToDetermineBiggestItemSize();
				}

				pHost.ResetPossibleItemStrings();
				return;
			});

			{
				DateTimeFormatter spFormatter;

				// month year formatter needs to be updated when calendar is updated (languages or calendar identifier changed).
				CreateDateTimeFormatter("month year", out spFormatter);
				m_tpMonthYearFormatter = spFormatter;

				// year formatter also needs to be updated when the calendar is updated.
				CreateDateTimeFormatter("year", out spFormatter);
				m_tpYearFormatter = spFormatter;
			}

			return;
		}

		private void DisconnectItemHosts()
		{

			if (m_itemHostsConnected)
			{
				m_itemHostsConnected = false;

				ForeachHost((CalendarViewGeneratorHost pHost) =>
				{
					var pPanel = pHost.Panel;
					if (pPanel is { })
					{
						pPanel.DisconnectItemsHost();
					}

					return;
				});
			}

			return;
		}

		private void RegisterItemHosts()
		{
			global::System.Diagnostics.Debug.Assert(!m_itemHostsConnected);

			m_itemHostsConnected = true;

			ForeachHost((CalendarViewGeneratorHost pHost) =>
			{
				var pPanel = pHost.Panel;
				if (pPanel is { })
				{
					pPanel.RegisterItemsHost(pHost);
				}

				return;
			});

			return;
		}

		private void RefreshItemHosts()
		{
			DateTime minDate;
			DateTime maxDate;

			minDate = MinDate;
			maxDate = MaxDate;

			// making sure our MinDate and MaxDate are supported by the current Calendar
			{
				DateTime tempDate;

				m_tpCalendar.SetToMin();
				tempDate = m_tpCalendar.GetDateTime();

				m_minDate.UniversalTime = Math.Max(minDate.UniversalTime, tempDate.UniversalTime);

				m_tpCalendar.SetToMax();
				tempDate = m_tpCalendar.GetDateTime();

				m_maxDate.UniversalTime = Math.Min(maxDate.UniversalTime, tempDate.UniversalTime);
			}

			if (m_dateComparer.LessThan(m_maxDate, m_minDate))
			{
				//ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_INVALID_MIN_MAX_DATE);
				throw new InvalidOperationException("ERROR_CALENDAR_INVALID_MIN_MAX_DATE");
			}

			CoerceDate(ref m_lastDisplayedDate);

			m_tpCalendar.SetDateTime(m_minDate);
			m_weekDayOfMinDate = m_tpCalendar.DayOfWeek;

			ForeachHost((CalendarViewGeneratorHost pHost) =>
			{
				pHost.ResetScope(); // reset the scope data to force update the scope and header text.
				pHost.ComputeSize();
				return;
			});

			return;
		}

		private void AttachVisibleIndicesUpdatedEvents()
		{
			ForeachHost((CalendarViewGeneratorHost pHost) =>
			{
				pHost.AttachVisibleIndicesUpdatedEvent();
			});
		}

		private void DetachVisibleIndicesUpdatedEvents()
		{
			ForeachHost((CalendarViewGeneratorHost pHost) =>
			{
				pHost.DetachVisibleIndicesUpdatedEvent();
			});
		}

		// attach FocusEngaged event for all three hosts
		private void AttachScrollViewerFocusEngagedEvents()
		{
			ForeachHost((CalendarViewGeneratorHost pHost) =>
			{
				pHost.AttachScrollViewerFocusEngagedEvent();
			});
		}

		// detach FocusEngaged event for all three hosts
		private void DetachScrollViewerFocusEngagedEvents()
		{
			ForeachHost((CalendarViewGeneratorHost pHost) =>
			{
				pHost.DetachScrollViewerFocusEngagedEvent();
			});
		}

		private void AttachButtonClickedEvents()
		{
			DetachButtonClickedEvents(); // Uno

			if (m_tpHeaderButton is { })
			{
				m_epHeaderButtonClickHandler ??= new RoutedEventHandler((object pSender, RoutedEventArgs pArgs) =>
				{
					OnHeaderButtonClicked();
				});
				((Button)m_tpHeaderButton).Click += m_epHeaderButtonClickHandler;
			}

			if (m_tpPreviousButton is { })
			{
				m_epPreviousButtonClickHandler ??= new RoutedEventHandler(
					(object pSender, RoutedEventArgs pArgs) =>
					{
						OnNavigationButtonClicked(false /*forward*/);
					});
				((Button)m_tpPreviousButton).Click += m_epPreviousButtonClickHandler;
			}

			if (m_tpNextButton is { })
			{
				m_epNextButtonClickHandler ??= new RoutedEventHandler(
					(object pSender, RoutedEventArgs pArgs) =>
					{
						OnNavigationButtonClicked(true /*forward*/);
					});
				((Button)m_tpNextButton).Click += m_epNextButtonClickHandler;
			}

		}

		private void DetachButtonClickedEvents()
		{
			if (m_epHeaderButtonClickHandler is { } && m_tpHeaderButton is { })
			{
				m_tpHeaderButton.Click -= m_epHeaderButtonClickHandler;
				m_epHeaderButtonClickHandler = null;
			}
			if (m_epPreviousButtonClickHandler is { } && m_tpPreviousButton is { })
			{
				m_tpPreviousButton.Click -= m_epPreviousButtonClickHandler;
				m_epPreviousButtonClickHandler = null;
			}
			if (m_epNextButtonClickHandler is { } && m_tpNextButton is { })
			{
				m_tpNextButton.Click -= m_epNextButtonClickHandler;
				m_epNextButtonClickHandler = null;
			}

		}

		private void AttachScrollViewerKeyDownEvents()
		{
			DetachScrollViewerKeyDownEvents(); // UNO

			//Engagement now prevents events from bubbling from an engaged element. Before we relied on the bubbling behavior to
			//receive the KeyDown events from the ScrollViewer in the CalendarView. Now instead we have to handle the ScrollViewer's
			//On Key Down. To prevent handling the same OnKeyDown twice we only call into OnKeyDown if the ScrollViewer is engaged,
			//if it isn't it will bubble up the event.
			if (m_tpMonthViewScrollViewer is { })
			{
				m_epMonthViewScrollViewerKeyDownEventHandler ??= new KeyEventHandler(
					(object pSender, KeyRoutedEventArgs pArgs) =>
				{
					bool isEngaged = false;
					isEngaged = ((ScrollViewer)m_tpMonthViewScrollViewer).IsFocusEngaged;
					if (isEngaged)
					{
						OnKeyDown(pArgs);
					}

					return;
				});
				m_tpMonthViewScrollViewer.KeyDown += m_epMonthViewScrollViewerKeyDownEventHandler;
			}

			if (m_tpYearViewScrollViewer is { })
			{
				m_epYearViewScrollViewerKeyDownEventHandler ??= new KeyEventHandler(
					(object pSender, KeyRoutedEventArgs pArgs) =>
				{
					bool isEngaged = false;
					isEngaged = ((ScrollViewer)m_tpYearViewScrollViewer).IsFocusEngaged;
					if (isEngaged)
					{
						OnKeyDown(pArgs);
					}

					return;
				});
				m_tpYearViewScrollViewer.KeyDown += m_epYearViewScrollViewerKeyDownEventHandler;
			}

			if (m_tpDecadeViewScrollViewer is { })
			{
				m_epDecadeViewScrollViewerKeyDownEventHandler ??= new KeyEventHandler(
					(object pSender, KeyRoutedEventArgs pArgs) =>
				{
					bool isEngaged = false;
					isEngaged = ((ScrollViewer)m_tpDecadeViewScrollViewer).IsFocusEngaged;
					if (isEngaged)
					{
						OnKeyDown(pArgs);
					}

					return;
				});
				m_tpDecadeViewScrollViewer.KeyDown += m_epDecadeViewScrollViewerKeyDownEventHandler;
			}

			return;
		}

		private void DetachScrollViewerKeyDownEvents()
		{
			if (m_epMonthViewScrollViewerKeyDownEventHandler is { } && m_tpMonthViewScrollViewer is { })
			{
				m_tpMonthViewScrollViewer.KeyDown -= m_epMonthViewScrollViewerKeyDownEventHandler;
				m_epMonthViewScrollViewerKeyDownEventHandler = null;
			}

			if (m_epYearViewScrollViewerKeyDownEventHandler is { } && m_tpYearViewScrollViewer is { })
			{
				m_tpYearViewScrollViewer.KeyDown -= m_epYearViewScrollViewerKeyDownEventHandler;
				m_epYearViewScrollViewerKeyDownEventHandler = null;
			}

			if (m_epDecadeViewScrollViewerKeyDownEventHandler is { } && m_tpDecadeViewScrollViewer is { })
			{
				m_tpDecadeViewScrollViewer.KeyDown -= m_epDecadeViewScrollViewerKeyDownEventHandler;
				m_epDecadeViewScrollViewerKeyDownEventHandler = null;
			}

			return;
		}

		private void UpdateHeaderText(bool withAnimation)
		{
			CalendarViewGeneratorHost spHost;

			GetActiveGeneratorHost(out spHost);

			CalendarViewTemplateSettings pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);
			pTemplateSettingsConcrete.HeaderText = spHost.GetHeaderTextOfCurrentScope();

			if (withAnimation)
			{
				bool bIgnored = false;
				// play animation on the HeaderText after view mode changed.
				bIgnored = GoToState(true, "ViewChanged");
				bIgnored = GoToState(true, "ViewChanging");
			}

			// If UpdateText is because navigation button is clicked, make narrator to say the header.
			if (m_isNavigationButtonClicked)
			{
				m_isNavigationButtonClicked = false;
				RaiseAutomationNotificationAfterNavigationButtonClicked();

			}

			return;
		}

		// disable the button if we don't have more content
		private void UpdateNavigationButtonStates()
		{
			CalendarViewGeneratorHost spHost;

			GetActiveGeneratorHost(out spHost);

			var pCalendarPanel = spHost.Panel;

			if (pCalendarPanel is { })
			{
				int firstVisibleIndex = 0;
				int lastVisibleIndex = 0;
				uint size = 0;
				CalendarViewTemplateSettings pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

				firstVisibleIndex = pCalendarPanel.FirstVisibleIndexBase;
				lastVisibleIndex = pCalendarPanel.LastVisibleIndexBase;

				size = spHost.Size();

				pTemplateSettingsConcrete.HasMoreContentBefore = firstVisibleIndex > 0;
				pTemplateSettingsConcrete.HasMoreContentAfter = lastVisibleIndex + 1 < (int)(size);
			}

			return;
		}

		private void OnHeaderButtonClicked()
		{
			CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;

			mode = DisplayMode;

			if (mode != CalendarViewDisplayMode.Decade)
			{
				if (mode == CalendarViewDisplayMode.Month)
				{
					mode = CalendarViewDisplayMode.Year;
				}
				else // if (mode == CalendarViewDisplayMode.Year)
				{
					mode = CalendarViewDisplayMode.Decade;
				}

				DisplayMode = mode;
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(false, "header button should be disabled already in decade view mode.");
			}

		}

		private void RaiseAutomationNotificationAfterNavigationButtonClicked()
		{
			if (m_tpHeaderButton is { })
			{
				string automationName;
				automationName = AutomationProperties.GetName(((Button)m_tpHeaderButton));
				if (automationName is null)
				{
					//FrameworkElement.GetStringFromObject(m_tpHeaderButton, automationName);
					automationName = m_tpHeaderButton.Content?.ToString();
				}

				if (automationName is { })
				{
					AutomationPeer calendarViewAutomationPeer;

					calendarViewAutomationPeer = GetAutomationPeer();
					if (calendarViewAutomationPeer is { })
					{
						// Two possible solution: RaisePropertyChangedEvent or RaiseNotificationEvent. If Raise PropertyChangedEvent each time when head is changing,
						// it would be overkilling since header is already included in other automation event. More information about RaiseNotificationEvent, please
						// refer to https://docs.microsoft.com/en-us/uwp/api/windows.ui.automation.peers.automationnotificationkind
						calendarViewAutomationPeer.RaiseNotificationEvent(
							AutomationNotificationKind.ActionCompleted,
							AutomationNotificationProcessing.MostRecent,
							automationName,
							"CalenderViewNavigationButtonCompleted");
					}
				}
			}

			return;
		}

		private void OnNavigationButtonClicked(bool forward)
		{
			CalendarViewGeneratorHost spHost;

			GetActiveGeneratorHost(out spHost);

			var pCalendarPanel = spHost.Panel;

			if (pCalendarPanel is { })
			{
				bool canPanelShowFullScope = false;

				int firstVisibleIndex = 0;
				DependencyObject spChildAsIDO;
				CalendarViewBaseItem spChildAsI;
				CalendarViewBaseItem spChild;
				DateTime dateOfFirstVisibleItem = default;
				DateTime targetDate = default;

				CanPanelShowFullScope(spHost, out canPanelShowFullScope);

				firstVisibleIndex = pCalendarPanel.FirstVisibleIndexBase;

				spChildAsIDO = pCalendarPanel.ContainerFromIndex(firstVisibleIndex);

				spChildAsI = spChildAsIDO as CalendarViewBaseItem;
				if (spChildAsI is { })
				{
					try
					{
						spChild = ((CalendarViewBaseItem)spChildAsI);
						dateOfFirstVisibleItem = spChild.DateBase;
						if (canPanelShowFullScope)
						{
							// if Panel can show a full scope, we navigate by a scope.
							spHost.GetFirstDateOfNextScope(dateOfFirstVisibleItem, forward, out targetDate);
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
							spHost.AddUnits(targetDate, distance);
#if DEBUG && false
							if (SUCCEEDED(hr))
							{
								// targetDate should be still in valid range.
								var temp = targetDate;
								CoerceDate(temp);
								global::System.Diagnostics.Debug.Assert(temp.UniversalTime == targetDate.UniversalTime);
							}

#endif
						}

					}
					catch (Exception)
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

			return;
		}


		// change the dimensions of YearView and DecadeView.
		// API name to be reviewed.
		public void SetYearDecadeDisplayDimensions(int columns, int rows)
		{
			global::System.Diagnostics.Debug.Assert(columns > 0 && rows > 0);

			// note once this is set, developer can't unset it
			m_areYearDecadeViewDimensionsSet = true;

			m_colsInYearDecadeView = columns;
			m_rowsInYearDecadeView = rows;

			var pYearPanel = m_tpYearViewItemHost.Panel;
			if (pYearPanel is { })
			{
				// Panel type is no longer Secondary_SelfAdaptive
				pYearPanel.PanelType = CalendarPanelType.Secondary;
				pYearPanel.SetSuggestedDimension(columns, rows);
			}

			var pDecadePanel = m_tpDecadeViewItemHost.Panel;
			if (pDecadePanel is { })
			{
				// Panel type is no longer Secondary_SelfAdaptive
				pDecadePanel.PanelType = CalendarPanelType.Secondary;
				pDecadePanel.SetSuggestedDimension(columns, rows);
			}

			return;
		}

		// When we call SetDisplayDate, we'll check if the current view is big enough to hold a whole scope.
		// If yes then we'll bring the first date in this scope into view,
		// otherwise bring the display date into view then the display date will be on first visible line.
		//
		// note: when panel can't show a fullscope, we might be still able to show the first day and the requested date
		// in the viewport (e.g. NumberOfWeeks is 4, we request to display 1/9/2000, in this case 1/1/2000 and 1/9/2000 can
		// be visible at the same time). To consider this case we need more logic, we can fix later when needed.
		private void BringDisplayDateintoView(
			CalendarViewGeneratorHost pHost)
		{
			bool canPanelShowFullScope = false;
			DateTime dateToBringintoView;

			CanPanelShowFullScope(pHost, out canPanelShowFullScope);

			if (canPanelShowFullScope)
			{
				m_tpCalendar.SetDateTime(m_lastDisplayedDate);
				pHost.AdjustToFirstUnitInThisScope(out dateToBringintoView);
				CoerceDate(ref dateToBringintoView);
			}
			else
			{
				dateToBringintoView = m_lastDisplayedDate;
			}

			ScrollToDate(pHost, dateToBringintoView);

			return;
		}

		// bring a item into view
		// This function will scroll to the target item immediately,
		// when target is far away from realized window, we'll not see unrealized area.
		private void ScrollToDate(
			CalendarViewGeneratorHost pHost,
			DateTime date)
		{
			int index = 0;

			index = pHost.CalculateOffsetFromMinDate(date);
			global::System.Diagnostics.Debug.Assert(index >= 0);
			global::System.Diagnostics.Debug.Assert(pHost.Panel is { });
			pHost.Panel.ScrollItemIntoView(
				index,
				ScrollIntoViewAlignment.Leading,
				0.0 /* offset */,
				true /* forceSynchronous */);

		}

		// Bring a item into view with animation.
		// This function will scroll to the target item with DManip animation so
		// if target is not realized yet, we might see unrealized area.
		// This only gets called in NavigationButton clicked event where
		// the target should be less than one page away from visible window.
		private void ScrollToDateWithAnimation(
			CalendarViewGeneratorHost pHost,
			DateTime date)
		{
			var pScrollViewer = pHost.ScrollViewer;
			if (pScrollViewer is { })
			{
				int index = 0;
				int firstVisibleIndex = 0;
				int cols = 0;
				DependencyObject spFirstVisibleItemAsI;
				CalendarViewBaseItem spFirstVisibleItem;
				object spVerticalOffset;
				double? spVerticalOffsetReference;
				bool handled = false;

				var pCalendarPanel = pHost.Panel;

				// The target item may be not realized yet so we can't get
				// the offset from virtualization information.
				// However all items have the same size so we could deduce the target's
				// exact offset from the current realized item, e.g. the firstVisibleItem

				// 1. compute the target index.
				index = pHost.CalculateOffsetFromMinDate(date);
				global::System.Diagnostics.Debug.Assert(index >= 0);

				cols = pCalendarPanel.Cols;
				global::System.Diagnostics.Debug.Assert(cols > 0);

				// cols should not be 0 at this point. If it is, perhaps
				// the calendar view has not been fully brought up yet.
				// If cols is 0, we do not want to bring the process down though.
				// Doing a no-op for the scroll to date in this case.
				if (cols > 0)
				{
					// 2. find the first visible index.
					firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;
					spFirstVisibleItemAsI = pCalendarPanel.ContainerFromIndex(firstVisibleIndex);
					spFirstVisibleItem = (CalendarViewBaseItem)spFirstVisibleItemAsI;

					global::System.Diagnostics.Debug.Assert(spFirstVisibleItem.GetVirtualizationInformation() is { });

					// 3. based on the first visible item's bounds, compute the target item's offset
					var bounds = spFirstVisibleItem.GetVirtualizationInformation().Bounds;
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
					spVerticalOffset = offset;
					spVerticalOffsetReference = offset; // (double)spVerticalOffset;

					handled = pScrollViewer.ChangeView(
						null /*horizontalOffset*/,
						spVerticalOffsetReference,
						null /*zoomFactor*/,
						false /*disableAnimation*/);
					global::System.Diagnostics.Debug.Assert(handled);
				}
			}

			return;
		}

		public void SetDisplayDate(global::System.DateTimeOffset date)
		{
			// Uno specific: Force conversion from System.DateTimeOffset to Windows.Foundation.DateTime
			// Don't use date parameter except in this line.
			DateTime wfDate = date;

			// if m_dateSourceChanged is true, this means we might changed m_minDate or m_maxDate
			// so we should not call CoerceDate until next measure pass, by then the m_minDate and
			// m_maxDate are updated.
			if (!m_dateSourceChanged)
			{
				CoerceDate(ref wfDate);

				SetDisplayDateInternal(wfDate);
			}
			else
			{
				// given that m_dateSourceChanged is true, we'll have a new layout pass soon.
				// we're going to honer the display date request in that layout pass.
				// note: there is an issue to call ScrollItemintoView in MCBP's measure pass
				// the workaround is call it in Arrange pass or later. here we'll call it
				// in the arrange pass.
				m_isSetDisplayDateRequested = true;
				m_lastDisplayedDate = wfDate;
			}

			return;
		}

		private void SetDisplayDateInternal(DateTime date)
		{
			CalendarViewGeneratorHost spHost;

			GetActiveGeneratorHost(out spHost);

			m_lastDisplayedDate = date;

			if (spHost.Panel is { })
			{
				// note if panel is not loaded yet (i.e. we call SetDisplayDate before Panel is loaded,
				// below call will fail silently. This is not a problem because
				// we'll call this again in panel loaded event.

				BringDisplayDateintoView(spHost);
			}

		}

		internal void CoerceDate(ref DateTime date)
		{
			// we should not call CoerceDate when m_dateSourceChanged is true, because
			// m_dateSourceChanged being true means the current m_minDate or m_maxDate is
			// out of dated.

			global::System.Diagnostics.Debug.Assert(!m_dateSourceChanged);
			if (m_dateComparer.LessThan(date, m_minDate))
			{
				date = m_minDate;
			}

			if (m_dateComparer.LessThan(m_maxDate, date))
			{
				date = m_maxDate;
			}
		}

		internal void OnVisibleIndicesUpdated(
			CalendarViewGeneratorHost pHost)
		{
			int firstVisibleIndex = 0;
			int lastVisibleIndex = 0;
			DependencyObject spTempChildAsIDO;
			CalendarViewBaseItem spTempChildAsI;
			DateTime firstDate = default;
			DateTime lastDate = default;
			bool isScopeChanged = false;
			int startIndex = 0;
			int numberOfItemsInCol;

			var pCalendarPanel = pHost.Panel;

			global::System.Diagnostics.Debug.Assert(pCalendarPanel is { });

			// We explicitly call UpdateLayout in OnDisplayModeChanged, this will ocassionaly make CalendarPanelType invalid,
			// which causes CalendarPanel to skip the row&col calculations.
			// If CalendarPanelType is invalid, just skip the current update
			// since this method will be called again in later layout passes.
			if (pCalendarPanel.PanelType != CalendarPanelType.Invalid)
			{
				startIndex = pCalendarPanel.StartIndex;
				numberOfItemsInCol = pCalendarPanel.Cols;

				global::System.Diagnostics.Debug.Assert(startIndex < numberOfItemsInCol);

				firstVisibleIndex = pCalendarPanel.FirstVisibleIndexBase;
				lastVisibleIndex = pCalendarPanel.LastVisibleIndexBase;

				spTempChildAsIDO = pCalendarPanel.ContainerFromIndex(firstVisibleIndex);

				spTempChildAsI = (CalendarViewBaseItem)spTempChildAsIDO;

				firstDate = ((CalendarViewBaseItem)spTempChildAsI).DateBase;

				spTempChildAsIDO = pCalendarPanel.ContainerFromIndex(lastVisibleIndex);

				spTempChildAsI = (CalendarViewBaseItem)spTempChildAsIDO;

				lastDate = ((CalendarViewBaseItem)spTempChildAsI).DateBase;

				//now determine the current scope based on this date.
				pHost.UpdateScope(firstDate, lastDate, out isScopeChanged);

				if (isScopeChanged)
				{
#if __ANDROID__
					// .InvalidateMeasure() bug https://github.com/unoplatform/uno/issues/6236
					DispatcherQueue.TryEnqueue(() => UpdateHeaderText(false /*withAnimation*/));
#else
					UpdateHeaderText(false /*withAnimation*/);
#endif
				}

				// everytime visible indices changed, we need to update
				// navigationButtons' states.
				UpdateNavigationButtonStates();

				UpdateItemsScopeState(
					pHost,
					true, /*ignoreWhenIsOutOfScopeDisabled*/
					true /*ignoreInDirectManipulation*/);
			}

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
		internal void UpdateItemsScopeState(
			CalendarViewGeneratorHost pHost,
			bool ignoreWhenIsOutOfScopeDisabled,
			bool ignoreInDirectManipulation)
		{
			var pCalendarPanel = pHost.Panel;
			if (pCalendarPanel is null || pCalendarPanel.DesiredSize == default)
			{
				// it is possible that we change IsOutOfScopeEnabled property before CalendarView enters visual tree.
				return;
			}

			bool isOutOfScopeEnabled = false;
			isOutOfScopeEnabled = IsOutOfScopeEnabled;

			if (ignoreWhenIsOutOfScopeDisabled && !isOutOfScopeEnabled)
			{
				return;
			}

			bool isInDirectManipulation = pHost.ScrollViewer is { } && pHost.ScrollViewer.IsInDirectManipulation;
			if (ignoreInDirectManipulation && isInDirectManipulation)
			{
				return;
			}

			bool canHaveOutOfScopeState = isOutOfScopeEnabled && !isInDirectManipulation;
			int firstIndex = -1;
			int lastIndex = -1;
			DependencyObject spChildAsIDO;
			CalendarViewBaseItem spChildAsI;
			CalendarViewBaseItem spChild;
			DateTime date;

			firstIndex = pCalendarPanel.FirstVisibleIndex;
			lastIndex = pCalendarPanel.LastVisibleIndex;

			// given that all items not in visible window have InScope state, so we only want
			// to check the visible window, plus the items in last visible window. this way
			// we don't need to check against virtualization window.
			var lastVisibleIndicesPair = pHost.GetLastVisibleIndicesPairRef();

			if (firstIndex != -1 && lastIndex != -1)
			{
				for (int index = firstIndex; index <= lastIndex; ++index)
				{
					spChildAsIDO = pCalendarPanel.ContainerFromIndex(index);
					spChildAsI = (CalendarViewBaseItem)spChildAsIDO;

					spChild = ((CalendarViewBaseItem)spChildAsI);
					date = spChild.DateBase;

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
						spChildAsIDO = pCalendarPanel.ContainerFromIndex(index);
						spChildAsI = spChildAsIDO as CalendarViewBaseItem;

						if (spChildAsI is { })
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
						spChildAsIDO = pCalendarPanel.ContainerFromIndex(index);
						spChildAsI = spChildAsIDO as CalendarViewBaseItem;

						if (spChildAsI is { })
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
			return;
		}

		// this property affects Today in MonthView, ThisMonth in YearView and ThisYear in DecadeView.
		private void OnIsTodayHighlightedPropertyChanged()
		{
			ForeachHost((CalendarViewGeneratorHost pHost) =>
			{
				var pPanel = pHost.Panel;
				if (pPanel is { })
				{
					int indexOfToday = -1;

					indexOfToday = pHost.CalculateOffsetFromMinDate(m_today);

					if (indexOfToday != -1)
					{
						DependencyObject spChildAsIDO;
						CalendarViewBaseItem spChildAsI;

						spChildAsIDO = pPanel.ContainerFromIndex(indexOfToday);
						spChildAsI = spChildAsIDO as CalendarViewBaseItem;
						// today item is realized already, we need to update the state here.
						// if today item is not realized yet, we'll update the state when today item is being prepared.
						if (spChildAsI is { })
						{
							bool isTodayHighlighted = false;

							isTodayHighlighted = IsTodayHighlighted;
							((CalendarViewBaseItem)spChildAsI).SetIsToday(isTodayHighlighted);
						}
					}
				}
			});
		}

		private void OnIsOutOfScopePropertyChanged()
		{
			CalendarViewGeneratorHost spHost;
			bool isOutOfScopeEnabled = false;

			isOutOfScopeEnabled = IsOutOfScopeEnabled;

			// when IsOutOfScopeEnabled property is false, we don't care about scope state (all are inScope),
			// so we don't need to hook to ScrollViewer's state change handler.
			// when IsOutOfScopeEnabled property is true, we need to do so.
			if (m_areDirectManipulationStateChangeHandlersHooked != isOutOfScopeEnabled)
			{
				m_areDirectManipulationStateChangeHandlersHooked = !m_areDirectManipulationStateChangeHandlersHooked;

				ForeachHost((CalendarViewGeneratorHost pHost) =>
				{
					var pScrollViewer = pHost.ScrollViewer;
					if (pScrollViewer is { })
					{
						pScrollViewer.SetDirectManipulationStateChangeHandler(
							isOutOfScopeEnabled ? pHost : null
						);
					}

					return;
				});
			}

			GetActiveGeneratorHost(out spHost);
			UpdateItemsScopeState(
				spHost,
				false, /*ignoreWhenIsOutOfScopeDisabled*/
				true /*ignoreInDirectManipulation*/);

			return;
		}

		internal void OnScrollViewerFocusEngaged(
			FocusEngagedEventArgs pArgs)
		{
			CalendarViewGeneratorHost spHost;

			GetActiveGeneratorHost(out spHost);

			if (spHost is { })
			{
				bool focused = false;
				m_focusItemAfterDisplayModeChanged = false;
				FocusEngagedEventArgs spArgs = pArgs;

				FocusItemByDate(spHost, m_lastDisplayedDate, m_focusStateAfterDisplayModeChanged, out focused);

				spArgs.Handled = focused;
			}

			return;
		}

		private void OnDisplayModeChanged(
			CalendarViewDisplayMode oldDisplayMode,
			CalendarViewDisplayMode newDisplayMode)
		{
			CalendarViewGeneratorHost spCurrentHost;
			CalendarViewGeneratorHost spOldHost;
			bool isEngaged = false;

			GetGeneratorHost(oldDisplayMode, out spOldHost);
			if (spOldHost is { })
			{
				var pScrollViewer = spOldHost.ScrollViewer;

				if (pScrollViewer is { })
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

			GetGeneratorHost(newDisplayMode, out spCurrentHost);
			var pCurrentPanel = spCurrentHost.Panel;
			if (pCurrentPanel is { })
			{
				// if panel is not loaded yet (e.g. the first time we switch to the YearView or DecadeView),
				// ScrollItemintoView (called by FocusItemByDate) will not work because ScrollViewer is not
				// hooked up yet. Give the panel an extra layout pass to hook up the ScrollViewer.
				if (newDisplayMode != CalendarViewDisplayMode.Month)
				{
					pCurrentPanel.UpdateLayout();
				}

				// If Engaged, make sure that the new scroll viewer is engaged. Note that
				// you want to Engage before focusing ItemByDate to land on the correct item.
				if (isEngaged)
				{
					var spScrollViewer = spCurrentHost.ScrollViewer;
					if (spScrollViewer is { })
					{
						// The old ScrollViewer was engaged, engage the new ScrollViewer

						//A control must be focused before we can set Engagement on it, attempt to set focus first
						bool focused = false;
						focused = FocusManager.SetFocusedElementWithDirection(spScrollViewer, FocusState.Keyboard, false /*animateIfBringintoView*/, false, FocusNavigationDirection.None);
						if (focused)
						{
							FocusManager.SetEngagedControl(spScrollViewer);
						}
					}
				}

				// If we requested to move focus to item, let's do it.
				if (m_focusItemAfterDisplayModeChanged)
				{
					bool focused = false;
					m_focusItemAfterDisplayModeChanged = false;

					FocusItemByDate(spCurrentHost, m_lastDisplayedDate, m_focusStateAfterDisplayModeChanged, out focused);
				}
				else // we only scroll to the focusedDate without moving focus to it
				{
					BringDisplayDateintoView(spCurrentHost);
				}
			}

			CalendarViewTemplateSettings pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

			pTemplateSettingsConcrete.HasMoreViews = (newDisplayMode != CalendarViewDisplayMode.Decade);
			UpdateHeaderText(true /*withAnimation*/);

			UpdateNavigationButtonStates();

			return;
		}


		private void UpdateLastDisplayedDate(CalendarViewDisplayMode lastDisplayMode)
		{
			CalendarViewGeneratorHost spPreviousHost;
			GetGeneratorHost(lastDisplayMode, out spPreviousHost);

			var pPreviousPanel = spPreviousHost.Panel;
			if (pPreviousPanel is { })
			{
				int firstVisibleIndex = 0;
				int lastVisibleIndex = 0;
				DateTime firstVisibleDate = default;
				DateTime lastVisibleDate = default;
				DependencyObject spChildAsIDO;
				CalendarViewBaseItem spChildAsI;

				firstVisibleIndex = pPreviousPanel.FirstVisibleIndexBase;
				lastVisibleIndex = pPreviousPanel.LastVisibleIndexBase;

				global::System.Diagnostics.Debug.Assert(firstVisibleIndex != -1 && lastVisibleIndex != -1);

				spChildAsIDO = pPreviousPanel.ContainerFromIndex(firstVisibleIndex);
				spChildAsI = spChildAsIDO as CalendarViewBaseItem;
				firstVisibleDate = ((CalendarViewBaseItem)spChildAsI).DateBase;

				spChildAsIDO = pPreviousPanel.ContainerFromIndex(lastVisibleIndex);
				spChildAsI = spChildAsIDO as CalendarViewBaseItem;
				lastVisibleDate = ((CalendarViewBaseItem)spChildAsI).DateBase;

				// check if last displayed Date is visible or not
				bool isLastDisplayedDateVisible = false;
				int result = 0;
				result = spPreviousHost.CompareDate(m_lastDisplayedDate, firstVisibleDate);
				if (result >= 0)
				{
					result = spPreviousHost.CompareDate(m_lastDisplayedDate, lastVisibleDate);
					if (result <= 0)
					{
						isLastDisplayedDateVisible = true;
					}
				}

				if (!isLastDisplayedDateVisible)
				{
					// if last displayed date is not visible, we use the first_visible_inscope_date as the last displayed date

					// first try to use the first_inscope_date
					DateTime firstVisibleInscopeDate = spPreviousHost.GetMinDateOfCurrentScope();
					// check if first_inscope_date is visible or not
					result = spPreviousHost.CompareDate(firstVisibleInscopeDate, firstVisibleDate);
					if (result < 0)
					{
						// the firstInscopeDate is not visible, then we use the firstVisibleDate.
#if DEBUG
						{
							// in this case firstVisibleDate must be in scope (i.e. it must be less than or equals to the maxDateOfCurrentScope).
							int temp = 0;
							temp = spPreviousHost.CompareDate(firstVisibleDate, spPreviousHost.GetMaxDateOfCurrentScope());
							global::System.Diagnostics.Debug.Assert(temp <= 0);
						}
#endif
						firstVisibleInscopeDate = firstVisibleDate;
					}

					// based on the display mode, partially copy the firstVisibleInscopeDate to m_lastDisplayedDate.
					CopyDate(
						lastDisplayMode,
						firstVisibleInscopeDate,
						ref m_lastDisplayedDate);
				}
			}

			return;
		}


		private void OnIsLabelVisibleChanged()
		{
			// we don't have label text in decade view.
			var hosts = new CalendarViewGeneratorHost[2]
				{
					m_tpMonthViewItemHost, m_tpYearViewItemHost
				}
			;

			bool isLabelVisible = false;

			isLabelVisible = IsGroupLabelVisible;

			for (uint i = 0; i < hosts.Length; ++i)
			{
				var pHost = hosts[i];
				var pPanel = pHost.Panel;

				if (pPanel is { })
				{
					ForeachChildInPanel(pPanel,
						(CalendarViewBaseItem pItem) =>
					{
						pHost.UpdateLabel(pItem, isLabelVisible);
					});
				}
			}

		}

		private void CreateDateTimeFormatter(
			string format,
			out DateTimeFormatter ppDateTimeFormatter)
		{
			DateTimeFormatter spFormatter;
			string strClock = "24HourClock"; // it doesn't matter if it is 24 or 12 hour clock
			string strGeographicRegion = "ZZ"; // geographicRegion doesn't really matter as we have no decimal separator or grouping
			string strCalendarIdentifier;

			strCalendarIdentifier = CalendarIdentifier;

			spFormatter = new DateTimeFormatter(format,
				m_tpCalendarLanguages,
				strGeographicRegion,
				strCalendarIdentifier,
				strClock);

			ppDateTimeFormatter = spFormatter;
		}

		private void FormatWeekDayNames()
		{
			if (m_tpMonthViewItemHost.Panel is { })
			{
				object spDayOfWeekFormat;
				bool isUnsetValue = false;
				DayOfWeek dayOfWeek = DayOfWeek.Sunday;
				DateTimeFormatter spFormatter = default;

				spDayOfWeekFormat = ReadLocalValue(DayOfWeekFormatProperty);
				isUnsetValue = spDayOfWeekFormat == DependencyProperty.UnsetValue;

				m_tpCalendar.SetToNow();
				dayOfWeek = m_tpCalendar.DayOfWeek;

				// adjust to next sunday.
				m_tpCalendar.AddDays((s_numberOfDaysInWeek - (int)dayOfWeek) % s_numberOfDaysInWeek);
				m_dayOfWeekNames.Clear();
				//m_dayOfWeekNames.reserve(s_numberOfDaysInWeek);

				// Fill m_dayOfWeekNamesFull. This will always be the full name of the day regardless of abbreviation used for m_dayOfWeekNames.
				m_dayOfWeekNamesFull.Clear();
				//m_dayOfWeekNamesFull.reserve(s_numberOfDaysInWeek);

				if (!isUnsetValue) // format is set, use this format.
				{
					string dayOfWeekFormat;

					dayOfWeekFormat = DayOfWeekFormat;

					// Workaround: we can't bind an unset value to a property.
					// Here we'll check if the format is empty or not - because in CalendarDatePicker this property
					// is bound to CalendarDatePicker.DayOfWeekFormat which will cause this value is always set.
					if (!dayOfWeekFormat.IsNullOrEmpty())
					{
						CreateDateTimeFormatter(dayOfWeekFormat, out spFormatter);

					}
				}

				for (int i = 0; i < s_numberOfDaysInWeek; ++i)
				{
					string str;
					if (spFormatter is { }) // there is a valid datetimeformatter specified by user, use it
					{
						DateTime date;
						date = m_tpCalendar.GetDateTime();
						str = spFormatter.Format(date);
					}
					else // otherwise use the shortest string formatted by calendar.
					{
						str = m_tpCalendar.DayOfWeekAsString(
							1 /*shortest length*/
							);
					}

					m_dayOfWeekNames.Add(str);

					// for automation peer name, we always use the full string.
					var @string = m_tpCalendar.DayOfWeekAsFullString();
					m_dayOfWeekNamesFull.Add(@string);

					m_tpCalendar.AddDays(1);
				}
			}

		}

		private void UpdateWeekDayNameAPName(string str, string name)
		{
			TextBlock spWeekDay;
			spWeekDay = this.GetTemplateChild<TextBlock>(str);
			AutomationProperties.SetName(((TextBlock)spWeekDay), name);
			return;
		}

		private void UpdateWeekDayNames()
		{
			var pMonthPanel = m_tpMonthViewItemHost.Panel;
			if (pMonthPanel is { })
			{
				DayOfWeek firstDayOfWeek = DayOfWeek.Sunday;
				int index = 0;
				CalendarViewTemplateSettings pTemplateSettingsConcrete = ((CalendarViewTemplateSettings)m_tpTemplateSettings);

				firstDayOfWeek = FirstDayOfWeek;

				if (m_dayOfWeekNames.Empty())
				{
					FormatWeekDayNames();
				}

				index = (int)(firstDayOfWeek - DayOfWeek.Sunday);

				pTemplateSettingsConcrete.WeekDay1 = (m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(("WeekDay1"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.WeekDay2 = (m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(("WeekDay2"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.WeekDay3 = (m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(("WeekDay3"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.WeekDay4 = (m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(("WeekDay4"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.WeekDay5 = (m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(("WeekDay5"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.WeekDay6 = (m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(("WeekDay6"), m_dayOfWeekNamesFull[index]);
				index = (index + 1) % s_numberOfDaysInWeek;

				pTemplateSettingsConcrete.WeekDay7 = (m_dayOfWeekNames[index]);
				UpdateWeekDayNameAPName(("WeekDay7"), m_dayOfWeekNamesFull[index]);

				m_monthViewStartIndex = (m_weekDayOfMinDate - firstDayOfWeek + s_numberOfDaysInWeek) % s_numberOfDaysInWeek;

				global::System.Diagnostics.Debug.Assert(m_monthViewStartIndex >= 0 && m_monthViewStartIndex < s_numberOfDaysInWeek);

				pMonthPanel.StartIndex = m_monthViewStartIndex;
			}

		}

		internal void GetActiveGeneratorHost(out CalendarViewGeneratorHost ppHost)
		{
			CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;
			ppHost = null;

			mode = DisplayMode;

			GetGeneratorHost(mode, out ppHost);
		}

		private void GetGeneratorHost(
			CalendarViewDisplayMode mode,
			out CalendarViewGeneratorHost ppHost)
		{
			if (mode == CalendarViewDisplayMode.Month)
			{
				ppHost = m_tpMonthViewItemHost;
			}
			else if (mode == CalendarViewDisplayMode.Year)
			{
				ppHost = m_tpYearViewItemHost;
			}
			else if (mode == CalendarViewDisplayMode.Decade)
			{
				ppHost = m_tpDecadeViewItemHost;
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(false);
				throw new InvalidOperationException();
			}
		}

		internal string FormatYearName(DateTime date)
		{
			var pName = m_tpYearFormatter.Format(date);

			return pName;
		}

		internal string FormatMonthYearName(DateTime date)
		{
			var pName = m_tpMonthYearFormatter.Format(date);

			return pName;
		}

		// Partially copy date from source to target.
		// Only copy the parts we want and keep the remaining part.
		// Once the remaining part becomes invalid with the new copied parts,
		// we need to adjust the remaining part to the most reasonable value.
		// e.g. target: 3/31/2014, source 2/1/2013 and we want to copy month part,
		// the target will become 2/31/2013 and we'll adjust the day to 2/28/2013.

		private void CopyDate(
			CalendarViewDisplayMode displayMode,
			DateTime source,
			ref DateTime target)
		{
			bool copyEra = true;
			bool copyYear = true;
			bool copyMonth = displayMode == CalendarViewDisplayMode.Month ||
				displayMode == CalendarViewDisplayMode.Year;
			bool copyDay = displayMode == CalendarViewDisplayMode.Month;

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
					//m_tpCalendar.Era = era; --- FIX FOR https://github.com/unoplatform/uno/issues/6160
				}

				if (copyYear)
				{
					// year might not be valid.
					int first = 0;
					int last = 0;
					first = m_tpCalendar.FirstYearInThisEra;
					last = m_tpCalendar.LastYearInThisEra;
					year = Math.Min(last, Math.Max(first, year));
					m_tpCalendar.Year = year;
				}

				if (copyMonth)
				{
					// month might not be valid.
					int first = 0;
					int last = 0;
					first = m_tpCalendar.FirstMonthInThisYear;
					last = m_tpCalendar.LastMonthInThisYear;
					month = Math.Min(last, Math.Max(first, month));
					m_tpCalendar.Month = month;
				}

				if (copyDay)
				{
					// day might not be valid.
					int first = 0;
					int last = 0;
					first = m_tpCalendar.FirstDayInThisMonth;
					last = m_tpCalendar.LastDayInThisMonth;
					day = Math.Min(last, Math.Max(first, day));
					m_tpCalendar.Day = day;
				}

				target = m_tpCalendar.GetDateTime();
				// make sure the target is still in range.
				CoerceDate(ref target);
			}
		}

		/*static*/
		internal static void CanPanelShowFullScope(
			CalendarViewGeneratorHost pHost,
			out bool pCanPanelShowFullScope)
		{
			var pCalendarPanel = pHost.Panel;
			int row = 0;
			int col = 0;
			pCanPanelShowFullScope = false;

			global::System.Diagnostics.Debug.Assert(pCalendarPanel is { });

			row = pCalendarPanel.Rows;
			col = pCalendarPanel.Cols;

			// Consider about the corner case: the first item in this scope
			// is laid on the last col in first row, so according dimension
			// row x col, we could arrange up to (row - 1) x col + 1 items

			pCanPanelShowFullScope = (row - 1) * col + 1 >= pHost.GetMaximumScopeSize();

			return;
		}

		private void ForeachChildInPanel(
			CalendarPanel pCalendarPanel,
			Action<CalendarViewBaseItem> func)
		{
			if (pCalendarPanel is { })
			{
				if (pCalendarPanel.IsInLiveTree)
				{
					int firstCacheIndex = 0;
					int lastCacheIndex = 0;

					firstCacheIndex = pCalendarPanel.FirstCacheIndex;
					lastCacheIndex = pCalendarPanel.LastCacheIndex;

					for (int i = firstCacheIndex; i <= lastCacheIndex; ++i)
					{
						DependencyObject spChildAsIDO;
						CalendarViewBaseItem spChildAsI;

						spChildAsIDO = pCalendarPanel.ContainerFromIndex(i);
						spChildAsI = spChildAsIDO as CalendarViewBaseItem;

						func(((CalendarViewBaseItem)spChildAsI));
					}
				}
			}

			return;
		}

		private void ForeachHost(Action<CalendarViewGeneratorHost> func)
		{
			func(m_tpMonthViewItemHost);
			func(m_tpYearViewItemHost);
			func(m_tpDecadeViewItemHost);
			return;
		}

		/*static*/
		internal static void SetDayItemStyle(
			CalendarViewBaseItem pItem,
			Style pStyle)
		{
			global::System.Diagnostics.Debug.Assert(pItem is CalendarViewDayItem);
			if (pStyle is { })
			{
				pItem.Style = pStyle;
			}
			else
			{
				pItem.ClearValue(FrameworkElement.StyleProperty);
			}

			return;
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			AutomationPeer ppAutomationPeer = null;

			CalendarViewAutomationPeer spAutomationPeer;
			spAutomationPeer = new CalendarViewAutomationPeer(this);
			ppAutomationPeer = spAutomationPeer;

			return ppAutomationPeer;
		}

		// Called when the IsEnabled property changes.
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
		{
			UpdateVisualState();
		}

		internal void GetRowHeaderForItemAutomationPeer(
			DateTime itemDate,
			CalendarViewDisplayMode displayMode,
			out uint pReturnValueCount,
			out IRawElementProviderSimple[] ppReturnValue)
		{
			pReturnValueCount = 0;
			ppReturnValue = null;

			CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;
			mode = DisplayMode;

			//Ensure we only read out this header when in Month mode or Year mode. Decade mode reading the header isn't helpful.
			if (displayMode == mode)
			{
				int month, year;
				m_tpCalendar.SetDateTime(itemDate);
				month = m_tpCalendar.Month;
				year = m_tpCalendar.Year;

				bool useCurrentHeaderPeer =
					m_currentHeaderPeer is { } &&
					(m_currentHeaderPeer.GetMonth() == month || mode == CalendarViewDisplayMode.Year) &&
					m_currentHeaderPeer.GetYear() == year &&
					m_currentHeaderPeer.GetMode() == mode;

				bool usePreviousHeaderPeer =
					m_previousHeaderPeer is { } &&
					(m_previousHeaderPeer.GetMonth() == month || mode == CalendarViewDisplayMode.Year) &&
					m_previousHeaderPeer.GetYear() == year &&
					m_previousHeaderPeer.GetMode() == mode;

				bool createNewHeaderPeer = !useCurrentHeaderPeer && !usePreviousHeaderPeer;

				if (createNewHeaderPeer)
				{
					CalendarViewHeaderAutomationPeer peer;
					peer = new CalendarViewHeaderAutomationPeer();

					string headerName;

					if (mode == CalendarViewDisplayMode.Month)
					{
						headerName = FormatMonthYearName(itemDate);
					}
					else
					{
						global::System.Diagnostics.Debug.Assert(mode == CalendarViewDisplayMode.Year);
						headerName = FormatYearName(itemDate);
					}

					peer.Initialize(headerName, month, year, mode);

					m_previousHeaderPeer = m_currentHeaderPeer;
					m_currentHeaderPeer = peer;
				}

				global::System.Diagnostics.Debug.Assert(m_currentHeaderPeer is { } || m_previousHeaderPeer is { });

				var peerToUse =
					usePreviousHeaderPeer ? m_previousHeaderPeer : m_currentHeaderPeer;

				IRawElementProviderSimple provider;
				provider = peerToUse.ProviderFromPeer(peerToUse);

				ppReturnValue = new[] { provider };
				pReturnValueCount = 1;
			}

			return;
		}

		private void UpdateFlowDirectionForView()
		{
			if (m_tpViewsGrid is { } && m_tpMonthYearFormatter is { })
			{
				bool isRTL = false;
				{
					IReadOnlyList<string> spPatterns;
					spPatterns = m_tpMonthYearFormatter.Patterns;

					string strFormatPattern;
					strFormatPattern = spPatterns[0];
					if (strFormatPattern is { })
					{
						//uint length = 0;
						var buffer = strFormatPattern;
						isRTL = buffer[0] == RTL_CHARACTER_CODE;
					}
				}

				var flowDirection = isRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
				((Grid)m_tpViewsGrid).FlowDirection = flowDirection;
			}

			return;
		}
	}
}
