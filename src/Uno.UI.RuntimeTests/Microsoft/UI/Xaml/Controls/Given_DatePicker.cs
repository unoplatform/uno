#if !WINAPPSDK
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Globalization;

#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_DatePicker
	{
		[TestInitialize]
		public async Task Setup()
		{
			TestServices.WindowHelper.WindowContent = null;

			await TestServices.WindowHelper.WaitForIdle();
		}

#if HAS_UNO
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
		public async Task When_Time_Zone()
		{
			for (int offset = -14; offset <= 14; offset++)
			{
				var now = DateTimeOffset.Now;
				now = now.Add(TimeSpan.FromHours(offset) - now.Offset);
				using (new TimeZoneModifier(TimeZoneInfo.CreateCustomTimeZone("FakeTestTimeZone", TimeSpan.FromHours(offset), "FakeTestTimeZone", "FakeTestTimeZone")))
				{
					var datePicker = new DatePicker();
					datePicker.UseNativeStyle = false;
					await UITestHelper.Load(datePicker);

					await DateTimePickerHelper.OpenDateTimePicker(datePicker);

					var openFlyouts = FlyoutBase.OpenFlyouts;
					Assert.HasCount(1, openFlyouts);
					var associatedFlyout = (DatePickerFlyout)openFlyouts[0];
					Assert.IsNotNull(associatedFlyout);

					await ControlHelper.ClickFlyoutCloseButton(datePicker, isAccept: true);

					Assert.AreEqual(datePicker.Date, associatedFlyout.Date);
					Assert.AreEqual(now.Year, associatedFlyout.Date.Year);
					Assert.AreEqual(now.Month, associatedFlyout.Date.Month);
					Assert.AreEqual(now.Day, associatedFlyout.Date.Day);

					bool unloaded = false;
					datePicker.Unloaded += (s, e) => unloaded = true;

					TestServices.WindowHelper.WindowContent = null;

					await TestServices.WindowHelper.WaitFor(() => unloaded, message: "DatePicker did not unload");

				}
			}
		}
#endif

		[TestMethod]
		public async Task When_United_States_Culture_Column_Order()
		{
			using var _ = new AssertionScope();
			using var lang = SetAmbiantLanguage("en-US");

			var datePicker = new DatePicker();

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForIdle();

			// US uses mm/dd/yyyy
			CheckDateTimeTextBlockPartPosition(datePicker, "YearTextBlock", expectedColumn: 4);
			CheckDateTimeTextBlockPartPosition(datePicker, "MonthTextBlock", expectedColumn: 0);
			CheckDateTimeTextBlockPartPosition(datePicker, "DayTextBlock", expectedColumn: 2);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
		[DataRow(2024, 1, "يناير")]
		[DataRow(2100, 3, "مارس")]
		public async Task When_Arabic_Language_Then_Month_Is_Gregorian(int year, int month, string expectedMonth)
		{
			var arabicGregorian = (CultureInfo)new CultureInfo("ar-SA").Clone();
			arabicGregorian.DateTimeFormat.Calendar = new GregorianCalendar();
			if (arabicGregorian.DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1))
			{
				Assert.Inconclusive("The ICU data loaded here has no Arabic names (the WebAssembly shard for the browser language).");
			}

			using var lang = SetAmbiantLanguage("ar-SA");

			// ar-SA defaults to the Um al-Qura calendar, in which January 2024 is Rajab (رجب), and which ends in 2077.
			// DatePicker's calendar is Gregorian.
			var datePicker = new DatePicker { SelectedDate = new DateTimeOffset(new DateTime(year, month, 15)) };
			TestServices.WindowHelper.WindowContent = datePicker;
			await TestServices.WindowHelper.WaitForIdle();

			var monthTextBlock = (TextBlock)VisualTreeUtils.FindVisualChildByName(datePicker, "MonthTextBlock");
			Assert.AreEqual(expectedMonth, monthTextBlock.Text);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
		public async Task When_Date_Selected_Then_Automation_Name_Has_The_Long_Date()
		{
			using var lang = SetAmbiantLanguage("en-US");

			var datePicker = new DatePicker { Header = "Birthday", SelectedDate = new DateTimeOffset(new DateTime(2024, 1, 15)) };
			TestServices.WindowHelper.WindowContent = datePicker;
			await TestServices.WindowHelper.WaitForIdle();

			// "<header> <date> date picker", the date from "day month.full year", as in WinRT.
			var flyoutButton = (ButtonBase)VisualTreeUtils.FindVisualChildByName(datePicker, "FlyoutButton");
			StringAssert.StartsWith(AutomationProperties.GetName(flyoutButton), "Birthday January 15, 2024");
		}

		[TestMethod]
		public async Task When_CanadaEnglish_Culture_Column_Order()
		{
			using var _ = new AssertionScope();
			using var lang = SetAmbiantLanguage("en-CA");

			var datePicker = new DatePicker();

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForIdle();

			// As in WinUI, the columns follow "day month.full year", which is en-CA's long date (January 15, 2024).
			CheckDateTimeTextBlockPartPosition(datePicker, "YearTextBlock", expectedColumn: 4);
			CheckDateTimeTextBlockPartPosition(datePicker, "MonthTextBlock", expectedColumn: 0);
			CheckDateTimeTextBlockPartPosition(datePicker, "DayTextBlock", expectedColumn: 2);
		}

		[TestMethod]
		public async Task When_CanadaFrench_Culture_Column_Order()
		{
			if (OperatingSystem.IsBrowser())
			{
				// this test is failing on browser, see https://github.com/unoplatform/uno/issues/9080
				Assert.Inconclusive("https://github.com/unoplatform/uno/issues/9080");
			}

			using var _ = new AssertionScope();
			using var lang = SetAmbiantLanguage("fr-CA");

			var datePicker = new DatePicker();

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForIdle();

			// As in WinUI, the columns follow "day month.full year", which is fr-CA's long date (15 janvier 2024).
			CheckDateTimeTextBlockPartPosition(datePicker, "YearTextBlock", expectedColumn: 4);
			CheckDateTimeTextBlockPartPosition(datePicker, "MonthTextBlock", expectedColumn: 2);
			CheckDateTimeTextBlockPartPosition(datePicker, "DayTextBlock", expectedColumn: 0);
		}

		[TestMethod]
		public async Task When_Czech_Culture_Column_Order()
		{
			if (OperatingSystem.IsBrowser())
			{
				// this test is failing on browser, see https://github.com/unoplatform/uno/issues/9080
				Assert.Inconclusive("https://github.com/unoplatform/uno/issues/9080");
			}

			using var _ = new AssertionScope();
			using var lang = SetAmbiantLanguage("cs-CZ");

			var datePicker = new DatePicker();

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForIdle();

			// CZ uses dd/mm/yyyy
			CheckDateTimeTextBlockPartPosition(datePicker, "YearTextBlock", expectedColumn: 4);
			CheckDateTimeTextBlockPartPosition(datePicker, "MonthTextBlock", expectedColumn: 2);
			CheckDateTimeTextBlockPartPosition(datePicker, "DayTextBlock", expectedColumn: 0);
		}

		[TestMethod]
		public async Task When_Hungarian_Culture_Column_Order()
		{
			if (OperatingSystem.IsBrowser())
			{
				// this test is failing on browser, see https://github.com/unoplatform/uno/issues/9080
				Assert.Inconclusive("https://github.com/unoplatform/uno/issues/9080");
			}

			using var _ = new AssertionScope();
			using var lang = SetAmbiantLanguage("hu-HU");

			var datePicker = new DatePicker();

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForIdle();

			// HU uses yyyy/mm/dd
			CheckDateTimeTextBlockPartPosition(datePicker, "YearTextBlock", expectedColumn: 0);
			CheckDateTimeTextBlockPartPosition(datePicker, "MonthTextBlock", expectedColumn: 2);
			CheckDateTimeTextBlockPartPosition(datePicker, "DayTextBlock", expectedColumn: 4);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15409")]
		public async Task When_Opened_From_Button_Flyout()
		{
			var button = new Button();
			var datePickerFlyout = new DatePickerFlyout();
			button.Flyout = datePickerFlyout;

			var root = new Grid();
			root.Children.Add(button);

			TestServices.WindowHelper.WindowContent = root;

			await TestServices.WindowHelper.WaitForLoaded(root);

			var buttonAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			var invokePattern = buttonAutomationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			invokePattern.Invoke();

			await TestServices.WindowHelper.WaitForIdle();

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).FirstOrDefault();
			var datePickerFlyoutPresenter = popup?.Child as DatePickerFlyoutPresenter;

			try
			{
				Assert.IsNotNull(datePickerFlyoutPresenter);
				Assert.AreEqual(1, datePickerFlyoutPresenter.Opacity);
			}
			finally
			{
				if (popup is not null)
				{
					popup.IsOpen = false;
				}
			}
		}

#if HAS_UNO
		// Validates the workaround for missing support of MonochromaticOverlayPresenter in Uno
		[TestMethod]
		public async Task When_MonochromaticOverlayPresenter_Workaround()
		{
			var timePicker = new DatePicker();
			timePicker.UseNativeStyle = false;

			TestServices.WindowHelper.WindowContent = timePicker;
			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).FirstOrDefault();
			var datePickerFlyoutPresenter = popup?.Child as DatePickerFlyoutPresenter;
			Assert.IsNotNull(datePickerFlyoutPresenter);

			var presenters = VisualTreeUtils.FindVisualChildrenByType<MonochromaticOverlayPresenter>(datePickerFlyoutPresenter);
			foreach (var presenter in presenters)
			{
				Assert.AreEqual(0, presenter.Opacity);
			}

			var highlightRect = VisualTreeUtils.FindVisualChildByName(datePickerFlyoutPresenter, "HighlightRect") as Grid;
			Assert.IsNotNull(highlightRect);
			Assert.AreEqual(0.5, highlightRect.Opacity);
		}
#endif

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Opened_And_Unloaded_Native() => await When_Opened_And_Unloaded(true);

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Opened_And_Unloaded_Managed() => await When_Opened_And_Unloaded(false);

		private async Task When_Opened_And_Unloaded(bool useNative)
		{
			var datePicker = new Microsoft.UI.Xaml.Controls.DatePicker();
#if HAS_UNO
			datePicker.UseNativeStyle = useNative;
#endif

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForLoaded(datePicker);

			await DateTimePickerHelper.OpenDateTimePicker(datePicker);

#if HAS_UNO // FlyoutBase.OpenFlyouts also includes native popups like NativeDatePickerFlyout
			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.HasCount(1, openFlyouts);
			var associatedFlyout = openFlyouts[0];
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.DatePickerFlyout));
#endif

			bool unloaded = false;
			datePicker.Unloaded += (s, e) => unloaded = true;

			TestServices.WindowHelper.WindowContent = null;

			await TestServices.WindowHelper.WaitFor(() => unloaded, message: "DatePicker did not unload");

			var openFlyoutsCount = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count;
			openFlyoutsCount.Should().Be(0, "There should be no open flyouts");

