using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Helpers;

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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
							ItemTemplate = new DataTemplate(null, (_, _) => new Border
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

#if !__APPLE_UIKIT__
			sut.Children.Count.Should().BeLessThanOrEqualTo(1);
#endif

			sv.ChangeView(null, sv.ExtentHeight, null, disableAnimation: true);

			await TestServices.WindowHelper.WaitForIdle();

			sut.Children.Count.Should().BeGreaterThan(1);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__ || __SKIA__
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
						ItemTemplate = new DataTemplate(null, (_, _) => new StackPanel
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
								ItemTemplate = new DataTemplate(null, (_, _) => new Border
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

				var groupView = sut.Children.Single(g => ((FrameworkElement)g).DataContext as string == "Group #05");
				var groupIr = (ItemsRepeater)((StackPanel)groupView).Children[1];

				var beforeVisibleItems = groupIr.Children.Select(i => ((FrameworkElement)i).DataContext?.ToString()).OrderBy(i => i).ToArray();

				// Scroll by baby step to not be above the threshold which would cause a complete redraw
				const int step = 10;
				for (var i = 0; i < viewportHeight * 5; i += step)
				{
					sv.ChangeView(null, sv.VerticalOffset + step, null, disableAnimation: true);
					await TestServices.WindowHelper.WaitForIdle();
				}

				var afterVisibleItems = groupIr.Children.Select(i => ((FrameworkElement)i).DataContext?.ToString()).OrderBy(i => i).ToArray();

				afterVisibleItems.Should().NotContain(beforeVisibleItems);
			}

			await TestHelper.RetryAssert(Do, 3);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_UNO
		[Ignore("Custom behavior of uno")]
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
					ItemTemplate = new DataTemplate(null, (_, _) => new Border
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
					ItemTemplate = new DataTemplate(null, (_, _) => new Border
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
			sut.Children.FirstOrDefault(g => ((FrameworkElement)g).DataContext as string == "Item #1 - Edited").Should().NotBeNull();

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
#if __ANDROID__ || __SKIA__
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
		public async Task When_EditItemWhileUnloaded_Then_MaterializeItems()
		{
			var sut = SUT.Create();

			await sut.Load();

			sut.Materialized.Should().Be(3);

			await sut.Unload();

			sut.Source[1] = "Item #1 - Edited";

			await sut.Load();

			sut.Repeater.Children.FirstOrDefault(g => ((FrameworkElement)g).DataContext as string == "Item #1 - Edited").Should().NotBeNull();
		}

		[TestMethod]
		[RunsOnUIThread]
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
		[Ignore("Validated the Uno-specific firstRealizedMajor clamping in StackLayout.GetExtent, which was removed in favor of WinUI parity (uno#24479). Fails on Skia (WinUI behavior) and on native targets (async native scrolling).")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24479")]
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
				new DataTemplate(null, (_, _) => new Border
				{
					Width = 120,
					Margin = new Thickness(10),
					Child = new ItemsControl
					{
						ItemTemplate = new DataTemplate(null, (_, _) => new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding())))
					}.Apply(tb => tb.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = nameof(MyItem.Lines) }))
				}
				.Apply(b => b.SetBinding(FrameworkElement.HeightProperty, new Binding { Path = nameof(MyItem.Height) }))
				.Apply(b => b.SetBinding(Border.BackgroundProperty, new Binding { Path = nameof(MyItem.Color) }))),
				new Size(120, 500)
			);

			await sut.Load();
			sut.Scroller.ViewChanged += (s, e) => Console.WriteLine($"Vertical: {sut.Scroller.VerticalOffset}");

			var originalEstimatedExtent = sut.Scroller.ExtentHeight;

			sut.Scroller.ChangeView(null, 800, null, disableAnimation: true); // First scroll enough to get item #3 to be materialized
			await TestServices.WindowHelper.WaitForIdle();

			sut.MaterializedItems.Should().Contain(sut.Source[3]); // Confirm that item has been materialized!
			sut.Scroller.ExtentHeight.Should().BeGreaterThan(originalEstimatedExtent); // Confirm that the extent has increased due to item #3

			sut.Scroller.ChangeView(null, 1500, null, disableAnimation: true); // Then scroll enough for first items to be DE-materialized
			await TestServices.WindowHelper.WaitForIdle();

			sut.MaterializedItems.Should().NotContain(sut.Source[0]); // Confirm that first items has been removed!
			sut.MaterializedItems.Should().NotContain(sut.Source[1]);

			sut.Scroller.ChangeView(null, 2940, null, disableAnimation: true); // Then scroll enough for first items to be DE-materialized
			await TestServices.WindowHelper.WaitForIdle();

			// The visual position of item #3 on screen is the authoritative check: when the estimated
			// extent is corrected via IScrollAnchorProvider anchor-shift, the repeater-internal
			// ActualOffset may change while the scroller compensates to keep the anchor visually fixed.
			var result = await UITestHelper.ScreenShot(sut.Root);
			ImageAssert.HasColorAt(result, 100, 10, Colors.FromARGB("#008000")); // Confirm item 3 (green) is still visible at the top of the viewport.
		}

		[TestMethod]
		[RunsOnUIThread]
		[Ignore("Validated the Uno-specific firstRealizedMajor clamping in StackLayout.GetExtent, which was removed in favor of WinUI parity (uno#24479). Fails on Skia (WinUI behavior) and on native targets (async native scrolling).")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24479")]
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
			LayoutInformation.GetLayoutSlot(sut.MaterializedElements.OrderBy(e => ((FrameworkElement)e).DataContext).First()).Y.Should().Be(0, "Item #0 should be at the origin of the IR (negative offset means we are in trouble!)");
		}

		[TestMethod]
		[RunsOnUIThread]
		// SkiaWasm excluded: render-loop-driven ChangeView smooth-scroll stalls under the headless xvfb browser (flaky). #23524
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23524")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
#if !__SKIA__
		[Ignore("Fails due to async native scrolling.")]
