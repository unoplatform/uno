#nullable enable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

//TODO:MZ: Replace all the DateTime with DateTimeOffset?
partial class TimePickerFlyoutPresenter
{
	private const string UIA_AP_TIMEPICKER_HOURNAME = nameof(UIA_AP_TIMEPICKER_HOURNAME);
	private const string UIA_AP_TIMEPICKER_MINUTENAME = nameof(UIA_AP_TIMEPICKER_MINUTENAME);
	private const string UIA_AP_TIMEPICKER_PERIODNAME = nameof(UIA_AP_TIMEPICKER_PERIODNAME);

	// This is July 15th, 2011 as our sentinel date. There are no known
	//  daylight savings transitions that happened on that date.
	private const int TIMEPICKER_SENTINELDATE_YEAR = 2011;
	private const int TIMEPICKER_SENTINELDATE_MONTH = 7;
	private const int TIMEPICKER_SENTINELDATE_DAY = 15;
	private const int TIMEPICKER_SENTINELDATE_TIMEFIELDS = 0;
	private const int TIMEPICKER_SENTINELDATE_HOUR12 = 12;
	private const int TIMEPICKER_SENTINELDATE_HOUR24 = 0;
	private const int TIMEPICKER_COERCION_INDEX = 0;
	//private const int TIMEPICKER_AM_INDEX = 0;
	//private const int TIMEPICKER_PM_INDEX = 1;
	private const int TIMEPICKER_RTL_CHARACTER_CODE = 8207;
	private const int TIMEPICKER_MINUTEINCREMENT_MIN = 0;
	private const int TIMEPICKER_MINUTEINCREMENT_MAX = 59;

	// When the minute increment is set to 0, we want to only have 00 at the minute picker. This
	// can be easily obtained by treating 0 as 60 with our existing logic. So during our logic, if we see
	// that minute increment is zero we will use 60 in our calculations.
	private const int TIMEPICKER_MINUTEINCREMENT_ZERO_REPLACEMENT = 60;

	private const string _hourLoopingSelectorAutomationId = "HourLoopingSelector";
	private const string _minuteLoopingSelectorAutomationId = "MinuteLoopingSelector";
	private const string _periodLoopingSelectorAutomationId = "PeriodLoopingSelector";

	internal const string _strTwelveHourClock = "12HourClock";
	private const string _strHourFormat = "{hour.integer(1)}";
	private const string _strMinuteFormat = "{minute.integer(2)}";
	private const string _strPeriodFormat = "{period.abbreviated(2)}";
	private const long _timeSpanTicksPerMinute = TimeSpan.TicksPerMinute;
	private const long _timeSpanTicksPerHour = TimeSpan.TicksPerHour;
	private const long _timeSpanTicksPerDay = TimeSpan.TicksPerDay;
	internal const int _periodCoercionOffset = 12;
	private const string _firstPickerHostName = "FirstPickerHost";
	private const string _secondPickerHostName = "SecondPickerHost";
	private const string _thirdPickerHostName = "ThirdPickerHost";
	private const string _backgroundName = "Background";
	private const string _contentPanelName = "ContentPanel";
	private const string _titlePresenterName = "TitlePresenter";

	internal TimePickerFlyoutPresenter()
	{
		DefaultStyleKey = typeof(TimePickerFlyoutPresenter);
		_is12HourClock = false;
		_reactionToSelectionChangeAllowed = false;
		_minuteIncrement = 0;
		_time = default;
		_acceptDismissButtonsVisible = true;
		InitializeImpl();
	}

	private void InitializeImpl()
	{
		//    IControlFactory> spInnerFactory;
		//    IControl> spInnerInstance;
		//    object> spInnerInspectable;

		//    TimePickerFlyoutPresenterGenerated.InitializeImpl();

		//    (wf.GetActivationFactory(
		//        stringReference(RuntimeClass_Microsoft_UI_Xaml_Controls_Control),
		//        &spInnerFactory));

		//    (spInnerFactory.CreateInstance(
		//        (object)((ITimePickerFlyoutPresenter*)(this)),
		//        &spInnerInspectable,
		//        &spInnerInstance));

		//    (SetComposableBasePointers(
		//            spInnerInspectable,
		//            spInnerFactory));

		DefaultStyleKey = typeof(TimePickerFlyoutPresenter);
	}

