using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using ProgressBar = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressBar;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls.ProgressBar;

using ProgressBar = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressBar;

[TestClass]
public class ProgressBarTest
{

	[RunsOnUIThread]
	[TestMethod]
	public async Task ProgressBarLayoutUpdate()
	{
		if (OperatingSystem.IsMacOS())
		{
			Assert.Inconclusive("This test is not valid on macOS");
			return;
		}

		var sut = new ProgressBar();
		sut.Width = 200;
		sut.Height = 40;
		var layoutCount = 0;
		sut.LayoutUpdated += async (s, e) =>
		{
			layoutCount++;
			await Task.Delay(1000);
		};

		TestServices.WindowHelper.WindowContent = sut;
		await Task.Delay(1000);
		await TestServices.WindowHelper.WaitForLoaded(sut);

		sut.IsIndeterminate = true;
		await Task.Delay(1000);

		Verify.IsLessThanOrEqual(layoutCount, 4);
	}
}