#if HAS_UNO // FlyoutBase.OpenFlyouts also includes native popups like NativeDatePickerFlyout
			openFlyoutsCount = FlyoutBase.OpenFlyouts.Count;
			openFlyoutsCount.Should().Be(0, "There should be no open flyouts");
#endif
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Invoked_Native() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(true);

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Invoked_Managed() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(false);

		private async Task When_Flyout_Closed_FlyoutBase_Closed_Invoked(bool useNative)
		{
			// Open flyout, close it via method or via native dismiss, check if event on flyoutbase was invoked
			var datePicker = new Microsoft.UI.Xaml.Controls.DatePicker();
#if HAS_UNO
			datePicker.UseNativeStyle = useNative;
#endif

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForLoaded(datePicker);

			await DateTimePickerHelper.OpenDateTimePicker(datePicker);

#if !HAS_UNO
			var openFlyouts = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);
			var flyoutBase = openFlyouts[0];
			var associatedFlyout = flyoutBase.AssociatedFlyout;
#else // FlyoutBase.OpenFlyouts also includes native popups like NativeDatePickerFlyout
			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.HasCount(1, openFlyouts);
			var associatedFlyout = openFlyouts[0];
#endif
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.DatePickerFlyout));
			var datePickerFlyout = (DatePickerFlyout)associatedFlyout;

			bool flyoutClosed = false;
			datePickerFlyout.Closed += (s, e) => flyoutClosed = true;
			datePickerFlyout.Hide();

			await TestServices.WindowHelper.WaitFor(() => flyoutClosed, message: "Flyout did not close");
		}

		private static IDisposable SetAmbiantLanguage(string language)
		{
			var previousLanguage = ApplicationLanguages.PrimaryLanguageOverride;
			var currentCulture = CultureInfo.CurrentCulture;
			var currentUICulture = CultureInfo.CurrentUICulture;
			var defaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentCulture;
			var defaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture;
			ApplicationLanguages.PrimaryLanguageOverride = language;
			ApplicationLanguages.ApplyCulture();
			return Disposable.Create(() =>
			{
				ApplicationLanguages.PrimaryLanguageOverride = previousLanguage;
				CultureInfo.CurrentCulture = currentCulture;
				CultureInfo.CurrentUICulture = currentUICulture;
				CultureInfo.DefaultThreadCurrentCulture = defaultThreadCurrentCulture;
				CultureInfo.DefaultThreadCurrentUICulture = defaultThreadCurrentUICulture;
			});
		}

