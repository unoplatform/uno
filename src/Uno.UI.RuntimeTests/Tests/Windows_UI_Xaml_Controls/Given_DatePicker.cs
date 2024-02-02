#if !WINDOWS_UWP
using System;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Globalization;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

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