	protected override void OnApplyTemplate()
	{
		if (_tpMinutePicker is not null)
		{
			_minuteSelectionChangedToken.Disposable = null;
		}

		if (_tpHourPicker is not null)
		{
			_hourSelectionChangedToken.Disposable = null;
		}

		if (_tpPeriodPicker is not null)
		{
			_periodSelectionChangedToken.Disposable = null;
		}

		_tpTitlePresenter = null;
		_tpMinutePicker = null;
		_tpHourPicker = null;
		_tpPeriodPicker = null;
		_tpFirstPickerHost = null;
		_tpSecondPickerHost = null;
		_tpThirdPickerHost = null;
		_tpBackgroundBorder = null;
		_tpContentPanel = null;
		_tpAcceptDismissHostGrid = null;
		_tpAcceptButton = null;
		_tpDismissButton = null;

		base.OnApplyTemplate();

		_tpBackgroundBorder = this.GetTemplateChild<Border>(_backgroundName);
		_tpTitlePresenter = GetTemplateChild<TextBlock>(_titlePresenterName);
		if (_tpTitlePresenter is not null)
		{
			_tpTitlePresenter.Visibility = string.IsNullOrEmpty(_title) ? Visibility.Collapsed : Visibility.Visible;
			_tpTitlePresenter.Text = _title;
		}

		_tpFirstPickerHost = GetTemplateChild<Border>(_firstPickerHostName);
		_tpSecondPickerHost = GetTemplateChild<Border>(_secondPickerHostName);
		_tpThirdPickerHost = GetTemplateChild<Border>(_thirdPickerHostName);
		_tpContentPanel = GetTemplateChild<FrameworkElement>(_contentPanelName);

		_tpFirstPickerHostColumn = GetTemplateChild<ColumnDefinition>("FirstPickerHostColumn");
		_tpSecondPickerHostColumn = GetTemplateChild<ColumnDefinition>("SecondPickerHostColumn");
		_tpThirdPickerHostColumn = GetTemplateChild<ColumnDefinition>("ThirdPickerHostColumn");

		_tpFirstPickerSpacing = GetTemplateChild<UIElement>("FirstPickerSpacing");
		_tpSecondPickerSpacing = GetTemplateChild<UIElement>("SecondPickerSpacing");
		_tpAcceptDismissHostGrid = GetTemplateChild<UIElement>("AcceptDismissHostGrid");
		_tpAcceptButton = GetTemplateChild<UIElement>("AcceptButton");
		_tpDismissButton = GetTemplateChild<UIElement>("DismissButton");

		int itemHeight;
		if (Application.Current.Resources.Lookup("TimePickerFlyoutPresenterItemHeight") is double itemHeightFromMarkup)
		{
			itemHeight = (int)itemHeightFromMarkup;
		}
		else
		{
			// Value for RS4. Used if resource values not found
			itemHeight = 44;
		}

		if (Application.Current.Resources.Lookup("TimePickerFlyoutPresenterItemPadding") is not Thickness itemPadding)
		{
			itemPadding = new Thickness(0, 3, 0, 5);
		}

		if (_tpFirstPickerHost is not null)
		{
			LoopingSelector spHourPicker = new();

			_tpHourPicker = spHourPicker;

			//Don't set ItemWidth. We want the item to size to the width of its parent.
			spHourPicker.ItemHeight = itemHeight;
			spHourPicker.Padding = itemPadding;
			spHourPicker.HorizontalContentAlignment = HorizontalAlignment.Center;

			_tpFirstPickerHost.Child = spHourPicker;
		}

		if (_tpSecondPickerHost is not null)
		{
			LoopingSelector spMinutePicker = new();

			_tpMinutePicker = spMinutePicker;

			//Don't set ItemWidth. We want the item to size to the width of its parent.
			spMinutePicker.ItemHeight = itemHeight;
			spMinutePicker.Padding = itemPadding;
			spMinutePicker.HorizontalContentAlignment = HorizontalAlignment.Center;

			_tpSecondPickerHost.Child = spMinutePicker;
		}

		if (_tpThirdPickerHost is not null)
		{
			LoopingSelector spPeriodPicker = new();

			_tpPeriodPicker = spPeriodPicker;

			spPeriodPicker.ShouldLoop = false;

			spPeriodPicker.ItemHeight = itemHeight;
			spPeriodPicker.Padding = itemPadding;
			spPeriodPicker.HorizontalContentAlignment = HorizontalAlignment.Center;

			_tpThirdPickerHost.Child = spPeriodPicker;
		}

		if (_tpHourPicker is not null)
		{
			_tpHourPicker.SelectionChanged += OnSelectorSelectionChanged;
			_hourSelectionChangedToken.Disposable = Disposable.Create(() => _tpHourPicker.SelectionChanged -= OnSelectorSelectionChanged);

			string localizedName = ResourceAccessor.GetLocalizedStringResource(UIA_AP_TIMEPICKER_HOURNAME);
			AutomationHelper.SetElementAutomationName(_tpHourPicker, localizedName);
			AutomationHelper.SetElementAutomationId(_tpHourPicker, _hourLoopingSelectorAutomationId);
		}
		if (_tpMinutePicker is not null)
		{
			_tpMinutePicker.SelectionChanged += OnSelectorSelectionChanged;
			_minuteSelectionChangedToken.Disposable = Disposable.Create(() => _tpMinutePicker.SelectionChanged -= OnSelectorSelectionChanged);

			string localizedName = ResourceAccessor.GetLocalizedStringResource(UIA_AP_TIMEPICKER_MINUTENAME);
			AutomationHelper.SetElementAutomationName(_tpMinutePicker, localizedName);
			AutomationHelper.SetElementAutomationId(_tpMinutePicker, _minuteLoopingSelectorAutomationId);
		}
		if (_tpPeriodPicker is not null)
		{
			_tpPeriodPicker.SelectionChanged += OnSelectorSelectionChanged;
			_periodSelectionChangedToken.Disposable = Disposable.Create(() => _tpPeriodPicker.SelectionChanged -= OnSelectorSelectionChanged);

			string localizedName = ResourceAccessor.GetLocalizedStringResource(UIA_AP_TIMEPICKER_PERIODNAME);

			AutomationHelper.SetElementAutomationName(_tpPeriodPicker, localizedName);
			AutomationHelper.SetElementAutomationId(_tpPeriodPicker, _periodLoopingSelectorAutomationId);
		}

		Initialize();
		RefreshSetup();
		SetAcceptDismissButtonsVisibility(_acceptDismissButtonsVisible);

		// Apply a shadow
		bool isDefaultShadowEnabled = IsDefaultShadowEnabled;
		if (isDefaultShadowEnabled)
		{
			var isDropShadowMode = ThemeShadow.IsDropShadowMode;
			UIElement? spShadowTarget = null;
			if (isDropShadowMode)
			{
				spShadowTarget = this;
			}
			else
			{
				spShadowTarget = _tpBackgroundBorder;
			}
			ApplyElevationEffect(_tpBackgroundBorder);
		}
	}

