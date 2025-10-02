using System;
using System.Threading.Tasks;
using Windows.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.Extensions;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.Globalization.DateTimeFormatting;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using AwesomeAssertions.Execution;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using System.Threading;


#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Uno.Logging;
#endif

namespace Microsoft.UI.Tests.Controls.DatePickerTests
{
	[TestClass]
	public class DatePickerIntegrationTests
	{
		// https://github.com/microsoft/CsWinRT/blob/7dc82799c7afeaf862c9fb7af78ad0e2fc03c48e/src/WinRT.Runtime/Projections/SystemTypes.cs#L71
		private const long ManagedUtcTicksAtNativeZero = 504911232000000000;

		[TestInitialize]
		public void ClassSetup()
		{
			TestServices.EnsureInitialized();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			TestServices.WindowHelper.VerifyTestCleanup();

			ApplicationLanguages.PrimaryLanguageOverride = null;
		}


		private DateTimeOffset CreateDate(int month = 6, int day = 15, int year = 1990)
		{
			return new DateTimeOffset(year, month, day, 1, 0, 0, 0, TimeSpan.Zero);
		}

		private Calendar CreateCalendar(DateTimeOffset date)
		{
			var calendar = new Calendar();
			calendar.SetDateTime(date);
			return calendar;

		}

		private Calendar CreateCalendar(int month = 6, int day = 15, int year = 1990)
		{
			return CreateCalendar(CreateDate(month, day, year));
		}

		private bool AreClose(double a, double b, double threshold)
		{
			return Math.Abs(a - b) <= threshold;
		}

		//
		// Test Cases
		//
		[TestMethod]
		public async Task VerifyDefaultProperties()
		{
			DatePicker datePicker = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker = new DatePicker();

				datePicker.Header = "DatePicker P0";

				TestServices.WindowHelper.WindowContent = datePicker;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				using var _ = new AssertionScope();

				datePicker.CalendarIdentifier.Should().Be(CalendarIdentifiers.Gregorian);
				datePicker.Orientation.Should().Be(Orientation.Horizontal);
				datePicker.DayFormat.Should().Be("day");
				datePicker.MonthFormat.Should().Be("{month.full}");
				datePicker.YearFormat.Should().Be("year.full");
				datePicker.DayVisible.Should().BeTrue();
				datePicker.MonthVisible.Should().BeTrue();
				datePicker.YearVisible.Should().BeTrue();

				var today = new Calendar();
				today.SetToNow();

				// Default value of Date should be the null sentinel value.
				datePicker.Date.WindowsFoundationUniversalTime().Should().Be(0);
				datePicker.SelectedDate.Should().BeNull();

				// Default value of MinYear should be 100 years ago.
				var datePickerMinYear = new Calendar();
				datePickerMinYear.SetDateTime(datePicker.MinYear);
				datePickerMinYear.Year.Should().Be(today.Year - 100);

				// Default value of MaxYear should be 100 years from now.
				var datePickerMaxYear = new Calendar();
				datePickerMaxYear.SetDateTime(datePicker.MaxYear);
				datePickerMaxYear.Year.Should().Be(today.Year + 100);
			});
		}

		[TestMethod]
		public async Task CanFireDateChangedEvent()
		{
			var datePickerValueChangedEvent = new TaskCompletionSource<object>();

			var datePicker = await SetupDatePickerTest();

			var cts = new CancellationTokenSource(1000);
			cts.Token.Register(() => datePickerValueChangedEvent.TrySetException(new TimeoutException()));

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var calendarNow = new Calendar();
				calendarNow.SetToNow();

				datePicker.SelectedDateChanged += OnDatePickerOnSelectedDateChanged;

				void OnDatePickerOnSelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
				{
					using var _ = new AssertionScope("OnDatePickerOnSelectedDateChanged");

					this.Log().Info("CanFireDateChangedEvent: SelectedDateChanged event fired.");

					Assert.IsNull(args.OldDate);

					var calendarNew = new Calendar();
					calendarNew.SetDateTime(args.NewDate.Value);

					calendarNew.Year.Should().Be(calendarNow.Year);
					calendarNew.Month.Should().Be(calendarNow.Month);
					calendarNew.Day.Should().Be(calendarNow.Day);
				}

				this.Log().Info("CanFireDateChangedEvent: Execute date change from null to today.");
				datePicker.SelectedDate = (calendarNow.GetDateTime());

				datePicker.SelectedDateChanged -= OnDatePickerOnSelectedDateChanged;

				var calendarChanged = new Calendar();
				calendarChanged.Year = 1990;
				calendarChanged.Month = 12;
				calendarChanged.Day = 31;

				datePicker.DateChanged += OnDatePickerOnDateChanged;

				void OnDatePickerOnDateChanged(object sender, DatePickerValueChangedEventArgs args)
				{
					using var _ = new AssertionScope("OnDatePickerOnDateChanged");

					this.Log().Info("CanFireDateChangedEvent: DateChanged event fired.");

					var calendarOld = new Calendar();
					calendarOld.SetDateTime(args.OldDate);

					var calendarNew = new Calendar();
					calendarNew.SetDateTime(args.NewDate);

					calendarOld.Year.Should().Be(calendarNow.Year);
					calendarOld.Month.Should().Be(calendarNow.Month);
					calendarOld.Day.Should().Be(calendarNow.Day);
					calendarNew.Year.Should().Be(1990);
					calendarNew.Month.Should().Be(12);
					calendarNew.Day.Should().Be(31);

					datePickerValueChangedEvent.TrySetResult(null);
				}

				this.Log().Info("CanFireDateChangedEvent: Execute date change from today to 12/31/1990.");
				datePicker.SelectedDate = (calendarChanged.GetDateTime());
				datePicker.DateChanged -= OnDatePickerOnDateChanged;
			});

			await datePickerValueChangedEvent.Task;
		}

