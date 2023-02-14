using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_CalendarDatePicker
{
#if !WINDOWS_UWP && !__MACOS__ // test is failling in macOS for some reason.
	[TestMethod]
	public async Task TestCalendarPanelSize()
	{
		var SUT = new CalendarDatePicker();
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForIdle();

		var root = (Grid)SUT.FindName("Root");
		var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(root);
		flyout.Open();

		await WindowHelper.WaitForIdle();
		var calendarView = (CalendarView)flyout.Content;

		Assert.IsTrue(calendarView.ActualHeight > 300);

		flyout.Close();
	}
#endif
}
