using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.GridPages;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Grid
	{
		[TestMethod]
		[DataRow(20, 20, 70, 60, 0, 0, 90, 20, 110, 0, 30, 20, 160, 0, 90, 20, 0, 40, 90, 260, 110, 40, 30, 120, 160, 40, 90, 120, 110, 180, 140, 120)]
		[DataRow(20, 80, 70, 180, 0, 0, 90, 20, 110, 0, 30, 20, 160, 0, 90, 20, 0, 100, 90, 200, 110, 100, 30, 60, 160, 100, 90, 60, 110, 240, 140, 60)]
		[DataRow(80, 80, 190, 180, 0, 0, 30, 20, 110, 0, 30, 20, 220, 0, 30, 20, 0, 100, 30, 200, 110, 100, 30, 60, 220, 100, 30, 60, 110, 240, 140, 60)]
		[DataRow(-20, 20, 0, 60, 0, 0, 130, 20, 110, 0, 30, 20, 120, 0, 130, 20, 0, 40, 130, 260, 110, 40, 30, 120, 120, 40, 130, 120, 110, 180, 140, 120)]
		[DataRow(20, -20, 70, 0, 0, 0, 90, 20, 110, 0, 30, 20, 160, 0, 90, 20, 0, 0, 90, 300, 110, 0, 30, 160, 160, 0, 90, 160, 110, 140, 140, 160)]
		[DataRow(-20, -20, 0, 0, 0, 0, 130, 20, 110, 0, 30, 20, 120, 0, 130, 20, 0, 0, 130, 300, 110, 0, 30, 160, 120, 0, 130, 160, 110, 140, 140, 160)]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_Has_ColumnSpacing(double columnSpacing,
			double rowSpacing,
			double gridDesiredWidthExpected, double gridDesiredHeightExpected,
			double child0LeftExpected, double child0TopExpected, double child0WidthExpected, double child0HeightExpected,
			double child1LeftExpected, double child1TopExpected, double child1WidthExpected, double child1HeightExpected,
			double child2LeftExpected, double child2TopExpected, double child2WidthExpected, double child2HeightExpected,
			double child3LeftExpected, double child3TopExpected, double child3WidthExpected, double child3HeightExpected,
			double child4LeftExpected, double child4TopExpected, double child4WidthExpected, double child4HeightExpected,
			double child5LeftExpected, double child5TopExpected, double child5WidthExpected, double child5HeightExpected,
			double child6LeftExpected, double child6TopExpected, double child6WidthExpected, double child6HeightExpected
			)
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				TestServices.WindowHelper.WindowContent = null;
			});

			Grid SUT = null;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				SUT = new Grid
				{
					ColumnSpacing = columnSpacing,
					RowSpacing = rowSpacing,
					ColumnDefinitions =
					{
						new ColumnDefinition {Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
						new ColumnDefinition {Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Auto)},
						new ColumnDefinition {Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
					},
					RowDefinitions =
					{
						new RowDefinition {Height = GridLengthHelper2.FromValueAndType(1, GridUnitType.Auto)},
						new RowDefinition {Height = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
						new RowDefinition {Height = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
					},
					Children =
					{
						GetChild(0, 0, height: 20),
						GetChild(1, 0, width:30, height:20),
						GetChild(2, 0, height:20),

						GetChild(0, 1, rowSpan:2),
						GetChild(1, 1, width:30),
						GetChild(2, 1),

						GetChild(1,2, colSpan: 2)
					}
				};

				var outerBorder = new Border { Width = 250, Height = 300 };
				outerBorder.Child = SUT;

				TestServices.WindowHelper.WindowContent = outerBorder;

				FrameworkElement GetChild(int gridCol, int gridRow, int? colSpan = null, int? rowSpan = null, double? width = null, double? height = null)
				{
					var child = new Border();

					Grid.SetColumn(child, gridCol);
					Grid.SetRow(child, gridRow);
					if (colSpan.HasValue)
					{
						Grid.SetColumnSpan(child, colSpan.Value);
					}
					if (rowSpan.HasValue)
					{
						Grid.SetRowSpan(child, rowSpan.Value);
					}
					if (width.HasValue)
					{
						child.Width = width.Value;
					}
					else
					{
						child.HorizontalAlignment = HorizontalAlignment.Stretch;
					}
					if (height.HasValue)
					{
						child.Height = height.Value;
					}
					else
					{
						child.VerticalAlignment = VerticalAlignment.Stretch;
					}

					return child;
				}
			});

			await WaitForMeasure(SUT);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var desiredSize = SUT.DesiredSize;
				var data = $"({columnSpacing}, {rowSpacing}, {desiredSize.Width}, {desiredSize.Height}";
				foreach (var child in SUT.Children)
				{
					var layoutRect = LayoutInformation.GetLayoutSlot(child as FrameworkElement);
					data += $", {layoutRect.Left}, {layoutRect.Top}, {layoutRect.Width}, {layoutRect.Height}";
				}
				data += ")";
				Debug.WriteLine(data);

				Assert.AreEqual(new Size(gridDesiredWidthExpected, gridDesiredHeightExpected), desiredSize);

#if !__ANDROID__ && !__IOS__ // These assertions fail on Android/iOS because layout slots aren't set the same way as UWP
				var layoutRect0Actual = LayoutInformation.GetLayoutSlot(SUT.Children[0] as FrameworkElement);
				var layoutRect0Expected = new Rect(child0LeftExpected, child0TopExpected, child0WidthExpected, child0HeightExpected);
				Assert.AreEqual(layoutRect0Expected, layoutRect0Actual);

				var layoutRect1Actual = LayoutInformation.GetLayoutSlot(SUT.Children[1] as FrameworkElement);
				var layoutRect1Expected = new Rect(child1LeftExpected, child1TopExpected, child1WidthExpected, child1HeightExpected);
				Assert.AreEqual(layoutRect1Expected, layoutRect1Actual);

				var layoutRect2Actual = LayoutInformation.GetLayoutSlot(SUT.Children[2] as FrameworkElement);
				var layoutRect2Expected = new Rect(child2LeftExpected, child2TopExpected, child2WidthExpected, child2HeightExpected);
				Assert.AreEqual(layoutRect2Expected, layoutRect2Actual);

				var layoutRect3Actual = LayoutInformation.GetLayoutSlot(SUT.Children[3] as FrameworkElement);
				var layoutRect3Expected = new Rect(child3LeftExpected, child3TopExpected, child3WidthExpected, child3HeightExpected);
				Assert.AreEqual(layoutRect3Expected, layoutRect3Actual);

				var layoutRect4Actual = LayoutInformation.GetLayoutSlot(SUT.Children[4] as FrameworkElement);
				var layoutRect4Expected = new Rect(child4LeftExpected, child4TopExpected, child4WidthExpected, child4HeightExpected);
				Assert.AreEqual(layoutRect4Expected, layoutRect4Actual);

				var layoutRect5Actual = LayoutInformation.GetLayoutSlot(SUT.Children[5] as FrameworkElement);
				var layoutRect5Expected = new Rect(child5LeftExpected, child5TopExpected, child5WidthExpected, child5HeightExpected);
				Assert.AreEqual(layoutRect5Expected, layoutRect5Actual);

				var layoutRect6Actual = LayoutInformation.GetLayoutSlot(SUT.Children[6] as FrameworkElement);
				var layoutRect6Expected = new Rect(child6LeftExpected, child6TopExpected, child6WidthExpected, child6HeightExpected);
				Assert.AreEqual(layoutRect6Expected, layoutRect6Actual);
#endif

				TestServices.WindowHelper.WindowContent = null;
			});
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_ColumnDefinition_Width_Changed()
		{
			var outerShell = new Grid { Width = 290, Height = 220 };

			var colDef0 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
			var colDef1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
			var SUT = new Grid
			{
				ColumnDefinitions =
				{
					colDef0,
					colDef1,
				}
			};
			outerShell.Children.Add(SUT);
			AddChild(SUT, new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }, 0, 0);
			AddChild(SUT, new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }, 0, 1);

			TestServices.WindowHelper.WindowContent = outerShell;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			const double expectedColWidth = 290 / 2;
			Assert.AreEqual(expectedColWidth, colDef0.ActualWidth);
			Assert.AreEqual(expectedColWidth, colDef1.ActualWidth);

			colDef0.Width = new GridLength(80, GridUnitType.Pixel);
			Assert.AreEqual(expectedColWidth, colDef0.ActualWidth);
			Assert.AreEqual(expectedColWidth, colDef1.ActualWidth);

			await TestServices.WindowHelper.WaitForRelayouted(SUT);

			Assert.AreEqual(80, colDef0.ActualWidth);
			Assert.AreEqual(210, colDef1.ActualWidth);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Definitions_Cleared_And_Empty()
		{
			var SUT = new Grid();
			var tb = new TextBlock { Text = "Text" };
			AddChild(SUT, new Border { Width = 50, Height = 20 }, 0, 0);
			AddChild(SUT, tb, 0, 0);

			TestServices.WindowHelper.WindowContent = SUT;
			SUT.ColumnDefinitions.Clear();

			await TestServices.WindowHelper.WaitForLoaded(SUT);

			await TestServices.WindowHelper.WaitForLoaded(tb); // Needed for iOS where measurement is async

			NumberAssert.Greater(tb.ActualWidth, 0);
			NumberAssert.Greater(tb.ActualHeight, 0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_Child_Added_Measure_And_Visible_Arrange()
		{
			// This test emulates the layout sequence associated with DataGridColumnHeadersPresenter in the WCT
			var gridAdder = new GridAdder() { MinWidth = 5, MinHeight = 5 };
			TestServices.WindowHelper.WindowContent = gridAdder;
			await TestServices.WindowHelper.WaitForLoaded(gridAdder);

			await TestServices.WindowHelper.WaitFor(() => gridAdder.WasArranged); // Needed for iOS where measurement is async

			if (gridAdder.Exception != null)
			{
				throw new AssertFailedException("Exception occurred", gridAdder.Exception);
			}

			var SUT = gridAdder.AddedGrid;
			Assert.IsNotNull(SUT);
			Assert.AreEqual(Visibility.Visible, SUT.Visibility);

#if !__ANDROID__ && !__IOS__ // The Grid contents doesn't seem to actually display properly when added this way, but at least it should not throw an exception.
			Assert.AreEqual(27, SUT.ActualHeight);
			NumberAssert.Greater(SUT.ActualWidth, 0);
#endif
		}

		private static void AddChild(Grid parent, FrameworkElement child, int row, int col, int? rowSpan = null, int? colSpan = null)
		{
			Grid.SetRow(child, row);
			Grid.SetColumn(child, col);

			if (rowSpan is { } rs)
			{
				Grid.SetRowSpan(child, rs);
			}
			if (colSpan is { } cs)
			{
				Grid.SetColumnSpan(child, cs);
			}

			parent.Children.Add(child);
		}

		private async Task WaitForMeasure(FrameworkElement view, int timeOutMs = 1000)
		{
			var isMeasured = false;
			var stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedMilliseconds < timeOutMs)
			{
				await RunOnUIThread.ExecuteAsync(() =>
				{
					isMeasured = view.DesiredSize != default(Size);
				});
				if (isMeasured)
				{
					return;
				}

				await Task.Delay(50);
			}
		}
	}
}
