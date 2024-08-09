// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable 168 // for cleanup imported member

using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Tests.Common;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;

using static Private.Infrastructure.TestServices;
using static Private.Infrastructure.CalendarHelper;

namespace Windows.UI.Xaml.Tests.Enterprise.CalendarDatePickerTests
{
	[TestClass]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
	public partial class CalendarDatePickerIntegrationTests : BaseDxamlTestClass
	{
		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			TestServices.WindowHelper.VerifyTestCleanup();
		}

		//
		// Test Cases
		//
		[TestMethod]
		public async Task ValidateDefaultPropertyValues()
		{
			await RunOnUIThread(() =>
			{
				using var _ = new AssertionScope();

				var cp = new Windows.UI.Xaml.Controls.CalendarDatePicker();
				VERIFY_ARE_EQUAL(cp.FirstDayOfWeek, global::Windows.Globalization.DayOfWeek.Sunday);
				VERIFY_ARE_EQUAL(cp.DisplayMode, Windows.UI.Xaml.Controls.CalendarViewDisplayMode.Month);
				VERIFY_ARE_EQUAL(cp.IsTodayHighlighted, true);
				VERIFY_ARE_EQUAL(cp.IsOutOfScopeEnabled, true);
				VERIFY_ARE_EQUAL(cp.IsGroupLabelVisible, false);

				global::Windows.Globalization.Calendar calendar = new global::Windows.Globalization.Calendar();
				calendar.SetToNow();

				calendar.AddYears(-100);

				calendar.Month = calendar.FirstMonthInThisYear;
				calendar.Day = calendar.FirstDayInThisMonth;

				var minDate = calendar.GetDateTime();

				calendar.AddYears(200);

				calendar.Month = calendar.LastMonthInThisYear;
				calendar.Day = calendar.LastDayInThisMonth;

				var maxDate = calendar.GetDateTime();

				CompareDate comparer = CalendarHelper.CompareDate;
				VERIFY_IS_TRUE(comparer(cp.MinDate, minDate));
				VERIFY_IS_TRUE(comparer(cp.MaxDate, maxDate));
			});
		}

		[TestMethod]
		public async Task CanEnterAndLeaveLiveTree()
		{
			// Note: CalendarDatePicker can't use below commented line to test "CanEnterAndLeaveLiveTree"
			// the problem is in below helper, we did these:
			//  1. create CalendarDatePicker
			//  2. added into visual tree
			//  3. test loaded and unloaded event
			//  4. remove CalendarDatePicker from visual tree
			//  5. destroy CalendarDatePicker
			//  .....

			// because we destroy CalendarDatePicker after we remove it from visual tree, so if there are any left work in build tree services, they can't be cleaned up correctly.
			// this should happens on ListView and GridView, however for default ListView and GridView (especially in below helper method) are empty and there is no buildtree work.
			// But for default CalendarDatePicker, we have! because default CalendarDatePicker will show the dates in 3 years.

			//Generic.FrameworkElementTests<Windows.UI.Xaml.Controls.CalendarDatePicker>.CanEnterAndLeaveLiveTree();

			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
			});

			await helper.WaitForLoaded();

			// remove from visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Clear();
			});

			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("Tapping is not implemented correctly on platforms that don't implement InputInjector")]
#endif
		public async Task CanOpenFlyoutByTapping()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			Grid root = null;
			TextBlock dateText = null;
			FlyoutBase flyout = null;
			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				root = Grid(helper.GetTemplateChild("Root"));
				dateText = TextBlock(helper.GetTemplateChild("DateText"));

				VERIFY_IS_NOT_NULL(root);
				VERIFY_IS_NOT_NULL(dateText);

				flyout = FlyoutBase.GetAttachedFlyout(root);
				VERIFY_IS_NOT_NULL(flyout);
			});

			await helper.PrepareOpenedEvent();

			TestServices.InputHelper.Tap(dateText
#if WINAPPSDK
				// On Windows, we might wait a bit after pressing for the popup to open
				// UNO TODO: why do we need this wait?
				, 600
#endif
			);

			await helper.WaitForOpened();

			await TestServices.WindowHelper.WaitForIdle();

			await helper.PrepareClosedEvent();

			await RunOnUIThread(() =>
			{
				// close the flyout before exiting.
				flyout.Hide();
			});
			await helper.WaitForClosed();

			await TestServices.WindowHelper.WaitForIdle();
		}


		[TestMethod]
