using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using static Private.Infrastructure.TestServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ScrollViewer
	{
		private ResourceDictionary _testsResources;

		public Style ScrollViewerCrowdedTemplateStyle => _testsResources["ScrollViewerCrowdedTemplateStyle"] as Style;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

#if __SKIA__ || __WASM__
		[TestMethod]
		public async Task When_CreateVerticalScroller_Then_DoNotLoadAllTemplate()
		{
			var sut = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollMode = ScrollMode.Disabled,
				Height = 100,
				Width = 100,
				Content = new Border {Height = 200, Width = 50}
			};
			WindowHelper.WindowContent = sut;

			await WindowHelper.WaitForIdle();

			var buttons = sut
				.EnumerateAllChildren(maxDepth: 256)
				.OfType<RepeatButton>()
				.Count();

			Assert.IsTrue(buttons > 0); // We make sure that we really loaded the right template
			Assert.IsTrue(buttons <= 4);
		}


		[TestMethod]
		public async Task When_NonScrollableScroller_Then_DoNotLoadAllTemplate()
		{
			var sut = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollMode = ScrollMode.Disabled,
				Height = 100,
				Width = 100,
				Content = new Border { Height = 50, Width = 50 }
			};
			WindowHelper.WindowContent = sut;

			await WindowHelper.WaitForIdle();

			var buttons = sut
				.EnumerateAllChildren(maxDepth: 256)
				.OfType<RepeatButton>()
				.Count();

			Assert.IsTrue(buttons == 0);
		}

		[TestMethod]
		public async Task When_HorizontallyScrollableTextBox_Then_DoNotLoadAllScrollerTemplate()
		{
			var sut = new TextBox
			{
				Width = 100,
				Text = "Hello world, this a long text that would cause the TextBox to enable horizontal scroll, so we should find some RepeatButton in the children of this TextBox."
			};
			WindowHelper.WindowContent = sut;

			await WindowHelper.WaitForIdle();

			var bars = sut
				.EnumerateAllChildren(maxDepth: 256)
				.OfType<ScrollBar>()
				.Count();

			Assert.IsTrue(bars == 0); // TextBox is actually not using scrollbars!
		}

		[TestMethod]
		public async Task When_NonScrollableTextBox_Then_DoNotLoadAllScrollerTemplate()
		{
			var sut = new TextBox
			{
				Width = 100,
				Text = "42"
			};
			WindowHelper.WindowContent = sut;

			await WindowHelper.WaitForIdle();

			var bars = sut
				.EnumerateAllChildren(maxDepth: 256)
				.OfType<ScrollBar>()
				.Count();

			Assert.IsTrue(bars == 0); // TextBox is actually not using scrollbars!
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ScrollViewer_Resized()
		{
			var content = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Background = new SolidColorBrush(Colors.Cyan)
			};

			var sut = new ScrollViewer {Content = content};

			var container = new Border {Child = sut};

			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForLoaded(content);

			using var _ = new AssertionScope();

			await CheckForSize(200, 400, "Initial");
			await CheckForSize(250, 450, "Growing 1st time");
			await CheckForSize(225, 425, "Shringking 1st time");
			await CheckForSize(16, 16, "Super-shrinking");
			await CheckForSize(200, 400, "Back Original");

			async Task CheckForSize(int width, int height, string name)
			{
				container.Width = width;
				container.Height = height;

				await WindowHelper.WaitForLoaded(content);

				await WindowHelper.WaitForIdle();

				using var _ = new AssertionScope($"{name} [{width}x{height}]");

#if !NETFX_CORE
				sut.ViewportMeasureSize.Width.Should().Be(width, "ViewportMeasureSize.Width");
				sut.ViewportMeasureSize.Height.Should().Be(height, "ViewportMeasureSize.Height");

				sut.ViewportArrangeSize.Width.Should().Be(width, "ViewportArrangeSize.Width");
				sut.ViewportArrangeSize.Height.Should().Be(height, "ViewportArrangeSize.Height");
#endif

				sut.ExtentWidth.Should().Be(width, "Extent");
				sut.ExtentHeight.Should().Be(height, "Extent");

				sut.ActualWidth.Should().Be(width, "ScrollViewer ActualWidth");
				sut.ActualHeight.Should().Be(height, "ScrollViewer ActualHeight");
				sut.RenderSize.Width.Should().Be(width, "ScrollViewer RenderSize.Width");
				sut.RenderSize.Height.Should().Be(height, "ScrollViewer RenderSize.Height");

				content.ActualWidth.Should().Be(width, "content ActualWidth");
				content.ActualHeight.Should().Be(height, "content ActualHeight");
				content.RenderSize.Width.Should().Be(width, "content RenderSize.Width");
				content.RenderSize.Height.Should().Be(height, "content RenderSize.Height");
			}
		}

		[TestMethod]
		public async Task When_Presenter_Doesnt_Take_Up_All_Space()
		{
			const int ContentWidth = 700;
			var content = new Ellipse
			{
				Width = ContentWidth,
				VerticalAlignment = VerticalAlignment.Stretch,
				Fill = new SolidColorBrush(Colors.Tomato)
			};
			const double ScrollViewerWidth = 300;
			var SUT = new ScrollViewer
			{
				Style = ScrollViewerCrowdedTemplateStyle,
				Width = ScrollViewerWidth,
				Height = 200,
				Content = content
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(content);

			const double ButtonWidth = 29;
			const double PresenterActualWidth = ScrollViewerWidth - 2 * ButtonWidth;
			await WindowHelper.WaitForEqual(ScrollViewerWidth, () => SUT.ActualWidth);
			Assert.AreEqual(PresenterActualWidth, SUT.ViewportWidth);
			Assert.AreEqual(ContentWidth, SUT.ExtentWidth);
			Assert.AreEqual(ContentWidth - PresenterActualWidth, SUT.ScrollableWidth);
			;
		}
	}
}
