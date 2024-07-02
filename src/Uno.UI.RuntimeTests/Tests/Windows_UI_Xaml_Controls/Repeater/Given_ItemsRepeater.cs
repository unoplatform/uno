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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using FluentAssertions;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO && !HAS_UNO_WINUI
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


		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#elif !__SKIA__
		[Ignore("Fails due to async native scrolling.")]
#endif
		public async Task When_ItemSignificantlyTaller_Then_VirtualizeProperly()
		{
			var sut = SUT.Create(
				new ObservableCollection<MyItem>
				{
					new (0, 200, Colors.FromARGB("#FF0000")),
					new (1, 400, Colors.FromARGB("#FF8000")),
					new (2, 200, Colors.FromARGB("#FFFF00")),
					new (3, 5000, Colors.FromARGB("#008000")),
					new (4, 100, Colors.FromARGB("#0000FF")),
					new (5, 100, Colors.FromARGB("#A000C0"))
				},
				new DataTemplate(() => new Border
				{
					Width = 120,
					Margin = new Thickness(10),
					Child = new ItemsControl
					{
						ItemTemplate = new DataTemplate(() => new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding())))
					}.Apply(tb => tb.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = nameof(MyItem.Lines) }))
				}
				.Apply(b => b.SetBinding(FrameworkElement.HeightProperty, new Binding { Path = nameof(MyItem.Height) }))
				.Apply(b => b.SetBinding(FrameworkElement.BackgroundProperty, new Binding { Path = nameof(MyItem.Color) }))),
				new Size(120, 500)
			);

			await sut.Load();
			sut.Scroller.ViewChanged += (s, e) => Console.WriteLine($"Vertical: {sut.Scroller.VerticalOffset}");

			var originalEstimatedExtent = sut.Scroller.ExtentHeight;

			sut.Scroller.ChangeView(null, 800, null, disableAnimation: true); // First scroll enough to get item #3 to be materialized
			await TestServices.WindowHelper.WaitForIdle();

			sut.MaterializedItems.Should().Contain(sut.Source[3]); // Confirm that item has been materialized!
			sut.Scroller.ExtentHeight.Should().BeGreaterThan(originalEstimatedExtent); // Confirm that the extent has increased due to item #3

			var item3OriginalVerticalOffset = sut.Repeater.Children.First(elt => ReferenceEquals(elt.DataContext, sut.Source[3])).ActualOffset.Y;

			sut.Scroller.ChangeView(null, 1500, null, disableAnimation: true); // Then scroll enough for first items to be DE-materialized
			await TestServices.WindowHelper.WaitForIdle();

			sut.MaterializedItems.Should().NotContain(sut.Source[0]); // Confirm that first items has been removed!
			sut.MaterializedItems.Should().NotContain(sut.Source[1]);

			sut.Scroller.ChangeView(null, 2940, null, disableAnimation: true); // Then scroll enough for first items to be DE-materialized
			await TestServices.WindowHelper.WaitForIdle();

			var item3UpdatedVerticalOffset = sut.Repeater.Children.First(elt => ReferenceEquals(elt.DataContext, sut.Source[3])).ActualOffset.Y;

			item3UpdatedVerticalOffset.Should().Be(item3OriginalVerticalOffset); // Confirm that item #3 has not been moved down
			var result = await UITestHelper.ScreenShot(sut.Root);
			ImageAssert.HasColorAt(result, 100, 10, Colors.FromARGB("#008000")); // For safety also check it's effectively the item 3 that is visible
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#elif !__SKIA__
		[Ignore("Fails due to async native scrolling.")]
#endif
		public async Task When_UnloadReload_Then_MaterializeItemsForCurrentViewport()
		{
			var sut = SUT.Create(30, new Size(100, 500));

			await sut.Load();

			// Only few first items should have been materialized so far
			sut.MaterializedItems.Should().Contain(sut.Source[0]);
			sut.MaterializedItems.Should().Contain(sut.Source[5]);
			sut.MaterializedItems.Should().NotContain(sut.Source[10]);
			sut.MaterializedItems.Should().NotContain(sut.Source[15]);

			await sut.Unload();
			await sut.Load();

			// Confirm that first items has been re-materialized
			sut.MaterializedItems.Should().Contain(sut.Source[0]);
			sut.MaterializedItems.Should().Contain(sut.Source[5]);
			sut.MaterializedItems.Should().NotContain(sut.Source[10]);
			sut.MaterializedItems.Should().NotContain(sut.Source[15]);

			// Item 0 should be at offset 0
			sut.MaterializedElements.OrderBy(e => e.DataContext).First().LayoutSlot.Y.Should().Be(0, "Item #0 should be at the origin of the IR (negative offset means we are in trouble!)");
		}

		private record MyItem(int Id, double Height, Color Color)
		{
			public string Title => $"Item {Id}";

			public string[] Lines { get; } = Enumerable.Range(0, (int)(Height / 10)).Select(i => $"Line {i:D3}").ToArray();
		}

#nullable enable
		private static class SUT
		{
			public static SUT<T> Create<T>(ObservableCollection<T> source, DataTemplate? itemTemplate = null, Size? viewport = default)
			{
				itemTemplate ??= new DataTemplate(() => new Border
				{
					Width = 100,
					Height = 100,
					Background = new SolidColorBrush(Colors.DeepSkyBlue),
					Margin = new Thickness(10),
					Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
				});

				var repeater = default(ItemsRepeater);
				var scroller = default(ScrollViewer);
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
							ItemTemplate = itemTemplate
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

			public static SUT<string> Create(int itemsCount = 3, Size? viewport = default)
				=> Create(new ObservableCollection<string>(Enumerable.Range(0, itemsCount).Select(i => $"Item #{i}")), viewport: viewport);
		}

		private record SUT<T>(Border Root, ScrollViewer Scroller, ItemsRepeater Repeater, ObservableCollection<T> Source)
		{
			public int Materialized => Repeater.Children.Count(elt => elt.ActualOffset.X >= 0);

			public IEnumerable<T> MaterializedItems => MaterializedElements.Select(elt => (T)elt.DataContext);

			public IEnumerable<UIElement> MaterializedElements => Repeater.Children.Where(elt => elt.ActualOffset.X >= 0);

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
