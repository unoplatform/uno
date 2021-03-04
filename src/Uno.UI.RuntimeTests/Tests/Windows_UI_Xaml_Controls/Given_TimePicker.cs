using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

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
	}

	class MyContext
	{
		public object StartTime => null;
	}
}
