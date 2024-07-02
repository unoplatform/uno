using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Input;
using MUXControlsTestApp.Utilities;
using static Private.Infrastructure.TestServices;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.Extensions;

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

#if !WINAPPSDK
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

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
		[TestMethod]
		[DataRow(175, 175, 26, 26)]
		// [DataRow(1, 0, 2, 2)] // https://github.com/unoplatform/uno/issues/13907
		[DataRow(1, 150, 2, 22)]
		[DataRow(16, 17, 2, 3)]
		[DataRow(123, 456, 18, 68)]
		[DataRow(96, 97, 14, 15)]
		[DataRow(393, 277, 59, 42)]
		public async Task When_ArrowKeys_Pressed(int width, int height, int horizontalDelta, int verticalDelta)
		{
			var border = new Border
			{
				Width = width,
				Height = height,
				Child = new ScrollViewer
				{
					VerticalScrollMode = ScrollMode.Enabled,
					HorizontalScrollMode = ScrollMode.Enabled,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
					Content = new Border
					{
						// anything large enough to make it scrollable
						Width = 2000,
						Height = 2000,
						Child = new ItemsControl() // any focusable element
					}
				}
			};

			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			border.FindVisualChildByType<ItemsControl>().Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var SUT = border.FindVisualChildByType<ScrollViewer>();

			KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
			KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
			KeyboardHelper.Right();
			await WindowHelper.WaitForIdle();
			KeyboardHelper.Right();
			await WindowHelper.WaitForIdle();

			// Horizontal and vertical scrolling amounts should be independent, and each depend on the corresponding ActualSize dimension
			Assert.AreEqual(verticalDelta * 2, SUT.VerticalOffset);
			Assert.AreEqual(horizontalDelta * 2, SUT.HorizontalOffset);

			KeyboardHelper.Up();
			await WindowHelper.WaitForIdle();
			KeyboardHelper.Left();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(verticalDelta, SUT.VerticalOffset);
			Assert.AreEqual(horizontalDelta, SUT.HorizontalOffset);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task When_ScrollViewer_Pressed()
		{
			var border = new Border
			{
				Width = 175,
				Height = 175,
				Child = new ScrollViewer
				{
					VerticalScrollMode = ScrollMode.Enabled,
					HorizontalScrollMode = ScrollMode.Enabled,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
					Content = new Border
					{
						// anything large enough to make it scrollable
						Width = 2000,
						Height = 2000,
						Child = new ItemsControl() // any focusable element
					}
				}
			};

			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(border.GetAbsoluteBounds().GetCenter());
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(border.FindVisualChildByType<ItemsControl>(), FocusManager.GetFocusedElement(border.XamlRoot));
		}

		[TestMethod]
		public async Task When_Home_End_PageDown_PageUp()
		{
			var border = new Border
			{
				Width = 175,
				Height = 175,
				Child = new ScrollViewer
				{
					VerticalScrollMode = ScrollMode.Enabled,
					HorizontalScrollMode = ScrollMode.Enabled,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
					Content = new Border
					{
						// anything large enough to make it scrollable
						Width = 2000,
						Height = 2000,
						Child = new ItemsControl() // any focusable element
					}
				}
			};

			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			border.FindVisualChildByType<ItemsControl>().Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var SUT = border.FindVisualChildByType<ScrollViewer>();

			KeyboardHelper.PageDown();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(175, SUT.VerticalOffset);
			Assert.AreEqual(0, SUT.HorizontalOffset);

			KeyboardHelper.PageDown();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(350, SUT.VerticalOffset);
			Assert.AreEqual(0, SUT.HorizontalOffset);

			KeyboardHelper.PressKeySequence("$d$_pageup#$u$_pageup");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(175, SUT.VerticalOffset);
			Assert.AreEqual(0, SUT.HorizontalOffset);

			KeyboardHelper.PressKeySequence("$d$_home#$u$_home");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.VerticalOffset);
			Assert.AreEqual(0, SUT.HorizontalOffset);

			KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1825, SUT.VerticalOffset);
			Assert.AreEqual(0, SUT.HorizontalOffset);
		}

		[TestMethod]
		public async Task When_Args_Handled_Home_End_PageDown_PageUp()
		{
			var SUT = new ScrollViewer
			{
				VerticalScrollMode = ScrollMode.Enabled,
				HorizontalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
				Content = new Border
				{
					// anything large enough to make it scrollable
					Width = 2000,
					Height = 2000,
					Child = new ItemsControl() // any focusable element
				}
			};

			var keyDownCount = 0;
			SUT.KeyDown += (_, _) => keyDownCount++;

			var border = new Border
			{
				Width = 175,
				Height = 175,
				Child = SUT
			};

			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			border.FindVisualChildByType<ItemsControl>().Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			KeyboardHelper.PressKeySequence("$d$_pageup#$u$_pageup");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, keyDownCount);

			KeyboardHelper.PageDown();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, keyDownCount);

			KeyboardHelper.PressKeySequence("$d$_pageup#$u$_pageup");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, keyDownCount);

			KeyboardHelper.PressKeySequence("$d$_home#$u$_home");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, keyDownCount);

			KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, keyDownCount);

			KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			KeyboardHelper.PageDown();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, keyDownCount);

			KeyboardHelper.PressKeySequence("$d$_home#$u$_home");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, keyDownCount);
		}

		[TestMethod]
		public async Task When_Args_Handled_ArrowKeys()
		{
			var SUT = new ScrollViewer
			{
				VerticalScrollMode = ScrollMode.Enabled,
				HorizontalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
				Content = new Border
				{
					// anything large enough to make it scrollable
					Width = 2000,
					Height = 2000,
					Child = new ItemsControl() // any focusable element
				}
			};

			var keyDownCount = 0;
			SUT.KeyDown += (_, _) => keyDownCount++;

			var border = new Border
			{
				Width = 175,
				Height = 175,
				Child = SUT
			};

			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			border.FindVisualChildByType<ItemsControl>().Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			KeyboardHelper.Left();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, keyDownCount);

			KeyboardHelper.Up();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			KeyboardHelper.Up();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			KeyboardHelper.Right();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			KeyboardHelper.Left();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			SUT.FindVisualChildByType<ScrollContentPresenter>().SetHorizontalOffset(999999);
			SUT.FindVisualChildByType<ScrollContentPresenter>().SetVerticalOffset(999999);

			KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			KeyboardHelper.Right();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, keyDownCount);
		}
