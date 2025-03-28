using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using ProgressBar = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressBar;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

using ProgressBar = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressBar;

[TestClass]
[RunsOnUIThread]
public class Given_ProgressBar
{
	[TestMethod]
	public async Task ProgressBarLayoutUpdate()
	{
		if (OperatingSystem.IsMacOS())
		{
			Assert.Inconclusive("This test is not valid on macOS");
			return;
		}

		var sut = new ProgressBar();
		try
		{
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
		finally
		{
			sut.IsIndeterminate = false;
		}
	}

	[TestMethod]
	public async Task When_CustomTemplate()
	{
		var sut = new ProgressBar();
		var sizeChangedRaised = false;
		sut.SizeChanged += (_, _) => sizeChangedRaised = true;
		sut.Template = new Windows.UI.Xaml.Controls.ControlTemplate();
		sut.Width = 50;
		sut.Height = 50;
		TestServices.WindowHelper.WindowContent = sut;

		await TestServices.WindowHelper.WaitFor(() => sizeChangedRaised);
	}
}
