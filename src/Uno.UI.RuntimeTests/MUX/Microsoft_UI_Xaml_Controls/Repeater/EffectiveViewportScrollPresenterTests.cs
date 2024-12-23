#if false

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
using UniformGridLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.UniformGridLayout;
using AnimationMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.AnimationMode;
using SnapPointsMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SnapPointsMode;
using ScrollingScrollOptions = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ScrollingScrollOptions;
using ScrollingZoomOptions = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ScrollingZoomOptions;
using ScrollPresenter = Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.ScrollPresenter;
using ScrollingScrollCompletedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ScrollingScrollCompletedEventArgs;
using ScrollingZoomCompletedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ScrollingZoomCompletedEventArgs;
using ContentOrientation = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ContentOrientation;
using IRepeaterScrollingSurface = Microsoft.UI.Private.Controls.IRepeaterScrollingSurface;
using ConfigurationChangedEventHandler = Microsoft.UI.Private.Controls.ConfigurationChangedEventHandler;
using PostArrangeEventHandler = Microsoft.UI.Private.Controls.PostArrangeEventHandler;
using ViewportChangedEventHandler = Microsoft.UI.Private.Controls.ViewportChangedEventHandler;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	public class EffectiveViewportScrollPresenterTests : MUXApiTestBase
	{
		[TestMethod]
		public void ValidateOneScrollPresenterScenario()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport feature is not available below RS5");
				return;
			}

			var realizationRects = new List<Rect>();
			ScrollPresenter scrollPresenter = null;
			var scrollCompletedEvent = new AutoResetEvent(false);
			var zoomCompletedEvent = new AutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				var repeater = new ItemsRepeater()
				{
					Layout = GetMonitoringLayout(new Size(500, 600), realizationRects),
					HorizontalCacheLength = 0.0,
					VerticalCacheLength = 0.0
				};

				scrollPresenter = new ScrollPresenter
				{
					Content = repeater,
					Width = 200,
					Height = 300
				};

				Content = scrollPresenter;
				Content.UpdateLayout();

				Verify.AreEqual(2, realizationRects.Count);
				Verify.AreEqual(new Rect(0, 0, 0, 0), realizationRects[0]);
				Verify.AreEqual(new Rect(0, 0, 200, 300), realizationRects[1]);
				realizationRects.Clear();

				scrollPresenter.ScrollCompleted += (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args) =>
				{
					scrollCompletedEvent.Set();
				};

				scrollPresenter.ZoomCompleted += (ScrollPresenter sender, ScrollingZoomCompletedEventArgs args) =>
				{
					zoomCompletedEvent.Set();
				};
			});
			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter.ScrollTo(0.0, 100.0, new ScrollingScrollOptions(AnimationMode.Disabled, SnapPointsMode.Ignore));
			});
			Verify.IsTrue(scrollCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			CompositionPropertySpy.SynchronouslyTickUIThread(1);

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(0, 100, 200, 300), realizationRects.Last());
				realizationRects.Clear();

				scrollPresenter.ZoomTo(2.0f, Vector2.Zero, new ScrollingZoomOptions(AnimationMode.Disabled, SnapPointsMode.Ignore));
			});
			Verify.IsTrue(zoomCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			CompositionPropertySpy.SynchronouslyTickUIThread(1);

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(
					new Rect(0, 100, 100, 150),
					realizationRects.Last());
				realizationRects.Clear();
			});
		}

		[TestMethod]
		public void ValidateTwoScrollPresentersScenario()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport feature is not available below RS5");
				return;
			}

			var realizationRects = new List<Rect>();
			ScrollPresenter horizontalScrollPresenter = null;
			ScrollPresenter verticalScrollPresenter = null;
			var horizontalScrollCompletedEvent = new AutoResetEvent(false);
			var verticalScrollCompletedEvent = new AutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				var repeater = new ItemsRepeater()
				{
					Layout = GetMonitoringLayout(new Size(500, 500), realizationRects),
					HorizontalCacheLength = 0.0,
					VerticalCacheLength = 0.0
				};

				horizontalScrollPresenter = new ScrollPresenter
				{
					Content = repeater,
					ContentOrientation = ContentOrientation.Horizontal
				};

				// Placing a Grid in between two ScrollPresenter controls to avoid
				// unsupported combined use of facades and ElementCompositionPreview.
				var grid = new Grid();
				grid.Children.Add(horizontalScrollPresenter);

				verticalScrollPresenter = new ScrollPresenter
				{
					Content = grid,
					Width = 200,
					Height = 200,
					ContentOrientation = ContentOrientation.Vertical
				};

				Content = verticalScrollPresenter;
				Content.UpdateLayout();

				Verify.AreEqual(2, realizationRects.Count);
				Verify.AreEqual(new Rect(0, 0, 0, 0), realizationRects[0]);
				Verify.AreEqual(new Rect(0, 0, 200, 200), realizationRects[1]);
				realizationRects.Clear();

				horizontalScrollPresenter.ScrollCompleted += (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args) =>
				{
					horizontalScrollCompletedEvent.Set();
				};

				verticalScrollPresenter.ScrollCompleted += (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args) =>
				{
					verticalScrollCompletedEvent.Set();
				};
			});
			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				verticalScrollPresenter.ScrollTo(0.0, 100.0, new ScrollingScrollOptions(AnimationMode.Disabled, SnapPointsMode.Ignore));
			});
			Verify.IsTrue(verticalScrollCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			CompositionPropertySpy.SynchronouslyTickUIThread(1);

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(0, 100, 200, 200), realizationRects.Last());
				realizationRects.Clear();

				// Max viewport offset is (300, 300). Horizontal viewport offset
				// is expected to get coerced from 400 to 300.
				horizontalScrollPresenter.ScrollTo(400.0, 100.0, new ScrollingScrollOptions(AnimationMode.Disabled, SnapPointsMode.Ignore));
			});
			Verify.IsTrue(horizontalScrollCompletedEvent.WaitOne(DefaultWaitTimeInMS));
			CompositionPropertySpy.SynchronouslyTickUIThread(1);

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(new Rect(300, 100, 200, 200), realizationRects.Last());
				realizationRects.Clear();
			});
		}

		[TestMethod]
		public void CanGrowCacheBuffer()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				Log.Warning("Skipping since version is less than RS5 and effective viewport feature is not available below RS5");
				return;
			}

			ScrollPresenter scrollPresenter = null;
			ItemsRepeater repeater = null;
			var measureRealizationRects = new List<Rect>();
			var arrangeRealizationRects = new List<Rect>();
			var fullCacheEvent = new ManualResetEvent(initialState: false);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Preparing the visual tree...");

				scrollPresenter = new ScrollPresenter
				{
					Width = 400,
					Height = 400
				};

				var layout = new MockVirtualizingLayout
				{
					MeasureLayoutFunc = (availableSize, context) =>
					{
						var ctx = (VirtualizingLayoutContext)context;
						measureRealizationRects.Add(ctx.RealizationRect);
						return new Size(1000, 2000);
					},

					ArrangeLayoutFunc = (finalSize, context) =>
					{
						var ctx = (VirtualizingLayoutContext)context;
						arrangeRealizationRects.Add(ctx.RealizationRect);

						if (ctx.RealizationRect.Height == scrollPresenter.Height * (repeater.VerticalCacheLength + 1))
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

				scrollPresenter.Content = repeater;
				Content = scrollPresenter;
			});

			if (!fullCacheEvent.WaitOne(500000)) Verify.Fail("Cache full size never reached.");
			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				var cacheLength = repeater.VerticalCacheLength;
				var expectedRealizationWindow = new Rect(
					-cacheLength / 2 * scrollPresenter.Width,
					-cacheLength / 2 * scrollPresenter.Height,
					(1 + cacheLength) * scrollPresenter.Width,
					(1 + cacheLength) * scrollPresenter.Height);

				Log.Comment("Validate that the realization window reached full size.");
				Verify.AreEqual(expectedRealizationWindow, measureRealizationRects.Last());

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
		public void CanBringIntoViewElements()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
			{
				// Note that UIElement.BringIntoViewRequested was added in RS4, and effective viewport was added in RS5
				Log.Warning("Skipping since version is less than RS5 and effective viewport feature is not available below RS5");
				return;
			}

			ScrollPresenter scrollPresenter = null;
			ItemsRepeater repeater = null;
			var rootLoadedEvent = new AutoResetEvent(initialState: false);
			var viewChangedEvent = new AutoResetEvent(initialState: false);
			var waitingForIndex = -1;
			var indexRealized = new AutoResetEvent(initialState: false);

			var viewChangedOffsets = new List<double>();

			RunOnUIThread.Execute(() =>
			{
				var lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus.";
				var root = (Grid)XamlReader.Load(TestUtilities.ProcessTestXamlForRepo(
					 @"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
							 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
							 xmlns:controls='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls' 
							 xmlns:primitives='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls.Primitives'> 
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
						 <primitives:ScrollPresenter x:Name='ScrollPresenter' Width='400' Height='600' ContentOrientation='Vertical' Background='Gray'>
						   <controls:ItemsRepeater
							 x:Name='ItemsRepeater'
							 ItemTemplate='{StaticResource ElementFactory}'
							 Layout='{StaticResource VerticalStackLayout}'
							 HorizontalCacheLength='0'
							 VerticalCacheLength='0' />
						 </primitives:ScrollPresenter>
					   </Grid>"));

				var elementFactory = (RecyclingElementFactory)root.Resources["ElementFactory"];
				scrollPresenter = (ScrollPresenter)root.FindName("ScrollPresenter");
				repeater = (ItemsRepeater)root.FindName("ItemsRepeater");

				repeater.ElementPrepared += (sender, args) =>
				{
					Log.Comment($"Realized index: {args.Index} Wating for index {waitingForIndex}");
					if (args.Index == waitingForIndex)
					{
						indexRealized.Set();
					}
				};

				var items = Enumerable.Range(0, 400).Select(i => string.Format("{0}: {1}", i, lorem.Substring(0, 250)));

				repeater.ItemsSource = items;

				scrollPresenter.ViewChanged += (o, e) =>
				{
					Log.Comment("ScrollPresenter.ViewChanged: VerticalOffset=" + scrollPresenter.VerticalOffset);
					viewChangedOffsets.Add(scrollPresenter.VerticalOffset);
					viewChangedEvent.Set();
				};

				scrollPresenter.BringingIntoView += (o, e) =>
				{
					Log.Comment("ScrollPresenter.BringingIntoView:");
					Log.Comment("TargetVerticalOffset=" + e.TargetVerticalOffset);
					Log.Comment("RequestEventArgs.AnimationDesired=" + e.RequestEventArgs.AnimationDesired);
					Log.Comment("RequestEventArgs.Handled=" + e.RequestEventArgs.Handled);
					Log.Comment("RequestEventArgs.VerticalAlignmentRatio=" + e.RequestEventArgs.VerticalAlignmentRatio);
					Log.Comment("RequestEventArgs.VerticalOffset=" + e.RequestEventArgs.VerticalOffset);
					Log.Comment("RequestEventArgs.TargetRect=" + e.RequestEventArgs.TargetRect);
				};

				scrollPresenter.EffectiveViewportChanged += (o, args) =>
				{
					Log.Comment("ScrollPresenter.EffectiveViewportChanged: VerticalOffset=" + scrollPresenter.VerticalOffset);
				};

				root.Loaded += delegate
				{
					rootLoadedEvent.Set();
				};

				Content = root;
			});
			Verify.IsTrue(rootLoadedEvent.WaitOne(DefaultWaitTimeInMS));
			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				waitingForIndex = 101;
				indexRealized.Reset();
				repeater.GetOrCreateElement(100).StartBringIntoView(new BringIntoViewOptions
				{
					VerticalAlignmentRatio = 0.0
				});
				repeater.UpdateLayout();
			});

			Verify.IsTrue(viewChangedEvent.WaitOne(DefaultWaitTimeInMS));
			IdleSynchronizer.Wait();
			Verify.AreEqual(1, viewChangedOffsets.Count);
			viewChangedOffsets.Clear();
			Verify.IsTrue(indexRealized.WaitOne(DefaultWaitTimeInMS));

			ValidateRealizedRange(repeater, 99, 106);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Scroll into view item 105 (already realized) w/ animation.");
				waitingForIndex = 99;
				repeater.TryGetElement(105).StartBringIntoView(new BringIntoViewOptions
				{
					VerticalAlignmentRatio = 0.5,
					AnimationDesired = true
				});
				repeater.UpdateLayout();
			});

			Verify.IsTrue(viewChangedEvent.WaitOne(DefaultWaitTimeInMS));
			IdleSynchronizer.Wait();
			Verify.IsLessThanOrEqual(1, viewChangedOffsets.Count);
			viewChangedOffsets.Clear();
			ValidateRealizedRange(repeater, 101, 109);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Scroll item 0 to the top w/ animation and 0.5 vertical alignment.");
				waitingForIndex = 1;
				indexRealized.Reset();
				repeater.GetOrCreateElement(0).StartBringIntoView(new BringIntoViewOptions
				{
					VerticalAlignmentRatio = 0.5,
					AnimationDesired = true
				});
			});

			Verify.IsTrue(viewChangedEvent.WaitOne(DefaultWaitTimeInMS));
			IdleSynchronizer.Wait();
			viewChangedOffsets.Clear();
			Verify.IsTrue(indexRealized.WaitOne(DefaultWaitTimeInMS));

			// Test Reliability fix. If offset is not 0 yet, give 
			// some more time for the animation to settle down.
			double verticalOffset = 0;
			RunOnUIThread.Execute(() =>
			{
				verticalOffset = scrollPresenter.VerticalOffset;
			});

			if (verticalOffset != 0)
			{
				Verify.IsTrue(viewChangedEvent.WaitOne(DefaultWaitTimeInMS));
				IdleSynchronizer.Wait();
				viewChangedOffsets.Clear();
			}

			ValidateRealizedRange(repeater, 0, 6);

			RunOnUIThread.Execute(() =>
			{
				// You can't align the first group in the middle obviously.
				Verify.AreEqual(0, scrollPresenter.VerticalOffset);

				Log.Comment("Scroll to item 20.");
				waitingForIndex = 21;
				indexRealized.Reset();
				repeater.GetOrCreateElement(20).StartBringIntoView(new BringIntoViewOptions
				{
					VerticalAlignmentRatio = 0.0
				});
				repeater.UpdateLayout();
			});

			Verify.IsTrue(viewChangedEvent.WaitOne(DefaultWaitTimeInMS));
			IdleSynchronizer.Wait();
			Verify.IsTrue(indexRealized.WaitOne(DefaultWaitTimeInMS));

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

		private static VirtualizingLayout GetMonitoringLayout(Size desiredSize, List<Rect> realizationRects)
		{
			return new MockVirtualizingLayout
			{
				MeasureLayoutFunc = (availableSize, context) =>
				{
					var ctx = (VirtualizingLayoutContext)context;
					realizationRects.Add(ctx.RealizationRect);
					return desiredSize;
				},

				ArrangeLayoutFunc = (finalSize, context) => finalSize
			};
		}
	}
}
#endif