	private void ApplyElevationEffect(Border border)
	{
		//TODO:MZ:
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new TimePickerFlyoutPresenterAutomationPeer(this);

	internal void PullPropertiesFromOwner(TimePickerFlyout pOwner)
	{
		TimePickerFlyout spOwner = pOwner;

		int oldMinuteIncrement = _minuteIncrement;
		string? oldClockID = _clockIdentifier;

		// Pull properties from owner
		var clockIdentifier = spOwner.ClockIdentifier;
		var title = PickerFlyoutBase.GetTitle(spOwner);
		var minuteIncrement = spOwner.MinuteIncrement;
		var time = spOwner.Time;

		// Check which values have changed
		var clockIdChanged = !string.Equals(oldClockID, clockIdentifier, StringComparison.Ordinal); //TODO:MZ: Ignore case?

		// Store new values
		_clockIdentifier = clockIdentifier;
		_title = title;
		if (_tpTitlePresenter is not null)
		{
			_tpTitlePresenter.Visibility = string.IsNullOrEmpty(_title) ? Visibility.Collapsed : Visibility.Visible;
			_tpTitlePresenter.Text = _title;
		}
		_minuteIncrement = minuteIncrement;

		// Perform updates
		if (clockIdChanged)
		{
			OnClockIdentifierChanged(oldClockID);
		}

		if (oldMinuteIncrement != _minuteIncrement)
		{
			OnMinuteIncrementChanged(oldMinuteIncrement, _minuteIncrement);
		}

		// Time has its own handler since it can be set through multiple codepaths
		SetTime(time);
	}

	internal void SetAcceptDismissButtonsVisibility(bool isVisible)
	{
		// If we have a named host grid for the buttons, we'll hide that.
		// Otherwise, we'll just hide the buttons, since we shouldn't
		// assume anything about the surrounding visual tree.
		if (_tpAcceptDismissHostGrid is not null)
		{
			_tpAcceptDismissHostGrid.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
		else if (_tpAcceptButton is not null && _tpDismissButton is not null)
		{
			_tpAcceptButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
			_tpDismissButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}

		_acceptDismissButtonsVisible = isVisible;
	}

	internal TimeSpan GetTime() => _time;

	private void SetTime(TimeSpan newTime)
	{
		// If we're setting the time to the null sentinel value,
		// we'll instead set it to the current time for the purposes
		// of where to place the user's position in the looping selectors.
		if (newTime.Ticks == -1)
		{
			DateTimeOffset dateTime = default;
			Calendar calendar = CreateNewCalendar(_clockIdentifier);
			calendar.SetToNow();
			dateTime = calendar.GetDateTime();
			newTime = GetTimeSpanFromDateTime(dateTime);
		}

		if (_time.Ticks != newTime.Ticks)
		{
			_time = newTime;
			OnTimeChanged();
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		DateTimePickerFlyoutHelper.OnKeyDownImpl(e, _tpFirstPickerAsControl, _tpSecondPickerAsControl, _tpThirdPickerAsControl, _tpContentPanel);
	}

	private void Initialize()
	{
		if (!(_tpMinuteSource is not null && _tpHourSource is not null && _tpPeriodSource is not null))
		{
			_tpMinuteSource = new List<object>(); //TODO:MZ: Should be ObservableCollection/TrckableCollection?
			_tpHourSource = new List<object>();
			_tpPeriodSource = new List<object>();
		}

		RefreshSetup();
	}

	// Reacts to change in MinuteIncrement property.
	private void OnMinuteIncrementChanged(int oldValue, int newValue)
	{
		if (newValue < TIMEPICKER_MINUTEINCREMENT_MIN || newValue > TIMEPICKER_MINUTEINCREMENT_MAX)
		{
			// revert the change
			_minuteIncrement = oldValue;
			throw new ArgumentOutOfRangeException(nameof(newValue));
		}

		RefreshSetup();
	}

	// Reacts to change in ClockIdentifier property.
	private void OnClockIdentifierChanged(string oldValue)
	{
		try
		{
			RefreshSetup();
		}
		catch
		{
			// revert the change
			_clockIdentifier = oldValue;
			RefreshSetup();
#if DEBUG
			throw;
#endif
		}
	}


	// Reacts to changes in Time property. Time may have changed programmatically or end user may have changed the
	// selection of one of our selectors causing a change in Time.
	private void OnTimeChanged()
	{
		var coercedTime = CheckAndCoerceTime(_time);

		// We are checking to see if new value is different from the current one. This is because even if they are same,
		// calling put_Time will break any Binding on Time (if there is any) that this TimePicker is target of.
		if (_time.Ticks != coercedTime.Ticks)
		{
			// We are coercing the time. The new property change will execute the necessary logic so
			// we will just go to cleanup after this call.
			SetTime(coercedTime);
			return;
		}

		UpdateDisplay();
	}


	// Checks whether the given time is in our acceptable range, coerces it or raises exception when necessary.
	private TimeSpan CheckAndCoerceTime(TimeSpan time)
	{
		TimeSpan coercedTime = default;
		DateTimeOffset dateTime = default;
		int minute = 0;
		int minuteIncrement = 0;

		// Check the value of time, we do not accept negative timespan values
		if (time.Ticks < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(time));
		}

		// If the time's duration is greater than 24 hours, we coerce it down to 24 hour range
		// by taking mod of it.
		coercedTime = new TimeSpan(time.Ticks % _timeSpanTicksPerDay);

		// Finally we coerce the minutes to a factor of MinuteIncrement
		minuteIncrement = GetAdjustedMinuteIncrement();
		dateTime = GetDateTimeFromTimeSpan(coercedTime);
		_tpCalendar!.SetDateTime(dateTime);
		minute = _tpCalendar.Minute;
		_tpCalendar.Minute = minute - (minute % minuteIncrement);
		dateTime = _tpCalendar.GetDateTime();
		return GetTimeSpanFromDateTime(dateTime);
	}


	// Reacts to change in selection of our selectors. Calculates the new date represented by the selected indices and updates the
	// Date property.
	private void OnSelectorSelectionChanged(object pSender, SelectionChangedEventArgs pArgs)
	{
		if (IsReactionToSelectionChangeAllowed())
		{
			UpdateTime();
		}
	}

	// Updates the Time property according to the selected indices of the selectors.
	private void UpdateTime()
	{
		int hourIndex = 0;
		int minuteIndex = 0;
		int minuteIncrement = 0;
		int periodIndex = 0;

		if (_tpHourPicker is not null)
		{
			hourIndex = _tpHourPicker.SelectedIndex;
		}

		if (_tpMinutePicker is not null)
		{
			minuteIndex = _tpMinutePicker.SelectedIndex;
		}

		if (_tpPeriodPicker is not null)
		{
			periodIndex = _tpPeriodPicker.SelectedIndex;
		}

		SetSentinelDate(_tpCalendar!);

		if (_is12HourClock)
		{
			var firstPeriodInThisDay = _tpCalendar!.FirstPeriodInThisDay;
			_tpCalendar.Period = periodIndex + firstPeriodInThisDay;
			// 12 hour clock time flow is 12, 1, 2, 3 ... 11 for both am and pm times. So if the index is 0 we need
			// to put hour 12 into hour calendar.
			if (hourIndex == TIMEPICKER_COERCION_INDEX)
			{
				_tpCalendar.Hour = _periodCoercionOffset;
			}
			else
			{
				_tpCalendar.Hour = hourIndex;
			}
		}
		else
		{
			_tpCalendar!.Hour = hourIndex;
		}

		minuteIncrement = GetAdjustedMinuteIncrement();
		_tpCalendar.Minute = minuteIncrement * minuteIndex;

		var dateTime = _tpCalendar!.GetDateTime();
		var timeSpan = GetTimeSpanFromDateTime(dateTime);

		// If there is not any pickers changing time will not mean anything semantically.
		if (_tpHourPicker is not null || _tpMinutePicker is not null || _tpPeriodPicker is not null)
		{
			// We are checking to see if new value is different from the current one. This is because even if they are same,
			// calling put_Time will break any Binding on Time (if there is any) that this TimePicker is target of.
			if (_time.Ticks != timeSpan.Ticks)
			{
				SetTime(timeSpan);
			}
		}
	}


	// Creates a new Calendar, taking into account the ClockIdentifier
	private Calendar CreateNewCalendar(string strClockIdentifier)
	{
		var spCalendar = new Calendar();
		var spLanguages = spCalendar.Languages;

		//Create the calendar

		spCalendar = new Calendar(
			spLanguages, /* Languages*/
			"GregorianCalendar", /* Calendar */
			strClockIdentifier); /* Clock */

		return spCalendar;
	}

	// Creates a new DateTimeFormatter with the given format and clock identifier.
	private DateTimeFormatter CreateNewFormatterWithClock(string strFormat, string strClockIdentifier)
	{
		var spFormatter = new DateTimeFormatter(strFormat);

		var strGeographicRegion = spFormatter.GeographicRegion;
		var spLanguages = spFormatter.Languages;
		var strCalendarSystem = spFormatter.Calendar;

		return new DateTimeFormatter(
			strFormat,/* Format string */
			spLanguages, /* Languages*/
			strGeographicRegion, /* Geographic region */
			strCalendarSystem, /* Calendar */
			strClockIdentifier); /* Clock */
	}

	// Creates a new DateTimeFormatter with the given format and calendar identifier.
	private DateTimeFormatter CreateNewFormatterWithCalendar(string strFormat, string strCalendarIdentifier)
	{
		var spFormatter = new DateTimeFormatter(strFormat);

		var strGeographicRegion = spFormatter.GeographicRegion;
		var spLanguages = spFormatter.Languages;
		var strClock = spFormatter.Clock;

		return new DateTimeFormatter(
			strFormat,/* Format string */
			spLanguages, /* Languages*/
			strGeographicRegion, /* Geographic region */
			strCalendarIdentifier, /* Calendar */
			strClock); /* Clock */
	}

	//private DateTimeFormatter GetTimeFormatter(string strClockIdentifier)
	//{
	//	// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
	//	// and cached formatter's clock identifier is the same as the new one's.
	//	if (!(_tpTimeFormatter is not null
	//		&& strClockIdentifier == _strTimeFormatterClockIdentifier))
	//	{
	//		// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
	//		// with its identifying info.
	//		_tpTimeFormatter = null;
	//		var spFormatter = CreateNewFormatterWithClock(
	//			"shorttime",
	//			strClockIdentifier);
	//		_tpTimeFormatter = spFormatter;
	//		_strTimeFormatterClockIdentifier = strClockIdentifier;
	//	}

	//	return _tpTimeFormatter;
	//}

	// Sets our sentinel date to the given calendar. This date is 21st of July 2011 midnight.
	// On this day there are no known daylight saving transitions.
	private void SetSentinelDate(Calendar pCalendar)
	{
		pCalendar.Year = TIMEPICKER_SENTINELDATE_YEAR;
		pCalendar.Month = TIMEPICKER_SENTINELDATE_MONTH;
		pCalendar.Day = TIMEPICKER_SENTINELDATE_DAY;

		if (_is12HourClock)
		{
			int firstPeriodInThisDay = 0;

			firstPeriodInThisDay = pCalendar.FirstPeriodInThisDay;
			pCalendar.Period = firstPeriodInThisDay;
			pCalendar.Hour = TIMEPICKER_SENTINELDATE_HOUR12;
		}
		else
		{
			pCalendar.Hour = TIMEPICKER_SENTINELDATE_HOUR24;
		}
		pCalendar.Minute = TIMEPICKER_SENTINELDATE_TIMEFIELDS;
		pCalendar.Second = TIMEPICKER_SENTINELDATE_TIMEFIELDS;
		pCalendar.Nanosecond = TIMEPICKER_SENTINELDATE_TIMEFIELDS;
	}

	// Generate the collection that we will populate our hour picker with.
	private void GenerateHours()
	{
		int firstHourInThisPeriod = 0;
		int numberOfHours = 0;

		var spFormatter = CreateNewFormatterWithClock(_strHourFormat, _clockIdentifier);

		SetSentinelDate(_tpCalendar!);
		numberOfHours = _tpCalendar!.NumberOfHoursInThisPeriod;
		firstHourInThisPeriod = _tpCalendar.FirstHourInThisPeriod;
		_tpCalendar.Hour = firstHourInThisPeriod;

		_tpHourSource!.Clear();

		for (int hourOffset = 0; hourOffset < numberOfHours; hourOffset++)
		{
			DatePickerFlyoutItem spItem = new();

			var dateTime = _tpCalendar.GetDateTime();
			var strHour = spFormatter.Format(dateTime);

#if HAS_UNO // Because DateTimeFormatter does not match WinAppSDK it is returning 0 for 12-hour clock hour 12. #22207
			// In 12-hour clock, hour 0 should display as 12.
			if (_is12HourClock && strHour == $"{TIMEPICKER_COERCION_INDEX}")
			{
				strHour = $"{_periodCoercionOffset}";
			}
#endif
			spItem.PrimaryText = strHour;

			_tpHourSource.Add(spItem);

			_tpCalendar.AddHours(1);
		}
	}

	// Generate the collection that we will populate our minute picker with.
	private void GenerateMinutes()
	{
		var spFormatter = CreateNewFormatterWithClock(_strMinuteFormat, _clockIdentifier);
		SetSentinelDate(_tpCalendar!);
		var minuteIncrement = GetAdjustedMinuteIncrement();
		var lastMinute = _tpCalendar!.LastMinuteInThisHour;
		var firstMinuteInThisHour = _tpCalendar.FirstMinuteInThisHour;

		_tpMinuteSource!.Clear();

		for (int i = firstMinuteInThisHour; i <= lastMinute / minuteIncrement; i++)
		{
			DatePickerFlyoutItem spItem = new DatePickerFlyoutItem();

			_tpCalendar.Minute = i * minuteIncrement;

			var dateTime = _tpCalendar.GetDateTime();
			var strMinute = spFormatter.Format(dateTime);

			spItem.PrimaryText = strMinute;

			_tpMinuteSource.Add(spItem);
		}
	}

	// Generate the collection that we will populate our period picker with.
	private void GeneratePeriods()
	{
		var spFormatter = CreateNewFormatterWithClock(_strPeriodFormat, _clockIdentifier);
		SetSentinelDate(_tpCalendar!);

		_tpPeriodSource!.Clear();

		var firstPeriodInThisDay = _tpCalendar!.FirstPeriodInThisDay;
		_tpCalendar.Period = firstPeriodInThisDay;
		var dateTime = _tpCalendar.GetDateTime();
		var strPeriod = spFormatter.Format(dateTime);

		DatePickerFlyoutItem spItem = new();
		spItem.PrimaryText = strPeriod;
		_tpPeriodSource.Add(spItem);

		_tpCalendar.AddPeriods(1);
		dateTime = _tpCalendar.GetDateTime();
		strPeriod = spFormatter.Format(dateTime);

		spItem = new DatePickerFlyoutItem();
		spItem.PrimaryText = strPeriod;
		_tpPeriodSource.Add(spItem);
	}

	// Clears the ItemsSource  properties of the selectors.
	private void ClearSelectors()
	{
		if (_tpHourPicker is not null)
		{
			_tpHourPicker.Items = null;
		}

		if (_tpMinutePicker is not null)
		{
			_tpMinutePicker.Items = null;
		}

		if (_tpPeriodPicker is not null)
		{
			_tpPeriodPicker.Items = null;
		}
	}

	// Gets the layout ordering of the selectors.
	internal void GetOrder(
		out int hourOrder,
		out int minuteOrder,
		out int periodOrder,
		out bool isRTL)
	{
		// Default ordering
		hourOrder = 0;
		minuteOrder = 1;
		periodOrder = 2;

		var spFormatterWithCalendar = CreateNewFormatterWithCalendar("month.full", "GregorianCalendar");
		var spPatterns = spFormatterWithCalendar.Patterns;
		var strPattern = spPatterns[0];

		var szPattern = strPattern;

		isRTL = szPattern[0] == TIMEPICKER_RTL_CHARACTER_CODE;

		var spFormatterWithClock = CreateNewFormatterWithClock("hour minute", _clockIdentifier);
		spPatterns = spFormatterWithClock.Patterns;
		strPattern = spPatterns[0];

		if (strPattern is not null)
		{
			szPattern = strPattern;

			// We do string search to determine the order of the fields.
			var hourOccurence = szPattern.IndexOf("{hour", StringComparison.InvariantCulture);
			var minuteOccurence = szPattern.IndexOf("{minute", StringComparison.InvariantCulture);
			var periodOccurence = szPattern.IndexOf("{period", StringComparison.InvariantCulture);

#if HAS_UNO // Because DateTimeFormatter does not match WinAppSDK, period may be ommitted even if clock identifier is set. #19349
			periodOccurence = periodOccurence == -1 ? int.MaxValue : periodOccurence;
#endif

			if (hourOccurence < minuteOccurence)
			{
				if (hourOccurence < periodOccurence)
				{
					hourOrder = 0;
					if (minuteOccurence < periodOccurence)
					{
						minuteOrder = 1;
						periodOrder = 2;
					}
					else
					{
						minuteOrder = 2;
						periodOrder = 1;
					}
				}
				else
				{
					hourOrder = 1;
					minuteOrder = 2;
					periodOrder = 0;
				}
			}
			else
			{
				if (hourOccurence < periodOccurence)
				{
					hourOrder = 1;
					minuteOrder = 0;
					periodOrder = 2;
				}
				else
				{
					hourOrder = 2;
					if (minuteOccurence < periodOccurence)
					{
						minuteOrder = 0;
						periodOrder = 1;
					}
					else
					{

						minuteOrder = 1;
						periodOrder = 0;
					}
				}
			}

			// Thi is a trick we are mimicking from from our js counterpart. In rtl languages if we just naively lay out our pickers right-to-left
			// it will not be correct. Say our LTR layout is HH MM PP, when we just lay it out RTL it will be PP MM HH, however we actually want
			// our pickers to be laid out as PP HH MM as hour and minute fields are english numerals and they should be laid out LTR. So we are
			// preempting our lay out mechanism and swapping hour and minute pickers, thus the final lay out will be correct.
			if (isRTL)
			{
				(hourOrder, minuteOrder) = (minuteOrder, hourOrder);
			}
		}
	}

	// Updates the order of selectors in our layout. Also takes care of hiding/showing the selectors and related spacing depending our
	// public properties set by the user.
	private void UpdateOrderAndLayout()
	{
		bool firstHostPopulated = false;
		bool secondHostPopulated = false;
		bool thirdHostPopulated = false;
		GridLength starGridLength = default;
		GridLength zeroGridLength = default;
		Control? firstPickerAsControl = null;
		Control? secondPickerAsControl = null;
		Control? thirdPickerAsControl = null;

		_tpFirstPickerAsControl = null;
		_tpSecondPickerAsControl = null;
		_tpThirdPickerAsControl = null;

		zeroGridLength.GridUnitType = GridUnitType.Pixel;
		zeroGridLength.Value = 0.0;
		starGridLength.GridUnitType = GridUnitType.Star;
		starGridLength.Value = 1.0;

		GetOrder(out var hourOrder, out var minuteOrder, out var periodOrder, out var isRTL);

		if (_tpContentPanel is not null)
		{
			_tpContentPanel.FlowDirection = isRTL ?
				FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		}

		// Clear the children of hosts first, so we never risk putting one picker in two hosts and failing.
		if (_tpFirstPickerHost is not null)
		{
			_tpFirstPickerHost.Child = null;
		}
		if (_tpSecondPickerHost is not null)
		{
			_tpSecondPickerHost.Child = null;
		}
		if (_tpThirdPickerHost is not null)
		{
			_tpThirdPickerHost.Child = null;
		}

		// Assign the selectors to the hosts.
		switch (hourOrder)
		{
			case 0:
				if (_tpFirstPickerHost is not null && _tpHourPicker is not null)
				{
					_tpFirstPickerHost.Child = _tpHourPicker;
					firstPickerAsControl = _tpHourPicker;
					firstHostPopulated = true;
				}
				break;
			case 1:
				if (_tpSecondPickerHost is not null && _tpHourPicker is not null)
				{
					_tpSecondPickerHost.Child = _tpHourPicker;
					secondPickerAsControl = _tpHourPicker;
					secondHostPopulated = true;
				}
				break;
			case 2:
				if (_tpThirdPickerHost is not null && _tpHourPicker is not null)
				{
					_tpThirdPickerHost.Child = _tpHourPicker;
					thirdPickerAsControl = _tpHourPicker;
					thirdHostPopulated = true;
				}
				break;
		}

		switch (minuteOrder)
		{
			case 0:
				if (_tpFirstPickerHost is not null && _tpMinutePicker is not null)
				{
					_tpFirstPickerHost.Child = _tpMinutePicker;
					firstPickerAsControl = _tpMinutePicker;
					firstHostPopulated = true;
				}
				break;
			case 1:
				if (_tpSecondPickerHost is not null && _tpMinutePicker is not null)
				{
					_tpSecondPickerHost.Child = _tpMinutePicker;
					secondPickerAsControl = _tpMinutePicker;
					secondHostPopulated = true;
				}
				break;
			case 2:
				if (_tpThirdPickerHost is not null && _tpMinutePicker is not null)
				{
					_tpThirdPickerHost.Child = _tpMinutePicker;
					thirdPickerAsControl = _tpMinutePicker;
					thirdHostPopulated = true;
				}
				break;
		}

		switch (periodOrder)
		{
			case 0:
				if (_tpFirstPickerHost is not null && _tpPeriodPicker is not null && _is12HourClock)
				{
					_tpFirstPickerHost.Child = _tpPeriodPicker;
					firstPickerAsControl = _tpPeriodPicker;
					firstHostPopulated = true;
				}
				break;
			case 1:
				if (_tpSecondPickerHost is not null && _tpPeriodPicker is not null && _is12HourClock)
				{
					_tpSecondPickerHost.Child = _tpPeriodPicker;
					secondPickerAsControl = _tpPeriodPicker;
					secondHostPopulated = true;
				}
				break;
			case 2:
				if (_tpThirdPickerHost is not null && _tpPeriodPicker is not null && _is12HourClock)
				{
					_tpThirdPickerHost.Child = _tpPeriodPicker;
					thirdPickerAsControl = _tpPeriodPicker;
					thirdHostPopulated = true;
				}
				break;
		}

		_tpFirstPickerAsControl = firstPickerAsControl;
		_tpSecondPickerAsControl = secondPickerAsControl;
		_tpThirdPickerAsControl = thirdPickerAsControl;

		if (_tpFirstPickerHost is not null)
		{
			_tpFirstPickerHost.Visibility = firstHostPopulated ?
				Visibility.Visible : Visibility.Collapsed;
			if (_tpFirstPickerHostColumn is not null)
			{
				_tpFirstPickerHostColumn.Width = firstHostPopulated ? starGridLength : zeroGridLength;
			}

		}
		if (_tpSecondPickerHost is not null)
		{
			_tpSecondPickerHost.Visibility = secondHostPopulated ?
				Visibility.Visible : Visibility.Collapsed;
			if (_tpSecondPickerHostColumn is not null)
			{
				_tpSecondPickerHostColumn.Width = secondHostPopulated ? starGridLength : zeroGridLength;
			}
		}
		if (_tpThirdPickerHost is not null)
		{
			_tpThirdPickerHost.Visibility = thirdHostPopulated ?
				Visibility.Visible : Visibility.Collapsed;
			if (_tpThirdPickerHostColumn is not null)
			{
				_tpThirdPickerHostColumn.Width = thirdHostPopulated ? starGridLength : zeroGridLength;
			}
		}

		if (_tpHourPicker is not null)
		{
			_tpHourPicker.TabIndex = hourOrder;
		}
		if (_tpMinutePicker is not null)
		{
			_tpMinutePicker.TabIndex = minuteOrder;
		}
		if (_tpPeriodPicker is not null)
		{
			_tpPeriodPicker.TabIndex = periodOrder;
		}

		// Determine if we will show the dividers and assign visibilities to them. We will determine if the dividers
		// are shown by looking at which borders are populated.
		if (_tpFirstPickerSpacing is not null)
		{
			_tpFirstPickerSpacing.Visibility =
				firstHostPopulated && (secondHostPopulated || thirdHostPopulated) ?
				Visibility.Visible : Visibility.Collapsed;
		}
		if (_tpSecondPickerSpacing is not null)
		{
			_tpSecondPickerSpacing.Visibility =
				secondHostPopulated && thirdHostPopulated ?
				Visibility.Visible : Visibility.Collapsed;
		}
	}

	// Updates the selector selected indices to display our Time property.
	private void UpdateDisplay()
	{
		try
		{
			int hour = 0;
			int minuteIncrement = 0;
			int minute;
			int period = 0;
			int firstPeriodInThisDay = 0;
			int firstMinuteInThisHour = 0;
			int firstHourInThisPeriod = 0;

			PreventReactionToSelectionChange();

			var dateTime = GetDateTimeFromTimeSpan(_time);
			_tpCalendar!.SetDateTime(dateTime);

			// Calculate the period index and set it
			if (_is12HourClock)
			{
				period = _tpCalendar.Period;
				firstPeriodInThisDay = _tpCalendar.FirstPeriodInThisDay;
				if (_tpPeriodPicker is not null)
				{
					_tpPeriodPicker.SelectedIndex = period - firstPeriodInThisDay;
				}
			}

			// Calculate the hour index and set it
			hour = _tpCalendar.Hour;
			firstHourInThisPeriod = _tpCalendar.FirstHourInThisPeriod;
			if (_is12HourClock)
			{
				// For 12 hour clock 12 am and 12 pm are always the first element (index 0) in hour picker.
				// Other hours translate directly to indices. So it is sufficient to make a mod operation while translating
				// hour to index.
				if (_tpHourPicker is not null)
				{
					_tpHourPicker.SelectedIndex = hour % _periodCoercionOffset;
				}
			}
			else
			{
				// For 24 hour clock, Hour translates exactly to the hour picker's selected index.
				if (_tpHourPicker is not null)
				{
					_tpHourPicker.SelectedIndex = hour;
				}
			}

			// Calculate the minute index and set it
			minuteIncrement = GetAdjustedMinuteIncrement();
			minute = _tpCalendar.Minute;
			firstMinuteInThisHour = _tpCalendar.FirstMinuteInThisHour;
			if (_tpMinutePicker is not null)
			{
				_tpMinutePicker.SelectedIndex = (minute / minuteIncrement - firstMinuteInThisHour);
			}
		}
		finally
		{
			AllowReactionToSelectionChange();
		}
	}

	// Clears everything, generates and sets the itemssources to selectors.
	private void RefreshSetup()
	{
		try
		{
			Calendar spCalendar;

			PreventReactionToSelectionChange();

			_is12HourClock = _clockIdentifier == _strTwelveHourClock;

			spCalendar = CreateNewCalendar(_clockIdentifier);
			SetSentinelDate(spCalendar);

			// Clock identifier change may have rendered _tpCalendar stale.
			spCalendar = CreateNewCalendar(_clockIdentifier);
			_tpCalendar = spCalendar;

			ClearSelectors();
			UpdateOrderAndLayout();

			if (_tpHourPicker is not null)
			{
				GenerateHours();
				_tpHourPicker.Items = _tpHourSource;
			}

			if (_tpMinutePicker is not null)
			{
				GenerateMinutes();
				_tpMinutePicker.Items = _tpMinuteSource;
				// If MinuteIncrement is zero, we don't want the minutes column to loop.
				_tpMinutePicker.ShouldLoop = _minuteIncrement != 0;
			}

			if (_tpPeriodPicker is not null)
			{
				GeneratePeriods();
				_tpPeriodPicker.Items = _tpPeriodSource;
			}

			UpdateDisplay();
			UpdateTime();
		}
		finally
		{
			AllowReactionToSelectionChange();
		}
	}

	// Translates ONLY the hour and minute fields of DateTime into TimeSpan.
	private TimeSpan GetTimeSpanFromDateTime(DateTimeOffset dateTime)
	{
		TimeSpan timeSpan = default;
		_tpCalendar!.SetDateTime(dateTime);

		var minute = _tpCalendar.Minute;
		timeSpan += new TimeSpan(minute * _timeSpanTicksPerMinute);

		var hour = _tpCalendar.Hour;
		if (_is12HourClock)
		{
			int period = 0;
			int firstPeriodInThisDay = 0;

			period = _tpCalendar.Period;
			firstPeriodInThisDay = _tpCalendar.FirstPeriodInThisDay;

			if (period == firstPeriodInThisDay)
			{
				if (hour == _periodCoercionOffset)
				{
					hour = 0;
				}
			}
			else
			{
				if (hour != _periodCoercionOffset)
				{
					hour += _periodCoercionOffset;
				}
			}
		}
		timeSpan += new TimeSpan(hour * _timeSpanTicksPerHour);

		return timeSpan;
	}

	// Translates a timespan to datetime. Note that, unrelated fields of datetime (year, day etc.)
	// are set to our sentinel values.
	private DateTimeOffset GetDateTimeFromTimeSpan(TimeSpan timeSpan)
	{
		SetSentinelDate(_tpCalendar!);
		var dateTime = _tpCalendar!.GetDateTime();

		dateTime += timeSpan;

		return dateTime;
	}

	// Gets the minute increment and if it is 0, adjusts it to 60 so we will handle the 0
	// case correctly.
	private int GetAdjustedMinuteIncrement()
	{
		var minuteIncrement = _minuteIncrement;
		if (minuteIncrement == 0)
		{
			minuteIncrement = TIMEPICKER_MINUTEINCREMENT_ZERO_REPLACEMENT;
		}

		return minuteIncrement;
	}

	private static object GetDefaultIsDefaultShadowEnabled() => true;
}
