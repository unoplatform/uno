#if __IOS__ || __ANDROID__
#define SUPPORTS_NATIVE_DATEPICKER
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DirectUI;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Header))]
	public partial class DatePicker : Control
	{
		internal const long DEFAULT_DATE_TICKS = 504910368000000000;

		public event EventHandler<DatePickerValueChangedEventArgs> DateChanged;
		public event TypedEventHandler<DatePicker, DatePickerSelectedValueChangedEventArgs> SelectedDateChanged;

		const int DATEPICKER_RTL_CHARACTER_CODE = 8207;
		const int DATEPICKER_MIN_MAX_YEAR_DEAFULT_OFFSET = 100;
		const int DATEPICKER_SENTINELTIME_HOUR = 12;
		const int DATEPICKER_SENTINELTIME_MINUTE = 0;
		const int DATEPICKER_SENTINELTIME_SECOND = 0;
		const int DATEPICKER_WRAP_AROUND_MONTHS_FIRST_INDEX = 1;

		// Reference to a Button for invoking the DatePickerFlyout in the form factor APISet
		ButtonBase m_tpFlyoutButton;

		// References to the TextBlocks that are used to display the Day/Month and Year.
		TextBlock m_tpYearTextBlock;
		TextBlock m_tpMonthTextBlock;
		TextBlock m_tpDayTextBlock;

		ObservableCollection<string> m_tpDaySource;
		ObservableCollection<string> m_tpMonthSource;
		ObservableCollection<string> m_tpYearSource;

		// Reference to daypicker Selector. We need this as we will change its item
		// source as our properties change.
		Selector m_tpDayPicker;

		// Reference to monthpicker Selector. We need this as we will change its item
		// source as our properties change.
		Selector m_tpMonthPicker;

		// Reference to yearpicker Selector. We need this as we will change its item
		// source as our properties change.
		Selector m_tpYearPicker;

		// Reference to the Header content presenter. We need this to collapse the visibility
		// when the Header and HeaderTemplate are null.
		UIElement m_tpHeaderPresenter;

		// References to the hosting borders.
		Border m_tpFirstPickerHost;
		Border m_tpSecondPickerHost;
		Border m_tpThirdPickerHost;

		// References to the columns that will hold the day/month/year textblocks and the spacers.
		ColumnDefinition m_tpDayColumn;
		ColumnDefinition m_tpMonthColumn;
		ColumnDefinition m_tpYearColumn;
		ColumnDefinition m_tpFirstSpacerColumn;
		ColumnDefinition m_tpSecondSpacerColumn;

		// Reference to the grid that holds the day/month/year textblocks.
		Grid m_tpFlyoutButtonContentGrid;

		// References to spacing borders which will act as our margins between the hosts.
		UIElement m_tpFirstPickerSpacing;
		UIElement m_tpSecondPickerSpacing;

		// Reference to our lay out root. We will be setting the flowdirection property on our root to achieve
		// RTL where necessary.
		FrameworkElement m_tpLayoutRoot;

		// This calendar will be used over and over while we are generating the ItemsSources instead
		// of creating new calendars.
		Calendar m_tpCalendar;

		// This DateTimeFormatter will be used over and over when updating the FlyoutButton Content property
		DateTimeFormatter m_tpDateFormatter;
		string m_strDateCalendarIdentifier;

		// We use Gregorian Calendar while calculating the default values of Min and Max years. Instead of creating
		// a new calendar every time these values are reached, we create one and reuse it.
		Calendar m_tpGregorianCalendar;

		// Cached DateTimeFormatter for year and Calendar - Format pair it is related to.
		DateTimeFormatter m_tpYearFormatter;
		string m_strYearFormat;
		string m_strYearCalendarIdentifier;

		// Cached DateTimeFormatter for year and Calendar - Format pair it is related to.
		DateTimeFormatter m_tpMonthFormatter;
		string m_strMonthFormat;
		string m_strMonthCalendarIdentifier;

		// Cached DateTimeFormatter for year and Calendar - Format pair it is related to.
		DateTimeFormatter m_tpDayFormatter;
		string m_strDayFormat;
		string m_strDayCalendarIdentifier;

		// Represent the first date choosable by  Note that the year of this date can be
		// different from the MinYear as the MinYear value can be unrepresentable depending on the
		// type of the calendar.
		DateTimeOffset m_startDate;

		// The year of this date is the latest year that can be selectable by the date picker. Note that
		// month and date values do not necessarily represent the end date of  our date picker since we
		// do not need that information readily. Also note that, this year may be different from the MaxYear
		// since MaxYear may be unrepresentable depending on the calendar.
		DateTimeOffset m_endDate;

		SerialDisposable m_epDaySelectionChangedHandler = new SerialDisposable();
		SerialDisposable m_epMonthSelectionChangedHandler = new SerialDisposable();
		SerialDisposable m_epYearSelectionChangedHandler = new SerialDisposable();

		SerialDisposable m_epFlyoutButtonClickHandler = new SerialDisposable();

		SerialDisposable m_epWindowActivatedHandler = new SerialDisposable();

		// See the comment of AllowReactionToSelectionChange method for use of this variable.
		bool m_reactionToSelectionChangeAllowed;

		bool m_isInitializing;

		// Specifies if we have a valid year range to generate dates. We do not have a valid range if our minimum year is
		// greater than our maximum year.
		bool m_hasValidYearRange;

		int m_numberOfYears;

		// Default date to be used if no Date is set by the user.
		DateTimeOffset? m_defaultDate;

		// Today's date.
		DateTimeOffset m_todaysDate;

		// Keeps track of the pending async operation and allows
		// for cancellation.
		IAsyncInfo m_tpAsyncSelectionInfo;

		bool m_isPropagatingDate;

#if false
		// The selection of the selectors in our template can be changed by two sources. First source is
		// the end user changing a field to select the desired date. Second source is us updating
		// the itemssources and selected indices. We only want to react to the first source as the
		// second one will cause an unintentional recurrence in our logic. So we use this locking mechanism to
		// anticipate selection changes caused by us and making sure we do not react to them. It is okay
		// that these locks are not atomic since they will be only accessed by a single thread so no race
		// condition can occur.
		void AllowReactionToSelectionChange()
		{
			m_reactionToSelectionChangeAllowed = true;
		}
#endif

		void PreventReactionToSelectionChange()
		{
			m_reactionToSelectionChangeAllowed = false;
		}

		bool IsReactionToSelectionChangeAllowed()
		{
			return m_reactionToSelectionChangeAllowed;
		}

		public DatePicker()
		{
			m_numberOfYears = 0;
			m_reactionToSelectionChangeAllowed = true;
			m_isInitializing = true;
			m_hasValidYearRange = false;
			m_startDate = NullDateSentinelValue;
			m_endDate = NullDateSentinelValue;
			m_defaultDate = NullDateSentinelValue;
			m_todaysDate = NullDateSentinelValue;

			DefaultStyleKey = typeof(DatePicker);

			this.Loaded += DatePicker_Loaded;
			this.Unloaded += DatePicker_Unloaded;

			InitPartial();

			PrepareState();
		}

		private readonly SerialDisposable _windowActivatedToken = new();

		private void DatePicker_Unloaded(object sender, RoutedEventArgs e)
		{
			_windowActivatedToken.Disposable = null;
		}

		private void DatePicker_Loaded(object sender, RoutedEventArgs e)
		{
			// TODO: Uno Specific: This portion of code was originally in PrepareState,
			// but was moved here as it requires XamlRoot for multiwindow purposes.
			if (XamlRoot.HostWindow is { } window)
			{
				WeakReference wrWeakThis = new WeakReference(this);

				window.Activated += OnWindowActivated;
				_windowActivatedToken.Disposable = Disposable.Create(() => window.Activated -= OnWindowActivated);

				void OnWindowActivated(object sender, WindowActivatedEventArgs args)
				{
					DatePicker spThis;

					spThis = wrWeakThis.Target as DatePicker;
					if (spThis != null)
					{
						CoreWindowActivationState state =
							CoreWindowActivationState.CodeActivated;
						state = (args.WindowActivationState);

						if (state == CoreWindowActivationState.CodeActivated
							|| state == CoreWindowActivationState.PointerActivated)
						{
							spThis.RefreshSetup();
						}
					}
				}
			}
		}

		~DatePicker()
		{
			// This will ensure the pending async operation
			// completes, closed the open dialog, and doesn't
			// try to execute a callback to a DatePicker that
			// no longer exists.
			if (m_tpAsyncSelectionInfo != null)
			{
				/*VERIFYHR*/
				m_tpAsyncSelectionInfo.Cancel();
			}

			m_epWindowActivatedHandler.Disposable = null;
		}

		// Initialize the DatePicker
		void PrepareState()
		{
			// DatePickerGenerated.PrepareState();

			// We should update our state during initialization because we still want our dps to function properly
			// until we get applied a template, to do this we need our state information.
			UpdateState();
		}

		// Called when the IsEnabled property changes.
		// UNO TODO
		//void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
		//{
		//	// UpdateVisualState();
		//}

		// Change to the correct visual state for the
		private protected override void ChangeVisualState(bool useTransitions)
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", useTransitions);
			}

			DateTimeOffset? selectedDate = SelectedDate;

			if (selectedDate != null)
			{
				VisualStateManager.GoToState(this, "HasDate", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "HasNoDate", useTransitions);
			}
		}

		protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
		{
			base.OnKeyDown(pArgs);

			//VirtualKeyModifiers nModifierKeys = pArgs
			//VirtualKey key = VirtualKey.None;

			//key = (pArgs.Key);
			//// Input_GetKeyboardModifiers(&nModifierKeys);

			//// Alt+Up or Alt+Down opens the DatePickerFlyout
			//// but only if a FlyoutButton is present (we don't want to be able to trigger the flyout with the keyboard but not the mouse)
			//if ((key == VirtualKey.Up || key == VirtualKey.Down)
			//	&& (0 != (nModifierKeys & VirtualKeyModifiers.Menu))
			//	&& m_tpFlyoutButton != null)
			//{
			//	pArgs.Handled = true;
			//	ShowPickerFlyout();
			//}
		}

		// Get DatePicker template parts and create the sources if they are not already there
		protected override void OnApplyTemplate()
		{
			try
			{
				Selector spDayPicker;
				Selector spMonthPicker;
				Selector spYearPicker;
				Border spFirstPickerHost;
				Border spSecondPickerHost;
				Border spThirdPickerHost;
				FrameworkElement spLayoutRoot;
				UIElement spSpacingHolderOne;
				UIElement spSpacingHolderTwo;
				ButtonBase spFlyoutButton;
				TextBlock spYearTextBlock;
				TextBlock spMonthTextBlock;
				TextBlock spDayTextBlock;
				ColumnDefinition spDayColumn;
				ColumnDefinition spMonthColumn;
				ColumnDefinition spYearColumn;
				ColumnDefinition spFirstSpacerColumn;
				ColumnDefinition spSecondSpacerColumn;
				Grid spFlyoutButtonContentGrid;

				string strAutomationName;
				string strParentAutomationName;
				string strComboAutomationName;

				//Clean up existing parts
				if (m_tpDayPicker != null)
				{
					m_epDaySelectionChangedHandler.Disposable = null;
				}

				if (m_tpMonthPicker != null)
				{
					m_epMonthSelectionChangedHandler.Disposable = null;
				}

				if (m_tpYearPicker != null)
				{
					m_epYearSelectionChangedHandler.Disposable = null;
				}

				if (m_tpFlyoutButton != null)
				{
					m_epFlyoutButtonClickHandler.Disposable = null;
				}

				m_tpDayPicker = null;
				m_tpMonthPicker = null;
				m_tpYearPicker = null;

				m_tpFirstPickerHost = null;
				m_tpSecondPickerHost = null;
				m_tpThirdPickerHost = null;

				m_tpDayColumn = null;
				m_tpMonthColumn = null;
				m_tpYearColumn = null;
				m_tpFirstSpacerColumn = null;
				m_tpSecondSpacerColumn = null;

				m_tpFirstPickerSpacing = null;
				m_tpSecondPickerSpacing = null;
				m_tpLayoutRoot = null;
				m_tpHeaderPresenter = null;

				m_tpFlyoutButton = null;
				m_tpFlyoutButtonContentGrid = null;

				m_tpYearTextBlock = null;
				m_tpMonthTextBlock = null;
				m_tpDayTextBlock = null;

				// UNO TODO
				// DatePickerGenerated.OnApplyTemplate();

				// Get selectors for day/month/year pickers
				GetTemplatePart<Selector>("DayPicker", out spDayPicker);
				GetTemplatePart<Selector>("MonthPicker", out spMonthPicker);
				GetTemplatePart<Selector>("YearPicker", out spYearPicker);

				// Get the borders which will be hosting the selectors
				GetTemplatePart<Border>("FirstPickerHost", out spFirstPickerHost);
				GetTemplatePart<Border>("SecondPickerHost", out spSecondPickerHost);
				GetTemplatePart<Border>("ThirdPickerHost", out spThirdPickerHost);

				GetTemplatePart<ColumnDefinition>("DayColumn", out spDayColumn);
				GetTemplatePart<ColumnDefinition>("MonthColumn", out spMonthColumn);
				GetTemplatePart<ColumnDefinition>("YearColumn", out spYearColumn);
				GetTemplatePart<ColumnDefinition>("FirstSpacerColumn", out spFirstSpacerColumn);
				GetTemplatePart<ColumnDefinition>("SecondSpacerColumn", out spSecondSpacerColumn);

				GetTemplatePart<TextBlock>("YearTextBlock", out spYearTextBlock);
				GetTemplatePart<TextBlock>("MonthTextBlock", out spMonthTextBlock);
				GetTemplatePart<TextBlock>("DayTextBlock", out spDayTextBlock);

				GetTemplatePart<Grid>("FlyoutButtonContentGrid", out spFlyoutButtonContentGrid);


				// Get the spacing holders which will be acting as margins between the hosts
				GetTemplatePart<UIElement>("FirstPickerSpacing", out spSpacingHolderOne);
				GetTemplatePart<UIElement>("SecondPickerSpacing", out spSpacingHolderTwo);
				GetTemplatePart<FrameworkElement>("LayoutRoot", out spLayoutRoot);

				GetTemplatePart<ButtonBase>("FlyoutButton", out spFlyoutButton);

				m_tpDayPicker = spDayPicker;
				m_tpMonthPicker = spMonthPicker;
				m_tpYearPicker = spYearPicker;

				m_tpFirstPickerSpacing = spSpacingHolderOne;
				m_tpSecondPickerSpacing = spSpacingHolderTwo;

				m_tpFirstPickerHost = spFirstPickerHost;
				m_tpSecondPickerHost = spSecondPickerHost;
				m_tpThirdPickerHost = spThirdPickerHost;

				m_tpDayColumn = spDayColumn;
				m_tpMonthColumn = spMonthColumn;
				m_tpYearColumn = spYearColumn;
				m_tpFirstSpacerColumn = spFirstSpacerColumn;
				m_tpSecondSpacerColumn = spSecondSpacerColumn;

				m_tpLayoutRoot = spLayoutRoot;

				m_tpYearTextBlock = spYearTextBlock;
				m_tpMonthTextBlock = spMonthTextBlock;
				m_tpDayTextBlock = spDayTextBlock;

				m_tpFlyoutButton = spFlyoutButton;
				m_tpFlyoutButtonContentGrid = spFlyoutButtonContentGrid;

				UpdateHeaderPresenterVisibility();

				strParentAutomationName = AutomationProperties.GetName(this);
				if (string.IsNullOrEmpty(strParentAutomationName))
				{
					object spHeaderAsInspectable;
					spHeaderAsInspectable = Header;
					if (spHeaderAsInspectable != null)
					{
						strParentAutomationName = FrameworkElement.GetStringFromObject(spHeaderAsInspectable);
					}
				}
				// Hook up the selection changed events for selectors, we will be reacting to these events.
				if (m_tpDayPicker != null)
				{
					m_tpDayPicker.SelectionChanged += OnSelectorSelectionChanged;
					m_epDaySelectionChangedHandler.Disposable =
						Disposable.Create(() => m_tpDayPicker.SelectionChanged -= OnSelectorSelectionChanged);

					strAutomationName = AutomationProperties.GetName(m_tpDayPicker);
					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_DATEPICKER_DAY");
						strComboAutomationName = strAutomationName + strParentAutomationName;
						AutomationProperties.SetName(m_tpDayPicker as ComboBox, strComboAutomationName);
					}
				}
				if (m_tpMonthPicker != null)
				{
					m_tpMonthPicker.SelectionChanged += OnSelectorSelectionChanged;
					m_epMonthSelectionChangedHandler.Disposable =
						Disposable.Create(() => m_tpMonthPicker.SelectionChanged -= OnSelectorSelectionChanged);

					strAutomationName = AutomationProperties.GetName(m_tpMonthPicker as ComboBox);
					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_DATEPICKER_MONTH");
						strComboAutomationName = strAutomationName + strParentAutomationName;
						AutomationProperties.SetName(m_tpMonthPicker as ComboBox, strComboAutomationName);
					}
				}
				if (m_tpYearPicker != null)
				{
					m_tpYearPicker.SelectionChanged += OnSelectorSelectionChanged;
					m_epYearSelectionChangedHandler.Disposable =
						Disposable.Create(() => m_tpYearPicker.SelectionChanged -= OnSelectorSelectionChanged);

					strAutomationName = AutomationProperties.GetName(m_tpYearPicker as ComboBox);
					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_DATEPICKER_YEAR");
						strComboAutomationName = strAutomationName + strParentAutomationName;
						AutomationProperties.SetName(m_tpYearPicker as ComboBox, strComboAutomationName);
					}
				}

				if (m_tpFlyoutButton != null)
				{
					m_tpFlyoutButton.Click += OnFlyoutButtonClick;
					m_epFlyoutButtonClickHandler.Disposable =
						Disposable.Create(() => m_tpFlyoutButton.Click -= OnFlyoutButtonClick);
					RefreshFlyoutButtonAutomationName();
				}

				// Create the collections that we will use as itemssources for the selectors.
				if (m_tpYearSource == null && m_tpMonthSource == null && m_tpDaySource == null)
				{
					m_tpYearSource = new ObservableCollection<string>();
					m_tpMonthSource = new ObservableCollection<string>();
					m_tpDaySource = new ObservableCollection<string>();
				}

				RefreshSetup();

				// UpdateVisualState(false);

			}
			finally
			{
				m_isInitializing = false;
			}
		}

		private void GetTemplatePart<T>(string name, out T element) where T : class
		{
			element = GetTemplateChild(name) as T;
		}

		// Clears the ItemsSource and SelectedItem properties of the selectors.

		void ClearSelectors(
			 bool clearDay,
			 bool clearMonth,
			 bool clearYear)
		{
			DependencyProperty pSelectedItemProperty = null;
			DependencyProperty pItemsSourceProperty = null;

			//Clear Selector SelectedItems and ItemSources
			pSelectedItemProperty = Selector.SelectedItemProperty;
			pItemsSourceProperty = ItemsControl.ItemsSourceProperty;

			if (m_tpDayPicker != null && clearDay)
			{
				(m_tpDayPicker as Selector).ClearValue(pItemsSourceProperty);
			}

			if (m_tpMonthPicker != null && clearMonth)
			{
				(m_tpMonthPicker as Selector).ClearValue(pItemsSourceProperty);
			}

			if (m_tpYearPicker != null && clearYear)
			{
				(m_tpYearPicker as Selector).ClearValue(pItemsSourceProperty);
			}
		}

		// Get indices of related fields of current Date for generated itemsources.

		void GetIndices(
			 ref int yearIndex,
			 ref int monthIndex,
			 ref int dayIndex)
		{
			Calendar spCurrentCalendar;
			DateTimeOffset? currentDate = default;
			string strCalendarIdentifier;
			int currentIndex = 0;
			int firstIndex = 0;
			int monthsInThisYear = 0;

			GetSelectedDateOrTodaysDateIfNull(out currentDate);
			strCalendarIdentifier = CalendarIdentifier;

			// We will need the second calendar for calculating the year difference
			CreateNewCalendar(strCalendarIdentifier, out spCurrentCalendar);
			//#ifndef _PREFAST_ // PREfast bug DevDiv:554051
			//			System.Diagnostics.Debug.Assert(spCurrentCalendar);
			//#endif

			spCurrentCalendar.SetDateTime(ClampDate(currentDate.Value, m_startDate, m_endDate));
			m_tpCalendar.SetDateTime(m_startDate);
			GetYearDifference(m_tpCalendar, spCurrentCalendar, out yearIndex);

			firstIndex = (spCurrentCalendar.FirstMonthInThisYear);
			currentIndex = (spCurrentCalendar.Month);
			monthsInThisYear = (spCurrentCalendar.NumberOfMonthsInThisYear);
			if (currentIndex - firstIndex >= 0)
			{
				monthIndex = currentIndex - firstIndex;
			}
			else
			{
				// A special case is in some ThaiCalendar years first month
				// of the year is April, last month is March and month flow is wrap-around
				// style; April, March .... November, December, January, February, March. So the first index
				// will be 4 and last index will be 3. We are handling the case to convert this wraparound behavior
				// into selected index.
				monthIndex = currentIndex - firstIndex + monthsInThisYear;
			}
			firstIndex = (spCurrentCalendar.FirstDayInThisMonth);
			currentIndex = (spCurrentCalendar.Day);
			dayIndex = currentIndex - firstIndex;
		}

		// Clears everything and refreshes the helper objects. After that, generates and
		// sets the itemssources to selectors.

		void RefreshSetup()
		{
			// Since we will be clearing itemssources / selecteditems and putting new ones, selection changes will be fired from the
			// selectors. However, we do not want to process them as if the end user has purposefully changed the selection on a selector.
			PreventReactionToSelectionChange();

			// This will recalculate the startyear/endyear etc and will tell us if we have a valid range to generate sources.
			UpdateState();

			if (m_hasValidYearRange)
			{
				// When we are refreshing all our setup, year selector should
				// also be refreshed.
				RefreshSources(true /*Refresh day */, true /* Refresh month*/, true /* Refresh year */);

				DateTimeOffset currentDate = default;
				currentDate = Date;

				if (currentDate != NullDateSentinelValue)
				{
					int yearIndex = 0;
					int monthIndex = 0;
					int dayIndex = 0;
					DateTimeOffset date = default;

					// If we refreshed our set up due to a property change, this may have caused us to coerce and change the current displayed date. For example
					// min/max year changes may have force us to coerce the current datetime to the nearest value inside the valid range.
					// So, we should update our DateTimeOffset property. If there is a change, we will end up firing the event as desired, if there isn't a change
					// we will just no_op.
					GetIndices(ref yearIndex, ref monthIndex, ref dayIndex);
					GetDateFromIndices(yearIndex, monthIndex, dayIndex, out date);
					// We are checking to see if new value is different from the current one. This is because even if they are same,
					// calling put_Date will break any Binding on Date (if there is any) that this DatePicker is target of.
					if (currentDate != date)
					{
						Date = date;
					}
				}
			}
		}

		// Regenerate the itemssource for the day/month/yearpickers and select the appropriate indices that represent the current DateTimeOffset.
		// Depending on which field changes we might not need to refresh some of the sources.
		void RefreshSources(
			 bool refreshDay,
			 bool refreshMonth,
			 bool refreshYear)
		{
			int yearIndex = 0;
			int monthIndex = 0;
			int dayIndex = 0;

			PreventReactionToSelectionChange();

			GetIndices(ref yearIndex, ref monthIndex, ref dayIndex);

			ClearSelectors(refreshDay, refreshMonth, refreshYear);

			if (m_tpYearPicker != null)
			{
				if (refreshYear)
				{
					GenerateYears();
					(m_tpYearPicker as Selector).ItemsSource = m_tpYearSource;
				}
				m_tpYearPicker.SelectedIndex = yearIndex;
			}

			if (m_tpMonthPicker != null)
			{
				if (refreshMonth)
				{
					GenerateMonths(yearIndex);
					(m_tpMonthPicker as Selector).ItemsSource = m_tpMonthSource;
				}
				m_tpMonthPicker.SelectedIndex = monthIndex;
			}

			if (m_tpDayPicker != null)
			{
				if (refreshDay)
				{
					GenerateDays(yearIndex, monthIndex);
					(m_tpDayPicker as Selector).ItemsSource = m_tpDaySource;
				}
				m_tpDayPicker.SelectedIndex = dayIndex;
			}

			if (m_tpFlyoutButton != null)
			{
				UpdateFlyoutButtonContent();
			}
		}

		// Generate the collection that we will populate our year picker with.
		void GenerateYears()
		{
			string strYearFormat;
			string strYear;
			string strCalendarIdentifier;
			DateTimeFormatter spFormatter;
			DateTimeOffset dateTime;

			strCalendarIdentifier = CalendarIdentifier;
			strYearFormat = YearFormat;
			GetYearFormatter(strYearFormat, strCalendarIdentifier, out spFormatter);

			m_tpYearSource = null;

			for (int yearOffset = 0; yearOffset < m_numberOfYears; yearOffset++)
			{
				m_tpCalendar.SetDateTime(m_startDate);
				m_tpCalendar.AddYears(yearOffset);

				dateTime = m_tpCalendar.GetDateTime();
				strYear = spFormatter.Format(dateTime);

				m_tpYearSource.Add(strYear);
			}
		}

		// Generate the collection that we will populate our month picker with.

		void GenerateMonths(
			 int yearOffset)
		{
			string strMonthFormat;
			string strMonth;
			string strCalendarIdentifier;
			DateTimeFormatter spFormatter;
			DateTimeOffset dateTime;
			int monthOffset = 0;
			int numberOfMonths = 0;
			int firstMonthInThisYear = 0;

			strCalendarIdentifier = CalendarIdentifier;
			strMonthFormat = MonthFormat;
			GetMonthFormatter(strMonthFormat, strCalendarIdentifier, out spFormatter);

			m_tpCalendar.SetDateTime(m_startDate);
			m_tpCalendar.AddYears(yearOffset);
			numberOfMonths = (m_tpCalendar.NumberOfMonthsInThisYear);
			firstMonthInThisYear = (m_tpCalendar.FirstMonthInThisYear);

			m_tpMonthSource.Clear();

			for (monthOffset = 0; monthOffset < numberOfMonths; monthOffset++)
			{
				m_tpCalendar.Month = firstMonthInThisYear;
				m_tpCalendar.AddMonths(monthOffset);
				dateTime = m_tpCalendar.GetDateTime();
				strMonth = spFormatter.Format(dateTime);

				m_tpMonthSource.Add(strMonth);
			}
		}

		// Generate the collection that we will populate our day picker with.
		void GenerateDays(
			 int yearOffset,
			 int monthOffset)
		{
			string strDayFormat;
			string strCalendarIdentifier;
			string strDay;
			DateTimeFormatter spFormatter;
			DateTimeOffset dateTime;
			int dayOffset = 0;
			int numberOfDays = 0;
			int firstDayInThisMonth = 0;
			int firstMonthInThisYear = 0;

			strCalendarIdentifier = CalendarIdentifier;
			strDayFormat = DayFormat;
			GetDayFormatter(strDayFormat, strCalendarIdentifier, out spFormatter);

			m_tpCalendar.SetDateTime(m_startDate);
			m_tpCalendar.AddYears(yearOffset);
			firstMonthInThisYear = m_tpCalendar.FirstMonthInThisYear;
			m_tpCalendar.Month = firstMonthInThisYear;
			m_tpCalendar.AddMonths(monthOffset);
			numberOfDays = (m_tpCalendar.NumberOfDaysInThisMonth);
			firstDayInThisMonth = (m_tpCalendar.FirstDayInThisMonth);

			m_tpDaySource.Clear();

			for (dayOffset = 0; dayOffset < numberOfDays; dayOffset++)
			{
				m_tpCalendar.Day = firstDayInThisMonth + dayOffset;
				dateTime = m_tpCalendar.GetDateTime();
				strDay = spFormatter.Format(dateTime);

				m_tpDaySource.Add(strDay);
			}
		}

		// Reacts to change in selection of our selectors. Calculates the new date represented by the selected indices and updates the
		// Date property.
		void OnSelectorSelectionChanged(
			 object pSender,
			 SelectionChangedEventArgs pArgs)
		{
			if (IsReactionToSelectionChangeAllowed())
			{
				int yearIndex = 0;
				int monthIndex = 0;
				int dayIndex = 0;
				DateTimeOffset date = default;
				DateTimeOffset currentDate = default;

				if (m_tpYearPicker != null)
				{
					yearIndex = m_tpYearPicker.SelectedIndex;
				}

				if (m_tpMonthPicker != null)
				{
					monthIndex = m_tpMonthPicker.SelectedIndex;
				}

				if (m_tpDayPicker != null)
				{
					dayIndex = m_tpDayPicker.SelectedIndex;
				}

				GetDateFromIndices(yearIndex, monthIndex, dayIndex, out date);
				currentDate = Date;
				// We are checking to see if new value is different from the current one. This is because even if they are same,
				// calling put_Date will break any Binding on Date (if there is any) that this DatePicker is target of.
				if (currentDate.ToUniversalTime() != date.ToUniversalTime())
				{
					Date = date;
				}
			}
		}

		// Reacts to the FlyoutButton being pressed. Invokes a form-factor specific flyout if present.
		void OnFlyoutButtonClick(
			object pSender,
			RoutedEventArgs pArgs)
		{
			_flyout.CalendarIdentifier = CalendarIdentifier;
			_flyout.DayFormat = DayFormat;
			_flyout.MonthFormat = MonthFormat;
			_flyout.YearFormat = YearFormat;
			_flyout.DayVisible = DayVisible;
			_flyout.MonthVisible = MonthVisible;
			_flyout.YearVisible = YearVisible;
			_flyout.MinYear = MinYear;
			_flyout.MaxYear = MaxYear;
			_flyout.Date = SelectedDate ?? Date;

#if SUPPORTS_NATIVE_DATEPICKER
			// UnoOnly
			if (_flyout is NativeDatePickerFlyout nativeFlyout)
			{
				nativeFlyout.UseNativeMinMaxDates = UseNativeMinMaxDates;
			}
#endif

			ShowPickerFlyout();
		}

		async void ShowPickerFlyout()
		{

			if (m_tpAsyncSelectionInfo == null)
			{
				var asyncOperation = _flyout.ShowAtAsync(this);
				m_tpAsyncSelectionInfo = asyncOperation;
				var getOperation = asyncOperation.AsTask();
				await getOperation;
				OnGetDatePickerSelectionAsyncCompleted(getOperation, asyncOperation.Status);
			}
		}

		// Callback passed to the GetDatePickerSelectionAsync method. Called when a form-factor specific
		// flyout returns with a new DateTimeOffset value to update the DatePicker's DateTimeOffset.
		void OnGetDatePickerSelectionAsyncCompleted(
		   Task<DateTimeOffset?> getOperation,
		   AsyncStatus status)
		{
			// CheckThread();

			m_tpAsyncSelectionInfo = null;

			if (status == AsyncStatus.Completed)
			{
				DateTimeOffset? selectedDate;
				selectedDate = getOperation.Result;

				// A null IReference object is returned when the user cancels out of the
				//
				if (selectedDate != null)
				{
					// We set SelectedDate instead of Date in order to ensure that the value
					// propagates to both SelectedDate and Date.
					// See the comment at the top of OnPropertyChanged2 for details.
					SelectedDate = selectedDate;
				}
			}
		}

		// Interprets the selected indices of the selectors and creates and returns a DateTimeOffset corresponding to the date represented by these
		// indices.

		void GetDateFromIndices(
			 int yearIndex,
			 int monthIndex,
			 int dayIndex,
			out DateTimeOffset date)
		{
			DateTimeOffset current = default;
			int safeIndex = 0;
			int firstIndex = 0;
			int totalNumber = 0;
			int period = 0;
			int hour = 0;
			int minute = 0;
			int second = 0;
			int nanosecond = 0;
			int newYear = 0;
			int newMonth = 0;
			int previousYear = 0;
			int previousMonth = 0;
			int previousDay = 0;
			int lastIndex = 0;

			current = Date;
			current = ClampDate(current, m_startDate, m_endDate);
			m_tpCalendar.SetDateTime(current);
			// We want to preserve the time information. So we keep them around in order to prevent them overwritten by our sentinel time.
			period = (m_tpCalendar.Period);
			hour = (m_tpCalendar.Hour);
			minute = (m_tpCalendar.Minute);
			second = (m_tpCalendar.Second);
			nanosecond = (m_tpCalendar.Nanosecond);
			previousYear = (m_tpCalendar.Year);
			previousMonth = (m_tpCalendar.Month);
			previousDay = (m_tpCalendar.Day);

			m_tpCalendar.SetDateTime(m_startDate);
			m_tpCalendar.Period = period;
			m_tpCalendar.Hour = hour;
			m_tpCalendar.Minute = minute;
			m_tpCalendar.Second = second;
			m_tpCalendar.Nanosecond = nanosecond;

			m_tpCalendar.AddYears(yearIndex);
			newYear = (m_tpCalendar.Year);

			firstIndex = (m_tpCalendar.FirstMonthInThisYear);
			totalNumber = (m_tpCalendar.NumberOfMonthsInThisYear);
			lastIndex = (m_tpCalendar.LastMonthInThisYear);

			if (firstIndex > lastIndex)
			{
				if (monthIndex + firstIndex > totalNumber)
				{
					safeIndex = monthIndex + firstIndex - totalNumber;
				}
				else
				{
					safeIndex = monthIndex + firstIndex;
				}

				if (previousYear != newYear)
				{
					// Year has changed in some transitions in Thai Calendar, this will change the first month, and last month indices of the year.
					safeIndex = Math.Max(Math.Min(previousMonth, totalNumber), DATEPICKER_WRAP_AROUND_MONTHS_FIRST_INDEX);
				}
			}
			else
			{
				if (previousYear == newYear)
				{
					safeIndex = Math.Max(Math.Min(monthIndex + firstIndex, firstIndex + totalNumber - 1), firstIndex);
				}
				else
				{
					// Year has changed in some transitions in Thai Calendar, this will change the first month, and last month indices of the year.
					safeIndex = Math.Max(Math.Min(previousMonth, firstIndex + totalNumber - 1), firstIndex);
				}
			}

			m_tpCalendar.Month = safeIndex;
			newMonth = (m_tpCalendar.Month);

			firstIndex = (m_tpCalendar.FirstDayInThisMonth);
			totalNumber = (m_tpCalendar.NumberOfDaysInThisMonth);
			// We also need to coerce the day index into the safe range because a change in month or year may have changed the number of days
			// rendering our previous index invalid.
			safeIndex = Math.Max(Math.Min(dayIndex + firstIndex, firstIndex + totalNumber - 1), firstIndex);
			if (previousYear != newYear || previousMonth != newMonth)
			{
				safeIndex = Math.Max(Math.Min(previousDay, firstIndex + totalNumber - 1), firstIndex);
			}
			m_tpCalendar.Day = safeIndex;

			date = m_tpCalendar.GetDateTime();
		}

		// Gives the default values for our properties.
		internal override bool GetDefaultValue2(
			 DependencyProperty pDP,
			 out object pValue)
		{
			Calendar spCalendar;

			if (pDP == DatePicker.MinYearProperty)
			{
				DateTimeOffset minYearDate = default;
				DateTimeOffset minCalendarDate = default;
				DateTimeOffset maxCalendarDate = default;

				if (m_tpGregorianCalendar == null)
				{
					CreateNewCalendar("GregorianCalendar", out spCalendar);
					m_tpGregorianCalendar = spCalendar;
				}
				m_tpCalendar.SetToMin();
				minCalendarDate = m_tpCalendar.GetDateTime();
				m_tpCalendar.SetToMax();
				maxCalendarDate = m_tpCalendar.GetDateTime();
				//Default value is today's date minus 100 Gregorian years.
				m_tpGregorianCalendar.SetToNow();
				m_tpGregorianCalendar.AddYears(-DATEPICKER_MIN_MAX_YEAR_DEAFULT_OFFSET);
				minYearDate = m_tpGregorianCalendar.GetDateTime();

				pValue = ClampDate(minYearDate, minCalendarDate, maxCalendarDate);
				return true;
			}
			else if (pDP == DatePicker.MaxYearProperty)
			{
				DateTimeOffset maxYearDate = default;
				DateTimeOffset minCalendarDate = default;
				DateTimeOffset maxCalendarDate = default;

				if (m_tpGregorianCalendar == null)
				{
					CreateNewCalendar("GregorianCalendar", out spCalendar);
					m_tpGregorianCalendar = spCalendar;
				}
				m_tpCalendar.SetToMin();
				minCalendarDate = m_tpCalendar.GetDateTime();
				m_tpCalendar.SetToMax();
				maxCalendarDate = m_tpCalendar.GetDateTime();
				//Default value is today's date plus 100 Gregorian years.
				m_tpGregorianCalendar.SetToNow();
				m_tpGregorianCalendar.AddYears(DATEPICKER_MIN_MAX_YEAR_DEAFULT_OFFSET);
				maxYearDate = m_tpGregorianCalendar.GetDateTime();

				pValue = ClampDate(maxYearDate, minCalendarDate, maxCalendarDate);
				return true;
			}
			else if (pDP == DatePicker.DateProperty)
			{
				pValue = m_defaultDate;
				return true;
			}

			pValue = null;
			return false;
		}

		// Reacts to the changes in string typed properties. Reverts the property value to the last valid value,
		// if property change causes an exception.
		void OnStringTypePropertyChanged(
			 DependencyProperty property,
			 string strOldValue,
			 string strNewValue)
		{
			try
			{
				RefreshSetup();
			}
			catch (Exception /*e*/)
			{
				if (property == CalendarIdentifierProperty)
				{
					CalendarIdentifier = strOldValue;
				}
				else if (property == DayFormatProperty)
				{
					DayFormat = strOldValue;
				}
				else if (property == MonthFormatProperty)
					MonthFormat = strOldValue;
				else if (property == YearFormatProperty)
				{
					YearFormat = strOldValue;
				}
			}
		}

		// Reacts to changes in Date property. Day may have changed programmatically or end user may have changed the
		// selection of one of our selectors causing a change in Date.

		void OnDateChanged(
			 DateTimeOffset oldValue,
			 DateTimeOffset newValue)
		{
			UpdateVisualState();

			if (m_hasValidYearRange)
			{
				DateTimeOffset clampedNewDate = default;
				DateTimeOffset clampedOldDate = default;

				// The DateTimeOffset value set may be out of our acceptable range.
				clampedNewDate =
					newValue.ToUniversalTime() != NullDateSentinelValue ?
						ClampDate(newValue, m_startDate, m_endDate) :
						newValue;
				clampedOldDate =
					oldValue.ToUniversalTime() != NullDateSentinelValue ?
						ClampDate(oldValue, m_startDate, m_endDate) :
						oldValue;

				// We are checking to see if new value is different from the current one. This is because even if they are same,
				// calling put_Date will break any Binding on Date (if there is any) that this DatePicker is target of.
				if (clampedNewDate.ToUniversalTime() != newValue.ToUniversalTime())
				{
					// We need to coerce the date into the acceptable range. This will trigger another OnDateChanged which
					// will take care of executing the logic needed.
					Date = clampedNewDate;
					return;
				}

				bool refreshMonth = false;
				bool refreshDay = false;

				// Some calendars don't start at day0 (1/1/1).
				// e.g. the first day of Japanese calendar is day681907 (1/1/1868).
				// However, we use day0 as a flag for "unset", so by default clampedOldDate is day0.
				// This will cause exceptions in SetDateTime below since it's not a valid date in Japanese calendar scope.
				// If either clampedOldDate or clampedNewDate is day0, which means we are either setting a new date to an uninitialize DatePicker or clearing the existing selected date,
				// skip the checks in "else" block and just refresh the calendar.
				if (clampedNewDate.ToUniversalTime() == clampedOldDate.ToUniversalTime()
					|| clampedOldDate.ToUniversalTime() == NullDateSentinelValue
					|| clampedNewDate.ToUniversalTime() == NullDateSentinelValue)
				{
					// It looks like we clamped an invalid date into an acceptable one, we need to refresh the sources.
					refreshMonth = true;
					refreshDay = true;
				}
				else
				{
					int newYear = 0;
					int oldYear = 0;
					int newMonth = 0;
					int oldMonth = 0;

					m_tpCalendar.SetDateTime(clampedOldDate);
					oldYear = m_tpCalendar.Year;
					oldMonth = m_tpCalendar.Month;

					m_tpCalendar.SetDateTime(clampedNewDate);
					newYear = m_tpCalendar.Year;
					newMonth = m_tpCalendar.Month;

					if (oldYear != newYear)
					{
						refreshMonth = true;
						refreshDay = true;
					}
					else if (oldMonth != newMonth)
					{
						refreshDay = true;
					}
				}

				// Change in year will invalidate month and days.
				// Change in month will invalidate days.
				// No need to refresh any itemsources only if the day changes.
				if (refreshDay || refreshMonth)
				{
					RefreshSources(refreshDay, refreshMonth, false);
				}
				else
				{
					// Just set the indices to correct values. If the date has been changed by the end user
					// using the comboboxes put_SelectedIndex will no-op since they are already at the correct
					// index.
					int yearIndex = 0;
					int monthIndex = 0;
					int dayIndex = 0;

					GetIndices(ref yearIndex, ref monthIndex, ref dayIndex);
					if (m_tpYearPicker != null)
					{
						m_tpYearPicker.SelectedIndex = yearIndex;
					}
					if (m_tpMonthPicker != null)
					{
						m_tpMonthPicker.SelectedIndex = monthIndex;
					}
					if (m_tpDayPicker != null)
					{
						m_tpDayPicker.SelectedIndex = dayIndex;
					}
				}

				if (m_tpFlyoutButton != null)
				{
					UpdateFlyoutButtonContent();
				}
			}

			// Create and populate the value changed event args
			DatePickerValueChangedEventArgs valueChangedEventArgs = new DatePickerValueChangedEventArgs(newValue, oldValue);

			// Raise event
			DateChanged?.Invoke(this, valueChangedEventArgs);

			DatePickerSelectedValueChangedEventArgs selectedValueChangedEventArgs = new DatePickerSelectedValueChangedEventArgs();

			// Create and populate the selected value changed event args

			if (oldValue.ToUniversalTime() != NullDateSentinelValue)
			{
				selectedValueChangedEventArgs.OldDate = oldValue;
			}
			else
			{
				selectedValueChangedEventArgs.OldDate = null;
			}

			if (newValue.ToUniversalTime() != NullDateSentinelValue)
			{
				selectedValueChangedEventArgs.NewDate = newValue;
			}
			else
			{
				selectedValueChangedEventArgs.NewDate = null;
			}

			// Raise event
			SelectedDateChanged?.Invoke(this, selectedValueChangedEventArgs);
		}

		// Updates the visibility of the Header ContentPresenter

		void UpdateHeaderPresenterVisibility()
		{
			DataTemplate spHeaderTemplate;
			object spHeader;

			spHeaderTemplate = HeaderTemplate;
			spHeader = Header;

			ConditionallyGetTemplatePartAndUpdateVisibility(
				"HeaderContentPresenter",
				(spHeader != null || spHeaderTemplate != null),
				ref m_tpHeaderPresenter);
		}


		// Handle the custom propery changed event and call the
		// OnPropertyChanged2 methods.
		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			// DatePickerGenerated.OnPropertyChanged2(args);

			// switch (args.m_pDP.GetIndex())
			{
				if (args.Property == DateProperty)
				{
					if (!m_isPropagatingDate)
					{
						DateTimeOffset date;
						date = Date;

						// When SelectedDate is set to null, we set Date to a sentinel value that also represents null,
						// and vice-versa.
						if (date.ToUniversalTime() != NullDateSentinelValue)
						{
							try
							{
								m_isPropagatingDate = true;
								SelectedDate = date;
							}
							finally
							{
								m_isPropagatingDate = false;
							}
						}
						else
						{
							m_isPropagatingDate = true;
							try
							{
								SelectedDate = null;
							}
							finally
							{
								m_isPropagatingDate = false;
							}
						}
					}
				}

				if (args.Property == SelectedDateProperty)
				{
					if (!m_isPropagatingDate)
					{
						if (SelectedDate != null)
						{
							try
							{
								m_isPropagatingDate = true;
								Date = SelectedDate.Value;
							}
							finally
							{
								m_isPropagatingDate = false;
							}
						}
						else
						{
							try
							{
								m_isPropagatingDate = true;
								Date = NullDateSentinel;
							}
							finally
							{
								m_isPropagatingDate = false;
							}
						}
					}
				}
			}

			// Except for the interaction between Date and SelectedDate, every other property change can wait
			// until we're done initializing in OnApplyTemplate, since they all only affect visual state.
			if (!m_isInitializing)
			{
				{
					if (args.Property == DateProperty)
					{
						OnDateChanged((DateTimeOffset)args.OldValue, (DateTimeOffset)args.NewValue);
					}

					if (args.Property == SelectedDateProperty)
					{
						if (m_tpFlyoutButton != null)
						{
							UpdateFlyoutButtonContent();
						}

						// UpdateVisualState();
					}

					if (args.Property == CalendarIdentifierProperty
					 || args.Property == DayFormatProperty
					 || args.Property == MonthFormatProperty
					 || args.Property == YearFormatProperty
					 || args.Property == MinYearProperty
					 || args.Property == MaxYearProperty)
					{
						OnStringTypePropertyChanged(args.Property, args.OldValue as string, args.NewValue as string);
					}

					if (args.Property == DayVisibleProperty
					|| args.Property == MonthVisibleProperty
					|| args.Property == YearVisibleProperty)
					{
						UpdateOrderAndLayout();
					}

					if (args.Property == HeaderProperty
					|| args.Property == HeaderTemplateProperty)
					{
						UpdateHeaderPresenterVisibility();
					}
				}
			}
		}

		// Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		// representing the years in our date range. If there isn't a cached DateTimeFormatter instance,
		// creates one and caches it to be returned for the following calls with the same pair.

		void
		GetYearFormatter(
			 string strFormat,
			 string strCalendarIdentifier,
			out DateTimeFormatter ppDateTimeFormatter)
		{
			// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
			// and cached formatter's calendar identifier is the same as the new one's.
			if (!(m_tpYearFormatter != null
				&& strFormat == m_strYearFormat
				&& strCalendarIdentifier == m_strYearCalendarIdentifier))
			{
				// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
				// with its identifying info.
				DateTimeFormatter spFormatter;

				m_tpYearFormatter = null;
				CreateNewFormatter(strFormat, strCalendarIdentifier, out spFormatter);
				m_tpYearFormatter = spFormatter;
				m_strYearFormat = strFormat;
				m_strYearCalendarIdentifier = strCalendarIdentifier;
			}

			ppDateTimeFormatter = m_tpYearFormatter;
		}

		// Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		// representing the months in our date range. If there isn't a cached DateTimeFormatter instance,
		// creates one and caches it to be returned for the following calls with the same pair.
		void GetMonthFormatter(
			 string strFormat,
			 string strCalendarIdentifier,
			out DateTimeFormatter ppDateTimeFormatter)
		{
			// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
			// and cached formatter's calendar identifier is the same as the new one's.
			if (!(m_tpMonthFormatter != null
				&& strFormat == m_strMonthFormat
				&& strCalendarIdentifier == m_strMonthCalendarIdentifier))
			{
				// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
				// with its identifying info.
				DateTimeFormatter spFormatter;

				m_tpMonthFormatter = null;
				CreateNewFormatter(strFormat, strCalendarIdentifier, out spFormatter);
				m_tpMonthFormatter = spFormatter;
				m_strMonthFormat = strFormat;
				m_strMonthCalendarIdentifier = strCalendarIdentifier;
			}

			ppDateTimeFormatter = m_tpMonthFormatter;
		}

		// Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		// representing the days in our date range. If there isn't a cached DateTimeFormatter instance,
		// creates one and caches it to be returned for the following calls with the same pair.

		void GetDayFormatter(
			 string strFormat,
			 string strCalendarIdentifier,
			out DateTimeFormatter ppDateTimeFormatter)
		{
			// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
			// and cached formatter's calendar identifier is the same as the new one's.
			if (!(m_tpDayFormatter != null
				&& strFormat == m_strDayFormat
				&& strCalendarIdentifier == m_strDayCalendarIdentifier))
			{
				// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
				// with its identifying info.
				DateTimeFormatter spFormatter;

				m_tpDayFormatter = null;
				CreateNewFormatter(strFormat, strCalendarIdentifier, out spFormatter);
				m_tpDayFormatter = spFormatter;
				m_strDayFormat = strFormat;
				m_strDayCalendarIdentifier = strCalendarIdentifier;
			}

			ppDateTimeFormatter = m_tpDayFormatter;
		}

		// Creates a new DateTimeFormatter with the given parameters.

		void CreateNewFormatter(
			 string strFormat,
			 string strCalendarIdentifier,
			 out DateTimeFormatter ppDateTimeFormatter)
		{
			DateTimeFormatter spFormatter;
			IReadOnlyList<string> spLanguages;
			string strGeographicRegion;
			string strClock;

			spFormatter = new DateTimeFormatter(strFormat);

			strGeographicRegion = spFormatter.GeographicRegion;
			spLanguages = spFormatter.Languages;
			strClock = spFormatter.Clock;

			//(spFormatterFactory.CreateDateTimeFormatterContext(
			//		strFormat,/* Format string */
			//		spLanguages.AsOrNull<wfc.__FIIterable_1_HSTRING_t>(), /* Languages*/
			//		strGeographicRegion, /* Geographic region */
			//		strCalendarIdentifier, /* Calendar */
			//		strClock, /* Clock */
			//		spFormatter));

			spFormatter = new DateTimeFormatter(
				strFormat,
				spLanguages,
				strGeographicRegion,
				strCalendarIdentifier,
				strClock
			);

			ppDateTimeFormatter = spFormatter;
		}

		// Creates a new wg.Calendar, taking into account the Calendar Identifier
		// represented by our public "Calendar" property.
		void CreateNewCalendar(
			 string strCalendarIdentifier,
			out Calendar ppCalendar)
		{
			Calendar spCalendar;
			IReadOnlyList<string> spLanguages;
			string strClock;

			ppCalendar = null;

			spCalendar = new Calendar();
			spLanguages = spCalendar.Languages;
			strClock = spCalendar.GetClock();

			spCalendar = new Calendar();

			//Create the calendar
			spCalendar = new Calendar(
					spLanguages, /* Languages*/
					strCalendarIdentifier, /* Calendar */
					strClock /* Clock */);

			ppCalendar = spCalendar;
		}

		// Returns the cached DateTimeFormatter for the given Calendar for generating the strings
		// representing the current Date for display on a FlyoutButton. If there isn't a cached
		// DateTimeFormatter instance, creates one and caches it to be returned for the following
		// calls with the same pair.
		void GetDateFormatter(
			 string strCalendarIdentifier,
		   out DateTimeFormatter ppDateFormatter)
		{
			// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
			// and cached formatter's calendar identifier is the same as the new one's.
			if (!(m_tpDateFormatter != null
				&& strCalendarIdentifier == m_strDateCalendarIdentifier))
			{
				// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
				// with its identifying info.
				DateTimeFormatter spFormatter;

				m_tpDateFormatter = null;
				CreateNewFormatter(
					"shortdate",
					strCalendarIdentifier,
					out spFormatter);
				m_tpDateFormatter = spFormatter;
				m_strDateCalendarIdentifier = strCalendarIdentifier;
			}

			ppDateFormatter = m_tpDateFormatter;
		}


		void UpdateFlyoutButtonContent()
		{
			string strFormattedDate;
			DateTimeFormatter spDateFormatter;
			string strCalendarIdentifier;
			DateTimeOffset? date = default;
			DateTimeFormatter spYearFormatter;
			string strYear;
			DateTimeFormatter spMonthFormatter;
			string strMonth;
			DateTimeFormatter spDayFormatter;
			string strDayFormat;
			string strDay;

			// Get the calendar identifier string from the DP and use it to retrieve the cached
			// DateFormatter.
			strCalendarIdentifier = CalendarIdentifier;
			GetDateFormatter(strCalendarIdentifier, out spDateFormatter);

			// Get the date to display.
			DateTimeOffset? selectedDate = SelectedDate;

			if (selectedDate != null)
			{
				date = selectedDate.Value;
			}
			else
			{
				GetTodaysDate(out date);
			}

			date = ClampDate(date.Value, m_startDate, m_endDate);

			// For Blue apps (or a DatePicker template based on what was shipped in Blue), we only have the FlyoutButton
			// Set the Content of the FlyoutButton to the formatted date.
			if (m_tpFlyoutButton != null && m_tpYearTextBlock == null && m_tpMonthTextBlock == null && m_tpDayTextBlock == null)
			{
				// Format the Date into a string and set it as the content of the FlyoutButton
				strFormattedDate = spDateFormatter.Format(date.Value);

				(m_tpFlyoutButton as Button).Content = strFormattedDate;
			}
			// For the Threshold template we set the Day, Month and Year strings on separate TextBlocks:
			if (m_tpYearTextBlock != null)
			{
				if (selectedDate != null)
				{
					GetYearFormatter(YearFormat, strCalendarIdentifier, out spYearFormatter);
					strYear = spYearFormatter.Format(date.Value);

					m_tpYearTextBlock.Text = strYear;
				}
				else
				{
					m_tpYearTextBlock.Text = ResourceAccessor.GetLocalizedStringResource("TEXT_DATEPICKER_YEAR_PLACEHOLDER");
				}
			}
			if (m_tpMonthTextBlock != null)
			{
				if (selectedDate != null)
				{
					GetMonthFormatter(MonthFormat, strCalendarIdentifier, out spMonthFormatter);
					strMonth = spMonthFormatter.Format(date.Value);

					m_tpMonthTextBlock.Text = strMonth;
				}
				else
				{
					m_tpMonthTextBlock.Text = ResourceAccessor.GetLocalizedStringResource("TEXT_DATEPICKER_MONTH_PLACEHOLDER");
				}
			}
			if (m_tpDayTextBlock != null)
			{
				if (selectedDate != null)
				{
					strDayFormat = DayFormat;
					GetDayFormatter(strDayFormat, strCalendarIdentifier, out spDayFormatter);
					strDay = spDayFormatter.Format(date.Value);

					m_tpDayTextBlock.Text = strDay;
				}
				else
				{
					m_tpDayTextBlock.Text = ResourceAccessor.GetLocalizedStringResource("TEXT_DATEPICKER_DAY_PLACEHOLDER");
				}
			}

			RefreshFlyoutButtonAutomationName();
		}

		// Given two calendars, finds the difference of years between them. Note that we are counting on the two
		// calendars will have the same system.
		void GetYearDifference(
			 Calendar pStartCalendar,
			 Calendar pEndCalendar,
			 out int difference)
		{
			int startEra = 0;
			int endEra = 0;
			int startYear = 0;
			int endYear = 0;
			string strStartCalendarSystem;
			string strEndCalendarSystem;

			strStartCalendarSystem = pStartCalendar.GetCalendarSystem();
			strEndCalendarSystem = pEndCalendar.GetCalendarSystem();
			global::System.Diagnostics.Debug.Assert(strStartCalendarSystem == strEndCalendarSystem, "Calendar systems do not match.");

			difference = 0;

			// Get the eras and years of the calendars.
			startEra = (pStartCalendar.Era);
			endEra = (pEndCalendar.Era);

			startYear = (pStartCalendar.Year);
			endYear = (pEndCalendar.Year);

			while (startEra != endEra || startYear != endYear)
			{
				// Add years to start calendar until their eras and years both match.
				pStartCalendar.AddYears(1);
				difference++;
				startEra = (pStartCalendar.Era);
				startYear = (pStartCalendar.Year);
			}
		}


		// Clamps the given date within the range defined by the min and max dates. Note that it is caller's responsibility
		// to feed appropriate min/max values that defines a valid date range.
		DateTimeOffset
		ClampDate(
			 DateTimeOffset date,
			 DateTimeOffset minDate,
			 DateTimeOffset maxDate)
		{
			if (date < minDate)
			{
				return minDate;
			}
			else if (date > maxDate)
			{
				return maxDate;
			}
			return date;
		}

		// The order of date fields vary depending on geographic region, calendar type etc. This function determines the M/D/Y order using
		// globalization APIs. It also determines whether the fields should be laid RTL.
		void GetOrder(
			out int yearOrder,
			out int monthOrder,
			out int dayOrder,
			out bool isRTL)
		{
			DateTimeFormatter spFormatter;
			IReadOnlyList<string> spPatterns;
			string strDate;
			string strCalendarIdentifier;

			strCalendarIdentifier = CalendarIdentifier;
			CreateNewFormatter("day month.full year", strCalendarIdentifier, out spFormatter);
			spPatterns = spFormatter.Patterns;
			strDate = spPatterns[0];

			if (strDate != null)
			{
				string szDate;
				int dayOccurence;
				int monthOccurence;
				int yearOccurence;

				szDate = strDate;

				//The calendar is right-to-left if the first character of the pattern string is the rtl character
				isRTL = szDate[0] == DATEPICKER_RTL_CHARACTER_CODE;

				// We do string search to determine the order of the fields.
				dayOccurence = szDate.IndexOf("{day", StringComparison.Ordinal);
				monthOccurence = szDate.IndexOf("{month", StringComparison.Ordinal);
				yearOccurence = szDate.IndexOf("{year", StringComparison.Ordinal);

				if (dayOccurence < monthOccurence)
				{
					if (dayOccurence < yearOccurence)
					{
						dayOrder = 0;
						if (monthOccurence < yearOccurence)
						{
							monthOrder = 1;
							yearOrder = 2;
						}
						else
						{
							monthOrder = 2;
							yearOrder = 1;
						}
					}
					else
					{
						dayOrder = 1;
						monthOrder = 2;
						yearOrder = 0;
					}
				}
				else
				{
					if (dayOccurence < yearOccurence)
					{
						dayOrder = 1;
						monthOrder = 0;
						yearOrder = 2;
					}
					else
					{
						dayOrder = 2;
						if (monthOccurence < yearOccurence)
						{
							monthOrder = 0;
							yearOrder = 1;
						}
						else
						{

							monthOrder = 1;
							yearOrder = 0;
						}
					}
				}
			}
			else
			{
				dayOrder = 0;
				monthOrder = 1;
				yearOrder = 2;
				isRTL = false;
			}
		}

		// Updates the order of selectors in our layout. Also takes care of hiding/showing the comboboxes and related spacing depending our
		// public properties set by the user.
		void UpdateOrderAndLayout()
		{
			int yearOrder = 0;
			int monthOrder = 0;
			int dayOrder = 0;
			bool isRTL = false;
			bool dayVisible = false;
			bool monthVisible = false;
			bool yearVisible = false;
			bool firstHostPopulated = false;
			bool secondHostPopulated = false;
			bool thirdHostPopulated = false;
			ColumnDefinitionCollection spColumns = new ColumnDefinitionCollection();
			int columnIndex = 0;
			ColumnDefinition firstTextBlockColumn = null;
			ColumnDefinition secondTextBlockColumn = null;
			ColumnDefinition thirdTextBlockColumn = null;

			GetOrder(out yearOrder, out monthOrder, out dayOrder, out isRTL);

			// Some of the Calendars are RTL (Hebrew, Um Al Qura) we need to change the flow direction of DatePicker to accomodate these
			// calendars.
			if (m_tpLayoutRoot != null)
			{
				m_tpLayoutRoot.FlowDirection = isRTL ?
					FlowDirection.RightToLeft : FlowDirection.LeftToRight;
			}

			// Clear the children of hosts first, so we never risk putting one picker in two hosts and failing.
			if (m_tpFirstPickerHost != null)
			{
				m_tpFirstPickerHost.Child = null;
			}
			if (m_tpSecondPickerHost != null)
			{
				m_tpSecondPickerHost.Child = null;
			}
			if (m_tpThirdPickerHost != null)
			{
				m_tpThirdPickerHost.Child = null;
			}

			// Clear the columns of the grid first. We will re-add the columns that we need further down.
			if (m_tpFlyoutButtonContentGrid != null)
			{
				spColumns = m_tpFlyoutButtonContentGrid.ColumnDefinitions;
				spColumns.Clear();
			}

			dayVisible = DayVisible;
			monthVisible = MonthVisible;
			yearVisible = YearVisible;


			// For Blue apps:
			// Assign the selectors to the hosts, if the selector is not shown, we will not put the selector inside the related hosts. Note that we
			// could have just collapsed selector or its host to accomplish hiding, however, we decided not to put the hidden fields to already
			// crowded visual tree.
			// For Threshold apps:
			// We want to add the YearColumn, MonthColumn and DayColumn into the grid.Columns collection in the correct order.
			switch (yearOrder)
			{
				case 0:
					if (m_tpFirstPickerHost != null && m_tpYearPicker != null && yearVisible)
					{
						m_tpFirstPickerHost.Child = m_tpYearPicker as Selector;
						firstHostPopulated = true;
					}
					else if (m_tpYearTextBlock != null && yearVisible)
					{
						firstHostPopulated = true;
						firstTextBlockColumn = m_tpYearColumn;
					}
					break;
				case 1:
					if (m_tpSecondPickerHost != null && m_tpYearPicker != null && yearVisible)
					{
						m_tpSecondPickerHost.Child = m_tpYearPicker as Selector;
						secondHostPopulated = true;
					}
					else if (m_tpYearTextBlock != null && yearVisible)
					{
						secondHostPopulated = true;
						secondTextBlockColumn = m_tpYearColumn;
					}
					break;
				case 2:
					if (m_tpThirdPickerHost != null && m_tpYearPicker != null && yearVisible)
					{
						m_tpThirdPickerHost.Child = m_tpYearPicker as Selector;
						thirdHostPopulated = true;
					}
					else if (m_tpYearTextBlock != null && yearVisible)
					{
						thirdHostPopulated = true;
						thirdTextBlockColumn = m_tpYearColumn;
					}
					break;
			}

			switch (monthOrder)
			{
				case 0:
					if (m_tpFirstPickerHost != null && m_tpMonthPicker != null && monthVisible)
					{
						m_tpFirstPickerHost.Child = m_tpMonthPicker as Selector;
						firstHostPopulated = true;
					}
					else if (m_tpMonthTextBlock != null && monthVisible)
					{
						firstHostPopulated = true;
						firstTextBlockColumn = m_tpMonthColumn;
					}
					break;
				case 1:
					if (m_tpSecondPickerHost != null && m_tpMonthPicker != null && monthVisible)
					{
						m_tpSecondPickerHost.Child = m_tpMonthPicker as Selector;
						secondHostPopulated = true;
					}
					else if (m_tpMonthTextBlock != null && monthVisible)
					{
						secondHostPopulated = true;
						secondTextBlockColumn = m_tpMonthColumn;
					}
					break;
				case 2:
					if (m_tpThirdPickerHost != null && m_tpMonthPicker != null && monthVisible)
					{
						m_tpThirdPickerHost.Child = m_tpMonthPicker as Selector;
						thirdHostPopulated = true;
					}
					else if (m_tpMonthTextBlock != null && monthVisible)
					{
						thirdHostPopulated = true;
						thirdTextBlockColumn = m_tpMonthColumn;
					}
					break;
			}

			switch (dayOrder)
			{
				case 0:
					if (m_tpFirstPickerHost != null && m_tpDayPicker != null && dayVisible)
					{
						m_tpFirstPickerHost.Child = m_tpDayPicker as Selector;
						firstHostPopulated = true;
					}
					else if (m_tpDayTextBlock != null && dayVisible)
					{
						firstHostPopulated = true;
						firstTextBlockColumn = m_tpDayColumn;
					}
					break;
				case 1:
					if (m_tpSecondPickerHost != null && m_tpDayPicker != null && dayVisible)
					{
						m_tpSecondPickerHost.Child = m_tpDayPicker as Selector;
						secondHostPopulated = true;
					}
					else if (m_tpDayTextBlock != null && dayVisible)
					{
						secondHostPopulated = true;
						secondTextBlockColumn = m_tpDayColumn;
					}
					break;
				case 2:
					if (m_tpThirdPickerHost != null && m_tpDayPicker != null && dayVisible)
					{
						m_tpThirdPickerHost.Child = m_tpDayPicker as Selector;
						thirdHostPopulated = true;
					}
					else if (m_tpDayTextBlock != null && dayVisible)
					{
						thirdHostPopulated = true;
						thirdTextBlockColumn = m_tpDayColumn;
					}
					break;
			}


			// Add the columns to the grid in the correct order (as computed in the switch statement above).
			if (spColumns != null)
			{
				if (firstTextBlockColumn != null)
				{
					spColumns.Add(firstTextBlockColumn);
				}

				if (m_tpFirstSpacerColumn != null)
				{
					spColumns.Add(m_tpFirstSpacerColumn);
				}

				if (secondTextBlockColumn != null)
				{
					spColumns.Add(secondTextBlockColumn);
				}

				if (m_tpSecondSpacerColumn != null)
				{
					spColumns.Add(m_tpSecondSpacerColumn);
				}

				if (thirdTextBlockColumn != null)
				{
					spColumns.Add(thirdTextBlockColumn);
				}
			}

			// Set the Grid.Column property on the Day/Month/Year TextBlocks to the index of the matching ColumnDefinition
			// (e.g. YearTextBlock Grid.Column = columns.IndexOf(YearColumn)
			if (m_tpYearTextBlock != null && m_tpYearColumn != null && yearVisible && spColumns != null)
			{
				columnIndex = spColumns.IndexOf(m_tpYearColumn);
				global::System.Diagnostics.Debug.Assert(columnIndex != -1);
				Grid.SetColumn(m_tpYearTextBlock, columnIndex);
			}
			if (m_tpMonthTextBlock != null && m_tpMonthColumn != null && monthVisible && spColumns != null)
			{
				columnIndex = spColumns.IndexOf(m_tpMonthColumn);
				global::System.Diagnostics.Debug.Assert(columnIndex != -1);
				Grid.SetColumn(m_tpMonthTextBlock, columnIndex);
			}
			if (m_tpDayTextBlock != null && m_tpDayColumn != null && dayVisible && spColumns != null)
			{
				columnIndex = spColumns.IndexOf(m_tpDayColumn);
				global::System.Diagnostics.Debug.Assert(columnIndex != -1);
				Grid.SetColumn(m_tpDayTextBlock, columnIndex);
			}

			// Collapse the Day/Month/Year TextBlocks if DayVisible/MonthVisible/YearVisible are false.
			if (m_tpDayTextBlock != null)
			{
				(m_tpDayTextBlock as TextBlock).Visibility = dayVisible ? Visibility.Visible : Visibility.Collapsed;
			}
			if (m_tpMonthTextBlock != null)
			{
				(m_tpMonthTextBlock as TextBlock).Visibility = monthVisible ? Visibility.Visible : Visibility.Collapsed;
			}
			if (m_tpYearTextBlock != null)
			{
				(m_tpYearTextBlock as TextBlock).Visibility = yearVisible ? Visibility.Visible : Visibility.Collapsed;
			}

			// Determine if we will show the spacings and assign visibilities to spacing holders. We will determine if the spacings
			// are shown by looking at which borders are populated.
			// Also move the spacers to the correct column.
			if (m_tpFirstPickerSpacing != null)
			{
				m_tpFirstPickerSpacing.Visibility =
					firstHostPopulated && (secondHostPopulated || thirdHostPopulated) ?
					Visibility.Visible : Visibility.Collapsed;

				if (m_tpFirstSpacerColumn != null && spColumns != null)
				{
					columnIndex = spColumns.IndexOf(m_tpFirstSpacerColumn);
					Grid.SetColumn(m_tpFirstPickerSpacing, columnIndex);
				}
			}
			if (m_tpSecondPickerSpacing != null)
			{
				m_tpSecondPickerSpacing.Visibility =
					secondHostPopulated && thirdHostPopulated ?
					Visibility.Visible : Visibility.Collapsed;

				if (m_tpSecondSpacerColumn != null && spColumns != null)
				{
					columnIndex = spColumns.IndexOf(m_tpSecondSpacerColumn);
					Grid.SetColumn(m_tpSecondPickerSpacing, columnIndex);
				}
			}
		}


		// We execute our logic depending on some state information such as start date, end date, number of years etc. These state
		// variables need to be updated whenever a public property change occurs which affects them.

		void UpdateState()
		{
			Calendar spCalendar;
			string strCalendarIdentifier;
			int month = 0;
			int day = 0;
			DateTimeOffset minYearDate = default;
			DateTimeOffset maxYearDate = default;
			DateTimeOffset maxCalendarDate = default;
			DateTimeOffset minCalendarDate = default;

			// Create a calendar with the the current CalendarIdentifier
			m_tpCalendar = null;
			strCalendarIdentifier = CalendarIdentifier;
			CreateNewCalendar(strCalendarIdentifier, out spCalendar);

#if false // _PREFAST_ // PREfast bug DevDiv:554051
			System.Diagnostics.Debug.Assert(spCalendar);
#endif
			m_tpCalendar = spCalendar;

			// Get the minYear and maxYear dates
			minYearDate = MinYear;
			maxYearDate = MaxYear;

			// We do not have a valid range if our MinYear is later than our MaxYear
			m_hasValidYearRange = minYearDate.ToUniversalTime() <= maxYearDate.ToUniversalTime();

			if (m_hasValidYearRange)
			{
				// Find the earliest and latest dates available for this calendar.
				m_tpCalendar.SetToMin();
				minCalendarDate = m_tpCalendar.GetDateTime();

				//Find the latest date available for this calendar.
				m_tpCalendar.SetToMax();
				maxCalendarDate = m_tpCalendar.GetDateTime();

				minYearDate = ClampDate(minYearDate, minCalendarDate, maxCalendarDate);
				maxYearDate = ClampDate(maxYearDate, minCalendarDate, maxCalendarDate);

				// Since we only care about the year field of minYearDate and maxYearDate we will change other fields into first day and last day
				// of the year respectively.
				m_tpCalendar.SetDateTime(minYearDate);
				month = (m_tpCalendar.FirstMonthInThisYear);
				m_tpCalendar.Month = month;
				day = (m_tpCalendar.FirstDayInThisMonth);
				m_tpCalendar.Day = day;
				minYearDate = m_tpCalendar.GetDateTime();

				m_tpCalendar.SetDateTime(maxYearDate);
				month = (m_tpCalendar.LastMonthInThisYear);
				m_tpCalendar.Month = month;
				day = (m_tpCalendar.LastDayInThisMonth);
				m_tpCalendar.Day = day;
				maxYearDate = m_tpCalendar.GetDateTime();

				m_tpCalendar.SetDateTime(minYearDate);
				//Set our sentinel time to the start date as we will be using it while generating item sources, we do not need to do this for end date
				m_tpCalendar.Hour = DATEPICKER_SENTINELTIME_HOUR;
				m_tpCalendar.Minute = DATEPICKER_SENTINELTIME_MINUTE;
				m_tpCalendar.Second = DATEPICKER_SENTINELTIME_SECOND;
				m_startDate = m_tpCalendar.GetDateTime();
				m_endDate = maxYearDate;

				// Find the number of years in our range
				m_tpCalendar.SetDateTime(m_startDate);
				CreateNewCalendar(strCalendarIdentifier, out spCalendar);
#if false // ifndef _PREFAST_ // PREfast bug DevDiv:554051
				System.Diagnostics.Debug.Assert(spCalendar);
#endif
				spCalendar.SetDateTime(m_endDate);

				GetYearDifference(m_tpCalendar, spCalendar, out m_numberOfYears);
				m_numberOfYears++; //since we should include both start and end years
			}
			else
			{
				// We do not want to display anything if we do not have a valid year range
				ClearSelectors(true /*Clear day*/, true /*Clear month*/, true/*Clear year*/);
			}

			UpdateOrderAndLayout();
		}

		// Create DatePickerAutomationPeer to represent the
		//override void OnCreateAutomationPeer(out xaml_automation_peers.IAutomationPeer** ppAutomationPeer)
		//{
		//	HRESULT hr = S_OK;
		//	xaml_automation_peers.IDatePickerAutomationPeer spDatePickerAutomationPeer;
		//	xaml_automation_peers.IDatePickerAutomationPeerFactory spDatePickerAPFactory;
		//	IActivationFactory spActivationFactory;
		//	DependencyObject spInner;

		//	IFCPTR(ppAutomationPeer);
		//	*ppAutomationPeer = null;

		//	spActivationFactory.Attach(ctl.ActivationFactoryCreator<DirectUI.DatePickerAutomationPeerFactory>.CreateActivationFactory());
		//	(spActivationFactory.As(&spDatePickerAPFactory));

		//	(spDatePickerAPFactory as DatePickerAutomationPeerFactory.CreateInstanceWithOwner(this,
		//		null,
		//		&spInner,
		//		&spDatePickerAutomationPeer));
		//	(spDatePickerAutomationPeer.CopyTo(ppAutomationPeer));

		//Cleanup:
		//	RRETURN(hr);
		//}

