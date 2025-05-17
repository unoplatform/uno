using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives.PopupPages;
using static Private.Infrastructure.TestServices.WindowHelper;
using Combinatorial.MSTest;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_LayoutInformation
	{
		[TestMethod]
		[CombinatorialData]
		public async Task When_PaddedElement_Then_LayoutSlot(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin)
		{
			Border sut;
			var root = new Border
			{
				Width = 200,
				Height = 200,
				BorderThickness = new Thickness(0),
				Child = sut = new Border
				{
					Margin = new Thickness(margin),
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(border),
					Width = 100,
					Height = 100,
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var slot = LayoutInformation.GetLayoutSlot(sut);

			slot.Should().Be(new Rect(0, 0, 200, 200));
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_ElementInPaddedElement_Then_LayoutSlot(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin)
		{
#if __WASM__
			if (border > 0)
			{
				Assert.Inconclusive("Border are not included in LayoutSlot on wasm https://github.com/unoplatform/uno/issues/6999");
				return;
			}
#endif

			Border sut;
			var root = new Border
			{
				Width = 200,
				Height = 200,
				Margin = new Thickness(margin),
				BorderBrush = new SolidColorBrush(Colors.Red),
				BorderThickness = new Thickness(border),
				Child = sut = new Border
				{
					BorderThickness = new Thickness(0),
					Width = 100,
					Height = 100,
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var slot = LayoutInformation.GetLayoutSlot(sut);

			slot.Should().Be(new Rect(border, border, root.Width - border * 2, root.Height - border * 2));
		}

		[TestMethod]
		[CombinatorialData]
#if __SKIA__
		[Ignore("Fails on CI for unkown reason (wokrs locally).")]
#endif
		[RequiresFullWindow] // the test fails if the available size for window content isn't wide enough
		public async Task When_ElementInPaddedGrid_Then_LayoutSlot(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin)
		{
#if __WASM__
			if (border > 0)
			{
				Assert.Inconclusive("Border are not included in LayoutSlot on wasm https://github.com/unoplatform/uno/issues/6999");
				return;
			}
#endif

			Border sut;
			var root = new Grid
			{
				ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
				RowDefinitions = { new RowDefinition(), new RowDefinition() },
				Width = 400,
				Height = 400,
				Margin = new Thickness(margin),
				BorderBrush = new SolidColorBrush(Colors.Red),
				BorderThickness = new Thickness(border),
				Children =
				{
					(sut = new Border
					{
						BorderThickness = new Thickness(0),
						Width = 100,
						Height = 100,
						HorizontalAlignment = hAlign,
						VerticalAlignment = vAlign
					}).Apply(s =>
					{
						Grid.SetColumn(s, 1);
						Grid.SetRow(s, 1);
					})
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var slot = LayoutInformation.GetLayoutSlot(sut);

			slot.Should().Be(new Rect(root.Width / 2, root.Height / 2, root.Width / 2 - border, root.Height / 2 - border));
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_ElementInPaddedScrollViewerAligned_Then_LayoutSlot(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin,
			bool canHorizontallyScroll,
			bool canVerticallyScroll)
		{
			Border sut;
			var root = new ScrollViewer
			{
				Width = 200,
				Height = 200,
				Margin = new Thickness(margin),
				BorderBrush = new SolidColorBrush(Colors.Red),
				BorderThickness = new Thickness(border),
				HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				Content = (sut = new Border
				{
					BorderThickness = new Thickness(0),
					Width = 100,
					Height = 100,
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}).Apply(s =>
				{
					Grid.SetColumn(s, 1);
					Grid.SetRow(s, 1);
				})
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var slot = LayoutInformation.GetLayoutSlot(sut);

			slot.Should().Be(new Rect(0, 0, root.Width - border * 2, root.Height - border * 2));
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_PaddedElementInScrollViewerAligned_Then_LayoutSlot(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin,
			bool canHorizontallyScroll,
			bool canVerticallyScroll)
		{
			Border sut;
			var root = new ScrollViewer
			{
				Width = 200,
				Height = 200,
				BorderThickness = new Thickness(0),
				HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				Content = (sut = new Border
				{
					Width = 100,
					Height = 100,
					Margin = new Thickness(margin),
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(border),
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}).Apply(s =>
				{
					Grid.SetColumn(s, 1);
					Grid.SetRow(s, 1);
				})
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var slot = LayoutInformation.GetLayoutSlot(sut);

			slot.Should().Be(new Rect(0, 0, root.Width, root.Height));
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_PaddedElementBiggerThanParent_Then_LayoutSlot(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin)
		{
#if __SKIA__ || __WASM__
			if (margin > 0)
			{
				Assert.Inconclusive("Margin are not supported by SV https://github.com/unoplatform/uno/issues/7000");
				return;
			}
#endif

			Border sut;
			var root = new Border
			{
				Width = 200,
				Height = 200,
				BorderThickness = new Thickness(0),
				Child = sut = new Border
				{
					Margin = new Thickness(margin),
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(border),
					Width = 300,
					Height = 300,
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var slot = LayoutInformation.GetLayoutSlot(sut);

			slot.Should().Be(new Rect(0, 0, 200, 200));
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_PaddedElementBiggerThanParentScrollViewer_Then_LayoutSlot(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin,
			bool canHorizontallyScroll,
			bool canVerticallyScroll)
		{
#if __SKIA__ || __WASM__
			if (margin > 0)
			{
				Assert.Inconclusive("Margin are not supported by SV https://github.com/unoplatform/uno/issues/7000");
				return;
			}
#endif

			Border sut;
			var root = new ScrollViewer
			{
				Width = 200,
				Height = 200,
				BorderThickness = new Thickness(0),
				HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				Content = sut = new Border
				{
					Margin = new Thickness(margin),
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(border),
					Width = 300,
					Height = 300,
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var expected = new Rect(
				0,
				0,
				canHorizontallyScroll ? sut.Width + margin * 2 : root.Width,
				canVerticallyScroll ? sut.Height + margin * 2 : root.Height);
			var actual = LayoutInformation.GetLayoutSlot(sut);

			actual.Should().Be(expected);
		}
	}
}