#if WINAPPSDK
		[Ignore("KeyboardHelper doesn't work on Windows")]
#elif !__SKIA__
		[Ignore("Fails")]
#endif
		public async Task CanOpenFlyoutByKeyboard()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cp.Focus(Windows.UI.Xaml.FocusState.Programmatic);
			});

			await helper.PrepareOpenedEvent();
			await TestServices.WindowHelper.WaitForIdle();

			// press enter to open flyout
			TestServices.KeyboardHelper.Enter();

			await helper.WaitForOpened();

			// escape to close the flyout
			TestServices.KeyboardHelper.Escape();

			await TestServices.WindowHelper.WaitForIdle();
			await helper.PrepareOpenedEvent();

			await RunOnUIThread(() =>
			{
				cp.Focus(Windows.UI.Xaml.FocusState.Programmatic);
			});
			await TestServices.WindowHelper.WaitForIdle();

			// press space to open flyout
			TestServices.KeyboardHelper.PressKeySequence("$d$_ #$u$_ ");

			await helper.WaitForOpened();

			// escape to close the flyout
			TestServices.KeyboardHelper.Escape();
			await TestServices.WindowHelper.WaitForIdle();
		}


		[TestMethod]
		public async Task CanOpenCloseFlyoutBySettingIsCalendarOpen()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await helper.PrepareOpenedEvent();

			await RunOnUIThread(() =>
			{
				cp.IsCalendarOpen = true;
			});
			await helper.WaitForOpened();

			await helper.PrepareClosedEvent();

			await RunOnUIThread(() =>
			{
				cp.IsCalendarOpen = false;
			});

			await helper.WaitForClosed();

			await RunOnUIThread(() =>
			{
				// disable CP to make sure input pane is not open during clean up.
				cp.IsEnabled = false;
			});
			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("Tapping is not implemented correctly on platforms that don't implement InputInjector")]