#if LOOPING_SELECTOR_AVAILABLE // TODO: Remove this when LoopingSelector is available https://github.com/unoplatform/uno/issues/4880
		[TestMethod]
		public async Task CanChooseDate()
		{
			// Verify that the DatePicker control can be used to choose a Date.
			// We verify:
			//   1. The DateChanged event is fired.
			//         The DatePickerValueChangedEventArgs OldDate and NewDate properties should be correct.
			//   2. DatePicker.Date property gets updated as appropriate.
			var datePicker = await SetupDatePickerTest();

			var initialDate = CreateDate(10, 15, 2015);
			var dateToSelect = CreateDate(8, 18, 2013);
			var minYear = CreateDate(1, 1, 1950);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.SelectedDate = (initialDate);
				datePicker.MinYear = minYear;
			});

			var dateChangedEvent = new TaskCompletionSource<object>();

			var cts = new CancellationTokenSource(1000);
			cts.Token.Register(() => dateChangedEvent.TrySetException(new TimeoutException()));

			datePicker.DateChanged += OnDatePickerOnDateChanged;

			void OnDatePickerOnDateChanged(object sender, DatePickerValueChangedEventArgs args)
			{
				Assert.AreEqual(initialDate, args.OldDate);
				Assert.AreEqual(dateToSelect, args.NewDate);

				dateChangedEvent.TrySetResult(null);
			}

			DateTimePickerHelper.OpenDateTimePicker(datePicker);
			TestServices.WindowHelper.WaitForIdle();

			DateTimePickerHelper.SelectDateInOpenDatePickerFlyout(dateToSelect, minYear.Year, LoopingSelectorHelper.SelectionMode.Keyboard);
			await dateChangedEvent.Task;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Assert.AreEqual(dateToSelect, datePicker.Date);
			});
			TestServices.WindowHelper.WaitForIdle();
		}
#endif
		private async Task<DatePicker> SetupDatePickerTest()
		{
			DatePicker datePicker = null;

			var loadedEvent = new TaskCompletionSource<object>();

			var cts = new CancellationTokenSource(1000);
			cts.Token.Register(() => loadedEvent.TrySetException(new TimeoutException()));

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var rootGrid = new Grid();
				TestServices.WindowHelper.WindowContent = rootGrid;

				datePicker = new DatePicker();

				datePicker.Loaded += (snd, evt) => loadedEvent.SetResult(null);

				rootGrid.Children.Add(datePicker);
			});

			await loadedEvent.Task;

			await TestServices.WindowHelper.WaitForIdle();

			return datePicker;
		}

		[Ignore] // Unstable test
		[TestMethod]
		public async Task ValidateDayMonthYearOrderForDifferentLocales()
		{
			using var _ = Disposable.Create(() => ApplicationLanguages.PrimaryLanguageOverride = null);

			// In this test, we force the app into a specified locale, and verify that the DatePicker lays out correctly.
			// We validate:
			// 1. The Day/Month/Year Columns are in the appropriate order.
			// 2. The Day/Month/Year TextBlocks are in the appropriate order.
			// 3. The FlowDirection is set correctly.
			// Note: In the DatePicker template's Grid the columns 1 and 3 are used by the cell dividers, so they are skipped.
			// That is why the expected column positions for day/month/year are {0, 2, 4} and not {0, 1, 2}.

			await VerifyDayMonthYearOrderAndFlowDirection("en-US", 2, 0, 4, FlowDirection.LeftToRight);
			await VerifyDayMonthYearOrderAndFlowDirection("en-GB", 0, 2, 4, FlowDirection.LeftToRight);
			await VerifyDayMonthYearOrderAndFlowDirection("ar", 0, 2, 4, FlowDirection.RightToLeft);
			await VerifyDayMonthYearOrderAndFlowDirection("ts-ZA", 4, 2, 0, FlowDirection.LeftToRight);
		}

		private async Task VerifyDayMonthYearOrderAndFlowDirection(string locale, int expectedDayTextBlockColumn, int
			expectedMonthTextBlockColumn, int expectedYearTextBlockColumn, FlowDirection expectedFlowDirection)
		{
			// This forces the app to use the specified language/locale:
			this.Log().InfoFormat("VerifyDayMonthYearOrder: Setting ApplicationLanguages.PrimaryLanguageOverride to {0}", locale);
			global::Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = locale;

			var datePicker = await SetupDatePickerTest();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var templateRoot = (FrameworkElement)(VisualTreeHelper.GetChild(datePicker, 0));
				Assert.IsNotNull(templateRoot);

				var dayTextBlock = (TextBlock)(templateRoot.FindName("DayTextBlock"));
				var monthTextBlock = (TextBlock)(templateRoot.FindName("MonthTextBlock"));
				var yearTextBlock = (TextBlock)(templateRoot.FindName("YearTextBlock"));
				Assert.IsNotNull(dayTextBlock);
				Assert.IsNotNull(monthTextBlock);
				Assert.IsNotNull(yearTextBlock);

				var dayColumn = (ColumnDefinition)(templateRoot.FindName("DayColumn"));
				var monthColumn = (ColumnDefinition)(templateRoot.FindName("MonthColumn"));
				var yearColumn = (ColumnDefinition)(templateRoot.FindName("YearColumn"));
				Assert.IsNotNull(dayColumn);
				Assert.IsNotNull(monthColumn);
				Assert.IsNotNull(yearColumn);

				var flyoutButtonContentGrid = (Grid)(templateRoot.FindName("FlyoutButtonContentGrid"));
				Assert.IsNotNull(flyoutButtonContentGrid);

				Assert.AreEqual(expectedFlowDirection, flyoutButtonContentGrid.FlowDirection);

				Assert.AreEqual(expectedDayTextBlockColumn, Grid.GetColumn(dayTextBlock));
				Assert.AreEqual(expectedMonthTextBlockColumn, Grid.GetColumn(monthTextBlock));
				Assert.AreEqual(expectedYearTextBlockColumn, Grid.GetColumn(yearTextBlock));

				bool isFound = false;
				int indexOfDayColumn = 0;
				int indexOfMonthColumn = 0;
				int indexOfYearColumn = 0;

				isFound = (indexOfDayColumn = flyoutButtonContentGrid.ColumnDefinitions.IndexOf(dayColumn)) == 0;
				Assert.IsTrue(isFound /* dayColumn */);
				Assert.AreEqual(expectedDayTextBlockColumn, (int)indexOfDayColumn);

				isFound = (indexOfMonthColumn = flyoutButtonContentGrid.ColumnDefinitions.IndexOf(monthColumn)) == 0;
				Assert.IsTrue(isFound /* monthColumn */);
				Assert.AreEqual(expectedMonthTextBlockColumn, (int)indexOfMonthColumn);

				isFound = (indexOfMonthColumn = flyoutButtonContentGrid.ColumnDefinitions.IndexOf(yearColumn)) == 0;
				Assert.IsTrue(isFound /* yearColumn */);
				Assert.AreEqual(expectedYearTextBlockColumn, (int)indexOfYearColumn);
			});
		}

