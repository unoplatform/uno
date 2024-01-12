using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using FluentAssertions;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;

#if !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater
{
	[TestClass]
	public class Given_ItemsRepeater
	{
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_NoScrollViewer_Then_ShowMoreThanFirstItem()
		{
			var sut = new ItemsRepeater
			{
				ItemsSource = new[] { "Item_1", "Item_2" }
			};
			var popup = new Popup
			{
				Child = new Grid
				{
					Width = 100,
					Height = 200,
					Children = { sut }
				}
			};

			TestServices.WindowHelper.WindowContent = popup;
			await TestServices.WindowHelper.WaitForIdle();

			popup.IsOpen = true;

			await TestServices.WindowHelper.WaitForIdle();
			sut.UpdateLayout();

			try
			{
				await TestHelper.RetryAssert(() =>
				{
					var second = sut
						.GetAllChildren()
						.OfType<TextBlock>()
						.FirstOrDefault(t => t.Text == "Item_2");

					Assert.IsNotNull(second);
				});
			}
			finally
			{
				popup.IsOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
#if __WASM__
		[Ignore("Currently flaky on WASM, part of #9080 epic")]
#endif
		public async Task When_NestedInSVAndOutOfViewportOnInitialLoad_Then_MaterializedEvenWhenScrollingOnMinorAxis()
		{
			var sut = default(ItemsRepeater);
			var sv = new ScrollViewer
			{
				Content = new StackPanel
				{
					Children = {
						new Border { Background = new SolidColorBrush(Colors.DeepPink), Height = 8192, Width = 150 },
						(sut = new ItemsRepeater
						{
							ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item #{i}"),
							Layout = new StackLayout { Orientation = Orientation.Horizontal },
							ItemTemplate = new DataTemplate(() => new Border
							{
								Width = 100,
								Height = 100,
								Background = new SolidColorBrush(Colors.DeepSkyBlue),
								Margin = new Thickness(10),
								Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
							})
						})
					}
				}
			};

			TestServices.WindowHelper.WindowContent = sv;
			await TestServices.WindowHelper.WaitForIdle();

#if !__IOS__
			sut.Children.Count.Should().BeLessOrEqualTo(1);
#endif

			sv.ChangeView(null, sv.ExtentHeight, null, disableAnimation: true);

			await TestServices.WindowHelper.WaitForIdle();

			sut.Children.Count.Should().BeGreaterThan(1);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#elif __ANDROID__ || __SKIA__
		[Ignore("Currently fails https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task When_NestedIRSlowlyChangeViewport_Then_MaterializedNeededItems()
		{
			async Task Do()
			{
				const int viewportHeight = 500;

				var sut = default(ItemsRepeater);
				var sv = new ScrollViewer
				{
					Height = viewportHeight,
					Content = (sut = new ItemsRepeater()
					{
						ItemsSource = Enumerable.Range(0, 10).Select(i => $"Group #{i:D2}"),
						ItemTemplate = new DataTemplate(() => new StackPanel
						{
							Children =
						{
							new Border
							{
								Background = new SolidColorBrush(Colors.DeepPink),
								Height = 100,
								Width = 150,
								Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
							},
							new ItemsRepeater
							{
								ItemsSource = Enumerable.Range(0, 50).Select(i => $"Item #{i:D2}"),
								ItemTemplate = new DataTemplate(() => new Border
								{
									Width = 150,
									Height = 100,
									Background = new SolidColorBrush(Colors.DeepSkyBlue),
									Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
								})
							}
						}
						})
					})
				};

				TestServices.WindowHelper.WindowContent = sv;
				await TestServices.WindowHelper.WaitForIdle();

				sv.ChangeView(null, sv.ExtentHeight / 2, null, disableAnimation: true);
				await TestServices.WindowHelper.WaitForIdle();

				var groupView = sut.Children.Single(g => g.DataContext as string == "Group #05");
				var groupIr = (ItemsRepeater)((StackPanel)groupView).Children[1];

				var beforeVisibleItems = groupIr.Children.Select(i => i.DataContext?.ToString()).OrderBy(i => i).ToArray();

				// Scroll by baby step to not be above the threshold which would cause a complete redraw
				const int step = 10;
				for (var i = 0; i < viewportHeight * 5; i += step)
				{
					sv.ChangeView(null, sv.VerticalOffset + step, null, disableAnimation: true);
					await TestServices.WindowHelper.WaitForIdle();
				}

				var afterVisibleItems = groupIr.Children.Select(i => i.DataContext?.ToString()).OrderBy(i => i).ToArray();

				afterVisibleItems.Should().NotContain(beforeVisibleItems);
			}

			await TestHelper.RetryAssert(Do, 3);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_UNO
		[Ignore("Custom behavior of uno")]
#elif __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_UnloadAndReload_Then_UnsubscribeAndResubscribeToEffectiveViewportChanged()
		{
			var evt = typeof(FrameworkElement).GetField("_effectiveViewportChanged", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new InvalidOperationException("Cannot find the private event backing field.");

			var sut = default(ItemsRepeater);
			var root = new Border
			{
				Child = (sut = new ItemsRepeater
				{
					ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item #{i}"),
					Layout = new StackLayout { Orientation = Orientation.Horizontal },
					ItemTemplate = new DataTemplate(() => new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
					})
				})
			};

			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			evt.GetValue(sut).Should().NotBeNull();

			// Unload the IR
			root.Child = new TextBlock { Text = "IR unloaded" };
			await TestServices.WindowHelper.WaitForIdle();

			evt.GetValue(sut).Should().BeNull("because the ViewportManagerWithPlatformFeatures should have remove handler in the ResetScrollers method");

			// Load again IR
			root.Child = sut;
			await TestServices.WindowHelper.WaitForIdle();

			evt.GetValue(sut).Should().NotBeNull("because the IR should have invalidated its measure, causing a layout pass driving to invoke the ViewportManagerWithPlatformFeatures.EnsureScroller which should have re-added handler");
		}
#endif
	}
}
