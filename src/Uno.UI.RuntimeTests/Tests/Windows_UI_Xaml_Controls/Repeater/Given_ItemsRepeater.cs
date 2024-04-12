using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
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
					Layout = new StackLayout(),
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

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_UnloadAndReload_Then_StillListenToCollectionChanged()
		{
			var sut = default(ItemsRepeater);
			var source = new ObservableCollection<string>(Enumerable.Range(0, 3).Select(i => $"Item #{i}"));
			var root = new Border
			{
				Child = (sut = new ItemsRepeater
				{
					ItemsSource = source,
					Layout = new StackLayout(),
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

			sut.Children.Count.Should().Be(3);

			// Unload the IR
			root.Child = new TextBlock { Text = "IR unloaded" };
			await TestServices.WindowHelper.WaitForIdle();

			// Load again IR
			root.Child = sut;
			await TestServices.WindowHelper.WaitForIdle();

			// Assert reload state
			sut.Children.Count.Should().Be(3);

			// Add an item
			source.Add("Additional item");
			await TestServices.WindowHelper.WaitForIdle();
			sut.Children.Count.Should().Be(4);

			// Edit an item
			source[1] = "Item #1 - Edited";
			await TestServices.WindowHelper.WaitForIdle();
			sut.Children.FirstOrDefault(g => g.DataContext as string == "Item #1 - Edited").Should().NotBeNull();

			// Remove an item
			source.RemoveAt(2);
			await TestServices.WindowHelper.WaitForIdle();
			sut.Children.Count(elt => elt.ActualOffset.X >= 0).Should().Be(3);

			// Clear the collection
			source.Clear();
			await TestServices.WindowHelper.WaitForIdle();
			sut.Children.Count(elt => elt.ActualOffset.X >= 0).Should().Be(0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#elif __ANDROID__ || __SKIA__
		[Ignore("Currently fails https://github.com/unoplatform/uno/issues/9080")]
#elif __WASM__
		[Ignore("Flaky on CI https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task When_UnloadAndReload_Then_ReMaterializeItems()
		{
			var sut = SUT.Create(5000, new Size(250, 500));

			await sut.Load();

			var topItems = sut.MaterializedItems.ToArray();

			sut.Scroller.ChangeView(null, sut.Scroller.ExtentHeight / 2, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();

			var middleItems = sut.MaterializedItems.ToArray();
			middleItems.Should().NotContain(topItems);

			await sut.Unload();
			await sut.Load();

			var reloadedItems = sut.MaterializedItems.ToArray();
			reloadedItems.Count(item => middleItems.Contains(item)).Should().BeGreaterThan(2);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_AddItemWhileUnloaded_Then_MaterializeItems()
		{
			var sut = SUT.Create();

			await sut.Load();

			sut.Materialized.Should().Be(3);

			await sut.Unload();

			sut.Source.Add("Additional item");

			await sut.Load();

			sut.Materialized.Should().Be(4);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_RemoveItemWhileUnloaded_Then_MaterializeItems()
		{
			var sut = SUT.Create();

			await sut.Load();

			sut.Materialized.Should().Be(3);

			await sut.Unload();

			sut.Source.RemoveAt(1);

			await sut.Load();

			sut.Materialized.Should().Be(2);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_EditItemWhileUnloaded_Then_MaterializeItems()
		{
			var sut = SUT.Create();

			await sut.Load();

			sut.Materialized.Should().Be(3);

			await sut.Unload();

			sut.Source[1] = "Item #1 - Edited";

			await sut.Load();

			sut.Repeater.Children.FirstOrDefault(g => g.DataContext as string == "Item #1 - Edited").Should().NotBeNull();
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_ClearItemsWhileUnloaded_Then_MaterializeItems()
		{
			var sut = SUT.Create();

			await sut.Load();

			sut.Materialized.Should().Be(3);

			await sut.Unload();

			sut.Source.Clear();

			await sut.Load();

			sut.Materialized.Should().Be(0);
		}

		private record SUT(Border Root, ScrollViewer Scroller, ItemsRepeater Repeater, ObservableCollection<string> Source)
		{
			public static SUT Create(int itemsCount = 3, Size? viewport = default)
			{
				var repeater = default(ItemsRepeater);
				var scroller = default(ScrollViewer);
				var source = new ObservableCollection<string>(Enumerable.Range(0, itemsCount).Select(i => $"Item #{i}"));
				var root = new Border
				{
					BorderThickness = new Thickness(5),
					BorderBrush = new SolidColorBrush(Colors.Purple),
					Child = (scroller = new ScrollViewer
					{
						Content = (repeater = new ItemsRepeater
						{
							ItemsSource = source,
							Layout = new StackLayout(),
							ItemTemplate = new DataTemplate(() => new Border
							{
								Width = 100,
								Height = 100,
								Background = new SolidColorBrush(Colors.DeepSkyBlue),
								Margin = new Thickness(10),
								Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
							})
						})
					})
				};

				if (viewport is not null)
				{
					root.Height = viewport.Value.Height;
					root.Width = viewport.Value.Width;
				}

				return new(root, scroller, repeater, source);
			}

			public int Materialized => Repeater.Children.Count(elt => elt.ActualOffset.X >= 0);

			public IEnumerable<string> MaterializedItems => Repeater.Children.Where(elt => elt.ActualOffset.X >= 0).Select(elt => elt.DataContext?.ToString());

			public async ValueTask Load()
			{
				Root.Child = Scroller;
				if (TestServices.WindowHelper.WindowContent != Root)
				{
					TestServices.WindowHelper.WindowContent = Root;
				}
				await TestServices.WindowHelper.WaitForIdle();
				Repeater.IsLoaded.Should().BeTrue();
			}

			public async ValueTask Unload()
			{
				Root.Child = new TextBlock { Text = "IR unloaded" };
				await TestServices.WindowHelper.WaitForIdle();
				Repeater.IsLoaded.Should().BeFalse();
			}
		}
#endif
	}
}