#if VALIDATE_UITREE_IMPLEMENTED
		[TestMethod]
		public async Task ValidateUIElementTree()
		{
			ControlHelper.ValidateUIElementTree(
					wf.Size(500, 600),
					1.f,
				[]()
			{
				var calendar = new Calendar();
				calendar.Year = 1990;
				calendar.Month = 12;
				calendar.Day = 31;
				var testDateTime = calendar.GetDateTime();

				DatePicker restDatePicker = null;
				DatePicker pointerOverDatePicker = null;
				DatePicker pressedDatePicker = null;
				DatePicker disabledDatePicker = null;

				DatePicker focusedRestDatePicker = null;
				DatePicker focusedPointerOverDatePicker = null;

				StackPanel rootPanel = null;

				await RunOnUIThread.ExecuteAsync(() =>
				{
					rootPanel = new StackPanel();

					restDatePicker = new DatePicker();
					restDatePicker.Header = "Rest";
					restDatePicker.SelectedDate = (testDateTime);
					restDatePicker.LightDismissOverlayMode = LightDismissOverlayMode.Off;
					rootPanel.Children.Add(restDatePicker);

					pointerOverDatePicker = new DatePicker();
					pointerOverDatePicker.Header = "Hover";
					pointerOverDatePicker.SelectedDate = (testDateTime);
					pointerOverDatePicker.LightDismissOverlayMode = LightDismissOverlayMode.Off;
					rootPanel.Children.Add(pointerOverDatePicker);

					pressedDatePicker = new DatePicker();
					pressedDatePicker.Header = "Pressed";
					pressedDatePicker.SelectedDate = (testDateTime);
					pressedDatePicker.LightDismissOverlayMode = LightDismissOverlayMode.Off;
					rootPanel.Children.Add(pressedDatePicker);

					disabledDatePicker = new DatePicker();
					disabledDatePicker.Header = "Disabled";
					disabledDatePicker.SelectedDate = (testDateTime);
					disabledDatePicker.IsEnabled = false;
					disabledDatePicker.LightDismissOverlayMode = LightDismissOverlayMode.Off;
					rootPanel.Children.Add(disabledDatePicker);

					focusedRestDatePicker = new DatePicker();
					focusedRestDatePicker.Header = "Focused";
					focusedRestDatePicker.SelectedDate = (testDateTime);
					focusedRestDatePicker.LightDismissOverlayMode = LightDismissOverlayMode.Off;
					rootPanel.Children.Add(focusedRestDatePicker);

					focusedPointerOverDatePicker = new DatePicker();
					focusedPointerOverDatePicker.Header = "Focused Hover";
					focusedPointerOverDatePicker.SelectedDate = (testDateTime);
					focusedPointerOverDatePicker.LightDismissOverlayMode = LightDismissOverlayMode.Off;
					rootPanel.Children.Add(focusedPointerOverDatePicker);

					TestServices.WindowHelper.WindowContent = rootPanel;
				});

				TestServices.WindowHelper.WaitForIdle();

				var pointerOverFlyoutButton = await GetFlyoutButtonFromDatePicker(pointerOverDatePicker);
				var pressedFlyoutButton = await GetFlyoutButtonFromDatePicker(pressedDatePicker);
				var focusedRestFlyoutButton = await GetFlyoutButtonFromDatePicker(focusedRestDatePicker);
				var focusedPointerOverFlyoutButton = await GetFlyoutButtonFromDatePicker(focusedPointerOverDatePicker);

				await RunOnUIThread.ExecuteAsync(() =>
				{
					VisualStateManager.GoToState(pointerOverFlyoutButton, "PointerOver", false);

					VisualStateManager.GoToState(pressedFlyoutButton, "Pressed", false);

					VisualStateManager.GoToState(focusedRestFlyoutButton, "Focused", false);

					VisualStateManager.GoToState(focusedPointerOverFlyoutButton, "PointerOver", false);
					VisualStateManager.GoToState(focusedPointerOverFlyoutButton, "Focused", false);
				});
				TestServices.WindowHelper.WaitForIdle();

				return rootPanel;
			});
		}
#endif

#if FOCUS_IMPLEMENTED
		[TestMethod]
		public async Task CanOpenAndCloseUsingKeyboard()
		{
			var datePicker = await SetupDatePickerTest();

			var dateChangedEvent = new TaskCompletionSource<object>();

			var cts = new CancellationTokenSource(1000);
			cts.Token.Register(() => dateChangedEvent.TrySetException(new TimeoutException()));

			var dateChangedRegistration = CreateSafeEventRegistration(DatePicker, DateChanged);
			dateChangedRegistration.Attach(datePicker, [&]() {
				dateChangedEvent.Set();
			});

			this.Log().Info("Ensuring datePicker has focus.");
			FocusTestHelper.EnsureFocus(datePicker, FocusState.Keyboard);

			this.Log().Info("Try to open and close DatePicker using space key press");
			DateTimePickerHelper.OpenAndCloseDateTimePickerUsingKeyboard(" ", " ", dateChangedEvent);

			this.Log().Info("Try to open and close DatePicker using enter");
			DateTimePickerHelper.OpenAndCloseDateTimePickerUsingKeyboard("$d$_enter#$u$_enter", "$d$_enter#$u$_enter",
				dateChangedEvent);

			this.Log().Info("Try to open and close DatePicker using Alt+Down");
			DateTimePickerHelper.OpenAndCloseDateTimePickerUsingKeyboard("$d$_alt#$d$_down#$u$_down#$u$_alt",
				"$d$_alt#$d$_down#$u$_down#$u$_alt", dateChangedEvent);

			this.Log().Info("Try to open and close DatePicker using Alt+Up");
			DateTimePickerHelper.OpenAndCloseDateTimePickerUsingKeyboard("$d$_alt#$d$_up#$u$_up#$u$_alt",
				"$d$_alt#$d$_up#$u$_up#$u$_alt", dateChangedEvent);
		}