#endif

		[TestMethod]
		public async Task When_Scrolled_ViewportSizeLargerThanContent()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive();
			}

			var SUT = new ScrollViewer
			{
				Height = 300,
				Width = 450,
				Content = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Children =
					{
						new TextBlock
						{
							Text = "Hello, Uno platform!",
							FontSize = 40
						}
					}
				}
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var before = await UITestHelper.ScreenShot(SUT, true);

			SUT.ChangeView(5, null, null);
			await WindowHelper.WaitForIdle();

			var after = await UITestHelper.ScreenShot(SUT, true);

			await ImageAssert.AreEqualAsync(before, after);
		}

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task When_WheelChanged_OnlyHorizontallyScrollable()
		{
			var SUT = new ScrollViewer
			{
				Height = 300,
				Width = 400,
				HorizontalScrollMode = ScrollMode.Enabled,
				VerticalScrollMode = ScrollMode.Disabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
				Content = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Children =
					{
						new TextBlock
						{
							Text = "Hello, Uno platform!",
							FontSize = 40
						}
					}
				}
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var scp = SUT.FindVisualChildByType<ScrollContentPresenter>();
			scp.HorizontalOffset.Should().Be(0);

			mouse.MoveTo(scp.GetAbsoluteBounds().GetCenter());
			mouse.WheelDown();

			await WindowHelper.WaitForIdle();

			scp.HorizontalOffset.Should().Be(0);
			scp.VerticalOffset.Should().Be(0);

			mouse.WheelUp();
			await WindowHelper.WaitForIdle();

			scp.HorizontalOffset.Should().Be(0);
			scp.VerticalOffset.Should().Be(0);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task When_Nested_ScrollViewers_WheelChanged()
		{
			var inner = new ScrollViewer
			{
				Height = 20,
				Content = new Button
				{
					Height = 100,
					Background = new SolidColorBrush(Colors.Green)
				},
			};

			var outer = new ScrollViewer
			{
				Height = 150,
				Content = new StackPanel
				{
					Children =
					{
						new Rectangle
						{
							Fill = new SolidColorBrush(Colors.Red),
							Height = 100,
							Width = 200
						},
						inner,
						new Rectangle
						{
							Fill = new SolidColorBrush(Colors.Blue),
							Height = 100,
							Width = 200
						}
					}
				}
			};

			WindowHelper.WindowContent = outer;

			await WindowHelper.WaitForLoaded(outer);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(inner.GetAbsoluteBounds().GetCenter());
			mouse.Wheel(-50, steps: 5);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, outer.VerticalOffset);
			Assert.IsTrue(inner.VerticalOffset > 0);

			mouse.Wheel(-500, steps: 5);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(outer.VerticalOffset > outer.ScrollableHeight / 2);
			Assert.AreEqual(inner.ScrollableHeight, inner.VerticalOffset);
		}