#endif
		public async Task When_Repeater_ChangedView()
		{
			var sut = SUT.Create(300, new Size(100, 500));

			await sut.Load();

			var items = sut.MaterializedItems.ToArray();

			var lastItem = sut.Source.Last();
			sut.MaterializedItems.Should().NotContain(lastItem);

			sut.Scroller.ChangeView(null, 1000000, null, disableAnimation: false);

			// Wait for the smooth-scroll to start materializing new items (progressive state), then
			// to reach the bottom — polling the actual conditions instead of fixed delays, which were
			// too short on slower runtimes (e.g. WASM) and left the view short of the end, flaking CI.
			await UITestHelper.WaitFor(
				() => sut.MaterializedItems.Except(items).Any() && sut.MaterializedItems.Count() >= 3,
				timeoutMS: 3000,
				message: "ChangeView should have started materializing new items");

			sut.MaterializedItems.Should().NotBeEquivalentTo(items);
			sut.MaterializedItems.Count().Should().BeGreaterThanOrEqualTo(3);

			await UITestHelper.WaitFor(
				() => sut.MaterializedItems.Contains(lastItem),
				timeoutMS: 5000,
				message: "ChangeView should have scrolled to the last item");
			sut.MaterializedItems.Should().Contain(lastItem);
		}


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_No_Layout_Set_Then_Uses_Default_StackLayout()
		{
			// Verifies that ItemsRepeater works without an explicit Layout property being set.
			// In WinUI 1.8.2, ItemsRepeater falls back to a default StackLayout when Layout is null.
			var sut = new ItemsRepeater
			{
				ItemsSource = new[] { "Item_1", "Item_2", "Item_3" },
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForLoaded(sut);
			await TestServices.WindowHelper.WaitForIdle();

			try
			{
				await TestHelper.RetryAssert(() =>
				{
					var items = sut.GetAllChildren().OfType<TextBlock>().ToList();
					Assert.IsTrue(items.Count >= 3, $"Expected at least 3 items, got {items.Count}");
					Assert.AreEqual("Item_1", items[0].Text);
					Assert.AreEqual("Item_2", items[1].Text);
					Assert.AreEqual("Item_3", items[2].Text);
				});
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NumberBox_In_Repeater_Then_CanFocus_And_Edit()
		{
			var sut = SUT.Create(
				source: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
				itemTemplate: XamlHelper.LoadXaml<DataTemplate>("""
					<DataTemplate>
						<Border>
							<Grid>
								<NumberBox Value="10" />
							</Grid>
						</Border>
					</DataTemplate>
				"""),
				viewport: new Size(120, 500)
			);

			await sut.Load();

			var numberBox = sut.Repeater.FindFirstDescendantOrThrow<NumberBox>();
			numberBox.Value.Should().Be(10);

			numberBox.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			numberBox.Value = 42;
			await TestServices.WindowHelper.WaitForIdle();

			numberBox.Value.Should().Be(42);
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
				itemTemplate ??= new DataTemplate(null, (_, _) => new Border
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

			public IEnumerable<T> MaterializedItems => MaterializedElements.Select(elt => (T)((FrameworkElement)elt).DataContext!);

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

		// The #nullable enable above lives inside the #if HAS_UNO block, so it does not reach here
		// on the WinAppSDK head.
#nullable enable

		#region uno#24447 - two UniformGridLayout repeaters sharing an item template must not spin the layout loop

		private const int Issue24447_ItemsPerRepeater = 3;

		private const string Issue24447_ItemTemplateXaml = """
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<Border Height="40"
						Margin="4"
						Background="LightSteelBlue"
						CornerRadius="8">
					<TextBlock HorizontalAlignment="Center"
							   VerticalAlignment="Center"
							   Text="{Binding}" />
				</Border>
			</DataTemplate>
			""";

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24447")]
		public async Task When_Two_UniformGridLayout_Repeaters_Share_ItemTemplate_Then_Layout_Settles()
		{
			// The issue's repro points both repeaters at the same {StaticResource} DataTemplate,
			// so both share the RecyclePool that is attached to that template instance.
			var sharedTemplate = Issue24447_CreateItemTemplate();

			var first = Issue24447_CreateRepeater(sharedTemplate, "A");
			var second = Issue24447_CreateRepeater(sharedTemplate, "B");
			var root = Issue24447_CreateRoot(first, second);

			try
			{
				TestServices.WindowHelper.WindowContent = root;

				Issue24447_AssertLayoutSettles(root, "two ItemsRepeaters sharing a single UniformGridLayout item template");

				await TestServices.WindowHelper.WaitForLoaded(first);
				await TestServices.WindowHelper.WaitForLoaded(second);
				await TestServices.WindowHelper.WaitForIdle();

				Issue24447_AssertMaterialized(first, "A");
				Issue24447_AssertMaterialized(second, "B");
				Issue24447_AssertNoSharedElements(first, second);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24447")]
		public async Task When_Two_UniformGridLayout_Repeaters_Use_Distinct_ItemTemplates_Then_Layout_Settles()
		{
			// Same page shape, but each repeater owns its DataTemplate (and therefore its own RecyclePool).
			// This is the contrast case: it isolates the shared pool as the trigger rather than the mere
			// presence of two UniformGridLayout repeaters.
			var first = Issue24447_CreateRepeater(Issue24447_CreateItemTemplate(), "A");
			var second = Issue24447_CreateRepeater(Issue24447_CreateItemTemplate(), "B");
			var root = Issue24447_CreateRoot(first, second);

			try
			{
				TestServices.WindowHelper.WindowContent = root;

				Issue24447_AssertLayoutSettles(root, "two ItemsRepeaters with distinct UniformGridLayout item templates");

				await TestServices.WindowHelper.WaitForLoaded(first);
				await TestServices.WindowHelper.WaitForLoaded(second);
				await TestServices.WindowHelper.WaitForIdle();

				Issue24447_AssertMaterialized(first, "A");
				Issue24447_AssertMaterialized(second, "B");
				Issue24447_AssertNoSharedElements(first, second);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static DataTemplate Issue24447_CreateItemTemplate()
			=> (DataTemplate)XamlReader.Load(Issue24447_ItemTemplateXaml);

		private static ItemsRepeater Issue24447_CreateRepeater(DataTemplate itemTemplate, string prefix)
			=> new()
			{
				ItemTemplate = itemTemplate,
				Layout = new UniformGridLayout
				{
					MinItemWidth = 80,
					MinColumnSpacing = 8,
					MinRowSpacing = 8,
				},
				ItemsSource = new ObservableCollection<string>(
					Enumerable.Range(1, Issue24447_ItemsPerRepeater).Select(i => prefix + i)),
			};

		private static FrameworkElement Issue24447_CreateRoot(ItemsRepeater first, ItemsRepeater second)
			=> new Grid
			{
				// Small enough to fit any test host area, wide enough for all items of a repeater
				// to sit on a single line and inside the effective viewport.
				Width = 300,
				Height = 300,
				Children =
				{
					new StackPanel
					{
						Children =
						{
							new TextBlock { Text = "If you can read this, the page rendered." },
							first,
							second,
						},
					},
				},
			};

		/// <summary>
		/// Runs the layout loop synchronously. <see cref="UIElement.UpdateLayout"/> is what raises
		/// <c>LayoutCycleException</c> when measure keeps re-invalidating itself, and driving it from the test
		/// keeps the failure on this stack - the dispatcher tick path only logs the exception and retries forever.
		/// </summary>
		private static void Issue24447_AssertLayoutSettles(FrameworkElement root, string scenario)
		{
			try
			{
				root.UpdateLayout();

				// The second pass covers a cycle that only shows up once the first effective viewport is known.
				root.UpdateLayout();
			}
			catch (Exception ex)
			{
				Assert.Fail(
					$"Layout never settled with {scenario}. " +
					$"{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
			}
		}

		private static void Issue24447_AssertMaterialized(ItemsRepeater repeater, string prefix)
		{
			Assert.IsTrue(
				repeater.ActualHeight > 0,
				$"Repeater '{prefix}' should have a non-zero height once the page has been laid out.");

			for (var index = 0; index < Issue24447_ItemsPerRepeater; index++)
			{
				var element = repeater.TryGetElement(index);

				Assert.IsNotNull(
					element,
					$"Repeater '{prefix}' should have realized the element at index {index}.");
				Assert.AreEqual(
					prefix + (index + 1),
					(element as FrameworkElement)?.DataContext,
					$"Element {index} of repeater '{prefix}' is bound to the wrong item.");
				Assert.AreSame(
					repeater,
					VisualTreeHelper.GetParent(element),
					$"Element {index} of repeater '{prefix}' should be parented to that repeater.");
			}
		}

		/// <summary>
		/// Recycled elements are pooled per <see cref="DataTemplate"/>; handing one to the sibling repeater
		/// re-parents it and re-invalidates measure on every pass. Both repeaters must own their own elements.
		/// </summary>
		private static void Issue24447_AssertNoSharedElements(ItemsRepeater first, ItemsRepeater second)
		{
			var shared = Issue24447_GetChildren(first).Intersect(Issue24447_GetChildren(second)).ToArray();

			Assert.AreEqual(
				0,
				shared.Length,
				"The two ItemsRepeaters must not host the same element instance.");
		}

		private static IReadOnlyList<DependencyObject> Issue24447_GetChildren(ItemsRepeater repeater)
		{
			var children = new List<DependencyObject>();
			var count = VisualTreeHelper.GetChildrenCount(repeater);
			for (var i = 0; i < count; i++)
			{
				children.Add(VisualTreeHelper.GetChild(repeater, i));
			}

			return children;
		}

		#endregion

		#region uno#23624 - a recycled element must not keep the removed item as its DataContext

		// {Binding}, not x:Bind, on purpose: the repeater only propagates the item as the element's
		// DataContext when the template root carries no IDataTemplateComponent, which is the case
		// the MustClearDataContext flag tracks.
		private const string Issue23624_ItemTemplateXaml = """
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<TextBlock Height="40" Text="{Binding Label}" />
			</DataTemplate>
			""";

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23624")]
		public async Task When_Item_Removed_Then_Recycled_Element_DataContext_Is_Cleared()
		{
			var (host, repeater, source) = Issue23624_CreateSut(itemCount: 4);

			try
			{
				await UITestHelper.Load(host);

				var lastIndex = source.Count - 1;
				var removedItem = source[lastIndex];
				var recycled = await Issue23624_GetRealizedElement(repeater, lastIndex);

				Assert.AreSame(
					removedItem,
					recycled.DataContext,
					"Precondition: the repeater must be the one that pushed the item onto the element.");

				// Removing the *last* item recycles its element while every surviving index keeps the
				// element it already had, so nothing pulls this one back out of the pool. That is the
				// "recycled but never reused" state the issue describes.
				source.RemoveAt(lastIndex);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(
					-1,
					repeater.GetElementIndex(recycled),
					"The element must be recycled, i.e. not realized for any index any more.");

				Assert.IsNull(
					recycled.DataContext,
					"A recycled element must not keep the removed item as its DataContext.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23624")]
		public async Task When_Item_Removed_Then_Recycled_Element_Raises_DataContextChanged()
		{
			var (host, repeater, source) = Issue23624_CreateSut(itemCount: 4);

			try
			{
				await UITestHelper.Load(host);

				var lastIndex = source.Count - 1;
				var recycled = await Issue23624_GetRealizedElement(repeater, lastIndex);

				var observed = new List<object?>();
				void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
					=> observed.Add(args.NewValue);

				recycled.DataContextChanged += OnDataContextChanged;
				try
				{
					source.RemoveAt(lastIndex);
					await TestServices.WindowHelper.WaitForIdle();
				}
				finally
				{
					recycled.DataContextChanged -= OnDataContextChanged;
				}

				Assert.IsTrue(
					observed.Any(value => value is null),
					"Recycling must raise DataContextChanged with a null DataContext so app cleanup logic "
					+ $"written against WinUI runs. Observed values: [{string.Join(", ", observed)}].");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23624")]
		public async Task When_Recycled_Element_Reused_For_Same_Item_Then_DataContext_Is_ReApplied()
		{
			var (host, repeater, source) = Issue23624_CreateSut(itemCount: 4);

			try
			{
				await UITestHelper.Load(host);

				var lastIndex = source.Count - 1;
				var item = source[lastIndex];
				var element = await Issue23624_GetRealizedElement(repeater, lastIndex);

				var transitions = 0;
				void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
					=> transitions++;

				element.DataContextChanged += OnDataContextChanged;
				try
				{
					source.RemoveAt(lastIndex);
					await TestServices.WindowHelper.WaitForIdle();

					// The very same instance goes back in. With the clear on recycle the element sees
					// null -> item, so every change-driven mechanism re-fires. Without it the element
					// sees item -> item, which is a no-op assignment that refreshes nothing.
					source.Add(item);
					host.UpdateLayout();
					await TestServices.WindowHelper.WaitForIdle();
				}
				finally
				{
					element.DataContextChanged -= OnDataContextChanged;
				}

				var reused = await Issue23624_GetRealizedElement(repeater, source.Count - 1);

				Assert.AreSame(element, reused, "The pooled element should be reused for the re-added item.");
				Assert.AreSame(item, reused.DataContext, "The reused element must carry the re-added item.");
				Assert.IsTrue(
					transitions >= 2,
					"Reusing a pooled element for the same item instance must still go through a "
					+ $"null -> item transition. Observed {transitions} DataContext transition(s), expected at least 2.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static (ScrollViewer Host, ItemsRepeater Repeater, ObservableCollection<Issue23624_ItemModel> Source) Issue23624_CreateSut(int itemCount)
		{
			var source = new ObservableCollection<Issue23624_ItemModel>(
				Enumerable.Range(0, itemCount).Select(i => new Issue23624_ItemModel($"Item {i}")));

			ItemsRepeater repeater = new()
			{
				ItemsSource = source,
				Layout = new StackLayout { Orientation = Orientation.Vertical },
				ItemTemplate = (DataTemplate)XamlReader.Load(Issue23624_ItemTemplateXaml),
			};

			// The whole list (4 x 40px) fits inside the viewport, so every index is realized before the
			// removal and the realization window never has to move.
			ScrollViewer host = new()
			{
				Width = 200,
				Height = 300,
				Content = repeater,
			};

			return (host, repeater, source);
		}

		private static async Task<FrameworkElement> Issue23624_GetRealizedElement(ItemsRepeater repeater, int index)
		{
			FrameworkElement? element = null;

			await UITestHelper.WaitFor(
				() => (element = repeater.TryGetElement(index) as FrameworkElement) is not null,
				message: $"Timeout waiting for the element at index {index} to be realized.");

			return element!;
		}

		private sealed class Issue23624_ItemModel
		{
			public Issue23624_ItemModel(string label) => Label = label;

			public string Label { get; }

			public override string ToString() => Label;
		}

		#endregion

		#region uno#21668 - clearing ItemsSource after an unload/reload cycle must not fault

		private const int Issue21668_WaitTimeoutMS = 5000;

		private const string Issue21668_ItemTemplateXaml = """
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<Border Width="120"
						Height="40"
						Background="SkyBlue">
					<TextBlock Text="{Binding}" />
				</Border>
			</DataTemplate>
			""";

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21668")]
		public async Task When_ItemsSource_Cleared_After_UnloadAndReload_Then_Does_Not_Throw()
		{
			var source = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Item #{i}"));
			var repeater = new ItemsRepeater
			{
				ItemsSource = source,
				Layout = new StackLayout(),
				ItemTemplate = Issue21668_CreateItemTemplate(),
			};
			var root = new Border
			{
				Width = 200,
				Height = 300,
				Child = repeater,
			};

			try
			{
				await UITestHelper.Load(root);

				Assert.IsTrue(repeater.IsLoaded, "The ItemsRepeater should be loaded.");
				Assert.IsTrue(
					Issue21668_MaterializedItems(repeater).Count > 0,
					"The ItemsRepeater should have materialized items on its first load.");

				// The unload/reload cycle is what arms the bug: unloading drops the data-source
				// subscription and reloading re-installs it from the Loaded handler.
				await Issue21668_UnloadRepeater(root, repeater);
				await Issue21668_ReloadRepeater(root, repeater);

				// Symptom under test: clearing the items source must not fault.
				try
				{
					repeater.ItemsSource = null;
				}
				catch (Exception ex)
				{
					Assert.Fail(
						$"Clearing ItemsSource after an unload/reload cycle threw {ex.GetType().Name}: " +
						$"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
				}

				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsNull(repeater.ItemsSourceView, "The ItemsSourceView should have been cleared.");

				// And the repeater must remain usable afterwards.
				repeater.ItemsSource = new ObservableCollection<string> { "Replaced #0", "Replaced #1" };
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitFor(
					() => Issue21668_MaterializedItems(repeater).Contains("Replaced #0"),
					timeoutMS: Issue21668_WaitTimeoutMS,
					message: "The ItemsRepeater should materialize the replacement items after ItemsSource was cleared.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21668")]
		public async Task When_Bound_ItemsSource_Goes_Null_After_UnloadAndReload_Then_Does_Not_Throw()
		{
			var source = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Item #{i}"));
			var repeater = new ItemsRepeater
			{
				Layout = new StackLayout(),
				ItemTemplate = Issue21668_CreateItemTemplate(),
			};
			repeater.SetBinding(
				ItemsRepeater.ItemsSourceProperty,
				new Binding { Path = new PropertyPath(nameof(Issue21668_ItemsHolder.Items)) });

			var root = new Border
			{
				Width = 200,
				Height = 300,
				DataContext = new Issue21668_ItemsHolder { Items = source },
				Child = repeater,
			};

			try
			{
				await UITestHelper.Load(root);

				Assert.IsTrue(repeater.IsLoaded, "The ItemsRepeater should be loaded.");
				Assert.IsNotNull(
					repeater.ItemsSourceView,
					"The binding should have pushed the collection into ItemsSource.");

				await Issue21668_UnloadRepeater(root, repeater);
				await Issue21668_ReloadRepeater(root, repeater);

				// Mirrors the reported crash: the hosting element is being torn down, the inherited
				// DataContext is replaced/cleared, and the ItemsSource binding pushes null into the
				// repeater while it holds the subscription re-installed on its second Loaded.
				try
				{
					root.DataContext = new Issue21668_ItemsHolder();
					await TestServices.WindowHelper.WaitForIdle();

					root.DataContext = null;
					await TestServices.WindowHelper.WaitForIdle();
				}
				catch (Exception ex)
				{
					Assert.Fail(
						$"Clearing the bound ItemsSource after an unload/reload cycle threw {ex.GetType().Name}: " +
						$"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
				}

				await TestServices.WindowHelper.WaitFor(
					() => repeater.ItemsSourceView is null,
					timeoutMS: Issue21668_WaitTimeoutMS,
					message: "The binding should have cleared the ItemsSource once the DataContext no longer provides a collection.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static DataTemplate Issue21668_CreateItemTemplate()
			=> (DataTemplate)XamlReader.Load(Issue21668_ItemTemplateXaml);

		private static async Task Issue21668_UnloadRepeater(Border root, ItemsRepeater repeater)
		{
			root.Child = new TextBlock { Text = "ItemsRepeater unloaded" };
			await TestServices.WindowHelper.WaitFor(
				() => !repeater.IsLoaded,
				timeoutMS: Issue21668_WaitTimeoutMS,
				message: "The ItemsRepeater should have been unloaded.");
			await TestServices.WindowHelper.WaitForIdle();
		}

		private static async Task Issue21668_ReloadRepeater(Border root, ItemsRepeater repeater)
		{
			root.Child = repeater;
			await TestServices.WindowHelper.WaitFor(
				() => repeater.IsLoaded,
				timeoutMS: Issue21668_WaitTimeoutMS,
				message: "The ItemsRepeater should have been re-loaded.");
			await TestServices.WindowHelper.WaitForIdle();
		}

		private static IReadOnlyList<string> Issue21668_MaterializedItems(ItemsRepeater repeater)
		{
			var items = new List<string>();
			var count = VisualTreeHelper.GetChildrenCount(repeater);
			for (var i = 0; i < count; i++)
			{
				// ItemsRepeater parks recycled/unrealized elements at large negative offsets.
				if (VisualTreeHelper.GetChild(repeater, i) is FrameworkElement { ActualHeight: > 0 } child
					&& child.ActualOffset.Y > -1000
					&& child.DataContext is string text)
				{
					items.Add(text);
				}
			}

			return items;
		}

		public sealed class Issue21668_ItemsHolder
		{
			public ObservableCollection<string>? Items { get; set; }
		}

		#endregion
	}
}