#if HAS_UNO
		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
		public async Task When_UseNativeMinMaxDates_Then_Forwarded_To_Native_Flyout()
		{
			var datePicker = new DatePicker
			{
				UseNativeStyle = true,
				UseNativeMinMaxDates = true,
			};

			TestServices.WindowHelper.WindowContent = datePicker;
			await TestServices.WindowHelper.WaitForLoaded(datePicker);

			try
			{
				await DateTimePickerHelper.OpenDateTimePicker(datePicker);

				var flyout = FlyoutBase.OpenFlyouts.Single();
				flyout.Should().BeAssignableTo<INativeDatePickerFlyout>();
				((INativeDatePickerFlyout)flyout).UseNativeMinMaxDates.Should().BeTrue();
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
				TestServices.WindowHelper.WindowContent = null;
			}
		}
#endif

		private static void CheckDateTimeTextBlockPartPosition(DatePicker datePicker, string id, int expectedColumn)
		{
			var textBlock = MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, id) as TextBlock;
			textBlock.Should().NotBeNull($"TextBlock {id} not found");
			if (textBlock != null)
			{
				var v = textBlock.GetValue(Grid.ColumnProperty);
				((int)v).Should().Be(expectedColumn, $"{id} column property");
			}
		}
	}
}
#endif
