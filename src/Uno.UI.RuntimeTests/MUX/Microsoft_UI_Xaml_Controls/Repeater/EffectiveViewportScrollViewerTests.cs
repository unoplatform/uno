// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading;
using System.Collections.Generic;
using Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks;
using System.Numerics;
using Common;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using VirtualizingLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.VirtualizingLayout;
using ItemsRepeater = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsRepeater;
using VirtualizingLayoutContext = Microsoft/* UWP don't rename */.UI.Xaml.Controls.VirtualizingLayoutContext;
using RecyclingElementFactory = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RecyclingElementFactory;
using StackLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.StackLayout;
using IRepeaterScrollingSurface = Microsoft.UI.Private.Controls.IRepeaterScrollingSurface;
using ConfigurationChangedEventHandler = Microsoft.UI.Private.Controls.ConfigurationChangedEventHandler;
using PostArrangeEventHandler = Microsoft.UI.Private.Controls.PostArrangeEventHandler;
using ViewportChangedEventHandler = Microsoft.UI.Private.Controls.ViewportChangedEventHandler;
using System.Threading.Tasks;
using Private.Infrastructure;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	[Ignore("Test are currently failing after target contract was raised (see issue #4830)")]
	public class EffectiveViewportTests : MUXApiTestBase
	{
		[TestMethod]
		public async Task ValidateBasicScrollViewerScenario()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport is not available below RS5");
				return;
			}

			var realizationRects = new List<Rect>();
			var viewChangeCompletedEvent = new UnoAutoResetEvent(false);
			ScrollViewer scrollViewer = null;
			UnoManualResetEvent viewChanged = new UnoManualResetEvent(false);
			UnoManualResetEvent layoutMeasured = new UnoManualResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				var repeater = new ItemsRepeater()
				{
					Layout = GetMonitoringLayout(new Size(500, 600), realizationRects, layoutMeasured),
					HorizontalCacheLength = 0.0,
					VerticalCacheLength = 0.0
				};

				scrollViewer = new ScrollViewer
				{
					Content = repeater,
					Width = 200,
					Height = 300,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
					VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				};

				scrollViewer.ViewChanged += (sender, args) =>
				{
					if (!args.IsIntermediate)
					{
						Log.Comment("ViewChanged " + scrollViewer.HorizontalOffset + ":" + scrollViewer.VerticalOffset);
						viewChanged.Set();
					}
				};

				Content = scrollViewer;
			});

			Verify.IsTrue(await layoutMeasured.WaitOne(), "Did not receive measure on layout");

			RunOnUIThread.Execute(() =>
			{
				// First layout pass will invalidate measure during the first arrange
				// so that we can get a viewport during the second measure/arrange pass.
				Verify.AreEqual(new Rect(0, 0, 0, 0), realizationRects[0]);
				Verify.AreEqual(new Rect(0, 0, 200, 300), realizationRects[1]);
				realizationRects.Clear();

				viewChanged.Reset();
				layoutMeasured.Reset();
				scrollViewer.ChangeView(null, 100.0, 1.0f, disableAnimation: true);
			});

			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsTrue(await viewChanged.WaitOne(), "Did not receive view changed event");
			Verify.IsTrue(await layoutMeasured.WaitOne(), "Did not receive measure on layout");
			viewChanged.Reset();
			layoutMeasured.Reset();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(0, 100, 200, 300), realizationRects.Last());
				realizationRects.Clear();
				viewChangeCompletedEvent.Reset();

				// Max viewport offset is (300, 400). Horizontal viewport offset
				// is expected to get coerced from 400 to 300.
				scrollViewer.ChangeView(400, 100.0, 1.0f, disableAnimation: true);
			});

			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsTrue(await viewChanged.WaitOne(), "Did not receive view changed event");
			Verify.IsTrue(await layoutMeasured.WaitOne(), "Did not receive measure on layout");
			viewChanged.Reset();
			layoutMeasured.Reset();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(300, 100, 200, 300), realizationRects.Last());
				realizationRects.Clear();
				viewChangeCompletedEvent.Reset();

				scrollViewer.ChangeView(null, null, 2.0f, disableAnimation: true);
			});

			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsTrue(await viewChanged.WaitOne(), "Did not receive view changed event");
			Verify.IsTrue(await layoutMeasured.WaitOne(), "Did not receive measure on layout");
			viewChanged.Reset();
			layoutMeasured.Reset();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(150, 50, 100, 150), realizationRects.Last());
				realizationRects.Clear();
			});
		}

		[TestMethod]
		public async Task ValidateOneScrollViewerScenarioAsync()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport is not available below RS5");
				return;
			}

			var realizationRects = new List<Rect>();
			ScrollViewer scrollViewer = null;
			var viewChangeCompletedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				var repeater = new ItemsRepeater()
				{
					Layout = GetMonitoringLayout(new Size(500, 600), realizationRects),
					HorizontalCacheLength = 0.0,
					VerticalCacheLength = 0.0
				};

				scrollViewer = new ScrollViewer
				{
					Content = repeater,
					Width = 200,
					Height = 300
				};

				Content = scrollViewer;
				Content.UpdateLayout();

				Verify.AreEqual(2, realizationRects.Count);
				Verify.AreEqual(new Rect(0, 0, 0, 0), realizationRects[0]);
				Verify.AreEqual(new Rect(0, 0, 200, 300), realizationRects[1]);
				realizationRects.Clear();

				scrollViewer.ViewChanged += (Object sender, ScrollViewerViewChangedEventArgs args) =>
				{
					if (!args.IsIntermediate)
					{
						viewChangeCompletedEvent.Set();
					}
				};
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				scrollViewer.ChangeView(0.0, 100.0, null, true);
			});
			Verify.IsTrue(await viewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(0, 100, 200, 300), realizationRects.Last());
				realizationRects.Clear();

				viewChangeCompletedEvent.Reset();
				scrollViewer.ChangeView(null, null, 2.0f, true);
			});
			Verify.IsTrue(await viewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(
					new Rect(0, 50, 100, 150),
					realizationRects.Last());
				realizationRects.Clear();
			});
		}

		[TestMethod]
		public async Task ValidateTwoScrollViewerScenarioAsync()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport is not available below RS5");
				return;
			}

			var realizationRects = new List<Rect>();
			ScrollViewer horizontalScroller = null;
			ScrollViewer verticalScroller = null;
			var horizontalViewChangeCompletedEvent = new UnoAutoResetEvent(false);
			var verticalViewChangeCompletedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				var repeater = new ItemsRepeater()
				{
					Layout = GetMonitoringLayout(new Size(500, 500), realizationRects),
					HorizontalCacheLength = 0.0,
					VerticalCacheLength = 0.0
				};

				horizontalScroller = new ScrollViewer
				{
					Content = repeater,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
					VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
					HorizontalScrollMode = ScrollMode.Enabled,
					VerticalScrollMode = ScrollMode.Disabled
				};

				verticalScroller = new ScrollViewer
				{
					Content = horizontalScroller,
					Width = 200,
					Height = 200
				};

				Content = verticalScroller;
				Content.UpdateLayout();

				Verify.AreEqual(2, realizationRects.Count);
				Verify.AreEqual(new Rect(0, 0, 0, 0), realizationRects[0]);
				Verify.AreEqual(new Rect(0, 0, 200, 200), realizationRects[1]);
				realizationRects.Clear();

				horizontalScroller.ViewChanged += (Object sender, ScrollViewerViewChangedEventArgs args) =>
				{
					if (!args.IsIntermediate)
					{
						horizontalViewChangeCompletedEvent.Set();
					}
				};

				verticalScroller.ViewChanged += (Object sender, ScrollViewerViewChangedEventArgs args) =>
				{
					if (!args.IsIntermediate)
					{
						verticalViewChangeCompletedEvent.Set();
					}
				};
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				verticalScroller.ChangeView(0.0, 100.0, null, true);
			});
			Verify.IsTrue(await verticalViewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(0, 100, 200, 200), realizationRects.Last());
				realizationRects.Clear();

				// Max viewport offset is (300, 300). Horizontal viewport offset
				// is expected to get coerced from 400 to 300.
				horizontalScroller.ChangeView(400.0, 100.0, null, true);
			});
			Verify.IsTrue(await horizontalViewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(300, 100, 200, 200), realizationRects.Last());
				realizationRects.Clear();
			});
		}

		[TestMethod]
		public async Task CanGrowCacheBufferWithScrollViewerAsync()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport is not available below RS5");
				return;
			}

			ScrollViewer scroller = null;
			ItemsRepeater repeater = null;
			var measureRealizationRects = new List<Rect>();
			var arrangeRealizationRects = new List<Rect>();
			var fullCacheEvent = new UnoManualResetEvent(initialState: false);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Preparing the visual tree...");

				scroller = new ScrollViewer
				{
					Width = 400,
					Height = 400
				};

				var layout = new MockVirtualizingLayout
				{
					MeasureLayoutFunc = (availableSize, context) =>
					{
						var ctx = (VirtualizingLayoutContext)context;
						Log.Comment("MeasureLayout - Rect:" + ctx.RealizationRect);
						if (measureRealizationRects.Count == 0 || measureRealizationRects.Last() != ctx.RealizationRect)
						{
							measureRealizationRects.Add(ctx.RealizationRect);
						}

						return new Size(1000, 2000);
					},

					ArrangeLayoutFunc = (finalSize, context) =>
					{
						var ctx = (VirtualizingLayoutContext)context;
						Log.Comment("ArrangeLayout - Rect:" + ctx.RealizationRect);
						if (arrangeRealizationRects.Count == 0 || arrangeRealizationRects.Last() != ctx.RealizationRect)
						{
							arrangeRealizationRects.Add(ctx.RealizationRect);
						}

						if (ctx.RealizationRect.Height == scroller.Height * (repeater.VerticalCacheLength + 1))
						{
							fullCacheEvent.Set();
						}

						return finalSize;
					}
				};

				repeater = new ItemsRepeater()
				{
					Layout = layout
				};

				scroller.Content = repeater;
				Content = scroller;
			});

			if (!await fullCacheEvent.WaitOne(DefaultWaitTimeInMS)) Verify.Fail("Cache full size never reached.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var cacheLength = repeater.VerticalCacheLength;
				var expectedRealizationWindow = new Rect(
					-cacheLength / 2 * scroller.Width,
					-cacheLength / 2 * scroller.Height,
					(1 + cacheLength) * scroller.Width,
					(1 + cacheLength) * scroller.Height);

				Log.Comment("Validate that the realization window reached full size.");
				Verify.AreEqual(expectedRealizationWindow, measureRealizationRects.Last());
				Verify.AreEqual(expectedRealizationWindow, arrangeRealizationRects.Last());

				Log.Comment("Validate that the realization window grew by 40 pixels each time during the process.");
				for (int i = 2; i < measureRealizationRects.Count; ++i)
				{
					Verify.AreEqual(-40, measureRealizationRects[i].X - measureRealizationRects[i - 1].X);
					Verify.AreEqual(-40, measureRealizationRects[i].Y - measureRealizationRects[i - 1].Y);
					Verify.AreEqual(80, measureRealizationRects[i].Width - measureRealizationRects[i - 1].Width);
					Verify.AreEqual(80, measureRealizationRects[i].Height - measureRealizationRects[i - 1].Height);

					Verify.AreEqual(-40, arrangeRealizationRects[i].X - arrangeRealizationRects[i - 1].X);
					Verify.AreEqual(-40, arrangeRealizationRects[i].Y - arrangeRealizationRects[i - 1].Y);
					Verify.AreEqual(80, arrangeRealizationRects[i].Width - arrangeRealizationRects[i - 1].Width);
					Verify.AreEqual(80, arrangeRealizationRects[i].Height - arrangeRealizationRects[i - 1].Height);
				}
			});
		}

		[TestMethod]
		public async Task CanBringIntoViewElementsAsync()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport is not available below RS5");
				return;
			}

			if (!PlatformConfiguration.IsOsVersionGreaterThan(OSVersion.Redstone3))
			{
				Log.Warning("Skipping CanBringIntoViewElements because UIElement.BringIntoViewRequested was added in RS4.");
				return;
			}

			ScrollViewer scroller = null;
			ItemsRepeater repeater = null;
			var rootLoadedEvent = new UnoAutoResetEvent(initialState: false);
			var effectiveViewChangeCompletedEvent = new UnoAutoResetEvent(initialState: false);
			var viewChangeCompletedEvent = new UnoAutoResetEvent(initialState: false);

			var viewChangedOffsets = new List<double>();

			RunOnUIThread.Execute(() =>
			{
				var lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus.";
				var root = (Grid)XamlReader.Load(TestUtilities.ProcessTestXamlForRepo(
					 @"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:controls='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls'> 
						 <Grid.Resources>
						   <controls:StackLayout x:Name='VerticalStackLayout' />
						   <controls:RecyclingElementFactory x:Key='ElementFactory'>
							 <controls:RecyclingElementFactory.RecyclePool>
							   <controls:RecyclePool />
							 </controls:RecyclingElementFactory.RecyclePool>
							 <DataTemplate x:Key='ItemTemplate'>
							   <Border Background='LightGray' Margin ='5'>
								 <TextBlock Text='{Binding}' TextWrapping='WrapWholeWords' />
							   </Border>
							 </DataTemplate>
						   </controls:RecyclingElementFactory>
						 </Grid.Resources>
						 <ScrollViewer x:Name='Scroller' Width='400' Height='600' Background='Gray'>
						   <controls:ItemsRepeater
							 x:Name='ItemsRepeater'
							 ItemTemplate='{StaticResource ElementFactory}'
							 Layout='{StaticResource VerticalStackLayout}'
							 HorizontalCacheLength='0'
							 VerticalCacheLength='0' />
						 </ScrollViewer>
					   </Grid>"));

				var elementFactory = (RecyclingElementFactory)root.Resources["ElementFactory"];
				scroller = (ScrollViewer)root.FindName("Scroller");
				repeater = (ItemsRepeater)root.FindName("ItemsRepeater");

				var items = Enumerable.Range(0, 400).Select(i => string.Format("{0}: {1}", i, lorem.Substring(0, 250)));

				repeater.ItemsSource = items;

				scroller.ViewChanged += (o, e) =>
				{
					Log.Comment("ViewChanged: " + scroller.VerticalOffset);
					viewChangedOffsets.Add(scroller.VerticalOffset);
					if (!e.IsIntermediate)
					{
						viewChangeCompletedEvent.Set();
					}
				};

				scroller.EffectiveViewportChanged += (o, args) =>
				{
					effectiveViewChangeCompletedEvent.Set();
				};

				Content = root;

				root.Loaded += delegate
				{
					rootLoadedEvent.Set();
				};
			});
			Verify.IsTrue(await rootLoadedEvent.WaitOne(DefaultWaitTimeInMS));
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				repeater.GetOrCreateElement(100).StartBringIntoView();
				repeater.UpdateLayout();
			});

			Verify.IsTrue(await viewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.AreEqual(1, viewChangedOffsets.Count);
			viewChangedOffsets.Clear();

			ValidateRealizedRange(repeater, 99, 106);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Scroll into view item 105 (already realized) w/ animation.");
				repeater.TryGetElement(105).StartBringIntoView(new BringIntoViewOptions
				{
					VerticalAlignmentRatio = 0.5,
					AnimationDesired = true
				});
				repeater.UpdateLayout();
			});

			Verify.IsTrue(await viewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsLessThanOrEqual(1, viewChangedOffsets.Count);
			viewChangedOffsets.Clear();
			ValidateRealizedRange(repeater, 101, 109);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Scroll item 0 to the top w/ animation and 0.5 vertical alignment.");
				repeater.GetOrCreateElement(0).StartBringIntoView(new BringIntoViewOptions
				{
					VerticalAlignmentRatio = 0.5,
					AnimationDesired = true
				});
				repeater.UpdateLayout();
			});

			Verify.IsTrue(await viewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			await TestServices.WindowHelper.WaitForIdle();
			viewChangedOffsets.Clear();
			ValidateRealizedRange(repeater, 0, 6);

			RunOnUIThread.Execute(() =>
			{
				// You can't align the first group in the middle obviously.
				Verify.AreEqual(0, scroller.VerticalOffset);

				Log.Comment("Scroll to item 20.");
				repeater.GetOrCreateElement(20).StartBringIntoView(new BringIntoViewOptions
				{
					VerticalAlignmentRatio = 0.0
				});
				repeater.UpdateLayout();
			});

			Verify.IsTrue(await viewChangeCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			await TestServices.WindowHelper.WaitForIdle();
			ValidateRealizedRange(repeater, 19, 26);
		}

		private void ValidateRealizedRange(
			ItemsRepeater repeater,
			int expectedFirstItemIndex,
			int expectedLastItemIndex)
		{
			Log.Comment("Validating Realized Range...");
			int actualFirstItemIndex = -1;
			int actualLastItemIndex = -1;
			int itemIndex = 0;
			RunOnUIThread.Execute(() =>
			{
				var items = repeater.ItemsSource as IEnumerable<string>;
				foreach (var item in items)
				{
					var itemElement = repeater.TryGetElement(itemIndex);

					if (itemElement != null)
					{
						actualFirstItemIndex =
							actualFirstItemIndex == -1 ?
							itemIndex :
							actualFirstItemIndex;
						actualLastItemIndex = itemIndex;
					}

					++itemIndex;
				}
			});

			Log.Comment(string.Format("FirstItemIndex	  - {0}   {1}", expectedFirstItemIndex, actualFirstItemIndex));
			Log.Comment(string.Format("LastItemIndex	   - {0}   {1}", expectedLastItemIndex, actualLastItemIndex));
			Verify.AreEqual(expectedFirstItemIndex, actualFirstItemIndex);
			Verify.AreEqual(expectedLastItemIndex, actualLastItemIndex);
		}

		private static VirtualizingLayout GetMonitoringLayout(Size desiredSize, List<Rect> realizationRects, UnoManualResetEvent layoutMeasured = null)
		{
			return new MockVirtualizingLayout
			{
				MeasureLayoutFunc = (availableSize, context) =>
				{
					var ctx = (VirtualizingLayoutContext)context;
					Log.Comment("MeasureLayout:" + ctx.RealizationRect);
					realizationRects.Add(ctx.RealizationRect);
					if (layoutMeasured != null)
					{
						layoutMeasured.Set();
					}
					return desiredSize;
				},

				ArrangeLayoutFunc = (finalSize, context) => finalSize
			};
		}
	}
}
