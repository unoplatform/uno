using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Xaml;
using Uno.UI.RuntimeTests.MUX.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_TimePicker
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SettingNullTime_ShouldNotCrash()
		{
			var timePicker = new TimePicker();
			timePicker.SetBinding(TimePicker.TimeProperty, new Binding { Path = new PropertyPath("StartTime") });

			var root = new Grid
			{
				DataContext = new MyContext()
			};

			root.Children.Add(timePicker);

			TestServices.WindowHelper.WindowContent = root;

			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PM_Opened_And_Closed()
		{
			// This tests whether the looping selector does not unexpectedly
			// change the time when the flyout is opened and closed.
			var timePicker = new TimePicker();
			var expectedTime = new TimeSpan(15, 0, 0);
			timePicker.Time = expectedTime;
			timePicker.SelectedTime = expectedTime;
			TestServices.WindowHelper.WindowContent = timePicker;
			await TestServices.WindowHelper.WaitForLoaded(timePicker);
			await DateTimePickerHelper.OpenDateTimePicker(timePicker);
			await TestServices.WindowHelper.WaitForIdle();

			// Confirm the "same time"
			await ControlHelper.ClickFlyoutCloseButton(timePicker, true /* isAccept */);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(expectedTime, timePicker.SelectedTime);
			Assert.AreEqual(expectedTime, timePicker.Time);
		}
	}

	class MyContext
	{
		public object StartTime => null;
	}
}
