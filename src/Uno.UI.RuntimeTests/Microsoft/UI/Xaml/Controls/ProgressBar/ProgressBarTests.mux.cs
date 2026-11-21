// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressBar/APITests/ProgressBarTests.cs and ProgressBar/InteractionTests/ProgressBarTests.cs

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class ProgressBarTests : MUXApiTestBase
	{
		[TestMethod]
		public void ResourceOverridablity()
		{
			RunOnUIThread.Execute(() =>
			{
				var root = (StackPanel)XamlReader.Load(
	@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
							 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
							 xmlns:primitives='using:Microsoft.UI.Xaml.Controls.Primitives'
							 xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
							 <Grid>
								<Grid.Resources>
									<x:Double x:Key='ProgressBarTrackHeight'>3</x:Double>
								</Grid.Resources>
								<controls:ProgressBar/>
							</Grid>
							<controls:ProgressBar/>
					   </StackPanel>");

				Content = root;
				Content.UpdateLayout();

				var grid = VisualTreeHelper.GetChild(root, 0);
				var progressBar1 = VisualTreeHelper.GetChild(grid, 0);
				var templateRoot1 = VisualTreeHelper.GetChild(progressBar1, 0);
				var Border11 = VisualTreeHelper.GetChild(templateRoot1, 0);
				var Border12 = VisualTreeHelper.GetChild(Border11, 0);
				var grid1 = VisualTreeHelper.GetChild(Border12, 0);
				var rect1 = (Rectangle)VisualTreeHelper.GetChild(grid1, 0);
				Verify.AreEqual(3, rect1.Height);

				var progressBar2 = VisualTreeHelper.GetChild(root, 1);
				var templateRoot2 = VisualTreeHelper.GetChild(progressBar2, 0);
				var Border21 = VisualTreeHelper.GetChild(templateRoot2, 0);
				var Border22 = VisualTreeHelper.GetChild(Border21, 0);
				var grid2 = VisualTreeHelper.GetChild(Border22, 0);
				var rect2 = (Rectangle)VisualTreeHelper.GetChild(grid2, 0);
				Verify.AreEqual(1, rect2.Height);
			});
		}
	}
}

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests
{
	// The UIA queries against ProgressBarPage are replaced by a ProgressBar configured like TestProgressBar,
	// with the page's button handlers and read-outs (indicator width, CommonStates state) inlined.
	// VerifyAxeScanPasses is not ported: it needs the Axe accessibility scanner.
	[TestClass]
	[RunsOnUIThread]
	public class ProgressBarTests
	{
		[TestMethod]
		public async Task ChangeValueTest()
		{
			await using var page = await ProgressBarTestPage.Create();

			Verify.AreEqual(0, page.ProgressBar.Value);

			double oldValue = page.ProgressBar.Value;

			await page.ChangeValue();

			double newValue = page.ProgressBar.Value;
			double diff = Math.Abs(oldValue - newValue);

			Verify.IsGreaterThan(diff, 0.0);
		}

		[TestMethod]
		public async Task UpdateIndicatorWidthTest()
		{
			await using var page = await ProgressBarTestPage.Create();
			await VerifyUpdateIndicatorWidth(page);
		}

		[TestMethod]
		public async Task UpdateMinMaxTest()
		{
			await using var page = await ProgressBarTestPage.Create();
			var progressBar = page.ProgressBar;

			double oldMinimumInputText = progressBar.Minimum;
			double oldMaximumInputText = progressBar.Maximum;

			await page.UpdateMinMax(10, 15);

			double newMinimumInputText = progressBar.Minimum;
			double newMaximumInputText = progressBar.Maximum;

			Verify.AreNotSame(oldMinimumInputText, newMinimumInputText, "Minimum updated");
			Verify.AreNotSame(oldMaximumInputText, newMaximumInputText, "Maximum updated");

			// Below edge cases are handled by Rangebase

			// Updating Minimum and Maximum when Maximum < Minimum
			await page.UpdateMinMax(10, 5);

			Verify.AreEqual(progressBar.Minimum, progressBar.Maximum, "Maximum updates to equal Minimum");

			// Updating Minimum and Maximum when Minimum > Value
			await page.UpdateMinMax(15, 5);

			Verify.AreEqual(progressBar.Value, progressBar.Minimum, "Value updates to equal Minimum");
			Verify.AreEqual(progressBar.Maximum, progressBar.Minimum, "Maximum also updates to equal Minimum");

			// Updating Minimum and Maximum to be a decimal number
			await page.UpdateMinMax(0.1, 1.1);

			double oldValue = progressBar.Value;

			await page.ChangeValue();

			double newValue = progressBar.Value;
			double diff = Math.Abs(oldValue - newValue);

			Verify.IsGreaterThan(diff, Convert.ToDouble(0), "Value of ProgressBar increments properly within range with decimal Minimum and Maximum");
		}

		[TestMethod]
		public async Task ChangeStateTest()
		{
			await using var page = await ProgressBarTestPage.Create();
			var progressBar = page.ProgressBar;

			// Verify all properties are set to false by default for testing
			Verify.IsFalse(progressBar.ShowPaused);
			Verify.IsFalse(progressBar.ShowError);
			Verify.IsFalse(progressBar.IsIndeterminate);

			// All properties to false updates ProgressBar to Determinate
			Verify.AreEqual(page.VisualState, "Determinate");

			// ShowPaused = true updates ProgressBar to Paused visual state
			await page.Toggle(() => progressBar.ShowPaused = !progressBar.ShowPaused);

			Verify.IsTrue(progressBar.ShowPaused);
			Verify.AreEqual(page.VisualState, "Paused");

			// ShowPaused = true && IsIndeterminate = true updates ProgressBar to IndeterminatePaused visual state
			await page.Toggle(() => progressBar.IsIndeterminate = !progressBar.IsIndeterminate);

			Verify.IsTrue(progressBar.ShowPaused);
			Verify.IsTrue(progressBar.IsIndeterminate);
			Verify.AreEqual(page.VisualState, "IndeterminatePaused");

			// IsIndeterminate = true updates ProgressBar to Indeterminate visual state
			await page.Toggle(() => progressBar.ShowPaused = !progressBar.ShowPaused);

			Verify.IsTrue(progressBar.IsIndeterminate);
			Verify.AreEqual(page.VisualState, "Indeterminate");

			// ShowError = true && IsIndeterminate = true updates ProgressBar to IndeterminateError visual state
			await page.Toggle(() => progressBar.ShowError = !progressBar.ShowError);

			Verify.IsTrue(progressBar.ShowError);
			Verify.AreEqual(page.VisualState, "IndeterminateError");

			// ShowError = true updates ProgressBar to Error visual state for Determinate
			await page.Toggle(() => progressBar.IsIndeterminate = !progressBar.IsIndeterminate);

			Verify.IsFalse(progressBar.IsIndeterminate);
			Verify.IsTrue(progressBar.ShowError);
			Verify.AreEqual(page.VisualState, "Error");
		}

		[TestMethod]
		public async Task PaddingOffsetTest()
		{
			await using var page = await ProgressBarTestPage.Create();
			var progressBar = page.ProgressBar;

			// Set ProgressBar Padding settings to default for testing
			await page.UpdatePadding(0, 0);

			Verify.AreEqual(progressBar.Padding.Left, 0);
			Verify.AreEqual(progressBar.Padding.Right, 0);

			// IndicatorWidth offsets where ProgressBar has Padding
			await page.UpdatePadding(10, 10);

			await page.UpdateValue(100);

			double maxIndicatorWidth = progressBar.Width - progressBar.Padding.Left - progressBar.Padding.Right;

			Verify.AreEqual(maxIndicatorWidth, page.IndicatorWidth, "Indicator at max width is reduced by Padding");
		}

		// Upstream runs this and ReTemplateChangeStateTest against the default-template page too:
		// the navigation to ProgressBarReTemplatePage is commented out there.
		[TestMethod]
		public async Task RetemplateUpdateIndicatorWidthTest()
		{
			await using var page = await ProgressBarTestPage.Create();
			await VerifyUpdateIndicatorWidth(page);
		}

		[TestMethod]
		public async Task ReTemplateChangeStateTest()
		{
			await using var page = await ProgressBarTestPage.Create();
			var progressBar = page.ProgressBar;

			// Verify all properties are set to false by default for testing
			Verify.IsFalse(progressBar.ShowPaused);
			Verify.IsFalse(progressBar.ShowError);
			Verify.IsFalse(progressBar.IsIndeterminate);

			// All properties to false updates Re-template ProgressBar to Determinate
			Verify.AreEqual(page.VisualState, "Determinate");

			// ShowPaused = true updates Re-template ProgressBar to Paused visual state
			await page.Toggle(() => progressBar.ShowPaused = !progressBar.ShowPaused);

			Verify.IsTrue(progressBar.ShowPaused);
			Verify.AreEqual(page.VisualState, "Paused");

			// IsIndeterminate = true updates Re-template ProgressBar to Indeterminate visual state
			await page.Toggle(() => progressBar.ShowPaused = !progressBar.ShowPaused);
			await page.Toggle(() => progressBar.IsIndeterminate = !progressBar.IsIndeterminate);

			Verify.IsTrue(progressBar.IsIndeterminate);
			Verify.AreEqual(page.VisualState, "Indeterminate");

			// ShowError = true updates Re-template ProgressBar to Error visual state for Determinate
			await page.Toggle(() => progressBar.IsIndeterminate = !progressBar.IsIndeterminate);
			await page.Toggle(() => progressBar.ShowError = !progressBar.ShowError);

			Verify.IsFalse(progressBar.IsIndeterminate);
			Verify.IsTrue(progressBar.ShowError);
			Verify.AreEqual(page.VisualState, "Error");
		}

		private static async Task VerifyUpdateIndicatorWidth(ProgressBarTestPage page)
		{
			var progressBar = page.ProgressBar;

			await page.UpdateMinMax(0, 100);
			await page.UpdateWidth(100);

			Verify.AreEqual(progressBar.Minimum, 0);
			Verify.AreEqual(progressBar.Maximum, 100);
			Verify.AreEqual(progressBar.Width, 100);

			// Changing value of ProgressBar updates Indicator Width
			await page.ChangeValue();

			page.VerifyIndicatorWidth(progressBar.Value);

			// Updating width of ProgressBar also updates Indicator Width
			await page.UpdateWidth(200);

			page.VerifyIndicatorWidth(progressBar.Value * 2, "Indicator width is adjusted to ProgressBar width");

			// Changing value of ProgressBar of different width updates Indicator width
			await page.ChangeValue();

			page.VerifyIndicatorWidth(progressBar.Value * 2, "Indicator width is adjusted to ProgressBar width");

			// Updating Maximum and Minimum also updates Indicator Width
			await page.UpdateMinMax(10, 16);

			await page.ChangeValue();

			double range = progressBar.Maximum - progressBar.Minimum;
			double adjustedValueFromRange = progressBar.Value - progressBar.Minimum;
			double calculatedValue = (adjustedValueFromRange / range) * progressBar.Width;

			Verify.IsLessThan(Math.Abs(calculatedValue - page.IndicatorWidth), 0.5, "Indicator Width is adjusted based on range and ProgressBar width");
		}

		private sealed class ProgressBarTestPage : IAsyncDisposable
		{
			private ProgressBarTestPage(ProgressBar progressBar) => ProgressBar = progressBar;

			public ProgressBar ProgressBar { get; }

			public static async Task<ProgressBarTestPage> Create()
			{
				var progressBar = new ProgressBar
				{
					Width = 300,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Left,
				};

				TestServices.WindowHelper.WindowContent = new StackPanel { Children = { progressBar } };
				await TestServices.WindowHelper.WaitForLoaded(progressBar);
				await TestServices.WindowHelper.WaitForIdle();

				return new ProgressBarTestPage(progressBar);
			}

			public async ValueTask DisposeAsync()
			{
				TestServices.WindowHelper.WindowContent = null;
				await TestServices.WindowHelper.WaitForIdle();
			}

			private Grid LayoutRoot => (Grid)VisualTreeHelper.GetChild(ProgressBar, 0);

			public string VisualState => VisualStateManager.GetVisualStateGroups(LayoutRoot)[0].CurrentState.Name;

			public double IndicatorWidth => ((Rectangle)VisualTreeUtils.FindVisualChildByName(ProgressBar, "DeterminateProgressBarIndicator")).ActualWidth;

			// The upstream lab runs at 100% scale; elsewhere ActualWidth is layout-rounded to physical pixels.
			public void VerifyIndicatorWidth(double expected, string message = null)
			{
				var scale = ProgressBar.XamlRoot.RasterizationScale;
				var rounded = Math.Round(expected * scale, MidpointRounding.AwayFromZero) / scale;
				Verify.IsLessThan(Math.Abs(rounded - IndicatorWidth), 0.001, message);
			}

			public async Task Toggle(Action toggle)
			{
				toggle();
				await TestServices.WindowHelper.WaitForIdle();
			}

			public async Task UpdateMinMax(double minimum, double maximum)
			{
				ProgressBar.Maximum = maximum;
				ProgressBar.Minimum = minimum;
				await TestServices.WindowHelper.WaitForIdle();
			}

			public async Task UpdateWidth(double width)
			{
				ProgressBar.Width = width;
				await TestServices.WindowHelper.WaitForIdle();
			}

			public async Task UpdateValue(double value)
			{
				ProgressBar.Value = value;
				await TestServices.WindowHelper.WaitForIdle();
			}

			public async Task ChangeValue()
			{
				if (ProgressBar.Value + 1 > ProgressBar.Maximum)
				{
					ProgressBar.Value = (int)(ProgressBar.Minimum + 0.5);
				}
				else
				{
					ProgressBar.Value += 1;
				}
				await TestServices.WindowHelper.WaitForIdle();
			}

			public async Task UpdatePadding(double left, double right)
			{
				ProgressBar.Padding = new Thickness(left, 0, right, 0);
				await TestServices.WindowHelper.WaitForIdle();
			}
		}
	}
}