#endif
		public async Task CanCloseFlyoutBySelectingADate()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			TextBlock dateText = null;
			Grid root = null;
			FlyoutBase flyout = null;
			CalendarView calendarView = null;
			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cp.IsOutOfScopeEnabled = false;
				rootPanel.Children.Append(cp);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				dateText = TextBlock(helper.GetTemplateChild("DateText"));
				VERIFY_IS_NOT_NULL(dateText);

				root = Grid(helper.GetTemplateChild("Root"));
				VERIFY_IS_NOT_NULL(root);

				flyout = FlyoutBase.GetAttachedFlyout(root);
				VERIFY_IS_NOT_NULL(flyout);

				var content = Flyout(flyout).Content;
				calendarView = CalendarView(content);
				VERIFY_IS_NOT_NULL(calendarView);


				calendarView.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				calendarView.MaxDate = ConvertToDateTime(1, 2001, 1, 1);
				calendarView.UpdateLayout();
			});


			TestServices.InputHelper.Tap(dateText
#if WINAPPSDK
				// On Windows, we might wait a bit after pressing for the popup to open
				// UNO TODO: why do we need this wait?
				, 600
#endif
			);

			await TestServices.WindowHelper.WaitForIdle();

			await helper.PrepareClosedEvent();

			await RunOnUIThread(() =>
			{
				calendarView.SelectedDates.Append(ConvertToDateTime(1, 2000, 10, 21));
			});

			await helper.WaitForClosed();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("actual text: %s.", dateText.Text);
				// Note: below string contains invisible unicode characters (BiDi characters),
				// you should always use copy&paste to get the string, directly
				// type the string will cause the string comparison fails.
				//VERIFY_IS_TRUE(dateText.Text == "‎10‎/‎21‎/‎2000");

				dateText.Text.Should().Be("10/21/2000"); // UNO: Those BiDi characters are not emitted by Uno
			});

			await RunOnUIThread(() =>
			{
				// disable CP to make sure input pane is not open during clean up.
				cp.IsEnabled = false;
			});
			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task ValidateDateIsCoerced()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
				cp.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				cp.MaxDate = ConvertToDateTime(1, 2002, 1, 1);
				cp.Date = ConvertToDateTime(1, 2001, 1, 1);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_NOT_NULL(cp.Date);
				VERIFY_DATES_ARE_EQUAL(cp.Date.Value.UniversalTime(), ConvertToDateTime(1, 2001, 1, 1).UniversalTime());

				// make date beyond the range.
				// it should be coerced to min/max
				cp.Date = ConvertToDateTime(1, 2010, 1, 1);
				cp.UpdateLayout();
				VERIFY_IS_NOT_NULL(cp.Date);
				VERIFY_DATES_ARE_EQUAL(cp.Date.Value.UniversalTime(), cp.MaxDate.UniversalTime());

				cp.Date = ConvertToDateTime(1, 1999, 1, 1);
				cp.UpdateLayout();
				VERIFY_IS_NOT_NULL(cp.Date);
				VERIFY_DATES_ARE_EQUAL(cp.Date.Value.UniversalTime(), cp.MinDate.UniversalTime());
			});
		}

		[TestMethod]
		[Ignore("UNO TODO - Calendar formatting is still not properly implemented")]
		public async Task CanFormatDate()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			TextBlock dateText = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
				cp.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				cp.MaxDate = ConvertToDateTime(1, 2002, 1, 1);
				cp.Date = ConvertToDateTime(1, 2001, 1, 1);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				dateText = TextBlock(helper.GetTemplateChild("DateText"));
				VERIFY_IS_NOT_NULL(dateText);
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cp.DateFormat = "{dayofweek.full}, {month.full} {day.integer}, {year.full}"; // equivalent to "longdate"
				cp.UpdateLayout();

				LOG_OUTPUT("actual text: %s.", dateText.Text);
				VERIFY_ARE_EQUAL(dateText.Text, "Monday, January 1, 2001");
			});
		}

		[TestMethod]
		[Ignore("UNO TODO - Fix custom date formatting")]
		public async Task SettingCalendarIdentifierChangesDateFormat()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			TextBlock dateText = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
				cp.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				cp.MaxDate = ConvertToDateTime(1, 2002, 1, 1);
				cp.Date = ConvertToDateTime(1, 2001, 1, 1);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				dateText = TextBlock(helper.GetTemplateChild("DateText"));
				VERIFY_IS_NOT_NULL(dateText);
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cp.DateFormat = "{dayofweek.full}, {month.full} {day.integer}, {year.full}"; // equivalent to "longdate"
				cp.UpdateLayout();

				cp.CalendarIdentifier = global::Windows.Globalization.CalendarIdentifiers.Taiwan;

				LOG_OUTPUT("actual text: %s.", dateText.Text);
				VERIFY_ARE_EQUAL("Monday, January 1, 90", dateText.Text);
			});

		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("Tapping is not implemented correctly on platforms that don't implement InputInjector")]
