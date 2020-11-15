using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
    public class Given_DatePicker
    {
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_United_States_Culture_Column_Order()
		{
			var originalCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("en-US");
				var datePicker = new DatePicker();

				TestServices.WindowHelper.WindowContent = datePicker;

				await TestServices.WindowHelper.WaitForIdle();

				// US uses mm/dd/yyyy

				var monthTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "MonthTextBlock");
				Assert.AreEqual(0, (int)monthTextBlock.GetValue(Grid.ColumnProperty));
				var dayTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "DayTextBlock");
				Assert.AreEqual(2, (int)dayTextBlock.GetValue(Grid.ColumnProperty));
				var yearTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "YearTextBlock");
				Assert.AreEqual(4, (int)yearTextBlock.GetValue(Grid.ColumnProperty));
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;

				TestServices.WindowHelper.WindowContent = null;

				await TestServices.WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Czech_Culture_Column_Order()
		{
			var originalCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("cs-CZ");
				var datePicker = new DatePicker();

				TestServices.WindowHelper.WindowContent = datePicker;

				await TestServices.WindowHelper.WaitForIdle();

				// CZ uses mm/dd/yyyy

				var dayTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "DayTextBlock");
				Assert.AreEqual(0, (int)dayTextBlock.GetValue(Grid.ColumnProperty));
				var monthTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "MonthTextBlock");
				Assert.AreEqual(2, (int)monthTextBlock.GetValue(Grid.ColumnProperty));
				var yearTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "YearTextBlock");
				Assert.AreEqual(4, (int)yearTextBlock.GetValue(Grid.ColumnProperty));
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;

				TestServices.WindowHelper.WindowContent = null;

				await TestServices.WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Hungarian_Culture_Column_Order()
		{
			var originalCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("hu-HU");
				var datePicker = new DatePicker();

				TestServices.WindowHelper.WindowContent = datePicker;

				await TestServices.WindowHelper.WaitForIdle();

				// HU uses yyyy/mm/dd

				var yearTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "YearTextBlock");
				Assert.AreEqual(0, (int)yearTextBlock.GetValue(Grid.ColumnProperty));
				var monthTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "MonthTextBlock");
				Assert.AreEqual(2, (int)monthTextBlock.GetValue(Grid.ColumnProperty));
				var dayTextBlock = (TextBlock)MUXControlsTestApp.Utilities.VisualTreeUtils.FindVisualChildByName(datePicker, "DayTextBlock");
				Assert.AreEqual(4, (int)dayTextBlock.GetValue(Grid.ColumnProperty));
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;

				TestServices.WindowHelper.WindowContent = null;

				await TestServices.WindowHelper.WaitForIdle();
			}
		}
	}
}