#endif

		[TestMethod]
		[DataRow("GregorianCalendar")]
		//[DataRow("HijriCalendar")]
		[DataRow("JapaneseCalendar")]
		[DataRow("JulianCalendar")]
		[DataRow("KoreanCalendar")]
		[DataRow("TaiwanCalendar")]
		[DataRow("ThaiCalendar")]
		//[DataRow("UmAlQuraCalendar")]

		// HebrewCalendar fails
		// AssertFailedException: Expected string to be "11", but "13" differs near "3" (index 1).
		// Expected string to be "February" with a length of 8, but "January" has a length of 7, differs near "Jan" (index 0).
		//[DataRow("HebrewCalendar")]

		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task ValidateCalendarIdentifierProperty(string cid)
		{
			ApplicationLanguages.PrimaryLanguageOverride = "en-US";

			// Validate that the DatePicker.CalendarIdentifier property causes the date to be displayed in format for that Calendar
			// We are not attempting to validate the correctness of what gets displayed. Only that it matches DateTimeFormatter returns for that calendar.

			this.Log().InfoFormat("Testing DatePicker with Calendar: {0}", cid);

			var calendar = new Calendar();
			calendar.Year = 2015;
			calendar.Month = 2;
			calendar.Day = 11;
			calendar.Hour = 1;
			calendar.ChangeCalendarSystem(cid);

			var dtf = new DateTimeFormatter("{month.full}");

			dtf = new DateTimeFormatter("{month.full}", dtf.Languages, dtf.GeographicRegion,
				cid, dtf.Clock);
			var expectedMonthString = dtf.Format(calendar.GetDateTime());
			dtf = new DateTimeFormatter("day", dtf.Languages, dtf.GeographicRegion, cid,
				dtf.Clock);
			var expectedDayString = dtf.Format(calendar.GetDateTime());
			dtf = new DateTimeFormatter("year.full", dtf.Languages, dtf.GeographicRegion, cid,
				dtf.Clock);
			var expectedYearString = dtf.Format(calendar.GetDateTime());

			var datePicker = await SetupDatePickerTest();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.CalendarIdentifier = cid;
				datePicker.SelectedDate = (calendar.GetDateTime());
			});
			await TestServices.WindowHelper.WaitForIdle();

			TextBlock dayTextBlock;
			TextBlock monthTextBlock;
			TextBlock yearTextBlock;
			(dayTextBlock, monthTextBlock, yearTextBlock) = await GetDayMonthYearTextBlocksFromDatePicker(datePicker);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				using var _ = new AssertionScope();

				dayTextBlock.Text.Should().Be(expectedDayString);
				monthTextBlock.Text.Should().Be(expectedMonthString);
				yearTextBlock.Text.Should().Be(expectedYearString);
			});
		}

		[TestMethod]
		public async Task ValidateDayMonthYearFormatProperties()
		{
			string dayFormat = "{day.integer(2)}";
			string monthFormat = "{month.abbreviated}";
			string yearFormat = "{year.abbreviated(2)}";

			var date = CreateDate();

			var expectedDayString = (new DateTimeFormatter(dayFormat)).Format(date);
			var expectedMonthString = (new DateTimeFormatter(monthFormat)).Format(date);
			var expectedYearString = (new DateTimeFormatter(yearFormat)).Format(date);

			var datePicker = await SetupDatePickerTest();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.DayFormat = dayFormat;
				datePicker.MonthFormat = monthFormat;
				datePicker.YearFormat = yearFormat;
				datePicker.SelectedDate = (date);
			});

			await TestServices.WindowHelper.WaitForIdle();

			TextBlock dayTextBlock;
			TextBlock monthTextBlock;
			TextBlock yearTextBlock;
			(dayTextBlock, monthTextBlock, yearTextBlock) = await GetDayMonthYearTextBlocksFromDatePicker(datePicker);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Assert.AreEqual(expectedDayString, dayTextBlock.Text);
				Assert.AreEqual(expectedMonthString, monthTextBlock.Text);
				Assert.AreEqual(expectedYearString, yearTextBlock.Text);
			});

			// Verify that we can update the properties of DatePicker
			// We want to make sure that the updated values get applied correctly and the old values don't end up getting used.
			await RunOnUIThread.ExecuteAsync(() =>
			{
				dayFormat = "dayofweek";
				monthFormat = "month.numeric";
				yearFormat = "year";
				expectedDayString = (new DateTimeFormatter(dayFormat)).Format(date);
				expectedMonthString = (new DateTimeFormatter(monthFormat)).Format(date);
				expectedYearString = (new DateTimeFormatter(yearFormat)).Format(date);
				datePicker.DayFormat = dayFormat;
				datePicker.MonthFormat = monthFormat;
				datePicker.YearFormat = yearFormat;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Assert.AreEqual(expectedDayString, dayTextBlock.Text);
				Assert.AreEqual(expectedMonthString, monthTextBlock.Text);
				Assert.AreEqual(expectedYearString, yearTextBlock.Text);
			});
		}

		[TestMethod]
		[Ignore] // this test is not compatible with Uno yet
		public async Task ValidateDayMonthYearVisibleProperties()
		{
			// Verifies that DayVisibile, MonthVisible and YearVisible properties have the appropriate effect.
			// We set these properties and verify:
			//   The corresponding TextBlock gets Collapsed.
			//   The corresponding ColumnDefinition gets removed from the FlyoutButtonContentGrid.ColumnDefinitions

			var datePicker = await SetupDatePickerTest();

			TextBlock dayTextBlock = default;
			TextBlock monthTextBlock = default;
			TextBlock yearTextBlock = default;
			ColumnDefinition dayColumn = default;
			ColumnDefinition monthColumn = default;
			ColumnDefinition yearColumn = default;
			Grid flyoutButtonContentGrid = default;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var templateRoot = (FrameworkElement)(VisualTreeHelper.GetChild(datePicker, 0));
				Assert.IsNotNull(templateRoot);

				dayTextBlock = (TextBlock)(templateRoot.FindName("DayTextBlock"));
				monthTextBlock = (TextBlock)(templateRoot.FindName("MonthTextBlock"));
				yearTextBlock = (TextBlock)(templateRoot.FindName("YearTextBlock"));
				Assert.IsNotNull(dayTextBlock);
				Assert.IsNotNull(monthTextBlock);
				Assert.IsNotNull(yearTextBlock);

				dayColumn = (ColumnDefinition)(templateRoot.FindName("DayColumn"));
				monthColumn = (ColumnDefinition)(templateRoot.FindName("MonthColumn"));
				yearColumn = (ColumnDefinition)(templateRoot.FindName("YearColumn"));
				Assert.IsNotNull(dayColumn);
				Assert.IsNotNull(monthColumn);
				Assert.IsNotNull(yearColumn);

				flyoutButtonContentGrid = (Grid)(templateRoot.FindName("FlyoutButtonContentGrid"));
				Assert.IsNotNull(flyoutButtonContentGrid);
			});

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.DayVisible = false;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await RunOnUIThread.ExecuteAsync(() =>
			{
				Assert.AreEqual(Visibility.Collapsed, dayTextBlock.Visibility);
				Assert.AreNotEqual(0, flyoutButtonContentGrid.ColumnDefinitions.IndexOf(dayColumn));

				datePicker.DayVisible = true;
				datePicker.MonthVisible = false;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await RunOnUIThread.ExecuteAsync(() =>
			{
				Assert.AreEqual(Visibility.Visible, dayTextBlock.Visibility);
				Assert.AreEqual(0, flyoutButtonContentGrid.ColumnDefinitions.IndexOf(dayColumn));

				Assert.AreEqual(Visibility.Collapsed, monthTextBlock.Visibility);
				Assert.AreNotEqual(0, flyoutButtonContentGrid.ColumnDefinitions.IndexOf(monthColumn));

				datePicker.MonthVisible = true;
				datePicker.YearVisible = false;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await RunOnUIThread.ExecuteAsync(() =>
			{
				Assert.AreEqual(Visibility.Visible, monthTextBlock.Visibility);
				Assert.AreEqual(0, flyoutButtonContentGrid.ColumnDefinitions.IndexOf(monthColumn));

				Assert.AreEqual(Visibility.Collapsed, yearTextBlock.Visibility);
				Assert.AreNotEqual(0, flyoutButtonContentGrid.ColumnDefinitions.IndexOf(yearColumn));
			});
		}

		[TestMethod]
		public async Task ValidateMinYearAndMaxYearProperties()
		{
			var datePicker = await SetupDatePickerTest();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.MinYear =
					CreateDate(7, 15, 1980); //Only the Year component should have any effect.
				datePicker.MaxYear = CreateDate(3, 21, 2010);

				// Try to set a Date that is too large, it should get clamped by MaxYear.
				datePicker.SelectedDate = (CreateDate(6, 6, 2020));

				// The date should get clamped to the last day of MaxYear:
				VerifyDatesAreEqual(CreateDate(12, 31, 2010), datePicker.Date);

				// Try to set a Date that is too small, it should get clamped by MinYear.
				datePicker.SelectedDate = (CreateDate(8, 12, 1970));

				// The date should get clamped to the first day of MinYear
				VerifyDatesAreEqual(CreateDate(1, 1, 1980), datePicker.Date);

				// Try to set a date between min and max. It should not get changed.
				var dateToSet = CreateDate(1, 9, 1984);
				datePicker.SelectedDate = (dateToSet);
				VerifyDatesAreEqual(dateToSet, datePicker.Date);

				// Setting MaxYear to below the currently set date should cause the date to get clamped:
				datePicker.MaxYear = CreateDate(1, 1, 1982);
				VerifyDatesAreEqual(CreateDate(12, 31, 1982), datePicker.Date);

				datePicker.MaxYear = CreateDate(1, 1, 2050);
				datePicker.SelectedDate = (CreateDate(6, 15, 1991));

				// Setting MinYear to above the currently set date should cause the date to get clamped:
				datePicker.MinYear = CreateDate(1, 1, 1995);
				VerifyDatesAreEqual(CreateDate(1, 1, 1995), datePicker.Date);
			});
		}

		[TestMethod]
		[Ignore]
		public async Task ValidateFlyoutPositioningForRightToLeftCalendar()
		{
			// We want to validate that a DatePickerFlyout opens in the correct location for a DatePicker
			// that is displaying an RTL date.

			DatePicker datePicker = null;
			Button button = null;
			Thickness datePickerMargin = new Thickness(5, 0, 10, 0);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var rootPanel = new Grid();

				datePicker = new DatePicker();
				datePicker.Margin = datePickerMargin;

				// The Hewbrew calendar is RTL.
				datePicker.CalendarIdentifier = CalendarIdentifiers.Hebrew;

				rootPanel.Children.Add(datePicker);
				TestServices.WindowHelper.WindowContent = rootPanel;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await DateTimePickerHelper.OpenDateTimePicker(datePicker);

			button = await GetFlyoutButtonFromDatePicker(datePicker);

			await RunOnUIThread.ExecuteAsync(() =>
			{
#if WINAPPSDK
				var flyoutPopup = VisualTreeHelper.GetOpenPopups(Window.Current)[0];
#else
				var flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(datePicker.XamlRoot)[0];
#endif
				var datepickerFlyoutPresenter = GetDatePickerFlyoutPresenter(datePicker.XamlRoot);

				// The flyout popup, the flyout presenter and the button should have an RTL flow direction.
				// The DatePicker itself should remain in LTR flow direction.
				Assert.AreEqual(FlowDirection.LeftToRight, datePicker.FlowDirection);
				Assert.AreEqual(FlowDirection.RightToLeft, flyoutPopup.FlowDirection);
				Assert.AreEqual(FlowDirection.RightToLeft, datepickerFlyoutPresenter.FlowDirection);
				Assert.AreEqual(FlowDirection.RightToLeft, button.FlowDirection);

				// The flyout presenter should be the same width as the datepicker.
				Assert.AreEqual(datePicker.ActualWidth, datepickerFlyoutPresenter.ActualWidth);

				// For a Popup with RTL flowdirection, HorizontalOffset represents the Popup's RIGHT most edge from the LEFT most edge of the screen.
				// For a DatePickerFlyout, we expect its right-most edge to line up with the right-most edge of the DatePicker.
				// The right edge of the DatePicker is equal to its left edge + its width.
				double rightEdgeOfDatePicker = datePickerMargin.Left + datePicker.ActualWidth;
				Assert.IsTrue(AreClose(flyoutPopup.HorizontalOffset, rightEdgeOfDatePicker, 1.0));
			});

			await ControlHelper.ClickFlyoutCloseButton(datePicker, true /* isAccept */);
		}

#if LOOPING_SELECTOR_AVAILABLE // TODO: Remove this when LoopingSelector is available https://github.com/unoplatform/uno/issues/4880
		[TestMethod]
		public async Task DatePickerShouldMaintainTime()
		{
			// Even though the DatePicker only deals with dates, the Date property is of type DateTime and so has a time component
			// DatePicker should leave the time component of the DateTime intact.

			DatePicker datePicker = null;

			var calendar = new Calendar();
			calendar.ChangeClock(ClockIdentifiers.TwelveHour);
			calendar.Year = 1990;
			calendar.Month = 12;
			calendar.Day = 31;
			calendar.Hour = 6;
			calendar.Minute = 45;
			calendar.Second = 30;
			calendar.Period = 1;

			var dateChangedEvent = new TaskCompletionSource<object>();

			var cts = new CancellationTokenSource(1000);
			cts.Token.Register(() => dateChangedEvent.TrySetException(new TimeoutException()));

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var rootGrid = new Grid();
				TestServices.WindowHelper.WindowContent = rootGrid;

				datePicker = new DatePicker();
				datePicker.SelectedDate = (calendar);

				datePicker.DateChanged += (sender, args) =>
				{
					dateChangedEvent.TrySetResult(null);
				};

				rootGrid.Children.Add(datePicker);
			});

			TestServices.WindowHelper.WaitForIdle();

			//We verify that the DateTime was not changed by the DatePicker:
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var calendarNew = new Calendar();
				calendarNew.SetDateTime(datePicker.Date);

				Assert.AreEqual(calendarNew.Year, calendar.Year);
				Assert.AreEqual(calendarNew.Month, calendar.Month);
				Assert.AreEqual(calendarNew.Day, calendar.Day);
				Assert.AreEqual(calendarNew.Hour, calendar.Hour);
				Assert.AreEqual(calendarNew.Minute, calendar.Minute);
				Assert.AreEqual(calendarNew.Second, calendar.Second);
				Assert.AreEqual(calendarNew.Period, calendar.Period);
			});

			// We also verify that if the Date is changed by interacting with the DatePicker
			// the time component of the DateTime remains unaffected.
			// This also covers testing the DatePickerFlyout to ensure it handles the time component of
			// the DateTime correctly.

			this.Log().Info("Launch the date picker flyout by using Tap.");
			await ControlHelper.DoClickUsingTap(await GetFlyoutButtonFromDatePicker(datePicker));

			TestServices.WindowHelper.WaitForIdle();

			this.Log().Info("Pan the looping selectors.");
			LoopingSelectorHelper.PanDateTimeLoopingSelector();

			TestServices.WindowHelper.WaitForIdle();

			this.Log().Info("Close the picker flyout.");
			ControlHelper.ClickFlyoutCloseButton(datePicker, true /* isAccept */);

			await dateChangedEvent.Task;
			TestServices.WindowHelper.WaitForIdle();

			// After changing the Date, the time component should remain unaffected:
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var calendarNew = new Calendar();
				calendarNew.SetDateTime(datePicker.Date);

				Assert.AreEqual(calendarNew.Hour, calendar.Hour);
				Assert.AreEqual(calendarNew.Minute, calendar.Minute);
				Assert.AreEqual(calendarNew.Second, calendar.Second);
				Assert.AreEqual(calendarNew.Period, calendar.Period);
			});
		}
