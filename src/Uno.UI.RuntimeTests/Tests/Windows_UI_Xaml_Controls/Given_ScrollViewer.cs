using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

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
				Content = new Border { Height = 200, Width = 50 }
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
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_ScrollViewer_Resized()
		{
			var content = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Background = new SolidColorBrush(Colors.Cyan)
			};

			var sut = new ScrollViewer { Content = content };

			var container = new Border { Child = sut };

			WindowHelper.WindowContent = container;

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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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

		[TestMethod]
#if __WASM__
		// Issue needs to be fixed first for WASM for Right and Bottom Margin missing
		// Details here: https://github.com/unoplatform/uno/issues/7000
		[Ignore]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_ScrollViewer_Centered_With_Margin_Inside_Tall_Rectangle()
		{
			const int ContentHeight = 300;
			const int ContentMargin = 10;
			var content = new Border
			{
				Width = 300,
				Height = ContentHeight,
				Margin = new Thickness(ContentMargin),
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var SUT = new ScrollViewer
			{
				Background = new SolidColorBrush(Colors.Pink),
				Width = 50,
				Height = double.NaN,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Content = content
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(content);

			const double ScrollViewerHeight = ContentHeight + 2 * ContentMargin;
			await WindowHelper.WaitForEqual(ScrollViewerHeight, () => SUT.ActualHeight);

			Assert.AreEqual(ScrollViewerHeight, SUT.ExtentHeight);
		}

		[TestMethod]
#if __WASM__
		// Issue needs to be fixed first for WASM for Right and Bottom Margin missing
		// Details here: https://github.com/unoplatform/uno/issues/7000
		[Ignore]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_ScrollViewer_Centered_With_Margin_Inside_Wide_Rectangle()
		{
			const int ContentWidth = 300;
			const int ContentMargin = 10;
			var content = new Border
			{
				Height = 300,
				Width = ContentWidth,
				Margin = new Thickness(ContentMargin),
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var SUT = new ScrollViewer
			{
				Background = new SolidColorBrush(Colors.Pink),
				Height = 50,
				Width = double.NaN,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Content = content
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(content);

			const double ScrollViewerWidth = ContentWidth + 2 * ContentMargin;
			await WindowHelper.WaitForEqual(ScrollViewerWidth, () => SUT.ActualWidth);

			Assert.AreEqual(ScrollViewerWidth, SUT.ExtentWidth);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_Direct_Content_BringIntoView()
		{
			var scrollViewer = new ScrollViewer()
			{
				Height = 200,
				Width = 200
			};
			var rectangle = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				Margin = new Thickness(0, 500, 0, 0),
				Width = 100,
				Height = 100
			};
			scrollViewer.Content = rectangle;
			WindowHelper.WindowContent = scrollViewer;
			await WindowHelper.WaitForLoaded(scrollViewer);
			bool viewChanged = false;
			scrollViewer.ViewChanged += (s, e) =>
			{
				viewChanged = true;
			};

			rectangle.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = false });

			await WindowHelper.WaitFor(() => viewChanged);

			Assert.AreEqual(400, scrollViewer.VerticalOffset, 5);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Nested_Scroll_BringIntoView()
		{
			var outerScrollViewer = new ScrollViewer()
			{
				Background = new SolidColorBrush(Colors.Yellow),
				Height = 300,
				Width = 200,
				Padding = new Thickness(20, 20, 20, 20)
			};

			var innerScrollViewer = new ScrollViewer()
			{
				Background = new SolidColorBrush(Colors.Blue),
				Height = 200,
				Margin = new Thickness(0, 400, 0, 0),
				Padding = new Thickness(20, 20, 20, 20)
			};

			var item = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				Margin = new Thickness(0, 600, 0, 0),
				Width = 100,
				Height = 100
			};

			innerScrollViewer.Content = item;
			outerScrollViewer.Content = innerScrollViewer;
			WindowHelper.WindowContent = outerScrollViewer;
			await WindowHelper.WaitForLoaded(outerScrollViewer);

			bool outerViewChanged = false;

			item.BringIntoViewRequested += (s, e) =>
			{
				Assert.AreEqual(false, e.AnimationDesired);
				Assert.AreEqual(false, e.Handled);
				Assert.IsTrue(double.IsNaN(e.HorizontalAlignmentRatio));
				Assert.IsTrue(double.IsNaN(e.VerticalAlignmentRatio));
				Assert.AreEqual(0, e.HorizontalOffset);
				Assert.AreEqual(0, e.VerticalOffset);
				Assert.AreEqual(item, e.OriginalSource);
				Assert.AreEqual(item, e.TargetElement);
				Assert.AreEqual(new Rect(0, 0, 100, 100), e.TargetRect);
			};

			innerScrollViewer.BringIntoViewRequested += (s, e) =>
			{
				Assert.AreEqual(false, e.AnimationDesired);
				Assert.AreEqual(false, e.Handled);
				Assert.IsTrue(double.IsNaN(e.HorizontalAlignmentRatio));
				Assert.IsTrue(double.IsNaN(e.VerticalAlignmentRatio));
				Assert.AreEqual(0, e.HorizontalOffset);
				Assert.AreEqual(0, e.VerticalOffset);
#if HAS_UNO // These values differ slightly from ScrollViewer's due to the fact that our implementation is based on the newer ScrollView control
				Assert.AreEqual(item, e.OriginalSource);
				Assert.AreEqual(innerScrollViewer, e.TargetElement);
				Assert.AreEqual(new Rect(0, 60, 100, 100), e.TargetRect);
#endif
			};

			outerScrollViewer.BringIntoViewRequested += (s, e) =>
			{
				Assert.AreEqual(false, e.AnimationDesired);
				Assert.AreEqual(false, e.Handled);
				Assert.IsTrue(double.IsNaN(e.HorizontalAlignmentRatio));
				Assert.IsTrue(double.IsNaN(e.VerticalAlignmentRatio));
				Assert.AreEqual(0, e.HorizontalOffset);
				Assert.AreEqual(0, e.VerticalOffset);
#if HAS_UNO // These values differ slightly from ScrollViewer's due to the fact that our implementation is based on the newer ScrollView control
				Assert.AreEqual(item, e.OriginalSource);
				Assert.AreEqual(outerScrollViewer, e.TargetElement);
				Assert.AreEqual(new Rect(20, 160, 100, 100), e.TargetRect);
#endif
			};

			outerScrollViewer.ViewChanged += (s, e) =>
			{
				outerViewChanged = true;
			};

			item.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = false });

			await WindowHelper.WaitFor(() => outerViewChanged);

			Assert.AreEqual(0, innerScrollViewer.HorizontalOffset);
			Assert.AreEqual(540, innerScrollViewer.VerticalOffset);
			Assert.AreEqual(0, outerScrollViewer.HorizontalOffset);
			Assert.AreEqual(320, outerScrollViewer.VerticalOffset);
		}
	}
}
