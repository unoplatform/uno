#if !WINAPPSDK
using System;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Globalization;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_DatePicker
	{
		[TestInitialize]
		public async Task Setup()
		{
			TestServices.WindowHelper.WindowContent = null;

			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[RunsOnUIThread]
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
		[RunsOnUIThread]
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
		[RunsOnUIThread]
		public async Task When_CanadaFrench_Culture_Column_Order()
		{
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
		[RunsOnUIThread]
		public async Task When_Czech_Culture_Column_Order()
		{
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
		[RunsOnUIThread]
		public async Task When_Hungarian_Culture_Column_Order()
		{
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
		[RunsOnUIThread]
		public async Task When_Opened_And_Unloaded_Native() => await When_Opened_And_Unloaded(true);

		[TestMethod]
		[RunsOnUIThread]
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

#if __ANDROID__ || __IOS__
			if (useNative)
			{
				var nativeDatePickerFlyout = (NativeDatePickerFlyout)associatedFlyout;
				Assert.IsFalse(nativeDatePickerFlyout.IsNativeDialogOpen);
			}
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Invoked_Native() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(true);

		[TestMethod]
		[RunsOnUIThread]
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

#if __ANDROID__ || __IOS__
			if (useNative)
			{
				var nativeDatePickerFlyout = (NativeDatePickerFlyout)datePickerFlyout;
				Assert.IsFalse(nativeDatePickerFlyout.IsNativeDialogOpen);
			}
#endif
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
