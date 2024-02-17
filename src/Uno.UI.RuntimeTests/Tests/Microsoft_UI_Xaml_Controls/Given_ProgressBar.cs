using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
<<<<<<< HEAD:src/Uno.UI.RuntimeTests/MUX/Microsoft_UI_Xaml_Controls/ProgressBar/ProgressBarTest.cs
using ProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;
=======
using Uno.UI.RuntimeTests.Helpers;
using ProgressBar = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressBar;
>>>>>>> fe561607ee (fix(android): Fix NullReferenceException in ProgressBar for custom templates):src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_ProgressBar.cs

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

using ProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;

[TestClass]
[RunsOnUIThread]
public class Given_ProgressBar
{
	[TestMethod]
	public async Task ProgressBarLayoutUpdate()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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

	[TestMethod]
	public async Task When_CustomTemplate()
	{
		var sut = new ProgressBar();
		var sizeChangedRaised = false;
		sut.SizeChanged += (_, _) => sizeChangedRaised = true;
		sut.Template = new Microsoft.UI.Xaml.Controls.ControlTemplate();
		sut.Width = 50;
		sut.Height = 50;
		TestServices.WindowHelper.WindowContent = sut;

		await TestServices.WindowHelper.WaitFor(() => sizeChangedRaised);
	}
}