#endif


		[TestMethod]
		[RunsOnUIThread]
#if NETFX_CORE
		[Ignore("KeyboardHelper doesn't work on Windows")]
#endif
		public async Task When_Space_Already_Handled()
		{
			var lv = new ListView
			{
				ItemsSource = "012345"
			};

			var SUT = new ScrollViewer
			{
				Height = 50,
				Content = lv
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var lvi1 = (ListViewItem)lv.ContainerFromIndex(0);
			lvi1.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// This test is targeted at WASM, but this doesn't simulate a real space (like e.g. puppeteer would),
			// so the test is not really validating much, merely documenting behaviour
			KeyboardHelper.Space();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.VerticalOffset);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task When_Pointer_Clicks_Inside_ScrollViewer()
		{
			var ts = new ToggleSwitch();
			var tb = new TextBox { Width = 300 };
			var SUT = new ScrollViewer
			{
				Content = new StackPanel
				{
					Children =
					{
						tb,
						ts
					}
				}
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.Press(ts.FindVisualChildByType<TextBlock>().GetAbsoluteBounds().GetCenter());
			mouse.Release();
			await WindowHelper.WaitForIdle();

			// The ScrollViewer should NOT grab focus here.
			Assert.AreEqual(ts, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));

			mouse.Press(tb.GetAbsoluteBounds().GetCenter() + new Point(0, 20)); // click somewhere empty inside the ScrollViewer
			mouse.Release();
			await WindowHelper.WaitForIdle();

			// This time, since no element inside handled PointerPressed, ScrollViewer should focus the first element inside.
			Assert.AreEqual(tb, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		}
#endif

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


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NonRound_Content_Height()
		{
			var outerScrollViewer = new ScrollViewer()
			{
				Background = new SolidColorBrush(Colors.Yellow)
			};

			var content = new TextBlock()
			{
				Text = "Hello",
				FontSize = 26.756,
				UseLayoutRounding = false
			};

			outerScrollViewer.Content = content;

			WindowHelper.WindowContent = outerScrollViewer;
			await WindowHelper.WaitForLoaded(outerScrollViewer);
			Assert.AreEqual(outerScrollViewer.ExtentHeight, outerScrollViewer.ViewportHeight, 0.000001);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_ChangeView_Offset()
		{
			const double offset = 100;

			var scroll = new ScrollViewer()
			{
				Background = new SolidColorBrush(Colors.Yellow)
			};

			var stackPanel = new StackPanel()
			{
				Orientation = Orientation.Vertical,
				Height = 5000,
			};

			stackPanel.Children.Add(new Border() { Height = 200 });

			var target = new Border()
			{
				Background = new SolidColorBrush(Colors.Lime),
				Height = 50,
			};

			stackPanel.Children.Add(target);

			var scrollChanged = false;
			scroll.ViewChanged += (s, e) =>
			{
				scrollChanged = true;
			};

			scroll.Content = stackPanel;

			try
			{

				var container = new Border() { Child = scroll };

				WindowHelper.WindowContent = container;

				await WindowHelper.WaitForLoaded(scroll);

				_ = scroll.ChangeView(null, offset, null, true);

				await WindowHelper.WaitFor(() => scrollChanged);

				var loc = target.TransformToVisual(scroll).TransformPoint(new Point(0, 0));
				Assert.AreEqual(offset, loc.Y);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}


#if __ANDROID__
		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_NonNested_BringIntoView()
		{
			if (ContextHelper.Current is not Android.App.Activity activity)
			{
				Assert.Inconclusive("The current android activity is not accessible.");
				return;
			}

			// set AdjustNothing mode and prepare for cleanup
			var oldMode = activity.Window.Attributes.SoftInputMode;
			activity.Window.SetSoftInputMode(oldMode & ~Android.Views.SoftInput.MaskAdjust | Android.Views.SoftInput.AdjustNothing);
			using var cleanup = Disposable.Create(() => activity.Window.SetSoftInputMode(oldMode));

			// load a tmp textbox to ...
			var tmpTextbox = new TextBox();
			WindowHelper.WindowContent = tmpTextbox;
			await WindowHelper.WaitForLoaded(tmpTextbox);
			await WindowHelper.WaitForIdle();
			tmpTextbox.Focus(FocusState.Programmatic);

			// ... measure keyboard height
			var kb = InputPane.GetForCurrentView();
			await WindowHelper.WaitFor(() => kb.Visible, message: "Failed to summon keyboard via Focus(FocusState.Programmatic).");
			await WindowHelper.WaitFor(() => kb.OccludedRect.Height > 0, message: "Failed to summon keyboard via Focus(FocusState.Programmatic).");
			var kbHeight = kb.OccludedRect.Height;
			kb.Visible = false;

			// load actual test setup
			// ScrollViewer's viewport is set to be 50px taller than the keyboard.
			// There is a 200px tall filler Rectangle above the TextBox, guaranteeing the latter will be hidden behind keyboard.
			var SUT = new TextBox();
			var panel = new StackPanel()
			{
				Spacing = 5,
				Children =
				{
					new Windows.UI.Xaml.Shapes.Rectangle() { Height = 200, Fill = SolidColorBrushHelper.SkyBlue },
					SUT,
				},
			};
			var sv = new ScrollViewer()
			{
				Height = kbHeight + 50,
				Content = panel,
				VerticalAlignment = VerticalAlignment.Bottom
			};
			var container = new Border() { Child = sv };

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			await WindowHelper.WaitForIdle();

			// when the TextBox is focused, we expect BringIntoView to push the TextBox above the keyboard
			// note: This test can be flaky, as it would randomly close the keyboard right after opening it.
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitFor(() => kb.Visible, message: "Failed to summon keyboard via Focus(FocusState.Programmatic).");
			Assert.IsTrue(SUT.ActualHeight < 50, $"TextBox should be no taller than 50px. (ActualHeight = {SUT.ActualHeight})");

			var minOffset = 200 - (50 - SUT.ActualHeight); // tbox sticks to the top of viewport
			var maxOffset = 205; // tbox sticks to the bottom of viewport
			await WindowHelper.WaitFor<double>(
				() => sv.VerticalOffset,
				default, // unused, since are we doing between comparison
				value => $"Failed to make keyboard appear above keyboard. (sv.VOffset = {value})",
				comparer: (value, _) => minOffset <= value && value <= maxOffset);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_DoublyNested_BringIntoView()
		{
			// note: Compared to When_NonNested_BringIntoView, we are using 2 SVs here, one nesting another.
			// Other than that, the expected result should still be the same, as in the outer SV should be the one
			// being padded and scrolled by the same amount, AND NOT the inner one.
			if (ContextHelper.Current is not Android.App.Activity activity)
			{
				Assert.Inconclusive("The current android activity is not accessible.");
				return;
			}

			// set AdjustNothing mode and prepare for cleanup
			var oldMode = activity.Window.Attributes.SoftInputMode;
			activity.Window.SetSoftInputMode(oldMode & ~Android.Views.SoftInput.MaskAdjust | Android.Views.SoftInput.AdjustNothing);
			using var cleanup = Disposable.Create(() => activity.Window.SetSoftInputMode(oldMode));

			// load a tmp textbox to ...
			var tmpTextbox = new TextBox();
			WindowHelper.WindowContent = tmpTextbox;
			await WindowHelper.WaitForLoaded(tmpTextbox);
			await WindowHelper.WaitForIdle();
			tmpTextbox.Focus(FocusState.Programmatic);

			// ... measure keyboard height
			var kb = InputPane.GetForCurrentView();
			await WindowHelper.WaitFor(() => kb.Visible, message: "Failed to summon keyboard via Focus(FocusState.Programmatic).");
			await WindowHelper.WaitFor(() => kb.OccludedRect.Height > 0, message: "Failed to summon keyboard via Focus(FocusState.Programmatic).");
			var kbHeight = kb.OccludedRect.Height;
			kb.Visible = false;

			// load actual test setup
			// ScrollViewer's viewport is set to be 50px taller than the keyboard.
			// There is a 200px tall filler Rectangle above the TextBox, guaranteeing the latter will be hidden behind keyboard.
			var SUT = new TextBox();
			var panel = new StackPanel()
			{
				Spacing = 5,
				Children =
				{
					new Windows.UI.Xaml.Shapes.Rectangle() { Height = 200, Fill = SolidColorBrushHelper.SkyBlue },
					SUT,
				},
			};
			var innerSV = new ScrollViewer() { Content = panel };
			var outerSV = new ScrollViewer()
			{
				Height = kbHeight + 50,
				Content = innerSV,
				VerticalAlignment = VerticalAlignment.Bottom
			};
			var container = new Border() { Child = outerSV };

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			await WindowHelper.WaitForIdle();

			// when the TextBox is focused, we expect BringIntoView to push the TextBox above the keyboard
			// note: This test can be flaky, as it would randomly close the keyboard right after opening it.
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitFor(() => kb.Visible, message: "Failed to summon keyboard via Focus(FocusState.Programmatic).");
			Assert.IsTrue(SUT.ActualHeight < 50, $"TextBox should be no taller than 50px. (ActualHeight = {SUT.ActualHeight})");

			var minOffset = 200 - (50 - SUT.ActualHeight); // tbox sticks to the top of viewport
			var maxOffset = 205; // tbox sticks to the bottom of viewport
			await WindowHelper.WaitFor<double>(
				() => outerSV.VerticalOffset,
				default, // unused, since are we doing between comparison
				value => $"Failed to make keyboard appear above keyboard. (sv.VOffset = {value})",
				comparer: (value, _) => minOffset <= value && value <= maxOffset);
		}

#endif

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("Pointer injection supported only on skia for now.")]
#endif
		public async Task When_TouchScroll_Then_NestedElementReceivePointerEvents()
		{
			var nested = new Border
			{
				Height = 4192,
				Width = 256,
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var sut = new ScrollViewer
			{
				Height = 512,
				Width = 256,
				Content = nested
			};

			var events = new List<string>();
			nested.PointerEntered += (snd, e) => events.Add("enter");
			nested.PointerExited += (snd, e) => events.Add("exited");
			nested.PointerPressed += (snd, e) => events.Add("pressed");
			nested.PointerReleased += (snd, e) => events.Add("release");
			nested.PointerCaptureLost += (snd, e) => events.Add("capturelost");
			nested.PointerCanceled += (snd, e) => events.Add("cancel");

			WindowHelper.WindowContent = new Grid { Children = { sut } };
			await WindowHelper.WaitForLoaded(nested);
			await WindowHelper.WaitForIdle();

			var input = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available on this platform.");
			using var finger = input.GetFinger();

			var sutLocation = sut.GetAbsoluteBounds().GetLocation();
			finger.Drag(sutLocation.Offset(5, 480), sutLocation.Offset(5, 5));

			// The expected proper sequence when all sub-elements are ManipulationMode=System should be "Enter", "Pressed", "PointerCaptureLost", "Exited"
			// This is caused by the Windows' Direct Manipulation.
			// The important thing is to not get Released as it can cause a click on the nested element while it's being scrolled.
			events.Should().BeEquivalentTo("enter", "pressed", "exited");
		}

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("Pointer injection supported only on skia for now.")]
#endif
		public async Task When_TouchTap_Then_NestedElementReceivePointerEvents()
		{
			var nested = new Border
			{
				Height = 4192,
				Width = 256,
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var sut = new ScrollViewer
			{
				Height = 512,
				Width = 256,
				Content = nested
			};

			var events = new List<string>();
			nested.PointerEntered += (snd, e) => events.Add("enter");
			nested.PointerExited += (snd, e) => events.Add("exited");
			nested.PointerPressed += (snd, e) => events.Add("pressed");
			nested.PointerReleased += (snd, e) => events.Add("release");
			nested.PointerCaptureLost += (snd, e) => events.Add("capturelost");
			nested.PointerCanceled += (snd, e) => events.Add("cancel");

			WindowHelper.WindowContent = new Grid { Children = { sut } };
			await WindowHelper.WaitForLoaded(nested);
			await WindowHelper.WaitForIdle();

			var input = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available on this platform.");
			using var finger = input.GetFinger();

			var sutLocation = sut.GetAbsoluteBounds().GetLocation();
			finger.Press(sutLocation.Offset(5, 5));
			finger.Release();

			events.Should().BeEquivalentTo("enter", "pressed", "release", "exited");
		}

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("Pointer injection supported only on skia for now.")]
#endif
		public async Task When_ReversedMouseWheel_Then_ScrollInReversedDirection()
		{
#if WINAPPSDK
			Assert.Inconclusive("Mouse pointer helper not supported on UWP.");
#else
			var sut = new ScrollViewer
			{
				Height = 512,
				Width = 256,
				Content = new Border
				{
					Height = 4192,
					Width = 256,
					Background = new SolidColorBrush(Colors.DeepPink)
				}
			};

			var sutBounds = await UITestHelper.Load(sut);

			Uno.UI.Xaml.Controls.ScrollContentPresenter.SetIsPointerWheelReversed(sut.Presenter, isReversed: true);
			await WindowHelper.WaitForIdle();

			var input = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available on this platform.");
			using var mouse = input.GetMouse();

			sut.VerticalOffset.Should().Be(0);

			mouse.MoveTo(sutBounds.GetCenter());
			mouse.WheelDown();

			sut.VerticalOffset.Should().Be(0);

			mouse.WheelUp();

			sut.VerticalOffset.Should().BeGreaterThan(0);

			mouse.WheelDown();

			sut.VerticalOffset.Should().Be(0);
#endif
		}

		[TestMethod]
#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
		[Ignore("We're only testing managed scrollers.")]
#endif
		public async Task When_SizeChanged_Offsets_Adjusted()
		{
			Rectangle rect;
			var SUT = new ScrollViewer
			{
				Height = 100,
				Content = rect = new Rectangle
				{
					Fill = new LinearGradientBrush
					{
						StartPoint = new Point(0.5, 0),
						EndPoint = new Point(0.5, 1),
						GradientStops = new GradientStopCollection()
							.Apply(stops => stops.AddRange(new[]
							{
								new GradientStop { Color = Windows.UI.Colors.Yellow, Offset = 0.0 },
								new GradientStop { Color = Windows.UI.Colors.Red, Offset = 0.25 },
								new GradientStop { Color = Windows.UI.Colors.Blue, Offset = 0.75 },
								new GradientStop { Color = Windows.UI.Colors.LimeGreen, Offset = 1.0 },
							}))
					},
					Height = 200,
					Width = 100
				}
			};

			await UITestHelper.Load(SUT);

			SUT.ScrollToVerticalOffset(50);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Point(0, -50), rect.TransformToVisual(SUT).TransformPoint(new Point(0, 0)));

			SUT.Height = 200;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Point(0, 0), rect.TransformToVisual(SUT).TransformPoint(new Point(0, 0)));
		}

#if HAS_UNO // uses internal ToMatrix
		[TestMethod]
#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
		[Ignore("We're only testing managed scrollers.")]
#endif
		public async Task When_SCP_TransformToVisual()
		{
			var SUT = new ScrollViewer
			{
				Height = 512,
				Width = 256,
				Content = new Border
				{
					Height = 4192,
					Width = 256,
					Background = new SolidColorBrush(Colors.DeepPink)
				}
			};

			await UITestHelper.Load(SUT);

			var scp = SUT.FindVisualChildByType<ScrollContentPresenter>();
			var ttv = ((MatrixTransform)scp.TransformToVisual(null)).ToMatrix(Point.Zero);
			var childTtvMatrix = ((MatrixTransform)((UIElement)scp.Content).TransformToVisual(null)).ToMatrix(Point.Zero);

			SUT.ScrollToVerticalOffset(100);
			await WindowHelper.WaitForIdle();

			// The content inside the SCP should move, not the SCP itself
#if __SKIA__ || __WASM__
			// "Only skia uses Visuals for TransformToVisual. The visual-less implementation adjusts the offset on the SCP itself instead of the child."
			Assert.AreEqual(ttv, ((MatrixTransform)scp.TransformToVisual(null)).ToMatrix(Point.Zero));
#else
			Assert.AreEqual(ttv * new Matrix3x2(1, 0, 0, 1, 0, -100), ((MatrixTransform)scp.TransformToVisual(null)).ToMatrix(Point.Zero));
#endif

#if __WASM__ // incorrect
			Assert.AreEqual(childTtvMatrix, ((MatrixTransform)((UIElement)scp.Content).TransformToVisual(null)).ToMatrix(Point.Zero));
#else
			Assert.AreEqual(childTtvMatrix * new Matrix3x2(1, 0, 0, 1, 0, -100), ((MatrixTransform)((UIElement)scp.Content).TransformToVisual(null)).ToMatrix(Point.Zero));
#endif
		}
#endif
	}
}