#endif

		[TestMethod]
		[Ignore]
		public async Task ValidateFootprint()
		{
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(500, 600));

			double expectedDatePickerWidth = 296;
			double expectedDatePickerWidth_WithWideHeader = 350;

			double expectedDatePickerHeight = 32;
			double expectedDatePickerHeight_WithHeader = 19 + 4 + expectedDatePickerHeight;

			DatePicker datePicker = default;
			DatePicker datePickerWithHeader = default;
			DatePicker datePickerWithWideHeader = default;
			DatePicker datePickerStretched = default;
			DatePicker datePicker_NoYear = default;
			DatePicker datePicker_NoMonth = default;
			DatePicker datePicker_NoDay = default;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var rootPanel = XamlReader.Load(@"<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
						<DatePicker x:Name=""datePicker"" />
						<DatePicker x:Name=""datePickerWithHeader"" Header=""H"" />
						<DatePicker x:Name=""datePickerWithWideHeader"" >
							<DatePicker.Header>
								<Rectangle Height=""19"" Width=""350"" Fill=""Red"" />
							</DatePicker.Header>
						</DatePicker>
						<DatePicker x:Name=""datePickerStretched"" HorizontalAlignment=""Stretch"" />
						<DatePicker x:Name=""datePicker_NoYear"" YearVisible=""False"" />
						<DatePicker x:Name=""datePicker_NoMonth"" MonthVisible=""False"" />
						<DatePicker x:Name=""datePicker_NoDay"" DayVisible=""False"" />
					</StackPanel>") as StackPanel;

				datePicker = rootPanel.FindName("datePicker") as DatePicker;
				datePickerWithHeader = rootPanel.FindName("datePickerWithHeader") as DatePicker;
				datePickerWithWideHeader = rootPanel.FindName("datePickerWithWideHeader") as DatePicker;
				datePickerStretched = rootPanel.FindName("datePickerStretched") as DatePicker;
				datePicker_NoYear = rootPanel.FindName("datePicker_NoYear") as DatePicker;
				datePicker_NoMonth = rootPanel.FindName("datePicker_NoMonth") as DatePicker;
				datePicker_NoDay = rootPanel.FindName("datePicker_NoDay") as DatePicker;

				TestServices.WindowHelper.WindowContent = rootPanel;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await RunOnUIThread.ExecuteAsync(() =>
			{
				using var _ = new AssertionScope();

				// Verify Footprint of DatePicker:
				datePicker.ActualWidth.Should().BeApproximately(expectedDatePickerWidth, 0.5, "datePicker.ActualWidth");
				datePicker.ActualHeight.Should().BeApproximately(expectedDatePickerHeight, 0.5, "datePicker.ActualHeight");

				// Verify Footprint of DatePicker with Header:
				datePickerWithHeader.ActualWidth.Should().BeApproximately(expectedDatePickerWidth, 0.5, "datePickerWithHeader.ActualWidth");
				//datePickerWithHeader.ActualHeight.Should().BeApproximately(expectedDatePickerHeight_WithHeader, 0.5, "datePickerWithHeader.ActualHeight");

				// Verify Footprint of DatePicker with wide Header:
				datePickerWithWideHeader.ActualWidth.Should().BeApproximately(expectedDatePickerWidth_WithWideHeader, 0.5, "datePickerWithWideHeader.ActualWidth");
				//datePickerWithWideHeader.ActualHeight.Should().BeApproximately(expectedDatePickerHeight_WithHeader, 0.5, "datePickerWithWideHeader.ActualHeight");

				// Verify Footprint of DatePicker with no Year/Month/Day:
				datePicker_NoYear.ActualWidth.Should().BeApproximately(expectedDatePickerWidth, 0.5, "datePicker_NoYear.ActualWidth");
				datePicker_NoYear.ActualHeight.Should().BeApproximately(expectedDatePickerHeight, 0.5, "datePicker_NoYear.ActualHeight");
				datePicker_NoMonth.ActualWidth.Should().BeApproximately(expectedDatePickerWidth, 0.5, "datePicker_NoMonth.ActualWidth");
				datePicker_NoMonth.ActualHeight.Should().BeApproximately(expectedDatePickerHeight, 0.5, "datePicker_NoMonth.ActualHeight");
				datePicker_NoDay.ActualWidth.Should().BeApproximately(expectedDatePickerWidth, 0.5, "datePicker_NoDay.ActualWidth");
				datePicker_NoDay.ActualHeight.Should().BeApproximately(expectedDatePickerHeight, 0.5, "datePicker_NoDay.ActualHeight");
			});
		}

		[TestMethod]
		public async Task HasPlaceholderTextByDefault()
		{
			var datePicker = await SetupDatePickerTest();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Assert.IsNull(datePicker.SelectedDate);
			});

			await VerifyHasPlaceholder(datePicker);
		}