#if false
		void GetSelectedDateAsString(out string strPlainText)
		{
			DateTimeFormatter spFormatter;
			string strCalendarIdentifier;
			DateTimeOffset? date = default;

			GetSelectedDateOrTodaysDateIfNull(out date);

			strCalendarIdentifier = CalendarIdentifier;
			CreateNewFormatter("day month.full year", strCalendarIdentifier, out spFormatter);
			strPlainText = spFormatter.Format(date.Value);
		}
#endif

		void RefreshFlyoutButtonAutomationName()
		{
			// UNO TODO
			//if (m_tpFlyoutButton != null)
			//{
			//	string strParentAutomationName;
			//	strParentAutomationName = AutomationProperties.GetName(this);
			//	if (string.IsNullOrEmpty(strParentAutomationName))
			//	{
			//		var spHeaderAsInspectable = Header;
			//		if (spHeaderAsInspectable != null)
			//		{
			//			(FrameworkElement.GetStringFromObject(spHeaderAsInspectable, strParentAutomationName));
			//		}
			//	}
			//	string pszParent = strParentAutomationName;


			//	string strSelectedValue;
			//	GetSelectedDateAsString(out strSelectedValue);
			//	string pszSelectedValue = strSelectedValue;

			//	string strMsgFormat;
			//	DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_NAME_DATEPICKER, strMsgFormat.GetAddressOf()));
			//	string pszMsgFormat = strMsgFormat.GetRawBuffer(null);

			//	char szBuffer[MAX_PATH];
			//	int cchBuffer = 0;
			//	cchBuffer = FormatMsg(szBuffer, pszMsgFormat, pszParent, pszSelectedValue);

			//	// no charater wrote, szBuffer is blank don't update NameProperty
			//	if (cchBuffer > 0)
			//	{
			//		(DirectUI.AutomationProperties.SetNameStatic(m_tpFlyoutButton as Button, stringReference(szBuffer)));
			//	}
			//}
		}

		/* static */

		private static DateTimeOffset NullDateSentinel { get; } =
			new DateTimeOffset(DEFAULT_DATE_TICKS, TimeSpan.Zero);

		/* static */

		private static DateTimeOffset NullDateSentinelValue { get; } =
			new DateTimeOffset(DEFAULT_DATE_TICKS, TimeSpan.Zero);

		void GetTodaysDate(out DateTimeOffset? todaysDate)
		{
			if (m_todaysDate.ToUniversalTime() == NullDateSentinelValue)
			{
				Calendar calendar;
				string calendarIdentifier;

				calendarIdentifier = CalendarIdentifier;
				CreateNewCalendar(calendarIdentifier, out calendar);

#if false // ifndef _PREFAST_ // PREfast bug DevDiv:554051
				System.Diagnostics.Debug.Assert(calendar);
#endif
				calendar.SetToNow();
				m_todaysDate = calendar.GetDateTime();
				m_todaysDate = ClampDate(m_todaysDate, m_startDate, m_endDate);
			}

			todaysDate = m_todaysDate;
		}

		void GetSelectedDateOrTodaysDateIfNull(out DateTimeOffset? date)
		{
			DateTimeOffset? selectedDate;
			selectedDate = SelectedDate;

			if (selectedDate != null)
			{
				date = selectedDate;
			}
			else
			{
				GetTodaysDate(out date);
			}
		}
	}
}

