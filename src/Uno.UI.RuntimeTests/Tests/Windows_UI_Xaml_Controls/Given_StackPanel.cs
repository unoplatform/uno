using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
	public class Given_StackPanel
	{
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

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(200, ((UIElement)VisualTreeHelper.GetChild(SUT, 0)).ActualOffset.Y);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_InsertingChildren_Then_ResultIsInRightOrder()
		{
			// This is an illustration of the bug https://github.com/unoplatform/uno/issues/3543

			var pnl = new StackPanel();
			pnl.Children.Add(new Button { Content = "abc" });
			pnl.Children.Insert(0, new TextBlock { Text = "TextBlock" });
			pnl.Children.Insert(0, new TextBox());

			TestServices.WindowHelper.WindowContent = pnl;

			using var _ = new AssertionScope();

			pnl.Children
				.Select(c => c.GetType())
				.Should()
				.Equal(typeof(TextBox), typeof(TextBlock), typeof(Button));

			await TestServices.WindowHelper.WaitForIdle();

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
			foreach (var orientation in Enum.GetValues(typeof(Orientation)).OfType<Orientation>())
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

				TestServices.WindowHelper.WindowContent = outer;

				await TestServices.WindowHelper.WaitForLoaded(constrained);

				if (!double.IsInfinity(maxConstraints.Width))
				{
					Assert.AreEqual(constrained.ActualWidth, maxConstraints.Width);
					Assert.AreEqual(constrained.DesiredSize.Width, maxConstraints.Width);
				}
				if (!double.IsInfinity(maxConstraints.Height))
				{
					Assert.AreEqual(constrained.ActualHeight, maxConstraints.Height);
					Assert.AreEqual(constrained.DesiredSize.Height, maxConstraints.Height);
				}
			}
		}
	}
}
