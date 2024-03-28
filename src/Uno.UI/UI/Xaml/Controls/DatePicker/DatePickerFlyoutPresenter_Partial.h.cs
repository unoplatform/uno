using System;
using System.Collections.Generic;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Xaml.Controls.Primitives;

using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class DatePickerFlyoutPresenter : IDatePickerFlyoutPresenter
	{
		//// public
		//    DatePickerFlyoutPresenter();

		//    // FrameworkElementOverrides
		//     void
		//    OnApplyTemplateImpl() override;

		//    // UIElementOverrides
		//     void
		//    OnCreateAutomationPeerImpl(
		//        out  xaml.Automation.Peers.IAutomationPeer returnValue) override;

		//    // IDatePickerFlyoutPresenter
		//     void OnPropertyChanged(
		//         xaml.IDependencyPropertyChangedEventArgs pArgs);

		//    static  void GetDefaultIsDefaultShadowEnabled(out  DependencyObject ppIsDefaultShadowEnabledValue);

		//     void PullPropertiesFromOwner( xaml_controls.IDatePickerFlyout pOwner);

		//     void SetAcceptDismissButtonsVisibility( bool isVisible);

		//     void GetDate(out DateTime pDate);

		//// protected
		//     void OnKeyDownImpl( xaml_input.IKeyRoutedEventArgs pEventArgs) override sealed;

		//// private
		//    ~DatePickerFlyoutPresenter() {}

		//     void InitializeImpl() override;

		//    // Creates a new DateTimeFormatter with the given parameters.
		//     void CreateNewFormatter(
		//         string strPattern,
		//         string strCalendarIdentifier,
		//        out  wg.DateTimeFormatting.IDateTimeFormatter ppDateTimeFormatter);

		//    // Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		//    // representing the years in our date range. If there isn't a cached DateTimeFormatter instance,
		//    // creates one and caches it to be returned for the following calls with the same pair.
		//     void GetYearFormatter(
		//         string strCalendarIdentifier,
		//        out  wg.DateTimeFormatting.IDateTimeFormatter ppPrimaryDateTimeFormatter);

		//    // Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		//    // representing the months in our date range. If there isn't a cached DateTimeFormatter instance,
		//    // creates one and caches it to be returned for the following calls with the same pair.
		//     void GetMonthFormatter(
		//         string strCalendarIdentifier,
		//        out  wg.DateTimeFormatting.IDateTimeFormatter ppPrimaryDateTimeFormatter);

		//    // Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		//    // representing the days in our date range. If there isn't a cached DateTimeFormatter instance,
		//    // creates one and caches it to be returned for the following calls with the same pair. Also returns a
		//    // formatter that will be used for
		//     void GetDayFormatter(
		//         string strCalendarIdentifier,
		//        out  wg.DateTimeFormatting.IDateTimeFormatter ppPrimaryDateTimeFormatter);

		//    // Clears everything and refreshes the helper objects. After that, generates and
		//    // sets the itemssources to selectors.
		//      void RefreshSetup();

		//     // We execute our logic depending on some state information such as start date, end date, number of years etc. These state
		//     // variables need to be updated whenever a public property change occurs which affects them.
		//      void UpdateState();

		//     // Creates a new wg.Calendar, taking into account the Calendar Identifier
		//     // represented by our public "Calendar" property.
		//      void CreateNewCalendar(
		//         string strCalendarIdentifier,
		//        ref wrl.ComPtr<wg.Calendar> pspCalendar);

		//     // Clamps the given date within the range defined by the min and max dates. Note that it is caller's responsibility
		//     // to feed appropriate min/max values that defines a valid date range.
		//     DateTime ClampDate(
		//          DateTime date,
		//          DateTime minDate,
		//          DateTime maxDate);

		//     // Reacts to change in selection of our selectors. Calculates the new date represented by the selected indices and updates the
		//     // Date property.
		//      void OnSelectorSelectionChanged(
		//         DependencyObject pSender,
		//         xaml_controls.ISelectionChangedEventArgs pArgs);

		//     // Reacts to the changes in string typed properties. Reverts the property value to the last valid value,
		//     // if property change causes an exception.
		//      void OnCalendarIdentifierPropertyChanged(
		//         string oldValue);

		//      void SetDate( DateTime newDate);

		//     // Reacts to changes in Date property. Day may have changed programmatically or end user may have changed the
		//     // selection of one of our selectors causing a change in Date.
		//      void OnDateChanged(
		//         DateTime oldValue,
		//         DateTime newValue);

		//     // Given two calendars, finds the difference of years between them. Note that we are counting on the two
		//     // calendars will have the same system.
		//      void GetYearDifference(
		//         Calendar pStartCalendar,
		//         Calendar pEndCalendar,
		//        ref int difference);

		//     // Clears the ItemsSource and SelectedItem properties of the selectors.
		//      void ClearSelectors(
		//          bool clearDay,
		//          bool clearMonth,
		//          bool clearYear);

		//     // Generate the collection that we will populate our year picker with.
		//      void GenerateYears();

		//     // Generate the collection that we will populate our month picker with.
		//      void GenerateMonths(
		//         int yearOffset);

		//     // Generate the collection that we will populate our day picker with.
		//      void GenerateDays(
		//         int yearOffset,
		//         int monthOffset);

		//     // Regenerate the itemssource for the day/month/yearpickers and select the appropriate indices that represent the current DateTime.
		//     // Depending on which field changes we might not need to refresh some of the sources.
		//      void RefreshSourcesAndSetSelectedIndices(
		//          bool refreshDay,
		//          bool refreshMonth,
		//          bool refreshYear);

		//     // Get indices of related fields of current Date for generated itemsources.
		//      void GetIndices(
		//        ref int yearIndex,
		//        ref int monthIndex,
		//        ref int dayIndex);

		//      void OnCalendarChanged(
		//         DependencyObject pOldValue,
		//         DependencyObject pNewValue);

		//     // Interprets the selected indices of the selectors and creates and returns a DateTime corresponding to the date represented by these
		//     // indices.
		//      void GetDateFromIndices(
		//         int yearIndex,
		//         int monthIndex,
		//         int dayIndex,
		//        out DateTime date);

		//     // The order of date fields vary depending on geographic region, calendar type etc. This function determines the M/D/Y order using
		//     // globalization APIs. It also determines whether the fields should be laid RTL.
		//      void GetOrder(
		//        out int yearOrder,
		//        out int monthOrder,
		//        out int dayOrder,
		//        out bool isRTL);

		//     // Updates the order of selectors in our layout. Also takes care of hiding/showing the comboboxes and related spacing depending our
		//     // public properties set by the user.
		//      void UpdateOrderAndLayout();

		// The selection of the selectors in our template can be changed by two sources. First source is
		// the end user changing a field to select the desired date. Second source is us updating
		// the itemssources and selected indices. We only want to react to the first source as the
		// second one will cause an unintentional recurrence in our logic. So we use this locking mechanism to
		// anticipate selection changes caused by us and making sure we do not react to them. It is okay
		// that these locks are not atomic since they will be only accessed by a single thread so no race
		// condition can occur.
		void AllowReactionToSelectionChange()
		{
			_reactionToSelectionChangeAllowed = true;
		}

		void PreventReactionToSelectionChange()
		{
			_reactionToSelectionChangeAllowed = false;
		}

		bool IsReactionToSelectionChangeAllowed()
		{
			return _reactionToSelectionChangeAllowed;
		}

		IList<object> _tpDaySource;
		IList<object> _tpMonthSource;
		IList<object> _tpYearSource;

		// Reference to daypicker Selector. We need this as we will change its item
		// source as our properties change.
		LoopingSelector _tpDayPicker;

		// Reference to monthpicker Selector. We need this as we will change its item
		// source as our properties change.
		LoopingSelector _tpMonthPicker;

		// Reference to yearpicker Selector. We need this as we will change its item
		// source as our properties change.
		LoopingSelector _tpYearPicker;

		// Reference the picker selectors by order of appearance.
		// Used for arrow navigation, so stored as IControls for easy access
		// to the Focus() method.
		Control _tpFirstPickerAsControl;
		Control _tpSecondPickerAsControl;
		Control _tpThirdPickerAsControl;

		// References to the hosting borders.
		Border _tpFirstPickerHost;
		Border _tpSecondPickerHost;
		Border _tpThirdPickerHost;

		// Reference to the Grid that will hold the LoopingSelectors and the spacers.
		Grid _tpPickerHostGrid;

		// References to the columns of the Grid that will hold the day/month/year LoopingSelectors and the spacers.
		ColumnDefinition _tpDayColumn;
		ColumnDefinition _tpMonthColumn;
		ColumnDefinition _tpYearColumn;
		ColumnDefinition _tpFirstSpacerColumn;
		ColumnDefinition _tpSecondSpacerColumn;

		// References to elements which will act as the dividers between the LoopingSelectors.
		UIElement _tpFirstPickerSpacing;
		UIElement _tpSecondPickerSpacing;

		// Reference to the background border
		Border _tpBackgroundBorder;

		// Reference to the title presenter
		TextBlock _tpTitlePresenter;

		// Reference to our content panel. We will be setting the flowdirection property on our root to achieve
		// RTL where necessary.
		FrameworkElement _tpContentPanel;

		// References to the elements for the accept and dismiss buttons.
		UIElement _tpAcceptDismissHostGrid;
		UIElement _tpAcceptButton;
		UIElement _tpDismissButton;

		bool _acceptDismissButtonsVisible;

		// This calendar will be used over and over while we are generating the ItemsSources instead
		// of creating new calendars.
		Calendar _tpCalendar;
		Calendar _tpBaselineCalendar;

		// We use Gregorian Calendar while calculating the default values of Min and Max years. Instead of creating
		// a new calendar every time these values are reached, we create one and reuse it.
		//Calendar _tpGregorianCalendar;

		// Cached DateTimeFormatter for year and Calendar - Format pair it is related to.
		DateTimeFormatter _tpPrimaryYearFormatter;
		string _strYearCalendarIdentifier;

		// Cached DateTimeFormatter for year and Calendar - Format pair it is related to.
		DateTimeFormatter _tpPrimaryMonthFormatter;
		string _strMonthCalendarIdentifier;

		// Cached DateTimeFormatter for year and Calendar - Format pair it is related to.
		DateTimeFormatter _tpPrimaryDayFormatter;
		string _strDayCalendarIdentifier;

		// Represent the first date choosable by datepicker. Note that the year of this date can be
		// different from the MinYear as the MinYear value can be unrepresentable depending on the
		// type of the calendar.
		DateTime _startDate;

		// The year of this date is the latest year that can be selectable by the date picker. Note that
		// month and date values do not necessarily represent the end date of  our date picker since we
		// do not need that information readily. Also note that, this year may be different from the MaxYear
		// since MaxYear may be unrepresentable depending on the calendar.
		DateTime _endDate;

		// See the comment of AllowReactionToSelectionChange method for use of this variable.
		bool _reactionToSelectionChangeAllowed;

		//bool _isInitializing;

		// Specifies if we have a valid year range to generate dates. We do not have a valid range if our minimum year is
		// greater than our maximum year.
		bool _hasValidYearRange;

		int _numberOfYears;

		// Properties pulled from owner DatePickerFlyout
		string _calendarIdentifier;
		string _title;
		bool _dayVisible;
		bool _monthVisible;
		bool _yearVisible;
		DateTime _minYear;
		DateTime _maxYear;
		DateTime _date; // Will be modified to reflect the user's selection
		string _dayFormat;
		string _monthFormat;
		string _yearFormat;
	};
}

