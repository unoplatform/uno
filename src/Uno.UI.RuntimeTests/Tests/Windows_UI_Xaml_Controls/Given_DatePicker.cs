#if !WINAPPSDK
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Android | RuntimeTestPlatforms.IOS)]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("Fails on Android and iOS")]
#endif
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
					Assert.AreEqual(1, openFlyouts.Count);
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
		public async Task When_CanadaEnglish_Culture_Column_Order()
		{
			using var _ = new AssertionScope();
			using var lang = SetAmbiantLanguage("en-CA");

			var datePicker = new DatePicker();

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForIdle();

			// en-CA uses yyyy/mm/dd
			CheckDateTimeTextBlockPartPosition(datePicker, "YearTextBlock", expectedColumn: 0);
			CheckDateTimeTextBlockPartPosition(datePicker, "MonthTextBlock", expectedColumn: 2);
			CheckDateTimeTextBlockPartPosition(datePicker, "DayTextBlock", expectedColumn: 4);
		}

		[TestMethod]
#if __WASM__
		[Ignore("https://github.com/unoplatform/uno/issues/9080")] // Works locally but not in chromium
#endif
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

			// fr-CA uses yyyy/mm/dd
			CheckDateTimeTextBlockPartPosition(datePicker, "YearTextBlock", expectedColumn: 0);
			CheckDateTimeTextBlockPartPosition(datePicker, "MonthTextBlock", expectedColumn: 2);
			CheckDateTimeTextBlockPartPosition(datePicker, "DayTextBlock", expectedColumn: 4);
		}

		[TestMethod]
#if __WASM__
		[Ignore("https://github.com/unoplatform/uno/issues/9080")] // Works locally but not in chromium
#endif
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
#if __WASM__
		[Ignore("https://github.com/unoplatform/uno/issues/9080")] // Works locally but not in chromium
#endif
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
			Assert.AreEqual(1, openFlyouts.Count);
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

#if __ANDROID__ || __APPLE_UIKIT__
			if (useNative)
			{
				var nativeDatePickerFlyout = (NativeDatePickerFlyout)associatedFlyout;
				Assert.IsFalse(nativeDatePickerFlyout.IsNativeDialogOpen);
			}
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
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];
#endif
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.DatePickerFlyout));
			var datePickerFlyout = (DatePickerFlyout)associatedFlyout;

			bool flyoutClosed = false;
			datePickerFlyout.Closed += (s, e) => flyoutClosed = true;
			datePickerFlyout.Close();

			await TestServices.WindowHelper.WaitFor(() => flyoutClosed, message: "Flyout did not close");

#if __ANDROID__ || __APPLE_UIKIT__
			if (useNative)
			{
				var nativeDatePickerFlyout = (NativeDatePickerFlyout)datePickerFlyout;
				Assert.IsFalse(nativeDatePickerFlyout.IsNativeDialogOpen);
			}
#endif
		}

#if __ANDROID__ || __APPLE_UIKIT__
		[TestMethod]
		public async Task When_Default_Flyout_Date_Native()
		{
			var now = DateTimeOffset.UtcNow;
			var datePicker = new Microsoft.UI.Xaml.Controls.DatePicker();
			datePicker.UseNativeStyle = true;

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForLoaded(datePicker);

			await DateTimePickerHelper.OpenDateTimePicker(datePicker);

			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.NativeDatePickerFlyout));

			var datePickerFlyout = (NativeDatePickerFlyout)associatedFlyout;

			try
			{
				Assert.AreEqual(DatePicker.NullDateSentinelValue, datePickerFlyout.Date);
				Assert.AreEqual(now.Day, datePickerFlyout.NativeDialogDate.Day);
				Assert.AreEqual(now.Month, datePickerFlyout.NativeDialogDate.Month);
				Assert.AreEqual(now.Year, datePickerFlyout.NativeDialogDate.Year);
			}
			finally
			{
				datePickerFlyout.Close();
			}
		}
#endif

#if __IOS__
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15263")]
		public async Task When_App_Theme_Dark_Native_Flyout_Theme()
		{
			using var _ = ThemeHelper.UseDarkTheme();
			await When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle.Dark);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15263")]
		public async Task When_App_Theme_Light_Native_Flyout_Theme() => await When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle.Light);

		private async Task When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle expectedStyle)
		{
			var datePicker = new Microsoft.UI.Xaml.Controls.DatePicker();
			datePicker.UseNativeStyle = true;

			TestServices.WindowHelper.WindowContent = datePicker;

			await TestServices.WindowHelper.WaitForLoaded(datePicker);

			await DateTimePickerHelper.OpenDateTimePicker(datePicker);

			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.DatePickerFlyout));
			var datePickerFlyout = (DatePickerFlyout)associatedFlyout;

			var nativeDatePickerFlyout = (NativeDatePickerFlyout)datePickerFlyout;

			var nativeDatePicker = nativeDatePickerFlyout._selector;
			Assert.AreEqual(expectedStyle, nativeDatePicker.OverrideUserInterfaceStyle);
		}
#endif

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