#if LOOPING_SELECTOR_AVAILABLE // TODO: Remove this when LoopingSelector is available https://github.com/unoplatform/uno/issues/4880
		[TestMethod]
		public async Task SelectingDateSetsSelectedDate()
		{
			var datePicker = await SetupDatePickerTest();
			var targetDate = CreateDate(1, 1, 2018);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.MinYear = targetDate;
			});

			this.Log().Info("Selecting January 1, 2018.");
			DateTimePickerHelper.OpenDateTimePicker(datePicker);
			TestServices.WindowHelper.WaitForIdle();

			DateTimePickerHelper.SelectDateInOpenDatePickerFlyout(targetDate, targetDate.Year,
				LoopingSelectorHelper.SelectionMode.Keyboard);
			TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				this.Log().Info("Date and SelectedDate should now refer to the same date.");
				VerifyDatesAreEqual(targetDate, datePicker.Date);
				Assert.IsNotNull(datePicker.SelectedDate);
				VerifyDatesAreEqual(targetDate, datePicker.SelectedDate.Value);
			});
		}
#endif

		[TestMethod]
		public async Task ValidateSelectedDatePropagatesToDate()
		{
			var datePicker = await SetupDatePickerTest();
			var date = CreateDate(1, 1, 2018);
			var date2 = CreateDate(2, 2, 2018);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				using var _ = new AssertionScope();

				this.Log().Info("Setting SelectedDate to null.  Date should be the null sentinel value.");
				datePicker.SelectedDate = null;

				datePicker.Date.WindowsFoundationUniversalTime().Should().Be(0);

				this.Log().Info("Setting SelectedDate to February 2, 2018. Date should change to this value.");
				datePicker.SelectedDate = (date2);

				VerifyDatesAreEqual(date2, datePicker.Date);
				datePicker.SelectedDate.Should().NotBeNull();
				VerifyDatesAreEqual(date2, datePicker.SelectedDate.Value);

				this.Log().Info("Setting Date to January 1, 2018. SelectedDate should change to this value.");
				datePicker.Date = date;

				VerifyDatesAreEqual(date, datePicker.Date);
				datePicker.SelectedDate.Should().NotBeNull();
				VerifyDatesAreEqual(date, datePicker.SelectedDate.Value);

				this.Log().Info("Setting Date to the null sentinel value. SelectedDate should become null.");
				datePicker.Date = new DateTimeOffset(ManagedUtcTicksAtNativeZero, TimeSpan.Zero).ToLocalTime();

				datePicker.SelectedDate.Should().BeNull("SelectedDate should be back to null");
			});
		}

		[TestMethod]
		public async Task CanProgrammaticallyClearSelectedDate()
		{
			var datePicker = await SetupDatePickerTest();

			await VerifyHasPlaceholder(datePicker);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.SelectedDate = (CreateDate(1, 1, 2018));
			});

			await VerifyDoesNotHavePlaceholder(datePicker);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.SelectedDate = null;
			});

			await VerifyHasPlaceholder(datePicker);
		}

		private async Task<Button> GetFlyoutButtonFromDatePicker(DatePicker datePicker)
		{
			Button flyoutButton = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var templateRoot = (FrameworkElement)(VisualTreeHelper.GetChild(datePicker, 0));
				flyoutButton = (Button)(templateRoot.FindName("FlyoutButton"));
			});

			return flyoutButton;
		}

		private async Task<(TextBlock dayTextBlock, TextBlock monthTextBlock, TextBlock yearTextBlock)> GetDayMonthYearTextBlocksFromDatePicker(DatePicker datePicker)
		{
			(TextBlock, TextBlock, TextBlock) result = default;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var templateRoot = (FrameworkElement)(VisualTreeHelper.GetChild(datePicker, 0));
				Assert.IsNotNull(templateRoot);

				var dayTextBlock = (TextBlock)(templateRoot.FindName("DayTextBlock"));
				var monthTextBlock = (TextBlock)(templateRoot.FindName("MonthTextBlock"));
				var yearTextBlock = (TextBlock)(templateRoot.FindName("YearTextBlock"));
				Assert.IsNotNull(dayTextBlock);
				Assert.IsNotNull(monthTextBlock);
				Assert.IsNotNull(yearTextBlock);

				result = (dayTextBlock, monthTextBlock, yearTextBlock);
			});

			return result;
		}

		[TestMethod]
		public async Task CanSelectDateInJapaneseCalendar()
		{
			var datePicker = await SetupDatePickerTest();
			var date = CreateDate(1, 9, 2019);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				datePicker.CalendarIdentifier = "JapaneseCalendar";

				datePicker.Date.WindowsFoundationUniversalTime().Should().Be(0);

				this.Log().Info("Setting SelectedDate to January 9, 2019. Date should change to this value.");
				datePicker.SelectedDate = (date);

				VerifyDatesAreEqual(date, datePicker.Date);
				datePicker.SelectedDate.Should().NotBeNull();
				VerifyDatesAreEqual(date, datePicker.SelectedDate.Value);

				this.Log().Info("Setting Date back to the null sentinel value. SelectedDate should become null.");
				datePicker.Date = new DateTimeOffset(ManagedUtcTicksAtNativeZero, TimeSpan.Zero).ToLocalTime();
				datePicker.SelectedDate.Should().BeNull();
			});
		}

		private DatePickerFlyoutPresenter GetDatePickerFlyoutPresenter(XamlRoot xamlRoot)
		{
			return FlyoutHelper.GetOpenFlyoutPresenter(xamlRoot) as DatePickerFlyoutPresenter;
		}

		private void VerifyDatesAreEqual(Calendar expected, DateTimeOffset actual)
		{
			using var _ = new AssertionScope();

			var actualCalendar = new Calendar();
			actualCalendar.SetDateTime(actual);

			actualCalendar.Year.Should().Be(expected.Year);
			actualCalendar.Month.Should().Be(expected.Month);
			actualCalendar.Day.Should().Be(expected.Day);
		}

		private void VerifyDatesAreEqual(DateTimeOffset expected, DateTimeOffset actual)
		{
			VerifyDatesAreEqual(CreateCalendar(expected), actual);
		}

		private async Task VerifyHasPlaceholder(DatePicker datePicker)
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var dayTextBlock = TreeHelper.GetVisualChildByName(datePicker, "DayTextBlock") as TextBlock;
				var monthTextBlock = TreeHelper.GetVisualChildByName(datePicker, "MonthTextBlock") as TextBlock;
				var yearTextBlock = TreeHelper.GetVisualChildByName(datePicker, "YearTextBlock") as TextBlock;

				void validatePlaceholder(TextBlock textBlock, string expectedPlaceholder)
				{
					this.Log().InfoFormat("Expected placeholder: \"{0}\"", expectedPlaceholder);
					this.Log().InfoFormat("Actual text: \"{0}\"", textBlock.Text);

					Assert.AreEqual(0, string.CompareOrdinal(expectedPlaceholder, textBlock.Text));
				}
				;

				validatePlaceholder(dayTextBlock, "day");
				validatePlaceholder(monthTextBlock, "month");
				validatePlaceholder(yearTextBlock, "year");
			});
		}

		private async Task VerifyDoesNotHavePlaceholder(DatePicker datePicker)
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var dayTextBlock = TreeHelper.GetVisualChildByName(datePicker, "DayTextBlock") as TextBlock;
				var monthTextBlock = TreeHelper.GetVisualChildByName(datePicker, "MonthTextBlock") as TextBlock;
				var yearTextBlock = TreeHelper.GetVisualChildByName(datePicker, "YearTextBlock") as TextBlock;

				void validateValue(TextBlock textBlock, string placeholder)
				{
					this.Log().InfoFormat("Placeholder: \"{0}\"", placeholder);
					this.Log().InfoFormat("Actual text: \"{0}\"", textBlock.Text);

					Assert.AreNotEqual(0, string.CompareOrdinal(placeholder, textBlock.Text));
				}
				;

				validateValue(dayTextBlock, "day");
				validateValue(monthTextBlock, "month");
				validateValue(yearTextBlock, "year");
			});
		}
	}
}
