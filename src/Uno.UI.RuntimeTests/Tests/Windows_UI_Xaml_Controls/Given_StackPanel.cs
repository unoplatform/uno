using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Media.Animation;
using SamplesApp.UITests;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
	public partial class Given_StackPanel
	{
		private partial class MyStackPanel : StackPanel
		{
			public int MeasureCount { get; private set; }
			public int ArrangeCount { get; private set; }

			public Size LastMeasureOverrideReturn { get; private set; }
			public Size LastArrangeOverrideReturn { get; private set; }

			protected override Size MeasureOverride(Size availableSize)
			{
				MeasureCount++;
				return LastMeasureOverrideReturn = base.MeasureOverride(availableSize);
			}

			protected override Size ArrangeOverride(Size arrangeSize)
			{
				ArrangeCount++;
				return LastArrangeOverrideReturn = base.ArrangeOverride(arrangeSize);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __IOS__
		[Ignore("Fails on iOS")]
#endif
		public async Task When_Adding_Or_Removing_Child_Should_Re_Measure()
		{
			var SUT = new MyStackPanel()
			{
				Children =
				{
					new Border()
					{
						Width = 50,
						Height = 50,
					}
				}
			};

			int expectedMeasureAndArrangeCount = 0;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();
			expectedMeasureAndArrangeCount++;
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.MeasureCount);
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.ArrangeCount);
			Assert.AreEqual(50, SUT.LastMeasureOverrideReturn.Height);
			Assert.AreEqual(50, SUT.LastArrangeOverrideReturn.Height);

			SUT.Children.Add(new Border()
			{
				Width = 50,
				Height = 50,
			});
			await TestServices.WindowHelper.WaitForRelayouted(SUT);
			expectedMeasureAndArrangeCount++;
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.MeasureCount);
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.ArrangeCount);
			Assert.AreEqual(100, SUT.LastMeasureOverrideReturn.Height);
			Assert.AreEqual(100, SUT.LastArrangeOverrideReturn.Height);

			SUT.Children.RemoveAt(1);
			await TestServices.WindowHelper.WaitForRelayouted(SUT);
			expectedMeasureAndArrangeCount++;
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.MeasureCount);
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.ArrangeCount);
			Assert.AreEqual(50, SUT.LastMeasureOverrideReturn.Height);
			Assert.AreEqual(50, SUT.LastArrangeOverrideReturn.Height);

			SUT.Children.Remove(SUT.Children.Single());
			await TestServices.WindowHelper.WaitForRelayouted(SUT);
			expectedMeasureAndArrangeCount++;
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.MeasureCount);
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.ArrangeCount);
			Assert.AreEqual(0, SUT.LastMeasureOverrideReturn.Height);
			Assert.AreEqual(0, SUT.LastArrangeOverrideReturn.Height);

			SUT.Children.Add(new Border()
			{
				Width = 50,
				Height = 50,
			});
			await TestServices.WindowHelper.WaitForRelayouted(SUT);
			expectedMeasureAndArrangeCount++;
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.MeasureCount);
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.ArrangeCount);
			Assert.AreEqual(50, SUT.LastMeasureOverrideReturn.Height);
			Assert.AreEqual(50, SUT.LastArrangeOverrideReturn.Height);

			SUT.Children.Clear();
			await TestServices.WindowHelper.WaitForRelayouted(SUT);
			expectedMeasureAndArrangeCount++;
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.MeasureCount);
			Assert.AreEqual(expectedMeasureAndArrangeCount, SUT.ArrangeCount);
			Assert.AreEqual(0, SUT.LastMeasureOverrideReturn.Height);
			Assert.AreEqual(0, SUT.LastArrangeOverrideReturn.Height);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Padding_Set_In_SizeChanged()
		{
			var SUT = new StackPanel()
			{
				Width = 200,
				Height = 200,
				Children =
				{
					new Border()
					{
						Child = new Ellipse()
						{
							Fill = new SolidColorBrush(Colors.DarkOrange)
						}
					}
				}
			};

			SUT.SizeChanged += (sender, args) => SUT.Padding = new Thickness(0, 200, 0, 0);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// We have a problem on IOS and Android where SUT isn't relayouted after the padding
			// change even though IsMeasureDirty is true. This is a workaround to explicity relayout.
#if __IOS__ || __ANDROID__
			SUT.InvalidateMeasure();
			SUT.UpdateLayout();
#endif

			Assert.AreEqual(200, ((UIElement)VisualTreeHelper.GetChild(SUT, 0)).ActualOffset.Y);
		}

		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/3543")]
		public async Task When_InsertingChildren_Then_ResultIsInRightOrder()
		{
			var pnl = new StackPanel();
			pnl.Children.Add(new Button { Content = "abc" });
			pnl.Children.Insert(0, new TextBlock { Text = "TextBlock" });
			pnl.Children.Insert(0, new TextBox());

			WindowHelper.WindowContent = pnl;

			using var _ = new AssertionScope();

			pnl.Children
				.Select(c => c.GetType())
				.Should()
				.Equal(typeof(TextBox), typeof(TextBlock), typeof(Button));

			await WindowHelper.WaitForIdle();

#if __WASM__
			// Ensure children are synchronized in the DOM
			var js = $@"
				(function() {{
					var stackPanel = document.getElementById(""{pnl.HtmlId}"");
					var result = """";
					for(const elem of stackPanel.children) {{
						result = result + "";"" + elem.id;
					}}
					return result;
				}})();";
			var expectedIds = ";" + string.Join(";", pnl.Children.Select(c => c.HtmlId));

			var ids = global::Uno.Foundation.WebAssemblyRuntime.InvokeJS(js);

			ids.Should().Be(expectedIds, "Expected from DOM");
#endif
		}


		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public Task When_MaxWidth_IsApplied() => MaxSizingTest(new Size(300, double.PositiveInfinity));

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public Task When_MaxHeight_Is_Applied() => MaxSizingTest(new Size(double.PositiveInfinity, 200));

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public Task When_Both_Max_Constraints_Are_Applied() => MaxSizingTest(new Size(300, 200));

		private async Task MaxSizingTest(Size maxConstraints)
		{
			foreach (var orientation in Enum.GetValues<Orientation>())
			{
				var outer = new StackPanel()
				{
					Orientation = orientation
				};
				var constrained = new StackPanel()
				{
					Orientation = orientation
				};
				if (!double.IsInfinity(maxConstraints.Width))
				{
					constrained.MaxWidth = maxConstraints.Width;
				}
				if (!double.IsInfinity(maxConstraints.Height))
				{
					constrained.MaxHeight = maxConstraints.Height;
				}

				var child = new Border()
				{
					Width = 1000,
					Height = 1000
				};

				outer.Children.Add(constrained);
				constrained.Children.Add(child);

				WindowHelper.WindowContent = outer;

				await WindowHelper.WaitForLoaded(constrained);

				if (!double.IsInfinity(maxConstraints.Width))
				{
#if WINAPPSDK || __CROSSRUNTIME__
					Assert.AreEqual(constrained.ActualWidth, orientation == Orientation.Horizontal ? 1000 : maxConstraints.Width);
#else
					// TODO: Align Uno with Windows behavior.
					Assert.AreEqual(constrained.ActualWidth, maxConstraints.Width);
#endif

					Assert.AreEqual(constrained.DesiredSize.Width, maxConstraints.Width);
				}
				if (!double.IsInfinity(maxConstraints.Height))
				{
#if WINAPPSDK || __CROSSRUNTIME__
					Assert.AreEqual(constrained.ActualHeight, orientation == Orientation.Vertical ? 1000 : maxConstraints.Height);
#else
					// TODO: Align Uno with Windows behavior.
					Assert.AreEqual(constrained.ActualHeight, maxConstraints.Height);
#endif

					Assert.AreEqual(constrained.DesiredSize.Height, maxConstraints.Height);
				}
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Big_Elements_Horizontal_SnapPoints()
		{
			var grid = (Grid)XamlReader.Load("""
											 <Grid Width="500" Height="500"
											 		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
											 		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
											 		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
											 		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
											 		mc:Ignorable="d">
											 	<StackPanel x:Name="sp" Margin="29" Orientation="Horizontal">
											 		<Border Width="400" Height="300" Margin="1,2,3,4">
											 			<Ellipse Fill="Red" />
											 		</Border>
											 		<Border Width="400" Height="300" Margin="10,20,30,40">
											 			<Ellipse Fill="Green" />
											 		</Border>
											 		<Border Width="400" Height="300" Margin="100,200,300,400">
											 			<Ellipse Fill="Blue" />
											 		</Border>
											 	</StackPanel>
											 </Grid>
											 """);

			var SUT = (StackPanel)grid.Children[0];

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentTo(new float[]
			{
				0,
				433,
				873
			});

			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentTo(new float[]
			{
				433,
				873,
				1673
			});

			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentTo(new float[]
			{
				231,
				653,
				1273
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Small_Elements_Horizontal_SnapPoints()
		{
			var grid = (Grid)XamlReader.Load("""
											 <Grid Width="500" Height="500"
											 		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
											 		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
											 		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
											 		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
											 		mc:Ignorable="d">
											 	<StackPanel x:Name="sp" Margin="29" Orientation="Horizontal">
											 		<Border Width="40" Height="30" Margin="1,2,3,4">
											 			<Ellipse Fill="Red" />
											 		</Border>
											 		<Border Width="40" Height="30" Margin="9,11,13,15">
											 			<Ellipse Fill="Green" />
											 		</Border>
											 		<Border Width="40" Height="30" Margin="10,20,30,40">
											 			<Ellipse Fill="Blue" />
											 		</Border>
											 	</StackPanel>
											 </Grid>
											 """);

			var SUT = (StackPanel)grid.Children[0];

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentTo(new float[]
			{
				0,
				73,
				135
			});

			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentTo(new float[]
			{
				73,
				135,
				215
			});

			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentTo(new float[]
			{
				51,
				104,
				175
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_No_Children_SnapPoints()
		{
			var SUT = new StackPanel()
			{
				Width = 100,
				Height = 100,
				BorderThickness = new Microsoft.UI.Xaml.Thickness(5),
				BorderBrush = new SolidColorBrush(Colors.Bisque)
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEmpty();
			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEmpty();
			SUT.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEmpty();
			SUT.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).ToList().Should().BeEmpty();
			SUT.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).ToList().Should().BeEmpty();
			SUT.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).ToList().Should().BeEmpty();
		}
	}
}
