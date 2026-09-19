using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using ProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

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
		sut.Template = new Microsoft.UI.Xaml.Controls.ControlTemplate();
		sut.Width = 50;
		sut.Height = 50;
		TestServices.WindowHelper.WindowContent = sut;

		await TestServices.WindowHelper.WaitFor(() => sizeChangedRaised);
	}

	[TestMethod]
	public async Task When_BorderThickness_Then_Determinate_Indicator_Fits_Inside_Border()
	{
		var sut = new ProgressBar
		{
			Width = 200,
			Maximum = 100,
			Value = 100,
			BorderThickness = new Thickness(4, 0, 4, 0),
		};

		await UITestHelper.Load(sut);

		var indicator = sut.FindFirstDescendant<Rectangle>("DeterminateProgressBarIndicator");
		Assert.IsNotNull(indicator);
		Assert.AreEqual(192, indicator.Width, 0.01);
	}

	[TestMethod]
	public async Task When_BorderThickness_Then_Indeterminate_Indicators_Use_Inner_Width()
	{
		var sut = new ProgressBar
		{
			Width = 200,
			IsIndeterminate = true,
			BorderThickness = new Thickness(4, 0, 4, 0),
		};

		await UITestHelper.Load(sut);

		var indicator = sut.FindFirstDescendant<Rectangle>("IndeterminateProgressBarIndicator");
		var indicator2 = sut.FindFirstDescendant<Rectangle>("IndeterminateProgressBarIndicator2");
		Assert.IsNotNull(indicator);
		Assert.IsNotNull(indicator2);
		Assert.AreEqual(192 * 0.4, indicator.Width, 0.01);
		Assert.AreEqual(192 * 0.6, indicator2.Width, 0.01);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Indeterminate_And_Paused_Or_Error_Then_Second_Indicator_Spans_Full_Width(bool useShowError)
	{
		var sut = new ProgressBar
		{
			Width = 200,
			IsIndeterminate = true,
		};

		await UITestHelper.Load(sut);

		var indicator2 = sut.FindFirstDescendant<Rectangle>("IndeterminateProgressBarIndicator2");
		Assert.IsNotNull(indicator2);
		Assert.AreEqual(200 * 0.6, indicator2.Width, 0.01);

		if (useShowError)
		{
			sut.ShowError = true;
		}
		else
		{
			sut.ShowPaused = true;
		}

		Assert.AreEqual(200, indicator2.Width, 0.01);
	}

	[TestMethod]
	public async Task When_Indeterminate_Collapsed_Then_Determinate_State()
	{
		var sut = new ProgressBar
		{
			Width = 200,
			IsIndeterminate = true,
		};

		await UITestHelper.Load(sut);

		var layoutRoot = sut.FindFirstDescendant<Grid>("LayoutRoot");
		Assert.IsNotNull(layoutRoot);
		var commonStates = VisualStateManager.GetVisualStateGroups(layoutRoot).Single(g => g.Name == "CommonStates");
		Assert.AreEqual("Indeterminate", commonStates.CurrentState?.Name);

		sut.Visibility = Visibility.Collapsed;

		Assert.AreEqual("Determinate", commonStates.CurrentState?.Name);
	}

	[TestMethod]
	public async Task When_AutomationPeer_SetValue_Then_Value_Updated()
	{
		var sut = new ProgressBar
		{
			Width = 200,
			Maximum = 100,
		};

		await UITestHelper.Load(sut);

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
		var rangeValueProvider = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);

		rangeValueProvider.SetValue(42);

		Assert.AreEqual(42, sut.Value);
		Assert.IsTrue(rangeValueProvider.IsReadOnly);
	}
}
