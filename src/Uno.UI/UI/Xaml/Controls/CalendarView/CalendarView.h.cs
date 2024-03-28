// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.Globalization;
using Windows.UI.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		// Public properties

		// Uno workaround => field has been converted to properties re-directing to DP (which has their default values)
		internal Brush m_pFocusBorderBrush => FocusBorderBrush;
		internal Brush m_pSelectedHoverBorderBrush => SelectedHoverBorderBrush;
		internal Brush m_pSelectedPressedBorderBrush => SelectedPressedBorderBrush;
		internal Brush m_pSelectedBorderBrush => SelectedBorderBrush;
		internal Brush m_pHoverBorderBrush => HoverBorderBrush;
		internal Brush m_pPressedBorderBrush => PressedBorderBrush;
		internal Brush m_pCalendarItemBorderBrush => CalendarItemBorderBrush;

		internal Brush m_pOutOfScopeBackground => OutOfScopeBackground;
		internal Brush m_pCalendarItemBackground => CalendarItemBackground;

		internal Brush m_pPressedForeground => PressedForeground;
		internal Brush m_pTodayForeground => TodayForeground;
		internal Brush m_pBlackoutForeground => BlackoutForeground;
		internal Brush m_pSelectedForeground => SelectedForeground;
		internal Brush m_pOutOfScopeForeground => OutOfScopeForeground;
		internal Brush m_pCalendarItemForeground => CalendarItemForeground;

		internal FontFamily m_pDayItemFontFamily => DayItemFontFamily;
		internal float m_dayItemFontSize => (float)DayItemFontSize;
		internal FontStyle m_dayItemFontStyle => DayItemFontStyle;
		internal FontWeight m_dayItemFontWeight => DayItemFontWeight;
		internal FontWeight m_todayFontWeight => TodayFontWeight;

		internal FontFamily m_pFirstOfMonthLabelFontFamily => FirstOfMonthLabelFontFamily;
		internal float m_firstOfMonthLabelFontSize => (float)FirstOfMonthLabelFontSize;
		internal FontStyle m_firstOfMonthLabelFontStyle => FirstOfMonthLabelFontStyle;
		internal FontWeight m_firstOfMonthLabelFontWeight => FirstOfMonthLabelFontWeight;

		internal FontFamily m_pMonthYearItemFontFamily => MonthYearItemFontFamily;
		internal float m_monthYearItemFontSize => (float)MonthYearItemFontSize;
		internal FontStyle m_monthYearItemFontStyle => MonthYearItemFontStyle;
		internal FontWeight m_monthYearItemFontWeight => MonthYearItemFontWeight;

		internal FontFamily m_pFirstOfYearDecadeLabelFontFamily => FirstOfYearDecadeLabelFontFamily;
		internal float m_firstOfYearDecadeLabelFontSize => (float)FirstOfYearDecadeLabelFontSize;
		internal FontStyle m_firstOfYearDecadeLabelFontStyle => FirstOfYearDecadeLabelFontStyle;
		internal FontWeight m_firstOfYearDecadeLabelFontWeight => FirstOfYearDecadeLabelFontWeight;


		internal HorizontalAlignment m_horizontalDayItemAlignment => HorizontalDayItemAlignment;
		internal VerticalAlignment m_verticalDayItemAlignment => VerticalDayItemAlignment;

		internal HorizontalAlignment m_horizontalFirstOfMonthLabelAlignment => HorizontalFirstOfMonthLabelAlignment;
		internal VerticalAlignment m_verticalFirstOfMonthLabelAlignment => VerticalFirstOfMonthLabelAlignment;

		internal Thickness m_calendarItemBorderThickness => CalendarItemBorderThickness;

		// Below brushes are Uno-specific in order to add more styling customization
		internal Brush m_pSelectedBackground => SelectedBackground;
		internal Brush m_pTodayBackground => TodayBackground ?? Resources[c_strTodayBackgroundStorage] as Brush;
		internal Brush m_pTodaySelectedBackground => TodaySelectedBackground;
		internal CornerRadius m_calendarItemCornerRadius => CalendarItemCornerRadius;
		internal CornerRadius m_dayItemCornerRadius => DayItemCornerRadius;


		// Below brushes are hardcoded and internal, we can make them public when needed.
		internal Brush m_pDisabledForeground;
		internal Brush m_pTodaySelectedInnerBorderBrush;
		internal Brush m_pTodayHoverBorderBrush;
		internal Brush m_pTodayPressedBorderBrush;
		internal Brush m_pTodayBlackoutBackground;


		//private IDictionary<uint, DependencyObject> m_colorToBrushMap;

		// hold the resource extension to keep the bindings for the internal properties.
		//private ThemeResourceExtension[] m_internalThemeResourceExtensions = new ThemeResourceExtension[6];


		internal DateTime Today { get { return m_today; } }

		internal Calendar Calendar { get { return m_tpCalendar; } }

		internal DateComparer DateComparer { get { return m_dateComparer; } }

		internal void SetKeyDownEventArgsFromCalendarItem(KeyRoutedEventArgs pArgs)
		{
			m_wrKeyDownEventArgsFromCalendarItem.SetTarget(pArgs);
		}

		internal bool IsMultipleEraCalendar { get { return m_isMultipleEraCalendar; } }
	}
}