#endif
		public async Task PressingDoesNotOpenMenuFlyout()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			Grid root = null;
			TextBlock dateText = null;
			FlyoutBase flyout = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			var gridPointerPressedEvent = new Event();
			var gridPointerPressedRegistration = CreateSafeEventRegistration<UIElement, PointerEventHandler>("PointerPressed");

			rootPanel = await CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
				cp.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				cp.MaxDate = ConvertToDateTime(1, 2002, 1, 1);
				cp.Date = ConvertToDateTime(1, 2001, 1, 1);

				gridPointerPressedRegistration.Attach(rootPanel,
					(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs args) =>
					{
						gridPointerPressedEvent.Set();
					});
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				root = Grid(helper.GetTemplateChild("Root"));
				dateText = TextBlock(helper.GetTemplateChild("DateText"));

				VERIFY_IS_NOT_NULL(root);
				VERIFY_IS_NOT_NULL(dateText);

				flyout = FlyoutBase.GetAttachedFlyout(root);
				VERIFY_IS_NOT_NULL(flyout);
			});
			await TestServices.WindowHelper.WaitForIdle();

			await helper.PrepareOpenedEvent();

			TestServices.InputHelper.Tap(dateText
#if WINAPPSDK
				// On Windows, we might wait a bit after pressing for the popup to open
				// UNO TODO: why do we need this wait?
				, 600
#endif
			);

			await helper.WaitForOpened();

			await TestServices.WindowHelper.WaitForIdle();
			await helper.PrepareClosedEvent();

			await RunOnUIThread(() =>
			{
				// close the flyout before exiting.
				flyout.Hide();
			});
			await helper.WaitForClosed();

			await TestServices.WindowHelper.WaitForIdle();

			VERIFY_IS_FALSE(gridPointerPressedEvent.HasFired());
		}

		[TestMethod]
		public async Task ValidateUIElementTree()
		{
			TestCleanupWrapper cleanup;

			StackPanel rootPanel = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();

			await helper.PrepareLoadedEvent();
			Windows.UI.Xaml.Controls.CalendarDatePicker cdp = await helper.GetCalendarDatePicker();

			await RunOnUIThread(() =>
			{
				rootPanel = StackPanel(XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
					"      Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left' Background='Black'/> ")
				);

				global::Private.Infrastructure.TestServices.WindowHelper.WindowContent = rootPanel;
			});

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cdp);
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cdp.Focus(Windows.UI.Xaml.FocusState.Pointer);
			});

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyUIElementTree();
		}

		[TestMethod]
		public async Task ValidateVisualStates()
		{
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);

			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			StackPanel rootPanel = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			await helper.PrepareLoadedEvent();
			CalendarDatePicker cpNormal = await helper.GetCalendarDatePicker();
			CalendarDatePicker cpPressed = null;
			CalendarDatePicker cpPointerOver = null;
			CalendarDatePicker cpDisabled = null;
			CalendarDatePicker cpFocused = null;
			CalendarDatePicker cpSelected = null;

			await RunOnUIThread(() =>
			{
				rootPanel = StackPanel(XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
					"      Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left' Background='Black'/> ")
				);

				global::Private.Infrastructure.TestServices.WindowHelper.WindowContent = rootPanel;
			});



			// load into visual tree
			await RunOnUIThread(() =>
			{
				cpPressed = new Windows.UI.Xaml.Controls.CalendarDatePicker();
				cpPointerOver = new Windows.UI.Xaml.Controls.CalendarDatePicker();
				cpDisabled = new Windows.UI.Xaml.Controls.CalendarDatePicker();
				cpFocused = new Windows.UI.Xaml.Controls.CalendarDatePicker();
				cpSelected = new Windows.UI.Xaml.Controls.CalendarDatePicker();

				rootPanel.Children.Append(cpNormal);
				rootPanel.Children.Append(cpPressed);
				rootPanel.Children.Append(cpPointerOver);
				rootPanel.Children.Append(cpDisabled);
				rootPanel.Children.Append(cpFocused);
				rootPanel.Children.Append(cpSelected);

				cpNormal.Header = "Normal";
				cpPressed.Header = "Pressed";
				cpPointerOver.Header = "PointerOver";
				cpDisabled.Header = "Disabled";
				cpFocused.Header = "Focused";
				cpSelected.Header = "Selected";
			});

			await helper.WaitForLoaded();

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				//cpNormal stays in common state
				VisualStateManager.GoToState(cpPressed, "Pressed", true);
				VisualStateManager.GoToState(cpPointerOver, "PointerOver", true);
				VisualStateManager.GoToState(cpDisabled, "Disabled", true);
				VisualStateManager.GoToState(cpFocused, "Focused", true);
				VisualStateManager.GoToState(cpSelected, "Selected", true);
			});

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("Causing bugs in asserts")]
		public async Task DonotResizeCalendarView()
		{
			TestCleanupWrapper cleanup;

			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			Grid rootPanel = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel = Grid(XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
					"      Width='200' Height='200' VerticalAlignment='Top' HorizontalAlignment='Left' Background='Black'/> ")
				);
				rootPanel.Children.Append(cp);
				global::Private.Infrastructure.TestServices.WindowHelper.WindowContent = rootPanel;
				cp.IsCalendarOpen = true;
				// there is not enough space to show the flyout, before this fix, the flyoutpresenter's content will be clipped
				cp.HorizontalAlignment = HorizontalAlignment.Center;
				cp.VerticalAlignment = VerticalAlignment.Center;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(rootPanel.XamlRoot);
				VERIFY_ARE_EQUAL(popups.Count, 1);
				var popup = popups.GetAt(0);
				var presenter = FrameworkElement(popup.Child);
				LOG_OUTPUT("actual height: %lf. expected height: 332.", presenter.ActualHeight);
				// was 284 before this fix, the calendarview is clipped.
				VERIFY_ARE_EQUAL(presenter.ActualHeight, 332);

				cp.IsCalendarOpen = false;
			});
			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Ignore("Calendar formatting is still not properly implemented")]
		public async Task CanPresetDate()
		{
			TestCleanupWrapper cleanup;

			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			Grid rootPanel = null;

			CalendarDatePickerHelper helper = new CalendarDatePickerHelper();
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = await helper.GetCalendarDatePicker();
			CalendarView calendarView = null;
			TextBlock dateText = null;

			rootPanel = await CreateTestResources();
			var date1 = ConvertToDateTime(1, 2000, 10, 21);
			var date2 = ConvertToDateTime(1, 2003, 1, 1);
			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Append(cp);
				cp.Date = date1;
				cp.IsCalendarOpen = true;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var root = Grid(helper.GetTemplateChild("Root"));
				VERIFY_IS_NOT_NULL(root);

				var flyout = FlyoutBase.GetAttachedFlyout(root);
				VERIFY_IS_NOT_NULL(flyout);

				var content = Flyout(flyout).Content;
				calendarView = CalendarView(content);
				VERIFY_IS_NOT_NULL(calendarView);

				dateText = TextBlock(helper.GetTemplateChild("DateText"));

				VERIFY_IS_NOT_NULL(dateText);
			});

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("actual text: %s.", dateText.Text);
				// Note: below string contains invisible unicode characters (BiDi characters),
				// you should always use copy&paste to get the string, directly
				// type the string will cause the string comparison fails.
				VERIFY_IS_TRUE(dateText.Text == "‎10‎/‎21‎/‎2000");

				VERIFY_ARE_EQUAL(calendarView.SelectedDates.Count, 1);
				VERIFY_DATES_ARE_EQUAL(calendarView.SelectedDates.GetAt(0).UniversalTime(), date1.UniversalTime());
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cp.Date = null;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("actual text: %s.", dateText.Text);
				// clear the Date property will display placehoder text.
				VERIFY_ARE_EQUAL(dateText.Text, cp.PlaceholderText);

				VERIFY_ARE_EQUAL(calendarView.SelectedDates.Count, 0);
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cp.Date = date2;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("actual text: %s.", dateText.Text);
				// Note: below string contains invisible unicode characters (BiDi characters),
				// you should always use copy&paste to get the string, directly
				// type the string will cause the string comparison fails.
				VERIFY_IS_TRUE(dateText.Text == "‎1‎/‎1‎/‎2003");

				VERIFY_ARE_EQUAL(calendarView.SelectedDates.Count, 1);
				VERIFY_DATES_ARE_EQUAL(calendarView.SelectedDates.GetAt(0).UniversalTime(), date2.UniversalTime());

				cp.IsCalendarOpen = false;
			});


			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task VerifyTwoWayBinding()
		{
			TestCleanupWrapper cleanup;
			StackPanel rootPanel = null;
			Windows.UI.Xaml.Controls.CalendarDatePicker cdp1 = null;
			Windows.UI.Xaml.Controls.CalendarDatePicker cdp2 = null;
			var date1 = ConvertToDateTime(1, 2000, 1, 1);
			var date2 = ConvertToDateTime(1, 2000, 1, 2);

			await RunOnUIThread(() =>
			{
				rootPanel = StackPanel(XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
					"      Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left' Background='Black'> " +
					"    <CalendarDatePicker x:Name='cdp1'/>" +
					"    <CalendarDatePicker x:Name='cdp2' Date='{Binding ElementName=cdp1, Path=Date, Mode=TwoWay}'/>" +
					"</StackPanel>"
				));

				global::Private.Infrastructure.TestServices.WindowHelper.WindowContent = rootPanel;
			});

			await TestServices.WindowHelper.WaitForIdle();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cdp1 = CalendarDatePicker(rootPanel.Children.GetAt(0));
				cdp2 = CalendarDatePicker(rootPanel.Children.GetAt(1));
				cdp1.Date = date1;
				// due to Bug 5678196:{Binding} doesn't work on nullable properties when source is null
				// we can't test the scenario that cdp1.Date is null
				CalendarHelper.DumpDate(cdp1.Date.Value, "Changing cdp1.Date to");
				CalendarHelper.DumpDate(cdp2.Date.Value, "Now cdp2.Date is");
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_DATES_ARE_EQUAL(cdp1.Date.Value.UniversalTime(), cdp2.Date.Value.UniversalTime());

				cdp2.Date = date2;
				CalendarHelper.DumpDate(cdp2.Date.Value, "Changing cdp2.Date to");
				CalendarHelper.DumpDate(cdp1.Date.Value, "Now cdp1.Date is");
				VERIFY_DATES_ARE_EQUAL(cdp1.Date.Value.UniversalTime(), cdp2.Date.Value.UniversalTime());
			});

		}

		[TestMethod]
		public async Task TestDateChangedEventWhenAssignDateToSameValue()
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;
			Windows.UI.Xaml.Controls.CalendarDatePicker cp = null;

			rootPanel = await CreateTestResources();
			var date = ConvertToDateTime(1, 2000, 1, 1);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cp = new Windows.UI.Xaml.Controls.CalendarDatePicker();
				cp.Date = date;

				rootPanel.Children.Append(cp);
			});

			await TestServices.WindowHelper.WaitForIdle();

			var dateChangedEvent = new Event();
			var dateChangedRegistration = CreateSafeEventRegistration<CalendarDatePicker, TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs>>("DateChanged");

			dateChangedRegistration.Attach(cp,
				(sender, args) =>
				{
					dateChangedEvent.Set();
				});

			await RunOnUIThread(() =>
			{
				cp.Date = date;
			});

			await TestServices.WindowHelper.WaitForIdle();

			// should not raise DateChanged event because we set the same date to cp.Date.
			VERIFY_IS_FALSE(dateChangedEvent.HasFired());
		}

		[TestMethod]
		[Ignore("FlyoutHelper.ValidateOpenFlyoutOverlayBrush() not implemented yet.")]
		public async Task ValidateOverlayBrush()
		{
			TestCleanupWrapper cleanup;

			CalendarDatePicker calendarDatePicker = null;

			await RunOnUIThread(() =>
			{
				calendarDatePicker = new CalendarDatePicker();
				calendarDatePicker.LightDismissOverlayMode = LightDismissOverlayMode.On;

				TestServices.WindowHelper.WindowContent = calendarDatePicker;
			});
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.InputHelper.Tap(calendarDatePicker);
			await TestServices.WindowHelper.WaitForIdle();

			FlyoutBase flyout = null;
			await RunOnUIThread(() =>
			{
				var root = FrameworkElement(VisualTreeHelper.GetChild(calendarDatePicker, 0));
				flyout = FlyoutBase.GetAttachedFlyout(root);
				THROW_IF_NULL_WITH_MSG(flyout, "An overlay element should exist for the flyout.");
			});

			FlyoutHelper.ValidateOpenFlyoutOverlayBrush("CalendarDatePickerLightDismissOverlayBackground");

			FlyoutHelper.HideFlyout(flyout);
		}

	}
}
