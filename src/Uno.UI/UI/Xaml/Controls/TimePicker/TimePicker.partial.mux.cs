#nullable enable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Represents TimePicker control. TimePicker is a XAML UI control that allows
//      the selection of times.

using System;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.System;
using CultureInfo = System.Globalization.CultureInfo;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class TimePicker
{
	private const string UIA_NAME_TIMEPICKER = nameof(UIA_NAME_TIMEPICKER);
	private const string UIA_TIMEPICKER_HOUR = nameof(UIA_TIMEPICKER_HOUR);
	private const string UIA_TIMEPICKER_MINUTE = nameof(UIA_TIMEPICKER_MINUTE);
	private const string UIA_TIMEPICKER_PERIOD = nameof(UIA_TIMEPICKER_PERIOD);
	private const string TEXT_TIMEPICKER_HOUR_PLACEHOLDER = nameof(TEXT_TIMEPICKER_HOUR_PLACEHOLDER);
	private const string TEXT_TIMEPICKER_MINUTE_PLACEHOLDER = nameof(TEXT_TIMEPICKER_MINUTE_PLACEHOLDER);
	private const string TEXT_TIMEPICKER_PERIOD_PLACEHOLDER = nameof(TEXT_TIMEPICKER_PERIOD_PLACEHOLDER);

	// This is July 15th, 2011 as our sentinel date. There are no known
	//  daylight savings transitions that happened on that date.
	private const int TIMEPICKER_SENTINELDATE_YEAR = 2011;
	private const int TIMEPICKER_SENTINELDATE_MONTH = 7;
	private const int TIMEPICKER_SENTINELDATE_DAY = 15;
	private const int TIMEPICKER_SENTINELDATE_TIMEFIELDS = 0;
	private const int TIMEPICKER_SENTINELDATE_HOUR12 = 12;
	private const int TIMEPICKER_SENTINELDATE_HOUR24 = 0;
	private const int TIMEPICKER_COERCION_INDEX = 0;
	private const int TIMEPICKER_COERCION_OFFSET = 12;
	//private const int TIMEPICKER_AM_INDEX = 0;
	//private const int TIMEPICKER_PM_INDEX = 1;
	private const int TIMEPICKER_RTL_CHARACTER_CODE = 8207;
	private const int TIMEPICKER_MINUTEINCREMENT_MIN = 0;
	private const int TIMEPICKER_MINUTEINCREMENT_MAX = 9;
	// When the minute increment is set to 0, we want to only have 00 at the minute picker. This
	// can be easily obtained by treating 0 as 60 with our existing logic. So during our logic, if we see
	// that minute increment is zero we will use 60 in our calculations.
	private const int TIMEPICKER_MINUTEINCREMENT_ZERO_REPLACEMENT = 60;

	// String to be compared against ClockIdentifier property to determine the clock system
	// we are using.
	private const string s_strTwelveHourClock = "12HourClock";

	// Formats to be used on datetimeformatters to get strings representing times
	private const string s_strHourFormat = "{hour.integer(1)}";
	private const string s_strMinuteFormat = "{minute.integer(2)}";
	private const string s_strPeriodFormat = "{period.abbreviated(2)}";

	// Corresponding number of timespan ticks per minute, hour and day. Note that a tick is a 100 nanosecond
	// interval.
	private const long s_timeSpanTicksPerMinute = TimeSpan.TicksPerMinute;
	private const long s_timeSpanTicksPerHour = TimeSpan.TicksPerHour;
	private const long s_timeSpanTicksPerDay = TimeSpan.TicksPerDay;

	public TimePicker()
	{
		this.DefaultStyleKey = typeof(TimePicker);
		m_is12HourClock = false;
		m_reactionToSelectionChangeAllowed = false;
		m_defaultTime = new TimeSpan(GetNullTimeSentinelValue());
		m_currentTime = new TimeSpan(GetNullTimeSentinelValue());
		PrepareState();
	}

	//TODO:MZ: Do this on Unloaded
	~TimePicker()
	{
		// This will ensure the pending async operation
		// completes and closes the open dialog
		if (m_tpAsyncSelectionInfo is not null)
		{
			/*VERIFYHR*/
			m_tpAsyncSelectionInfo.Cancel();
		}
		if (m_loadedEventHandler.Disposable is not null)
		{
			m_loadedEventHandler.Disposable = null;
		}

		if (m_windowActivatedHandler.Disposable is not null)
		{
			m_windowActivatedHandler.Disposable = null;
		}
	}

	private void PrepareState()
	{
		TrackerCollection<object> spCollection;

		// base.PrepareState();

		//Initialize the collections we will be using as itemssources.
		spCollection = new();
		m_tpHourSource = spCollection;

		spCollection = new();
		m_tpMinuteSource = spCollection;

		spCollection = new();
		m_tpPeriodSource = spCollection;

		RefreshSetup();

		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (DXamlCore.Current.GetAssociatedWindow(this) is { } window)
		{
			var weakThis = WeakReferencePool.RentSelfWeakReference(this);

			void OnWindowActivated(object sender, object args)
			{
				if (!weakThis!.IsAlive && weakThis.Target is TimePicker timePicker)
				{
					timePicker.RefreshSetup();
				}
			}

			window.Activated += OnWindowActivated;
			m_windowActivatedHandler.Disposable = Disposable.Create(() => window.Activated -= OnWindowActivated);
		}
	}

	/// <summary>
	/// Called when the IsEnabled property changes.
	/// </summary>
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
	{
		UpdateVisualState();
	}

	// Change to the correct visual state for the 
	private protected override void ChangeVisualState(bool useTransitions)
	{
		bool isEnabled = IsEnabled;

		if (!isEnabled)
		{
			GoToState(useTransitions, "Disabled");
		}
		else
		{
			GoToState(useTransitions, "Normal");
		}

		var selectedTime = SelectedTime;

		if (selectedTime.HasValue)
		{
			GoToState(useTransitions, "HasTime");
		}
		else
		{
			GoToState(useTransitions, "HasNoTime");
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		var key = e.Key;
		var nModifierKeys = PlatformHelpers.GetKeyboardModifiers();

		// Alt+Up or Alt+Down opens the TimePickerFlyout
		// but only if a FlyoutButton is present (we don't want to be able to trigger the flyout with the keyboard but not the mouse)
		if ((key == VirtualKey.Up || key == VirtualKey.Down)
			&& (0 != (nModifierKeys & VirtualKeyModifiers.Menu))
			&& m_tpFlyoutButton is not null)
		{
			e.Handled = true;
			ShowPickerFlyout();
		}
	}

	/// <summary>
	/// Gives the default values for our properties.
	/// </summary>
	internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
	{
		if (property == ClockIdentifierProperty)
		{
			// Our default clock identifier is user's system clock. We use datetimeformatter to get the system clock.
			var spFormatter = new DateTimeFormatter(s_strHourFormat);
			var strClockIdentifier = spFormatter.Clock;

			defaultValue = strClockIdentifier;
			return true;
		}
		else if (property == TimeProperty)
		{
			defaultValue = m_defaultTime;
			return true;
		}
		else
		{
			return base.GetDefaultValue2(property, out defaultValue);
		}
	}

	// Reacts to change in MinuteIncrement property.
	private void OnMinuteIncrementChanged(object pOldValue, object pNewValue)
	{

		int newValue = (int)pNewValue;
		if (newValue < TIMEPICKER_MINUTEINCREMENT_MIN || newValue > TIMEPICKER_MINUTEINCREMENT_MAX)
		{
			int oldValue = 0;

			oldValue = (int)pOldValue;
			MinuteIncrement = oldValue;
			throw new ArgumentOutOfRangeException(nameof(pNewValue));
		}
		RefreshSetup();
	}

	// Reacts to change in ClockIdentifier property.
	private void OnClockIdentifierChanged(object pOldValue, object pNewValue)
	{
		try
		{
			RefreshSetup();
		}
		catch
		{
			ClockIdentifier = (string)pOldValue;
#if DEBUG
			throw;
#endif
		}
	}

	/// <summary>
	/// Handle the custom property changed event and call the
	/// </summary>
	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == TimeProperty)
		{
			if (!m_isPropagatingTime)
			{
				TimeSpan newValue = (TimeSpan)args.NewValue;

				// When SelectedTime is set to null, we set Time to a sentinel value that also represents null,
				// and vice-versa.
				if (newValue.Ticks != GetNullTimeSentinelValue())
				{
					{
						m_isPropagatingTime = true;
						using var scopeGuard = Disposable.Create(() => m_isPropagatingTime = false);
						SelectedTime = newValue;
					}
				}
				else
				{
					m_isPropagatingTime = true;
					using var scopeGuard = Disposable.Create(() => m_isPropagatingTime = false);
					SelectedTime = null;
				}
			}

			OnTimeChanged(args.OldValue, args.NewValue);
		}
		else if (args.Property == SelectedTimeProperty)
		{
			if (!m_isPropagatingTime)
			{
				var selectedTime = SelectedTime;

				if (selectedTime.HasValue)
				{
					TimeSpan time;
					time = selectedTime.Value;

					{
						m_isPropagatingTime = true;
						using var scopeGuard = Disposable.Create(() => m_isPropagatingTime = false);
						Time = time;
					}
				}
				else
				{
					m_isPropagatingTime = true;
					using var scopeGuard = Disposable.Create(() => m_isPropagatingTime = false);
					Time = GetNullTimeSentinel();
				}
			}

			if (m_tpFlyoutButton is not null)
			{
				UpdateFlyoutButtonContent();
			}

			UpdateVisualState();
		}
		else if (args.Property == MinuteIncrementProperty)
		{
			OnMinuteIncrementChanged(args.OldValue, args.NewValue);
		}
		else if (args.Property == ClockIdentifierProperty)
		{
			OnClockIdentifierChanged(args.OldValue, args.NewValue);
		}
		else if (args.Property == HeaderProperty || args.Property == HeaderTemplateProperty)
		{
			UpdateHeaderPresenterVisibility();
		}
	}

	/// <summary>
	/// Reacts to changes in Time property. Time may have changed programmatically or end user may have changed the
	/// selection of one of our selectors causing a change in Time.
	/// </summary>
	private void OnTimeChanged(object pOldValue, object pNewValue)
	{
		TimeSpan oldValue = (TimeSpan)pOldValue;
		TimeSpan newValue = (TimeSpan)pNewValue;
		TimeSpan coercedTime = default;
		Calendar spCalendar;

		// It is possible for the value of ClockIdentifier to change without us getting a property changed notification.
		// This can happen if the property is unset (i.e. default value) and so the effective value matches the system settings.
		// If the system settings changes, the effective value of ClockIdentifier can change.
		var strClockIdentifier = ClockIdentifier;
		bool newIs12HourClock = strClockIdentifier == s_strTwelveHourClock;
		if (newIs12HourClock != m_is12HourClock)
		{
			RefreshSetup();
		}

		// The Time might have changed due to a change in the time zone.
		// To account for this we need to re-create m_tpCalendar (which internally uses the time zone as it was at the time of its creation).
		spCalendar = CreateNewCalendar(strClockIdentifier);
		m_tpCalendar = null;
		m_tpCalendar = spCalendar;

		coercedTime = CheckAndCoerceTime(newValue);

		// We are checking to see if new value is different from the current one. This is because even if they are same,
		// calling put_Time will break any Binding on Time (if there is any) that this TimePicker is target of.
		if (newValue.Ticks != coercedTime.Ticks)
		{
			// We are coercing the time. The new property change will execute the necessary logic so
			// we will just go to cleanup after this call.
			Time = coercedTime;
			return;
		}

		UpdateDisplay();

		// Create and populate the value changed event args
		var valueChangedEventArgs = new TimePickerValueChangedEventArgs(oldValue, newValue);

		// Raise event
		TimeChanged?.Invoke(this, valueChangedEventArgs);

		// Create and populate the selected value changed event args
		var selectedValueChangedEventArgs = new TimePickerSelectedValueChangedEventArgs();

		if (oldValue.Ticks != GetNullTimeSentinelValue())
		{
			selectedValueChangedEventArgs.OldTime = oldValue;
		}
		else
		{
			selectedValueChangedEventArgs.OldTime = null;
		}

		if (newValue.Ticks != GetNullTimeSentinelValue())
		{
			selectedValueChangedEventArgs.NewTime = newValue;
		}
		else
		{
			selectedValueChangedEventArgs.NewTime = null;
		}

		// Raise event
		SelectedTimeChanged?.Invoke(this, selectedValueChangedEventArgs);
	}

	// Reacts to the FlyoutButton being pressed. Invokes a form-factor specific flyout if present.
	private void OnFlyoutButtonClick(object pSender, RoutedEventArgs pArgs)
	{
		ShowPickerFlyout();
	}

	private async void ShowPickerFlyout()
	{
		//if (m_tpAsyncSelectionInfo is null)
		//{
		//	object spAsyncAsInspectable;
		//	IAsyncOperation<TimeSpan?> spAsyncOperation;
		//	var wpThis = WeakReferencePool.RentSelfWeakReference(this);
		//	var callbackPtr = (IAsyncOperation<TimeSpan?> getOperation, AsyncStatus status) =>
		//	{
		//		if (wpThis.IsAlive && wpThis.Target is TimePicker spThis)
		//		{
		//			spThis.OnGetTimePickerSelectionAsyncCompleted(getOperation, status);
		//		}
		//	};

		//	var xamlControlsGetTimePickerSelectionPtr = reinterpret_cast < decltype(&XamlControlsGetTimePickerSelection) > (.GetProcAddress(GetPhoneModule(), "XamlControlsGetTimePickerSelection"));
		//	xamlControlsGetTimePickerSelectionPtr(as_iinspectable(this), as_iinspectable(m_tpFlyoutButton), spAsyncAsInspectable.GetAddressOf());

		//	spAsyncOperation = spAsyncAsInspectable.AsOrNull<IAsyncOperation<IReference<TimeSpan>*>>();
		//	IFCEXPECT(spAsyncOperation);
		//	spAsyncOperation.Completed = callbackPtr;
		//	m_tpAsyncSelectionInfo = sp);
		//}

		if (m_tpAsyncSelectionInfo == null)
		{
			var asyncOperation = SelectionExports.XamlControls_GetTimePickerSelection(this, m_tpFlyoutButton);
			var getOperation = asyncOperation.AsTask();
			await getOperation;
			OnGetTimePickerSelectionAsyncCompleted(getOperation, asyncOperation.Status);
		}
	}

	// Callback passed to the GetTimePickerSelectionAsync method. Called when a form-factor specific
	// flyout returns with a new TimeSpan value to update the TimePicker's DateTime.
	private void OnGetTimePickerSelectionAsyncCompleted(
		Task<TimeSpan?> getOperation,
		AsyncStatus status)
	{
		//    CheckThread(); TODO:MZ:

		m_tpAsyncSelectionInfo = null;

		if (status == AsyncStatus.Completed)
		{
			TimeSpan? selectedTime = getOperation.Result;

			// A null IReference object is returned when the user cancels out of the
			// 
			if (selectedTime is not null)
			{
				// We set SelectedTime instead of Time in order to ensure that the value
				// propagates to both SelectedTime and Time.
				// See the comment at the top of OnPropertyChanged2 for details.
				SelectedTime = selectedTime;
			}
		}
	}

	// Checks whether the given time is in our acceptable range, coerces it or raises exception when necessary.
	private TimeSpan CheckAndCoerceTime(TimeSpan time)
	{
		TimeSpan coercedTime = default;
		DateTimeOffset dateTime = default;
		int minute = 0;
		int minuteIncrement = 0;

		// Check the value of time, we do not accept negative timespan values
		// except for the null-time sentinel value.
		if (time.Ticks == GetNullTimeSentinelValue())
		{
			coercedTime = time;
			return coercedTime;
		}
		else if (time.Ticks < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(time));
		}

		// If the time's duration is greater than 24 hours, we coerce it down to 24 hour range
		// by taking mod of it.
		coercedTime = new TimeSpan(time.Ticks % s_timeSpanTicksPerDay);

		// Finally we coerce the minutes to a factor of MinuteIncrement
		minuteIncrement = GetAdjustedMinuteIncrement();
		dateTime = GetDateTimeFromTimeSpan(coercedTime);
		m_tpCalendar!.SetDateTime(dateTime);
		minute = m_tpCalendar.Minute;
		m_tpCalendar.Minute = minute - (minute % minuteIncrement);
		dateTime = m_tpCalendar.GetDateTime();
		coercedTime = GetTimeSpanFromDateTime(dateTime);
		return coercedTime;
	}

	// Get TimePicker template parts and create the sources if they are not already there
	protected override void OnApplyTemplate()
	{
		Selector spHourPicker;
		Selector spMinutePicker;
		Selector spPeriodPicker;
		Border spFirstPickerHost;
		Border spSecondPickerHost;
		Border spThirdPickerHost;
		FrameworkElement spLayoutRoot;
		ButtonBase spFlyoutButton;
		TextBlock spHourTextBlock;
		TextBlock spMinuteTextBlock;
		TextBlock spPeriodTextBlock;
		ColumnDefinition spFirstTextBlockColumn;
		ColumnDefinition spSecondTextBlockColumn;
		ColumnDefinition spThirdTextBlockColumn;
		UIElement spFirstColumnDivider;
		UIElement spSecondColumnDivider;
		string strAutomationName;
		string strParentAutomationName;
		string strComboAutomationName;
		// Clean up existing template parts
		if (m_tpHourPicker is not null)
		{
			m_epHourSelectionChangedHandler.Disposable = null;
		}

		if (m_tpMinutePicker is not null)
		{
			m_epMinuteSelectionChangedHandler.Disposable = null;
		}

		if (m_tpPeriodPicker is not null)
		{
			m_epPeriodSelectionChangedHandler.Disposable = null;
		}

		if (m_tpFlyoutButton is not null)
		{
			m_epFlyoutButtonClickHandler.Disposable = null;
		}

		m_tpHourPicker = null;
		m_tpMinutePicker = null;
		m_tpPeriodPicker = null;

		m_tpFirstPickerHost = null;
		m_tpSecondPickerHost = null;
		m_tpThirdPickerHost = null;
		m_tpFirstTextBlockColumn = null;
		m_tpSecondTextBlockColumn = null;
		m_tpThirdTextBlockColumn = null;
		m_tpFirstColumnDivider = null;
		m_tpSecondColumnDivider = null;
		m_tpHeaderPresenter = null;
		m_tpLayoutRoot = null;
		m_tpFlyoutButton = null;

		m_tpHourTextBlock = null;
		m_tpMinuteTextBlock = null;
		m_tpPeriodTextBlock = null;

		base.OnApplyTemplate();

		// Get selectors for hour/minute/period pickers
		spHourPicker = GetTemplateChild<Selector>("HourPicker");
		spMinutePicker = GetTemplateChild<Selector>("MinutePicker");
		spPeriodPicker = GetTemplateChild<Selector>("PeriodPicker");

		// Get the hosting borders
		spFirstPickerHost = GetTemplateChild<Border>("FirstPickerHost");
		spSecondPickerHost = GetTemplateChild<Border>("SecondPickerHost");
		spThirdPickerHost = GetTemplateChild<Border>("ThirdPickerHost");

		// Get the TextBlocks that are used to display the Hour/Minute/Period
		spHourTextBlock = GetTemplateChild<TextBlock>("HourTextBlock");
		spMinuteTextBlock = GetTemplateChild<TextBlock>("MinuteTextBlock");
		spPeriodTextBlock = GetTemplateChild<TextBlock>("PeriodTextBlock");

		// Get the ColumnDefinitions that are used to lay out the Hour/Minute/Period TextBlocks.
		spFirstTextBlockColumn = GetTemplateChild<ColumnDefinition>("FirstTextBlockColumn");
		spSecondTextBlockColumn = GetTemplateChild<ColumnDefinition>("SecondTextBlockColumn");
		spThirdTextBlockColumn = GetTemplateChild<ColumnDefinition>("ThirdTextBlockColumn");

		// Get the the column dividers between the hour/minute/period textblocks.
		spFirstColumnDivider = GetTemplateChild<UIElement>("FirstColumnDivider");
		spSecondColumnDivider = GetTemplateChild<UIElement>("SecondColumnDivider");

		//Get the LayoutRoot
		spLayoutRoot = GetTemplateChild<FrameworkElement>("LayoutRoot");

		spFlyoutButton = GetTemplateChild<ButtonBase>("FlyoutButton");

		m_tpHourPicker = spHourPicker;
		m_tpMinutePicker = spMinutePicker;
		m_tpPeriodPicker = spPeriodPicker;

		m_tpFirstPickerHost = spFirstPickerHost;
		m_tpSecondPickerHost = spSecondPickerHost;
		m_tpThirdPickerHost = spThirdPickerHost;

		m_tpFirstTextBlockColumn = spFirstTextBlockColumn;
		m_tpSecondTextBlockColumn = spSecondTextBlockColumn;
		m_tpThirdTextBlockColumn = spThirdTextBlockColumn;

		m_tpFirstColumnDivider = spFirstColumnDivider;
		m_tpSecondColumnDivider = spSecondColumnDivider;

		m_tpHourTextBlock = spHourTextBlock;
		m_tpMinuteTextBlock = spMinuteTextBlock;
		m_tpPeriodTextBlock = spPeriodTextBlock;

		m_tpLayoutRoot = spLayoutRoot;

		m_tpFlyoutButton = spFlyoutButton;

		UpdateHeaderPresenterVisibility();

		strParentAutomationName = AutomationProperties.GetName(this);
		if (string.IsNullOrEmpty(strParentAutomationName))
		{
			object spHeaderAsInspectable = Header;
			if (spHeaderAsInspectable is not null)
			{
				strParentAutomationName = FrameworkElement.GetStringFromObject(spHeaderAsInspectable);
			}
		}

		// Hook up the selection changed events for selectors, we will be reacting to these events.
		if (m_tpHourPicker is not null)
		{
			m_tpHourPicker.SelectionChanged += OnSelectorSelectionChanged;
			m_epHourSelectionChangedHandler.Disposable = Disposable.Create(() => m_tpHourPicker.SelectionChanged -= OnSelectorSelectionChanged);

			strAutomationName = AutomationProperties.GetName(m_tpHourPicker);
			if (strAutomationName == null)
			{
				strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_TIMEPICKER_HOUR);
				strComboAutomationName = strAutomationName + strParentAutomationName;
				AutomationProperties.SetName(m_tpHourPicker, strComboAutomationName);
			}
		}
		if (m_tpMinutePicker is not null)
		{
			m_tpMinutePicker.SelectionChanged += OnSelectorSelectionChanged;
			m_epMinuteSelectionChangedHandler.Disposable = Disposable.Create(() => m_tpMinutePicker.SelectionChanged -= OnSelectorSelectionChanged);

			strAutomationName = AutomationProperties.GetName(m_tpMinutePicker);
			if (strAutomationName == null)
			{
				strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_TIMEPICKER_MINUTE);
				strComboAutomationName = strAutomationName + strParentAutomationName;
				AutomationProperties.SetName(m_tpMinutePicker, strComboAutomationName);
			}
		}
		if (m_tpPeriodPicker is not null)
		{
			m_tpPeriodPicker.SelectionChanged += OnSelectorSelectionChanged;
			m_epPeriodSelectionChangedHandler.Disposable = Disposable.Create(() => m_tpPeriodPicker.SelectionChanged -= OnSelectorSelectionChanged);

			strAutomationName = AutomationProperties.GetName(m_tpPeriodPicker);
			if (strAutomationName == null)
			{
				strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_TIMEPICKER_PERIOD);
				strComboAutomationName = strAutomationName + strParentAutomationName;
				AutomationProperties.SetName(m_tpPeriodPicker, strComboAutomationName);
			}
		}

		RefreshSetup();

		if (m_tpFlyoutButton is not null)
		{
			m_tpFlyoutButton.Click += OnFlyoutButtonClick;
			m_epFlyoutButtonClickHandler.Disposable = Disposable.Create(() => m_tpFlyoutButton.Click -= OnFlyoutButtonClick);

			RefreshFlyoutButtonAutomationName();

			UpdateFlyoutButtonContent();
		}

		UpdateVisualState(false);
	}

	// Updates the visibility of the Header ContentPresenter
	private void UpdateHeaderPresenterVisibility()
	{
		DataTemplate spHeaderTemplate = HeaderTemplate;
		object spHeader = Header;

		ConditionallyGetTemplatePartAndUpdateVisibility(
			"HeaderContentPresenter",
			(spHeader is not null || spHeaderTemplate is not null),
			ref m_tpHeaderPresenter);
	}

	//// Reacts to change in selection of our selectors. Calculates the new date represented by the selected indices and updates the
	//// Date property.

	//HRESULT
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
		TimeSpan timeSpan;
		var currentTime = Time;

		// When the selectors are template bound the time is coerces through the
		// setting of their valid indices.
		if (m_tpHourPicker is not null || m_tpMinutePicker is not null || m_tpPeriodPicker is not null)
		{
			if (m_tpHourPicker is not null)
			{
				hourIndex = m_tpHourPicker.SelectedIndex;
			}

			if (m_tpMinutePicker is not null)
			{
				minuteIndex = m_tpMinutePicker.SelectedIndex;
			}

			if (m_tpPeriodPicker is not null)
			{
				periodIndex = m_tpPeriodPicker.SelectedIndex;
			}

			SetSentinelDate(m_tpCalendar!);

			if (m_is12HourClock)
			{
				var firstPeriodInThisDay = m_tpCalendar!.FirstPeriodInThisDay;
				m_tpCalendar.Period = periodIndex + firstPeriodInThisDay;
				// 12 hour clock time flow is 12, 1, 2, 3 ... 11 for both am and pm times. So if the index is 0 we need
				// to put hour 12 into hour calendar.
				if (hourIndex == TIMEPICKER_COERCION_INDEX)
				{
					m_tpCalendar.Hour = TIMEPICKER_COERCION_OFFSET;
				}
				else
				{
					m_tpCalendar.Hour = hourIndex;
				}
			}
			else
			{
				m_tpCalendar!.Hour = hourIndex;
			}

			minuteIncrement = GetAdjustedMinuteIncrement();
			m_tpCalendar.Minute = minuteIncrement * minuteIndex;

			var dateTime = m_tpCalendar!.GetDateTime();
			timeSpan = GetTimeSpanFromDateTime(dateTime);
		}
		else
		{
			// When no selectors are template bound (phone template) we
			// still want to coerce the time to sit on a valid MinuteIncrement.
			timeSpan = CheckAndCoerceTime(currentTime);
		}

		// We are checking to see if new value is different from the current one. This is because even if they are same,
		// calling put_Time will break any Binding on Time (if there is any) that this TimePicker is target of.
		if (currentTime.Ticks != timeSpan.Ticks)
		{
			Time = timeSpan;
		}
	}

	//Updates the Content of the FlyoutButton to be the current time.
	private void UpdateFlyoutButtonContent()
	{
		string strFormattedDate;
		DateTimeFormatter spDateFormatter;
		string strClockIdentifier;
		TimeSpan timeSpan = default;

		// Get the calendar and clock identifier strings from the DP and use it to retrieve the cached
		// DateFormatter.
		strClockIdentifier = ClockIdentifier;

		// Get the DateTime that will be used to ruct the string(s) to display.
		var selectedTime = SelectedTime;

		if (selectedTime.HasValue)
		{
			timeSpan = selectedTime.Value;
		}

		var date = GetDateTimeFromTimeSpan(timeSpan);

		// For Blue apps (or a TimePicker template based on what was shipped in Blue), we only have the FlyoutButton.
		// Set the Content of the FlyoutButton to the formatted time.
		if (m_tpFlyoutButton is not null && m_tpHourTextBlock is null && m_tpMinuteTextBlock is null && m_tpPeriodTextBlock is null)
		{
			spDateFormatter = GetTimeFormatter(strClockIdentifier);
			strFormattedDate = spDateFormatter.Format(date);

			m_tpFlyoutButton.Content = strFormattedDate;
		}
		// For the Threshold template we set the Hour, Minute and Period strings on separate TextBlocks:
		if (m_tpHourTextBlock is not null)
		{
			if (selectedTime.HasValue)
			{
				spDateFormatter = CreateNewFormatterWithClock(s_strHourFormat, strClockIdentifier);
				strFormattedDate = spDateFormatter.Format(date);

				m_tpHourTextBlock.Text = strFormattedDate;
			}
			else
			{
				string placeholderText = DXamlCore.Current.GetLocalizedResourceString(TEXT_TIMEPICKER_HOUR_PLACEHOLDER);

				m_tpHourTextBlock.Text = placeholderText;
			}
		}
		if (m_tpMinuteTextBlock is not null)
		{
			if (selectedTime.HasValue)
			{
				spDateFormatter = CreateNewFormatterWithClock(s_strMinuteFormat, strClockIdentifier);
				strFormattedDate = spDateFormatter.Format(date);

				m_tpMinuteTextBlock.Text = strFormattedDate;
			}
			else
			{
				string placeholderText = DXamlCore.Current.GetLocalizedResourceString(TEXT_TIMEPICKER_MINUTE_PLACEHOLDER);
				m_tpMinuteTextBlock.Text = placeholderText;
			}
		}
		if (m_tpPeriodTextBlock is not null)
		{
			if (selectedTime.HasValue)
			{
				spDateFormatter = CreateNewFormatterWithClock(s_strPeriodFormat, strClockIdentifier);
				strFormattedDate = spDateFormatter.Format(date);

				m_tpPeriodTextBlock.Text = strFormattedDate;
			}
			else
			{
				string placeholderText = DXamlCore.Current.GetLocalizedResourceString(TEXT_TIMEPICKER_PERIOD_PLACEHOLDER);

				m_tpPeriodTextBlock.Text = placeholderText;
			}
		}
		RefreshFlyoutButtonAutomationName();
	}

	// Creates a new Calendar, taking into account the ClockIdentifier
	private Calendar CreateNewCalendar(string strClockIdentifier)
	{
		var spCalendar = new Calendar();
		var spLanguages = spCalendar.Languages;

		// Create the calendar
		return new Calendar(
			spLanguages, /* Languages*/
			"GregorianCalendar", /* Calendar */
			strClockIdentifier /* Clock */
		);
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
	private DateTimeFormatter CreateNewFormatterWithCalendar(
			 string strFormat,
			 string strCalendarIdentifier)
	{
		var spFormatter = new DateTimeFormatter(strFormat);

		var strGeographicRegion = spFormatter.GeographicRegion;
		var spLanguages = spFormatter.Languages;
		var strClock = spFormatter.Clock;

		return new DateTimeFormatter(
			strFormat,
			spLanguages,
			strGeographicRegion,
			strCalendarIdentifier,
			strClock);
	}

	private DateTimeFormatter GetTimeFormatter(string strClockIdentifier) =>
		CreateNewFormatterWithClock("shorttime", strClockIdentifier);

	// Sets our sentinel date to the given calendar. This date is 21st of July 2011 midnight.
	// On this day there are no known daylight saving transitions.

	private void SetSentinelDate(Calendar pCalendar)
	{
		pCalendar.Year = TIMEPICKER_SENTINELDATE_YEAR;
		pCalendar.Month = TIMEPICKER_SENTINELDATE_MONTH;
		pCalendar.Day = TIMEPICKER_SENTINELDATE_DAY;

		if (m_is12HourClock)
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
		var strClockIdentifier = ClockIdentifier;
		var spFormatter = CreateNewFormatterWithClock(s_strHourFormat, strClockIdentifier);

		SetSentinelDate(m_tpCalendar!);
		var numberOfHours = m_tpCalendar!.NumberOfHoursInThisPeriod;
		var firstHourInThisPeriod = m_tpCalendar.FirstHourInThisPeriod;
		m_tpCalendar.Hour = firstHourInThisPeriod;

		m_tpHourSource!.Clear();

		for (int hourOffset = 0; hourOffset < numberOfHours; hourOffset++)
		{
			var dateTime = m_tpCalendar.GetDateTime();
			var strHour = spFormatter.Format(dateTime);

			m_tpHourSource.Add(strHour);

			m_tpCalendar.AddHours(1);
		}
	}

	//// Generate the collection that we will populate our minute picker with.
	private void GenerateMinutes()
	{
		var strClockIdentifier = ClockIdentifier;
		var spFormatter = CreateNewFormatterWithClock(s_strMinuteFormat, strClockIdentifier);
		SetSentinelDate(m_tpCalendar!);
		var minuteIncrement = GetAdjustedMinuteIncrement();
		var lastMinute = m_tpCalendar!.LastMinuteInThisHour;
		var firstMinuteInThisHour = m_tpCalendar.FirstMinuteInThisHour;

		m_tpMinuteSource!.Clear();


		for (int i = firstMinuteInThisHour; i <= lastMinute / minuteIncrement; i++)
		{
			m_tpCalendar.Minute = i * minuteIncrement;

			var dateTime = m_tpCalendar.GetDateTime();
			var strMinute = spFormatter.Format(dateTime);

			m_tpMinuteSource.Add(strMinute);
		}
	}

	//// Generate the collection that we will populate our period picker with.
	private void GeneratePeriods()
	{
		bool twelveHourNotSupported = false;

		var strClockIdentifier = ClockIdentifier;
		var spFormatter = CreateNewFormatterWithClock(s_strPeriodFormat, strClockIdentifier);
		SetSentinelDate(m_tpCalendar!);

		m_tpPeriodSource!.Clear();

		var firstPeriodInThisDay = m_tpCalendar!.FirstPeriodInThisDay;
		m_tpCalendar.Period = firstPeriodInThisDay;
		var dateTime = m_tpCalendar.GetDateTime();
		var strPeriod = spFormatter.Format(dateTime);

		if (string.IsNullOrEmpty(strPeriod))
		{
			// In some locales AM/PM symbols are not defined for periods. For those cases, Globalization will give us ""(empty string)
			// for AM and "." for PM. Empty string causes ContentPresenter to clear DataContext, this causes problems for us becasuse if
			// someone sets a DataContext to an ancestor of TimePicker, when the ContentPresenter on the ComboBox's nameplate gets its
			// Content set to empty string, it will display an unrelated string using the DataContext.
			twelveHourNotSupported = true;
			strPeriod = strPeriod + ".";
		}

		m_tpPeriodSource.Add(strPeriod);

		m_tpCalendar.AddPeriods(1);
		dateTime = m_tpCalendar.GetDateTime();
		strPeriod = spFormatter.Format(dateTime);

		if (twelveHourNotSupported)
		{
			strPeriod = "." + strPeriod;
		}

		m_tpPeriodSource.Add(strPeriod);
	}

	// Clears the ItemsSource  properties of the selectors.
	private void ClearSelectors()
	{
		//Clear Selector ItemSources
		DependencyProperty pItemsSourceProperty = ItemsControl.ItemsSourceProperty;

		if (m_tpHourPicker is not null)
		{
			m_tpHourPicker.ClearValue(pItemsSourceProperty, DependencyPropertyValuePrecedences.Local); //TODO:MZ: Are these three precedences correct?
		}

		if (m_tpMinutePicker is not null)
		{
			m_tpMinutePicker.ClearValue(pItemsSourceProperty, DependencyPropertyValuePrecedences.Local);
		}

		if (m_tpPeriodPicker is not null)
		{
			m_tpPeriodPicker.ClearValue(pItemsSourceProperty, DependencyPropertyValuePrecedences.Local);
		}
	}

	/// <summary>
	/// Gets the layout ordering of the selectors.
	/// </summary>
	private void GetOrder(
		ref int hourOrder,
		ref int minuteOrder,
		ref int periodOrder,
		ref bool isRTL)
	{
		var spFormatterWithCalendar = CreateNewFormatterWithCalendar("month.full", "GregorianCalendar");
		var spPatterns = spFormatterWithCalendar.Patterns;
		var strPattern = spPatterns[0];

		var szPattern = strPattern;

		isRTL = szPattern[0] == TIMEPICKER_RTL_CHARACTER_CODE;

		var strClockIdentifier = ClockIdentifier;
		var spFormatterWithClock = CreateNewFormatterWithClock("hour minute", strClockIdentifier);
		spPatterns = spFormatterWithClock.Patterns;
		strPattern = spPatterns[0];

		if (strPattern is not null)
		{
			szPattern = strPattern;

			// We do string search to determine the order of the fields.
			var hourOccurence = szPattern.IndexOf("{hour", StringComparison.OrdinalIgnoreCase);
			var minuteOccurence = szPattern.IndexOf("{minute", StringComparison.OrdinalIgnoreCase);
			var periodOccurence = szPattern.IndexOf("{period", StringComparison.OrdinalIgnoreCase);

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

	/// <summary>
	/// Updates the order of selectors in our layout. Also takes care of hiding/showing the selectors and related spacing depending our
	/// public properties set by the user.
	/// </summary>
	private void UpdateOrderAndLayout()
	{
		int hourOrder = 0;
		int minuteOrder = 0;
		int periodOrder = 0;
		bool firstHostPopulated = false;
		bool secondHostPopulated = false;
		bool thirdHostPopulated = false;
		bool isRTL = false;
		GridLength starGridLength = default;
		GridLength zeroGridLength = default;

		zeroGridLength.GridUnitType = GridUnitType.Pixel;
		zeroGridLength.Value = 0.0;
		starGridLength.GridUnitType = GridUnitType.Star;
		starGridLength.Value = 1.0;

		GetOrder(ref hourOrder, ref minuteOrder, ref periodOrder, ref isRTL);

		if (m_tpLayoutRoot is Border layoutRootBorder)
		{
			layoutRootBorder.FlowDirection = isRTL ?
				FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		}

		// Clear the children of hosts first, so we never risk putting one picker in two hosts and failing.
		if (m_tpFirstPickerHost is not null)
		{
			m_tpFirstPickerHost.Child = null;
		}
		if (m_tpSecondPickerHost is not null)
		{
			m_tpSecondPickerHost.Child = null;
		}
		if (m_tpThirdPickerHost is not null)
		{
			m_tpThirdPickerHost.Child = null;
		}

		UIElement? spHourElement = m_tpHourPicker is not null ? (UIElement?)m_tpHourPicker : m_tpHourTextBlock;
		UIElement? spMinuteElement = m_tpMinutePicker is not null ? (UIElement?)m_tpMinutePicker : m_tpMinuteTextBlock;
		UIElement? spPeriodElement = m_tpPeriodPicker is not null ? (UIElement?)m_tpPeriodPicker : m_tpPeriodTextBlock;

		// Assign the selectors to the hosts.
		switch (hourOrder)
		{
			case 0:
				if (m_tpFirstPickerHost is not null && spHourElement is not null)
				{
					m_tpFirstPickerHost.Child = spHourElement;
					firstHostPopulated = true;
				}
				break;
			case 1:
				if (m_tpSecondPickerHost is not null && spHourElement is not null)
				{
					m_tpSecondPickerHost.Child = spHourElement;
					secondHostPopulated = true;
				}
				break;
			case 2:
				if (m_tpThirdPickerHost is not null && spHourElement is not null)
				{
					m_tpThirdPickerHost.Child = spHourElement;
					thirdHostPopulated = true;
				}
				break;
		}

		switch (minuteOrder)
		{
			case 0:
				if (m_tpFirstPickerHost is not null && spMinuteElement is not null)
				{
					m_tpFirstPickerHost.Child = spMinuteElement;
					firstHostPopulated = true;
				}
				break;
			case 1:
				if (m_tpSecondPickerHost is not null && spMinuteElement is not null)
				{
					m_tpSecondPickerHost.Child = spMinuteElement;
					secondHostPopulated = true;
				}
				break;
			case 2:
				if (m_tpThirdPickerHost is not null && spMinuteElement is not null)
				{
					m_tpThirdPickerHost.Child = spMinuteElement;
					thirdHostPopulated = true;
				}
				break;
		}

		switch (periodOrder)
		{
			case 0:
				if (m_tpFirstPickerHost is not null && spPeriodElement is not null && m_is12HourClock)
				{
					m_tpFirstPickerHost.Child = spPeriodElement;
					firstHostPopulated = true;
				}
				break;
			case 1:
				if (m_tpSecondPickerHost is not null && spPeriodElement is not null && m_is12HourClock)
				{
					m_tpSecondPickerHost.Child = spPeriodElement;
					secondHostPopulated = true;
				}
				break;
			case 2:
				if (m_tpThirdPickerHost is not null && spPeriodElement is not null && m_is12HourClock)
				{
					m_tpThirdPickerHost.Child = spPeriodElement;
					thirdHostPopulated = true;
				}
				break;
		}

		//Show the columns that are in use. Hide the columns that are not in use.
		if (m_tpFirstTextBlockColumn is not null)
		{
			m_tpFirstTextBlockColumn.Width = firstHostPopulated ? starGridLength : zeroGridLength;
		}
		if (m_tpSecondTextBlockColumn is not null)
		{
			m_tpSecondTextBlockColumn.Width = secondHostPopulated ? starGridLength : zeroGridLength;
		}
		if (m_tpThirdTextBlockColumn is not null)
		{
			m_tpThirdTextBlockColumn.Width = thirdHostPopulated ? starGridLength : zeroGridLength;
		}

		if (m_tpFirstPickerHost is not null)
		{
			m_tpFirstPickerHost.Visibility = firstHostPopulated ? Visibility.Visible : Visibility.Collapsed;
		}
		if (m_tpSecondPickerHost is not null)
		{
			m_tpSecondPickerHost.Visibility = secondHostPopulated ? Visibility.Visible : Visibility.Collapsed;
		}
		if (m_tpThirdPickerHost is not null)
		{
			m_tpThirdPickerHost.Visibility = thirdHostPopulated ? Visibility.Visible : Visibility.Collapsed;
		}

		// Determine if we will show the dividers and assign visibilities to them. We will determine if the dividers
		// are shown by looking at which borders are populated.
		if (m_tpFirstColumnDivider is not null)
		{
			m_tpFirstColumnDivider.Visibility = firstHostPopulated && (secondHostPopulated || thirdHostPopulated) ?
				Visibility.Visible : Visibility.Collapsed;
		}
		if (m_tpSecondColumnDivider is not null)
		{
			m_tpSecondColumnDivider.Visibility = secondHostPopulated && thirdHostPopulated ?
				Visibility.Visible : Visibility.Collapsed;
		}
	}

	/// <summary>
	/// Updates the selector selected indices to display our Time property.
	/// </summary>
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

			var timeSpan = GetSelectedTime();
			var dateTime = GetDateTimeFromTimeSpan(timeSpan);
			m_tpCalendar!.SetDateTime(dateTime);

			// Calculate the period index and set it
			if (m_is12HourClock)
			{
				period = m_tpCalendar.Period;
				firstPeriodInThisDay = m_tpCalendar.FirstPeriodInThisDay;
				if (m_tpPeriodPicker is not null)
				{
					m_tpPeriodPicker.SelectedIndex = period - firstPeriodInThisDay;
				}
			}

			// Calculate the hour index and set it
			hour = m_tpCalendar.Hour;
			firstHourInThisPeriod = m_tpCalendar.FirstHourInThisPeriod;
			if (m_is12HourClock)
			{
				// For 12 hour clock 12 am and 12 pm are always the first element (index 0) in hour picker.
				// Other hours translate directly to indices. So it is sufficient to make a mod operation while translating
				// hour to index.
				if (m_tpHourPicker is not null)
				{
					m_tpHourPicker.SelectedIndex = hour % TIMEPICKER_COERCION_OFFSET;
				}
			}
			else
			{
				// For 24 hour clock, Hour translates exactly to the hour picker's selected index.
				if (m_tpHourPicker is not null)
				{
					m_tpHourPicker.SelectedIndex = hour;
				}
			}

			// Calculate the minute index and set it
			minuteIncrement = GetAdjustedMinuteIncrement();
			minute = m_tpCalendar.Minute;
			firstMinuteInThisHour = m_tpCalendar.FirstMinuteInThisHour;
			if (m_tpMinutePicker is not null)
			{
				m_tpMinutePicker.SelectedIndex = (minute / minuteIncrement - firstMinuteInThisHour);
			}

			if (m_tpFlyoutButton is not null)
			{
				UpdateFlyoutButtonContent();
			}
		}
		finally
		{
			AllowReactionToSelectionChange();
		}
	}

	/// <summary>
	/// Clears everything, generates and sets the itemssources to selectors.
	/// </summary>
	private void RefreshSetup()
	{
		try
		{
			PreventReactionToSelectionChange();

			var strClockIdentifier = ClockIdentifier;

			m_is12HourClock = strClockIdentifier == s_strTwelveHourClock;

			var spCalendar = CreateNewCalendar(strClockIdentifier);
			SetSentinelDate(spCalendar);

			// Clock identifier change may have rendered m_tpCalendar stale.
			strClockIdentifier = ClockIdentifier;
			spCalendar = CreateNewCalendar(strClockIdentifier);
			m_tpCalendar = null;
			m_tpCalendar = spCalendar;

			ClearSelectors();
			UpdateOrderAndLayout();

			if (m_tpHourPicker is not null)
			{
				GenerateHours();
				m_tpHourPicker.ItemsSource = m_tpHourSource;
			}

			if (m_tpMinutePicker is not null)
			{
				GenerateMinutes();
				m_tpMinutePicker.ItemsSource = m_tpMinuteSource;
			}

			if (m_tpPeriodPicker is not null)
			{
				GeneratePeriods();
				m_tpPeriodPicker.ItemsSource = m_tpPeriodSource;
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
		int hour = 0;
		int minute = 0;

		MUX_ASSERT(m_tpCalendar != null);
		m_tpCalendar!.SetDateTime(dateTime);

		minute = m_tpCalendar.Minute;
		timeSpan += new TimeSpan(minute * s_timeSpanTicksPerMinute);

		hour = m_tpCalendar.Hour;
		if (m_is12HourClock)
		{
			int period = 0;
			int firstPeriodInThisDay = 0;

			period = m_tpCalendar.Period;
			firstPeriodInThisDay = m_tpCalendar.FirstPeriodInThisDay;

			if (period == firstPeriodInThisDay)
			{
				if (hour == TIMEPICKER_COERCION_OFFSET)
				{
					hour = 0;
				}
			}
			else
			{
				if (hour != TIMEPICKER_COERCION_OFFSET)
				{
					hour += TIMEPICKER_COERCION_OFFSET;
				}
			}
		}
		timeSpan += new TimeSpan(hour * s_timeSpanTicksPerHour);

		return timeSpan;
	}

	// Translates a timespan to datetime. Note that, unrelated fields of datetime (year, day etc.)
	// are set to our sentinel values.
	private DateTimeOffset GetDateTimeFromTimeSpan(TimeSpan timeSpan)
	{
		MUX_ASSERT(m_tpCalendar is not null);
		SetSentinelDate(m_tpCalendar!);
		var dateTime = m_tpCalendar!.GetDateTime();

		dateTime += timeSpan;

		return dateTime;
	}

	/// <summary>
	/// Gets the minute increment and if it is 0, adjusts it to 60 so we will handle the 0
	/// case correctly.
	/// </summary>
	/// <returns>Minute increment.</returns>
	private int GetAdjustedMinuteIncrement()
	{
		var minuteIncrement = MinuteIncrement;
		if (minuteIncrement == 0)
		{
			minuteIncrement = TIMEPICKER_MINUTEINCREMENT_ZERO_REPLACEMENT;
		}

		return minuteIncrement;
	}


	/// <summary>
	/// Create TimePickerAutomationPeer to represent the control.
	/// </summary>
	/// <returns>Automation peer.</returns>
	protected override AutomationPeer OnCreateAutomationPeer() => new TimePickerAutomationPeer(this);

	private string GetSelectedTimeAsString()
	{
		var currentTime = GetSelectedTime();
		var date = GetDateTimeFromTimeSpan(currentTime);

		if (currentTime.Ticks != 0)
		{
			var strClockIdentifier = ClockIdentifier;
			var spTimeFormatter = GetTimeFormatter(strClockIdentifier);
			var strData = spTimeFormatter.Format(date);
			return strData;
		}

		return string.Empty;
	}

	private void RefreshFlyoutButtonAutomationName()
	{
		if (m_tpFlyoutButton is not null)
		{
			string strParentAutomationName = AutomationProperties.GetName(this);
			if (string.IsNullOrEmpty(strParentAutomationName))
			{
				var spHeaderAsInspectable = Header;
				if (spHeaderAsInspectable is not null)
				{
					strParentAutomationName = FrameworkElement.GetStringFromObject(spHeaderAsInspectable);
				}
			}
			string pszParent = strParentAutomationName;

			var strSelectedValue = GetSelectedTimeAsString();
			string pszSelectedValue = strSelectedValue;

			string strMsgFormat = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_NAME_TIMEPICKER);
			if (!string.IsNullOrEmpty(strMsgFormat))
			{
				// TODO:MZ: adjust to work with string.Format
				string pszMsgFormat = strMsgFormat;
				string cchBuffer = string.Empty;
				var selectedTime = SelectedTime;
				if (selectedTime.HasValue)
				{
					cchBuffer = string.Format(CultureInfo.InvariantCulture, pszMsgFormat, pszParent, pszSelectedValue);
				}
				else
				{
					cchBuffer = string.Format(CultureInfo.InvariantCulture, pszMsgFormat, pszParent, "");
				}

				// no charater wrote, szBuffer is blank don't update NameProperty
				if (!string.IsNullOrEmpty(cchBuffer))
				{
					AutomationProperties.SetName(m_tpFlyoutButton, cchBuffer);
				}
			}
		}
	}

	/* static */
	private TimeSpan GetNullTimeSentinel()
	{
		return new TimeSpan(GetNullTimeSentinelValue());
	}

	/* static */
	private long GetNullTimeSentinelValue()
	{
		return -1;
	}

	//private TimeSpan GetCurrentTime()
	//{
	//	TimeSpan currentTime;
	//	if (m_currentTime.Ticks == GetNullTimeSentinelValue())
	//	{
	//		var calendar = CreateNewCalendar(s_strTwelveHourClock);
	//		calendar.SetToNow();
	//		var dateTime = calendar.GetDateTime();
	//		m_currentTime = GetTimeSpanFromDateTime(dateTime);
	//	}

	//	currentTime = m_currentTime;
	//	return currentTime;
	//}

	private TimeSpan GetSelectedTime()
	{
		TimeSpan time = TimeSpan.Zero;
		var selectedTime = SelectedTime;

		if (selectedTime.HasValue)
		{
			time = selectedTime.Value;
		}

		return time;
	}
}
