using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_StackPanel
	{
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

		public async Task When_AvailableSizeIsGreaterThanContent()
		{
			using var _ = new AssertionScope();

			var pnl = new StackPanel { Spacing = 1 };
			pnl.Children.Add(new Rectangle { Height = 100, Fill = new SolidColorBrush(Colors.Red) });
			pnl.Children.Add(new Border { Height = 20 });
			pnl.Children.Add(new TextBlock { Height = 100 });

			var container = new Border
			{
				Child = pnl,
				Width = 200,
				Height = 400,
				Background = new SolidColorBrush(Colors.Yellow)
			};

			var tcs = new TaskCompletionSource<Size>();

			pnl.SizeChanged += (snd, evt) => tcs.SetResult(evt.NewSize);

			TestServices.WindowHelper.WindowContent = container;

			await tcs.Task;

			await TestServices.WindowHelper.WaitForIdle();

			var availableSize = LayoutInformation.GetAvailableSize(pnl);
			var desiredSize = pnl.DesiredSize;
			var layoutSlot = LayoutInformation.GetLayoutSlot(pnl);
			var sizeChanged = await tcs.Task;
			var actualSize = new Size(pnl.ActualWidth, pnl.ActualHeight);
			var renderSize = pnl.RenderSize;

			availableSize.Should().Be(new Size(200, 400), because: nameof(availableSize));
			desiredSize.Should().Be(new Size(0, 222), because: nameof(desiredSize));
			layoutSlot.Should().Be(new Rect(0, 0, 200, 400), because: nameof(layoutSlot));
			sizeChanged.Should().Be(new Size(200, 400), because: nameof(sizeChanged));
			actualSize.Should().Be(new Size(200, 400), because: nameof(actualSize));
			renderSize.Should().Be(new Size(200, 400), because: nameof(renderSize));
		}

		[TestMethod]
		[RunsOnUIThread]

		public async Task When_AvailableSizeIsSmallerThanContent()
		{
			using var _ = new AssertionScope();

			var pnl = new StackPanel { Spacing = 1 };
			pnl.Children.Add(new Rectangle { Height = 100, Fill = new SolidColorBrush(Colors.Red) });
			pnl.Children.Add(new Border { Height = 50 });
			pnl.Children.Add(new TextBlock { Height = 100 });

			var container = new Border
			{
				Child = pnl,
				Width = 100,
				Height = 200,
				Background = new SolidColorBrush(Colors.Yellow)
			};

			var tcs = new TaskCompletionSource<Size>();

			pnl.SizeChanged += (snd, evt) => tcs.SetResult(evt.NewSize);

			TestServices.WindowHelper.WindowContent = container;

			await tcs.Task;

			await TestServices.WindowHelper.WaitForIdle();

			var availableSize = LayoutInformation.GetAvailableSize(pnl);
			var desiredSize = pnl.DesiredSize;
			var layoutSlot = LayoutInformation.GetLayoutSlot(pnl);
			var sizeChanged = await tcs.Task;
			var actualSize = new Size(pnl.ActualWidth, pnl.ActualHeight);
			var renderSize = pnl.RenderSize;

			availableSize.Should().Be(new Size(100, 200), because: nameof(availableSize));
			desiredSize.Should().Be(new Size(0, 200), because: nameof(desiredSize));
			layoutSlot.Should().Be(new Rect(0, 0, 100, 200), because: nameof(layoutSlot));
			sizeChanged.Should().Be(new Size(100, 252), because: nameof(sizeChanged));
			actualSize.Should().Be(new Size(100, 252), because: nameof(actualSize));
			renderSize.Should().Be(new Size(100, 252), because: nameof(renderSize));
			
		}
	}
}
