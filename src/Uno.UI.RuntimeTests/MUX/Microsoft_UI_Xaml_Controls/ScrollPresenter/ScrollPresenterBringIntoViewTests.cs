// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft.UI.Private.Controls;
using MUXControlsTestApp.Utilities;
using Windows.Foundation;
using Windows.UI;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

// These are failing due to missing InteractionTracker's features (zoom, custom animation)
#if !HAS_UNO

partial class ScrollPresenterTests : MUXApiTestBase
{
	private const double c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize = 600.0;
	private const double c_defaultBringIntoViewUIScrollPresenterConstrainedSize = 300.0;
	private const int c_defaultBringIntoViewUIStackPanelChildrenCount = 16;

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view.")]
	public void BringElementIntoHorizontalScrollPresenterViewFromNearEdge()
	{
		BringElementIntoView(Orientation.Horizontal, 1083.0 /*expectedHorizontalOffset*/, 0.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view, with snap points.")]
	public void BringElementIntoHorizontalScrollPresenterViewWithSnapPoints()
	{
		BringElementIntoView(
			orientation: Orientation.Horizontal,
			expectedHorizontalOffset: 1134.0,
			expectedVerticalOffset: 0.0,
			options: null,
			originalHorizontalOffset: 0.0,
			originalVerticalOffset: 0.0,
			originalZoomFactor: 1.0f,
			applyOptionsInBringingIntoViewHandler: false,
			applySnapPointsInBringingIntoViewHandler: true);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view.")]
	public void BringElementIntoVerticalScrollPresenterViewFromNearEdge()
	{
		BringElementIntoView(Orientation.Vertical, 0.0 /*expectedHorizontalOffset*/, 1083.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view, with snap points.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public void BringElementIntoVerticalScrollPresenterViewWithSnapPoints()
	{
		BringElementIntoView(
			orientation: Orientation.Vertical,
			expectedHorizontalOffset: 0.0,
			expectedVerticalOffset: 1134.0,
			options: null,
			originalHorizontalOffset: 0.0,
			originalVerticalOffset: 0.0,
			originalZoomFactor: 1.0f,
			applyOptionsInBringingIntoViewHandler: false,
			applySnapPointsInBringingIntoViewHandler: true);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view, starting from the maximum offset.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public void BringElementIntoHorizontalScrollPresenterViewFromFarEdge()
	{
		BringElementIntoView(
			Orientation.Horizontal,
			3126.0 /*expectedHorizontalOffset*/,
			130.0 /*expectedVerticalOffset*/,
			null /*options*/,
			3624.0 /*originalHorizontalOffset*/,
			0.0 /*originalVerticalOffset*/,
			2.0f /*originalZoomFactor*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view, starting from the maximum offset.")]
	public void BringElementIntoVerticalScrollPresenterViewFromFarEdge()
	{
		BringElementIntoView(
			Orientation.Vertical,
			130.0 /*expectedHorizontalOffset*/,
			3126.0 /*expectedVerticalOffset*/,
			null /*options*/,
			0.0 /*originalHorizontalOffset*/,
			3624.0 /*originalVerticalOffset*/,
			2.0f /*originalZoomFactor*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view, with left alignment.")]
	public void BringElementIntoHorizontalScrollPresenterViewWithNearAlignment()
	{
		BringElementIntoViewWithAlignment(
			Orientation.Horizontal,
			0.0 /*alignmentRatio*/,
			1512.0 /*expectedHorizontalOffset*/,
			0.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view, with center alignment.")]
	public void BringElementIntoHorizontalScrollPresenterViewWithMiddleAlignment()
	{
		BringElementIntoViewWithAlignment(
			Orientation.Horizontal,
			0.5 /*alignmentRatio*/,
			1323.0 /*expectedHorizontalOffset*/,
			0.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view, with right alignment.")]
	public void BringElementIntoHorizontalScrollPresenterViewWithFarAlignment()
	{
		BringElementIntoViewWithAlignment(
			Orientation.Horizontal,
			1.0 /*alignmentRatio*/,
			1083.0 /*expectedHorizontalOffset*/,
			0.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view, with left alignment.")]
	public void BringElementIntoVerticalScrollPresenterViewWithNearAlignment()
	{
		BringElementIntoViewWithAlignment(
			Orientation.Vertical,
			0.0 /*alignmentRatio*/,
			0.0 /*expectedHorizontalOffset*/,
			1512.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view, with center alignment.")]
	public void BringElementIntoVerticalScrollPresenterViewWithMiddleAlignment()
	{
		BringElementIntoViewWithAlignment(
			Orientation.Vertical,
			0.5 /*alignmentRatio*/,
			0.0 /*expectedHorizontalOffset*/,
			1323.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view, with right alignment.")]
	public void BringElementIntoVerticalScrollPresenterViewWithFarAlignment()
	{
		BringElementIntoViewWithAlignment(
			Orientation.Vertical,
			1.0 /*alignmentRatio*/,
			0.0 /*expectedHorizontalOffset*/,
			1083.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view, with a shift.")]
	public void BringElementIntoHorizontalScrollPresenterViewWithOffset()
	{
		BringElementIntoViewWithOffset(
			Orientation.Horizontal,
			10.0 /*offset*/,
			1073.0 /*expectedHorizontalOffset*/,
			0.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view, with a shift.")]
	public void BringElementIntoVerticalScrollPresenterViewWithOffset()
	{
		BringElementIntoViewWithOffset(
			Orientation.Vertical,
			-10.0 /*offset*/,
			0.0 /*expectedHorizontalOffset*/,
			1093.0 /*expectedVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a horizontal ScrollPresenter into view, with a shift, starting from the maximum offset.")]
	public void BringElementIntoHorizontalScrollPresenterViewWithOffsetFromFarEdge()
	{
		BringElementIntoViewWithOffset(
			Orientation.Horizontal,
			10.0 /*offset*/,
			3116.0 /*expectedHorizontalOffset*/,
			130.0 /*expectedVerticalOffset*/,
			3624.0 /*originalHorizontalOffset*/,
			0.0 /*originalVerticalOffset*/,
			2.0f /*originalZoomFactor*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a vertical ScrollPresenter into view, with a shift, starting from the maximum offset.")]
	public void BringElementIntoVerticalScrollPresenterViewWithOffsetFromFarEdge()
	{
		BringElementIntoViewWithOffset(
			Orientation.Vertical,
			-10.0 /*offset*/,
			130.0 /*expectedHorizontalOffset*/,
			3136.0 /*expectedVerticalOffset*/,
			0.0 /*originalHorizontalOffset*/,
			3624.0 /*originalVerticalOffset*/,
			2.0f /*originalZoomFactor*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a ScrollPresenter into view, with a TargetRect.")]
	public void BringElementIntoViewWithTargetRect()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions();
			options.TargetRect = new Rect(15, 0, 50, 100);
		});

		BringElementIntoView(
			Orientation.Horizontal,
			1028.0 /*expectedHorizontalOffset*/,
			0.0 /*expectedVerticalOffset*/,
			options);
	}

	[TestMethod]
	[TestProperty("Description", "Brings an element within a ScrollPresenter into view, with a TargetRect and VerticalOffset.")]
	public void BringElementIntoViewWithAdjustmentInBringingIntoViewHandler()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions();
			options.TargetRect = new Rect(0, -15, 100, 50);
			options.AnimationDesired = false;
			options.VerticalOffset = -10.0;
		});

		BringElementIntoView(
			Orientation.Vertical,
			0.0 /*expectedHorizontalOffset*/,
			1008.0 /*expectedVerticalOffset*/,
			options,
			0.0 /*originalHorizontalOffset*/,
			0.0 /*originalVerticalOffset*/,
			1.0f /*originalZoomFactor*/,
			true /*applyOptionsInBringingIntoViewHandler*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollPresenters into view.")]
	public void BringNestedElementIntoHorizontalScrollPresenterView()
	{
		BringElementInNestedScrollPresentersIntoView(
			Orientation.Horizontal,
			1056.0 /*expectedOuterHorizontalOffset*/,
			0.0 /*expectedOuterVerticalOffset*/,
			1083.0 /*expectedInnerHorizontalOffset*/,
			0.0 /*expectedInnerVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside vertical ScrollPresenters into view.")]
	public void BringNestedElementIntoVerticalScrollPresenterView()
	{
		BringElementInNestedScrollPresentersIntoView(
			Orientation.Vertical,
			0.0 /*expectedOuterHorizontalOffset*/,
			1056.0 /*expectedOuterVerticalOffset*/,
			0.0 /*expectedInnerHorizontalOffset*/,
			1083.0 /*expectedInnerVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollViewers into view.")]
	public void BringNestedElementIntoHorizontalScrollViewerView()
	{
		BringElementInNestedScrollViewersIntoView(
			Orientation.Horizontal,
			1056.0 /*expectedOuterHorizontalOffset*/,
			0.0 /*expectedOuterVerticalOffset*/,
			1083.0 /*expectedInnerHorizontalOffset*/,
			0.0 /*expectedInnerVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside vertical ScrollViewers into view.")]
	public void BringNestedElementIntoVerticalScrollViewerView()
	{
		BringElementInNestedScrollViewersIntoView(
			Orientation.Vertical,
			0.0 /*expectedOuterHorizontalOffset*/,
			1056.0 /*expectedOuterVerticalOffset*/,
			0.0 /*expectedInnerHorizontalOffset*/,
			1083.0 /*expectedInnerVerticalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollPresenters into view, starting from the maximum offsets.")]
	public void BringNestedElementIntoHorizontalScrollPresenterViewFromFarEdge()
	{
		BringElementInNestedScrollPresentersIntoView(
			Orientation.Horizontal,
			528.0 /*expectedOuterHorizontalOffset*/,
			0.0 /*expectedOuterVerticalOffset*/,
			2344.5 /*expectedInnerHorizontalOffset*/,
			52.5 /*expectedInnerVerticalOffset*/,
			null /*options*/,
			756.0 /*originalOuterHorizontalOffset*/,
			0.0 /*originalOuterVerticalOffset*/,
			0.5f /*originalOuterZoomFactor*/,
			2568.0 /*originalInnerHorizontalOffset*/,
			0.0 /*originalInnerVerticalOffset*/,
			1.5f /*originalInnerZoomFactor*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside vertical ScrollPresenters into view, starting from the maximum offsets.")]
	public void BringNestedElementIntoVerticalScrollPresenterViewFromFarEdge()
	{
		BringElementInNestedScrollPresentersIntoView(
			Orientation.Vertical,
			0.0 /*expectedOuterHorizontalOffset*/,
			528.0 /*expectedOuterVerticalOffset*/,
			52.5 /*expectedInnerHorizontalOffset*/,
			2344.5 /*expectedInnerVerticalOffset*/,
			null /*options*/,
			0.0 /*originalOuterHorizontalOffset*/,
			756.0 /*originalOuterVerticalOffset*/,
			0.5f /*originalOuterZoomFactor*/,
			0.0 /*originalInnerHorizontalOffset*/,
			2568.0 /*originalInnerVerticalOffset*/,
			1.5f /*originalInnerZoomFactor*/);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollPresenters into view, with left alignment.")]
	public void BringNestedElementIntoHorizontalScrollPresenterViewWithNearAlignment()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions() { HorizontalAlignmentRatio = 0.0 };
		});

		BringElementInNestedScrollPresentersIntoView(
			Orientation.Horizontal,
			1107.0 /*expectedOuterHorizontalOffset*/,
			0.0 /*expectedOuterVerticalOffset*/,
			1512.0 /*expectedInnerHorizontalOffset*/,
			0.0 /*expectedInnerVerticalOffset*/,
			options);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollPresenters into view, with snap points.")]
	public void BringNestedElementIntoHorizontalScrollPresenterViewWithSnapPoints()
	{
		BringElementInNestedScrollPresentersIntoView(
			orientation: Orientation.Horizontal,
			expectedOuterHorizontalOffset: 1008.0,
			expectedOuterVerticalOffset: 0.0,
			expectedInnerHorizontalOffset: 1134.0,
			expectedInnerVerticalOffset: 0.0,
			options: null,
			originalOuterHorizontalOffset: 0.0,
			originalOuterVerticalOffset: 0.0,
			originalOuterZoomFactor: 1.0f,
			originalInnerHorizontalOffset: 0.0,
			originalInnerVerticalOffset: 0.0,
			originalInnerZoomFactor: 1.0f,
			applySnapPointsInBringingIntoViewHandler: true);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside vertical ScrollPresenters into view, with center alignment.")]
	public void BringNestedElementIntoVerticalScrollPresenterViewWithMiddleAlignment()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions() { VerticalAlignmentRatio = 0.5 };
		});

		BringElementInNestedScrollPresentersIntoView(
			Orientation.Vertical,
			0.0 /*expectedOuterHorizontalOffset*/,
			1056.0 /*expectedOuterVerticalOffset*/,
			0.0 /*expectedInnerHorizontalOffset*/,
			1323.0 /*expectedInnerVerticalOffset*/,
			options);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollViewers into view, with left alignment.")]
	public void BringNestedElementIntoHorizontalScrollViewerViewWithNearAlignment()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions() { HorizontalAlignmentRatio = 0.0 };
		});

		BringElementInNestedScrollViewersIntoView(
			Orientation.Horizontal,
			1107.0 /*expectedOuterHorizontalOffset*/,
			0.0 /*expectedOuterVerticalOffset*/,
			1512.0 /*expectedInnerHorizontalOffset*/,
			0.0 /*expectedInnerVerticalOffset*/,
			options);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside vertical ScrollViewers into view, with center alignment.")]
	public void BringNestedElementIntoVerticalScrollViewerViewWithMiddleAlignment()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions() { VerticalAlignmentRatio = 0.5 };
		});

		BringElementInNestedScrollViewersIntoView(
			Orientation.Vertical,
			0.0 /*expectedOuterHorizontalOffset*/,
			1056.0 /*expectedOuterVerticalOffset*/,
			0.0 /*expectedInnerHorizontalOffset*/,
			1323.0 /*expectedInnerVerticalOffset*/,
			options);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollPresenters into view, with offset.")]
	public void BringNestedElementIntoHorizontalScrollPresenterViewWithOffset()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			BringIntoViewOptions options = null;

			RunOnUIThread.Execute(() =>
			{
				options = new BringIntoViewOptions() { HorizontalOffset = -20.0 };
			});

			BringElementInNestedScrollPresentersIntoView(
				Orientation.Horizontal,
				1056.0 /*expectedOuterHorizontalOffset*/,
				0.0 /*expectedOuterVerticalOffset*/,
				1103.0 /*expectedInnerHorizontalOffset*/,
				0.0 /*expectedInnerVerticalOffset*/,
				options);
		}
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside vertical ScrollPresenters into view, with offset.")]
	public void BringNestedElementIntoVerticalScrollPresenterViewWithOffset()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			BringIntoViewOptions options = null;

			RunOnUIThread.Execute(() =>
			{
				options = new BringIntoViewOptions() { VerticalOffset = -20.0 };
			});

			BringElementInNestedScrollPresentersIntoView(
				Orientation.Vertical,
				0.0 /*expectedOuterHorizontalOffset*/,
				1056.0 /*expectedOuterVerticalOffset*/,
				0.0 /*expectedInnerHorizontalOffset*/,
				1103.0 /*expectedInnerVerticalOffset*/,
				options);
		}
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside horizontal ScrollPresenters into view, with offset.")]
	public void BringNestedElementIntoHorizontalScrollViewerViewWithOffset()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions() { HorizontalOffset = -20.0 };
		});

		BringElementInNestedScrollViewersIntoView(
			Orientation.Horizontal,
			1056.0 /*expectedOuterHorizontalOffset*/,
			0.0 /*expectedOuterVerticalOffset*/,
			1103.0 /*expectedInnerHorizontalOffset*/,
			0.0 /*expectedInnerVerticalOffset*/,
			options);
	}

	[TestMethod]
	[TestProperty("Description", "Brings a nested element inside vertical ScrollPresenters into view, with offset.")]
	public void BringNestedElementIntoVerticalScrollViewerViewWithOffset()
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions() { VerticalOffset = -20.0 };
		});

		BringElementInNestedScrollViewersIntoView(
			Orientation.Vertical,
			0.0 /*expectedOuterHorizontalOffset*/,
			1056.0 /*expectedOuterVerticalOffset*/,
			0.0 /*expectedInnerHorizontalOffset*/,
			1103.0 /*expectedInnerVerticalOffset*/,
			options);
	}

	[TestMethod]
	[TestProperty("Description", "ScrollPresenter should handle BringIntoView for its direct content.")]
	[Ignore("Fails in Uno, waiting forever on ViewChanged")]
	public void BringContentWithMarginIntoView()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		AutoResetEvent scrollPresenterViewChangedEvent = new AutoResetEvent(false);
		AutoResetEvent scrollPresenterLoadedEvent = new AutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle() { Margin = new Thickness(0, 500, 0, 0) };
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter.ViewChanged += (s, e) =>
			{
				scrollPresenterViewChangedEvent.Set();
			};

			rectangleScrollPresenterContent.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = false });
		});

		WaitForEvent("Waiting for ViewChanged event", scrollPresenterViewChangedEvent);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(500.0, scrollPresenter.VerticalOffset);
		});
	}

	private void BringElementIntoViewWithAlignment(
		Orientation orientation,
		double alignmentRatio,
		double expectedHorizontalOffset,
		double expectedVerticalOffset)
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions();
			if (orientation == Orientation.Horizontal)
				options.HorizontalAlignmentRatio = alignmentRatio;
			else
				options.VerticalAlignmentRatio = alignmentRatio;
		});

		BringElementIntoView(
			orientation,
			expectedHorizontalOffset,
			expectedVerticalOffset,
			options);
	}

	private void BringElementIntoViewWithOffset(
		Orientation orientation,
		double offset,
		double expectedHorizontalOffset,
		double expectedVerticalOffset,
		double originalHorizontalOffset = 0.0,
		double originalVerticalOffset = 0.0,
		float originalZoomFactor = 1.0f)
	{
		BringIntoViewOptions options = null;

		RunOnUIThread.Execute(() =>
		{
			options = new BringIntoViewOptions();
			if (orientation == Orientation.Horizontal)
				options.HorizontalOffset = offset;
			else
				options.VerticalOffset = offset;
		});

		BringElementIntoView(
			orientation,
			expectedHorizontalOffset,
			expectedVerticalOffset,
			options,
			originalHorizontalOffset,
			originalVerticalOffset,
			originalZoomFactor);
	}

	private void BringElementIntoView(
		Orientation orientation,
		double expectedHorizontalOffset,
		double expectedVerticalOffset,
		BringIntoViewOptions options = null,
		double originalHorizontalOffset = 0.0,
		double originalVerticalOffset = 0.0,
		float originalZoomFactor = 1.0f,
		bool applyOptionsInBringingIntoViewHandler = false,
		bool applySnapPointsInBringingIntoViewHandler = false)
	{
		ScrollPresenter scrollPresenter = null;
		AutoResetEvent scrollPresenterLoadedEvent = new AutoResetEvent(false);
		AutoResetEvent scrollPresenterViewChangedEvent = new AutoResetEvent(false);
		AutoResetEvent bringIntoViewCompletedEvent = new AutoResetEvent(false);
		int bringIntoViewChangeCorrelationId = -1;

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter = new ScrollPresenter();

			SetupDefaultBringIntoViewUI(orientation, scrollPresenter, scrollPresenterLoadedEvent);
		});

		WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		if (originalZoomFactor != 1.0f)
		{
			ZoomTo(scrollPresenter, originalZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);
		}

		if (originalHorizontalOffset != 0 || originalVerticalOffset != 0)
		{
			ScrollTo(scrollPresenter, originalHorizontalOffset, originalVerticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, originalZoomFactor == 1.0f /*hookViewChanged*/);
		}

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
			{
				Log.Comment("ViewChanged - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
					sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
				scrollPresenterViewChangedEvent.Set();
			};

			scrollPresenter.ScrollCompleted += delegate (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args)
			{
				ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);
				Log.Comment("ScrollPresenter bring-into-view OffsetsChangeCorrelationId={0} completed with Result={1}", args.CorrelationId, result);
				if (bringIntoViewChangeCorrelationId == args.CorrelationId)
					bringIntoViewCompletedEvent.Set();
			};

			scrollPresenter.BringingIntoView += (ScrollPresenter sender, ScrollingBringingIntoViewEventArgs args) =>
			{
				Log.Comment("ScrollPresenter.BringingIntoView ScrollPresenter={0} - TargetHorizontalOffset={1}, TargetVerticalOffset={2}, OffsetsChangeCorrelationId={3}, SnapPointsMode={4}",
					sender.Name, args.TargetHorizontalOffset, args.TargetVerticalOffset, args.CorrelationId, args.SnapPointsMode);
				bringIntoViewChangeCorrelationId = args.CorrelationId;

				if (applyOptionsInBringingIntoViewHandler && options != null)
				{
					Log.Comment("ScrollPresenter.BringingIntoView - Applying custom options");
					args.RequestEventArgs.AnimationDesired = options.AnimationDesired;
					args.RequestEventArgs.HorizontalOffset = options.HorizontalOffset;
					args.RequestEventArgs.VerticalOffset = options.VerticalOffset;
					if (options.TargetRect != null)
					{
						args.RequestEventArgs.TargetRect = (Rect)options.TargetRect;
					}
				}

				if (applySnapPointsInBringingIntoViewHandler)
				{
					Log.Comment("ScrollPresenter.BringingIntoView - Applying mandatory snap points");
					AddSnapPoints(scrollPresenter: sender, stackPanel: (sender.Content as Border).Child as StackPanel);
					args.SnapPointsMode = ScrollingSnapPointsMode.Default;
				}
			};

			UIElement targetElement = ((scrollPresenter.Content as Border).Child as StackPanel).Children[12];
			BringIntoViewOptions startingOptions = null;

			if (options == null)
			{
				targetElement.StartBringIntoView();
			}
			else
			{
				if (applyOptionsInBringingIntoViewHandler)
				{
					startingOptions = new BringIntoViewOptions();
					startingOptions.HorizontalAlignmentRatio = options.HorizontalAlignmentRatio;
					startingOptions.VerticalAlignmentRatio = options.VerticalAlignmentRatio;
				}
				else
				{
					startingOptions = options;
				}
				targetElement.StartBringIntoView(startingOptions);
			}
		});

		WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);
		WaitForEvent("Waiting for bring-into-view operation completion event", bringIntoViewCompletedEvent);
		IdleSynchronizer.Wait();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final view - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);
			Verify.AreEqual(expectedHorizontalOffset, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(expectedVerticalOffset, scrollPresenter.VerticalOffset);
			Verify.AreEqual(originalZoomFactor, scrollPresenter.ZoomFactor);
		});
	}

	private void BringElementInNestedScrollPresentersIntoView(
		Orientation orientation,
		double expectedOuterHorizontalOffset,
		double expectedOuterVerticalOffset,
		double expectedInnerHorizontalOffset,
		double expectedInnerVerticalOffset,
		BringIntoViewOptions options = null,
		double originalOuterHorizontalOffset = 0.0,
		double originalOuterVerticalOffset = 0.0,
		float originalOuterZoomFactor = 1.0f,
		double originalInnerHorizontalOffset = 0.0,
		double originalInnerVerticalOffset = 0.0,
		float originalInnerZoomFactor = 1.0f,
		bool applySnapPointsInBringingIntoViewHandler = false)
	{
		ScrollPresenter outerScrollPresenter = null;
		ScrollPresenter innerScrollPresenter = null;
		AutoResetEvent outerScrollPresenterLoadedEvent = new AutoResetEvent(false);
		AutoResetEvent innerScrollPresenterLoadedEvent = new AutoResetEvent(false);
		AutoResetEvent outerScrollPresenterViewChangedEvent = new AutoResetEvent(false);
		AutoResetEvent innerScrollPresenterViewChangedEvent = new AutoResetEvent(false);
		AutoResetEvent outerBringIntoViewCompletedEvent = new AutoResetEvent(false);
		AutoResetEvent innerBringIntoViewCompletedEvent = new AutoResetEvent(false);
		int outerBringIntoViewChangeCorrelationId = -1;
		int innerBringIntoViewChangeCorrelationId = -1;

		RunOnUIThread.Execute(() =>
		{
			outerScrollPresenter = new ScrollPresenter();
			innerScrollPresenter = new ScrollPresenter();

			SetupBringIntoViewUIWithScrollPresenterInsideScrollPresenter(orientation, outerScrollPresenter, innerScrollPresenter, outerScrollPresenterLoadedEvent, innerScrollPresenterLoadedEvent);
		});

		WaitForEvent("Waiting for Inner Loaded event", innerScrollPresenterLoadedEvent);
		WaitForEvent("Waiting for Outer Loaded event", outerScrollPresenterLoadedEvent);

		if (originalOuterZoomFactor != 1.0f)
		{
			ZoomTo(outerScrollPresenter, originalOuterZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);
		}

		if (originalOuterHorizontalOffset != 0 || originalOuterVerticalOffset != 0)
		{
			ScrollTo(outerScrollPresenter, originalOuterHorizontalOffset, originalOuterVerticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, originalOuterZoomFactor == 1.0f /*hookViewChanged*/);
		}

		if (originalInnerZoomFactor != 1.0f)
		{
			ZoomTo(innerScrollPresenter, originalInnerZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);
		}

		if (originalInnerHorizontalOffset != 0 || originalInnerVerticalOffset != 0)
		{
			ScrollTo(innerScrollPresenter, originalInnerHorizontalOffset, originalInnerVerticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, originalInnerZoomFactor == 1.0f /*hookViewChanged*/);
		}

		RunOnUIThread.Execute(() =>
		{
			innerScrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
			{
				Log.Comment("Inner ViewChanged ScrollPresenter={0} - HorizontalOffset={1}, VerticalOffset={2}, ZoomFactor={3}",
					sender.Name, sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
				innerScrollPresenterViewChangedEvent.Set();
			};

			innerScrollPresenter.ScrollCompleted += delegate (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args)
			{
				ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);

				Log.Comment("Inner ScrollPresenter bring-into-view OffsetsChangeCorrelationId={0} completed with Result={1}", args.CorrelationId, result);
				if (innerBringIntoViewChangeCorrelationId == args.CorrelationId)
					innerBringIntoViewCompletedEvent.Set();
			};

			innerScrollPresenter.BringingIntoView += (ScrollPresenter sender, ScrollingBringingIntoViewEventArgs args) =>
			{
				Log.Comment("Inner ScrollPresenter.BringingIntoView ScrollPresenter={0} - TargetHorizontalOffset={1}, TargetVerticalOffset={2}, OffsetsChangeCorrelationId={3}, SnapPointsMode={4}",
					sender.Name, args.TargetHorizontalOffset, args.TargetVerticalOffset, args.CorrelationId, args.SnapPointsMode);
				innerBringIntoViewChangeCorrelationId = args.CorrelationId;

				if (applySnapPointsInBringingIntoViewHandler)
				{
					Log.Comment("ScrollPresenter.BringingIntoView - Applying mandatory snap points");
					AddSnapPoints(scrollPresenter: sender, stackPanel: (sender.Content as Border).Child as StackPanel);
					args.SnapPointsMode = ScrollingSnapPointsMode.Default;
				}
			};

			outerScrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
			{
				Log.Comment("Outer ViewChanged ScrollPresenter={0} - HorizontalOffset={1}, VerticalOffset={2}, ZoomFactor={3}",
					sender.Name, sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
				outerScrollPresenterViewChangedEvent.Set();
			};

			outerScrollPresenter.ScrollCompleted += delegate (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args)
			{
				ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);
				Log.Comment("Outer ScrollPresenter bring-into-view OffsetsChangeCorrelationId={0} completed with Result={1}", args.CorrelationId, result);
				if (outerBringIntoViewChangeCorrelationId == args.CorrelationId)
					outerBringIntoViewCompletedEvent.Set();
			};

			outerScrollPresenter.BringingIntoView += (ScrollPresenter sender, ScrollingBringingIntoViewEventArgs args) =>
			{
				Log.Comment("Outer ScrollPresenter.BringingIntoView ScrollPresenter={0} - TargetHorizontalOffset={1}, TargetVerticalOffset={2}, OffsetsChangeCorrelationId={3}, SnapPointsMode={4}",
					sender.Name, args.TargetHorizontalOffset, args.TargetVerticalOffset, args.CorrelationId, args.SnapPointsMode);
				outerBringIntoViewChangeCorrelationId = args.CorrelationId;

				if (applySnapPointsInBringingIntoViewHandler)
				{
					Log.Comment("ScrollPresenter.BringingIntoView - Applying mandatory snap points");
					AddSnapPoints(scrollPresenter: sender, stackPanel: (sender.Content as Border).Child as StackPanel);
					args.SnapPointsMode = ScrollingSnapPointsMode.Default;
				}
			};

			UIElement targetElement = ((innerScrollPresenter.Content as Border).Child as StackPanel).Children[12];

			if (options == null)
			{
				targetElement.StartBringIntoView();
			}
			else
			{
				targetElement.StartBringIntoView(options);
			}
		});

		WaitForEvent("Waiting for inner ScrollPresenter.ViewChanged event", innerScrollPresenterViewChangedEvent);
		WaitForEvent("Waiting for outer ScrollPresenter.ViewChanged event", outerScrollPresenterViewChangedEvent);
		WaitForEvent("Waiting for inner bring-into-view operation completion event", innerBringIntoViewCompletedEvent);
		WaitForEvent("Waiting for outer bring-into-view operation completion event", outerBringIntoViewCompletedEvent);
		IdleSynchronizer.Wait();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final inner view - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				innerScrollPresenter.HorizontalOffset, innerScrollPresenter.VerticalOffset, innerScrollPresenter.ZoomFactor);
			Log.Comment("Final outer view - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				outerScrollPresenter.HorizontalOffset, outerScrollPresenter.VerticalOffset, outerScrollPresenter.ZoomFactor);

			Verify.AreEqual(expectedInnerHorizontalOffset, innerScrollPresenter.HorizontalOffset);
			Verify.AreEqual(expectedInnerVerticalOffset, innerScrollPresenter.VerticalOffset);
			Verify.AreEqual(originalInnerZoomFactor, innerScrollPresenter.ZoomFactor);

			Verify.AreEqual(expectedOuterHorizontalOffset, outerScrollPresenter.HorizontalOffset);
			Verify.AreEqual(expectedOuterVerticalOffset, outerScrollPresenter.VerticalOffset);
			Verify.AreEqual(originalOuterZoomFactor, outerScrollPresenter.ZoomFactor);
		});
	}

	private void BringElementInNestedScrollViewersIntoView(
		Orientation orientation,
		double expectedOuterHorizontalOffset,
		double expectedOuterVerticalOffset,
		double expectedInnerHorizontalOffset,
		double expectedInnerVerticalOffset,
		BringIntoViewOptions options = null)
	{
		ScrollViewer outerScrollViewer = null;
		ScrollViewer innerScrollViewer = null;
		AutoResetEvent outerScrollViewerLoadedEvent = new AutoResetEvent(false);
		AutoResetEvent innerScrollViewerLoadedEvent = new AutoResetEvent(false);
		AutoResetEvent outerScrollViewerViewChangedEvent = new AutoResetEvent(false);
		AutoResetEvent innerScrollViewerViewChangedEvent = new AutoResetEvent(false);
		AutoResetEvent outerBringIntoViewCompletedEvent = new AutoResetEvent(false);
		AutoResetEvent innerBringIntoViewCompletedEvent = new AutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			outerScrollViewer = new ScrollViewer();
			innerScrollViewer = new ScrollViewer();

			SetupBringIntoViewUIWithScrollViewerInsideScrollViewer(orientation, outerScrollViewer, innerScrollViewer, outerScrollViewerLoadedEvent, innerScrollViewerLoadedEvent);
		});

		WaitForEvent("Waiting for Inner Loaded event", innerScrollViewerLoadedEvent);
		WaitForEvent("Waiting for Outer Loaded event", outerScrollViewerLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			innerScrollViewer.ViewChanged += delegate (object sender, ScrollViewerViewChangedEventArgs args)
			{
				ScrollViewer sv = sender as ScrollViewer;
				Log.Comment("Inner ViewChanged ScrollViewer={0} - HorizontalOffset={1}, VerticalOffset={2}, ZoomFactor={3}",
					sv.Name, sv.HorizontalOffset, sv.VerticalOffset, sv.ZoomFactor);
				if (!args.IsIntermediate)
					innerScrollViewerViewChangedEvent.Set();
			};

			outerScrollViewer.ViewChanged += delegate (object sender, ScrollViewerViewChangedEventArgs args)
			{
				ScrollViewer sv = sender as ScrollViewer;
				Log.Comment("Outer ViewChanged ScrollViewer={0} - HorizontalOffset={1}, VerticalOffset={2}, ZoomFactor={3}",
					sv.Name, sv.HorizontalOffset, sv.VerticalOffset, sv.ZoomFactor);
				if (!args.IsIntermediate)
					outerScrollViewerViewChangedEvent.Set();
			};

			UIElement targetElement = ((innerScrollViewer.Content as Border).Child as StackPanel).Children[12];

			if (options == null)
			{
				targetElement.StartBringIntoView();
			}
			else
			{
				targetElement.StartBringIntoView(options);
			}
		});

		WaitForEvent("Waiting for inner ScrollViewer.ViewChanged event", innerScrollViewerViewChangedEvent);
		WaitForEvent("Waiting for outer ScrollViewer.ViewChanged event", outerScrollViewerViewChangedEvent);
		IdleSynchronizer.Wait();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final inner view - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				innerScrollViewer.HorizontalOffset, innerScrollViewer.VerticalOffset, innerScrollViewer.ZoomFactor);
			Log.Comment("Final outer view - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				outerScrollViewer.HorizontalOffset, outerScrollViewer.VerticalOffset, outerScrollViewer.ZoomFactor);

			Verify.AreEqual(expectedInnerHorizontalOffset, innerScrollViewer.HorizontalOffset);
			Verify.AreEqual(expectedInnerVerticalOffset, innerScrollViewer.VerticalOffset);

			Verify.AreEqual(expectedOuterHorizontalOffset, outerScrollViewer.HorizontalOffset);
			Verify.AreEqual(expectedOuterVerticalOffset, outerScrollViewer.VerticalOffset);
		});
	}

	private void SetupDefaultBringIntoViewUI(
		Orientation orientation,
		ScrollPresenter scrollPresenter,
		AutoResetEvent scrollPresenterLoadedEvent,
		bool setAsContentRoot = true)
	{
		Log.Comment("Setting up default bring-into-view UI with ScrollPresenter");

		StackPanel stackPanel = new StackPanel();
		stackPanel.Name = "stackPanel";
		stackPanel.Orientation = orientation;
		stackPanel.Margin = new Thickness(30);

		Border border = new Border();
		border.Name = "border";
		border.BorderThickness = new Thickness(3);
		border.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse);
		border.Margin = new Thickness(15);
		border.Background = new SolidColorBrush(Windows.UI.Colors.Beige);
		border.Child = stackPanel;

		Verify.IsNotNull(scrollPresenter);
		scrollPresenter.Name = "scrollPresenter";
		if (orientation == Orientation.Vertical)
		{
			scrollPresenter.ContentOrientation = ScrollingContentOrientation.Vertical;
			scrollPresenter.Width = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
			scrollPresenter.Height = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
		}
		else
		{
			scrollPresenter.ContentOrientation = ScrollingContentOrientation.Horizontal;
			scrollPresenter.Width = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
			scrollPresenter.Height = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
		}
		scrollPresenter.Background = new SolidColorBrush(Windows.UI.Colors.AliceBlue);
		scrollPresenter.Content = border;

		InsertStackPanelChild(stackPanel, 0 /*operationCount*/, 0 /*newIndex*/, c_defaultBringIntoViewUIStackPanelChildrenCount /*newCount*/);

		if (scrollPresenterLoadedEvent != null)
		{
			scrollPresenter.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ScrollPresenter.Loaded event handler");
				scrollPresenterLoadedEvent.Set();
			};
		}

		scrollPresenter.BringingIntoView += (ScrollPresenter sender, ScrollingBringingIntoViewEventArgs args) =>
		{
			Log.Comment("ScrollPresenter.BringingIntoView ScrollPresenter={0} - TargetHorizontalOffset={1}, TargetVerticalOffset={2}, OffsetsChangeCorrelationId={3}",
				sender.Name, args.TargetHorizontalOffset, args.TargetVerticalOffset, args.CorrelationId);
			Log.Comment("RequestEventArgs - AnimationDesired={0}, Handled={1}, HorizontalAlignmentRatio={2}, VerticalAlignmentRatio={3}",
				args.RequestEventArgs.AnimationDesired,
				args.RequestEventArgs.Handled,
				args.RequestEventArgs.HorizontalAlignmentRatio,
				args.RequestEventArgs.VerticalAlignmentRatio);
			Log.Comment("RequestEventArgs - HorizontalOffset={0}, VerticalOffset={1}, TargetElement={2}, TargetRect={3}",
				args.RequestEventArgs.HorizontalOffset,
				args.RequestEventArgs.VerticalOffset,
				(args.RequestEventArgs.TargetElement as FrameworkElement).Name,
				args.RequestEventArgs.TargetRect.ToString());
		};

		if (setAsContentRoot)
		{
			Log.Comment("Setting window content");
			Content = scrollPresenter;
		}
	}

	private void SetupDefaultBringIntoViewUIWithScrollViewer(
		Orientation orientation,
		ScrollViewer scrollViewer,
		AutoResetEvent scrollViewerLoadedEvent,
		bool setAsContentRoot = true)
	{
		Log.Comment("Setting up default bring-into-view UI with ScrollViewer");

		StackPanel stackPanel = new StackPanel();
		stackPanel.Name = "stackPanel";
		stackPanel.Orientation = orientation;
		stackPanel.Margin = new Thickness(30);

		Border border = new Border();
		border.Name = "border";
		border.BorderThickness = new Thickness(3);
		border.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse);
		border.Margin = new Thickness(15);
		border.Background = new SolidColorBrush(Windows.UI.Colors.Beige);
		border.Child = stackPanel;

		Verify.IsNotNull(scrollViewer);
		scrollViewer.Name = "scrollViewer";
		if (orientation == Orientation.Vertical)
		{
			scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			scrollViewer.Width = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
			scrollViewer.Height = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
		}
		else
		{
			scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			scrollViewer.Width = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
			scrollViewer.Height = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
		}
		scrollViewer.Background = new SolidColorBrush(Windows.UI.Colors.AliceBlue);
		scrollViewer.Content = border;

		InsertStackPanelChild(stackPanel, 0 /*operationCount*/, 0 /*newIndex*/, c_defaultBringIntoViewUIStackPanelChildrenCount /*newCount*/);

		if (scrollViewerLoadedEvent != null)
		{
			scrollViewer.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ScrollViewer.Loaded event handler");
				scrollViewerLoadedEvent.Set();
			};
		}

		if (setAsContentRoot)
		{
			Log.Comment("Setting window content");
			Content = scrollViewer;
		}
	}

	private void SetupBringIntoViewUIWithScrollPresenterInsideScrollPresenter(
		Orientation orientation,
		ScrollPresenter outerScrollPresenter,
		ScrollPresenter innerScrollPresenter,
		AutoResetEvent outerScrollPresenterLoadedEvent,
		AutoResetEvent innerScrollPresenterLoadedEvent)
	{
		Log.Comment("Setting up bring-into-view UI with ScrollPresenter inside ScrollPresenter");

		Log.Comment("Setting up inner ScrollPresenter");
		SetupDefaultBringIntoViewUI(
			orientation,
			innerScrollPresenter,
			innerScrollPresenterLoadedEvent,
			false /*setAsContentRoot*/);

		Log.Comment("Setting up outer ScrollPresenter");
		StackPanel stackPanel = new StackPanel();
		stackPanel.Name = "outerStackPanel";
		stackPanel.Orientation = orientation;
		stackPanel.Margin = new Thickness(30);

		Border border = new Border();
		border.Name = "outerBorder";
		border.BorderThickness = new Thickness(3);
		border.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse);
		border.Margin = new Thickness(15);
		border.Background = new SolidColorBrush(Windows.UI.Colors.Beige);
		border.Child = stackPanel;

		Verify.IsNotNull(outerScrollPresenter);
		outerScrollPresenter.Name = "outerScrollPresenter";
		if (orientation == Orientation.Vertical)
		{
			outerScrollPresenter.ContentOrientation = ScrollingContentOrientation.Vertical;
			outerScrollPresenter.Width = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
			outerScrollPresenter.Height = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
		}
		else
		{
			outerScrollPresenter.ContentOrientation = ScrollingContentOrientation.Horizontal;
			outerScrollPresenter.Width = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
			outerScrollPresenter.Height = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
		}
		outerScrollPresenter.Background = new SolidColorBrush(Windows.UI.Colors.AliceBlue);
		outerScrollPresenter.Content = border;

		InsertStackPanelChild(stackPanel, 0 /*operationCount*/, 0 /*newIndex*/, c_defaultBringIntoViewUIStackPanelChildrenCount / 2 /*newCount*/, "outer" /*namePrefix*/);

		stackPanel.Children.Add(innerScrollPresenter);

		InsertStackPanelChild(stackPanel, 0 /*operationCount*/, c_defaultBringIntoViewUIStackPanelChildrenCount / 2 + 1 /*newIndex*/, c_defaultBringIntoViewUIStackPanelChildrenCount / 2 /*newCount*/, "outer" /*namePrefix*/);

		if (outerScrollPresenterLoadedEvent != null)
		{
			outerScrollPresenter.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("Outer ScrollPresenter.Loaded event handler");
				outerScrollPresenterLoadedEvent.Set();
			};
		}

		outerScrollPresenter.BringingIntoView += (ScrollPresenter sender, ScrollingBringingIntoViewEventArgs args) =>
		{
			Log.Comment("Outer ScrollPresenter.BringingIntoView ScrollPresenter={0} - TargetHorizontalOffset={1}, TargetVerticalOffset={2}, OffsetsChangeCorrelationId={3}, SnapPointsMode={4}",
				sender.Name, args.TargetHorizontalOffset, args.TargetVerticalOffset, args.CorrelationId, args.SnapPointsMode);
			Log.Comment("RequestEventArgs - AnimationDesired={0}, Handled={1}, HorizontalAlignmentRatio={2}, VerticalAlignmentRatio={3}",
				args.RequestEventArgs.AnimationDesired,
				args.RequestEventArgs.Handled,
				args.RequestEventArgs.HorizontalAlignmentRatio,
				args.RequestEventArgs.VerticalAlignmentRatio);
			Log.Comment("RequestEventArgs - HorizontalOffset={0}, VerticalOffset={1}, TargetElement={2}, TargetRect={3}",
				args.RequestEventArgs.HorizontalOffset,
				args.RequestEventArgs.VerticalOffset,
				(args.RequestEventArgs.TargetElement as FrameworkElement).Name,
				args.RequestEventArgs.TargetRect.ToString());
		};

		Log.Comment("Setting window content");
		Content = outerScrollPresenter;
	}

	private void SetupBringIntoViewUIWithScrollViewerInsideScrollViewer(
		Orientation orientation,
		ScrollViewer outerScrollViewer,
		ScrollViewer innerScrollViewer,
		AutoResetEvent outerScrollViewerLoadedEvent,
		AutoResetEvent innerScrollViewerLoadedEvent)
	{
		Log.Comment("Setting up bring-into-view UI with ScrollViewer inside ScrollViewer");

		Log.Comment("Setting up inner ScrollViewer");
		SetupDefaultBringIntoViewUIWithScrollViewer(
			orientation,
			innerScrollViewer,
			innerScrollViewerLoadedEvent,
			false /*setAsContentRoot*/);

		Log.Comment("Setting up outer ScrollViewer");
		StackPanel stackPanel = new StackPanel();
		stackPanel.Name = "outerStackPanel";
		stackPanel.Orientation = orientation;
		stackPanel.Margin = new Thickness(30);

		Border border = new Border();
		border.Name = "outerBorder";
		border.BorderThickness = new Thickness(3);
		border.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse);
		border.Margin = new Thickness(15);
		border.Background = new SolidColorBrush(Windows.UI.Colors.Beige);
		border.Child = stackPanel;

		Verify.IsNotNull(outerScrollViewer);
		outerScrollViewer.Name = "outerScrollViewer";
		if (orientation == Orientation.Vertical)
		{
			outerScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			outerScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			outerScrollViewer.Width = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
			outerScrollViewer.Height = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
		}
		else
		{
			outerScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			outerScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			outerScrollViewer.Width = c_defaultBringIntoViewUIScrollPresenterNonConstrainedSize;
			outerScrollViewer.Height = c_defaultBringIntoViewUIScrollPresenterConstrainedSize;
		}
		outerScrollViewer.Background = new SolidColorBrush(Windows.UI.Colors.AliceBlue);
		outerScrollViewer.Content = border;

		InsertStackPanelChild(stackPanel, 0 /*operationCount*/, 0 /*newIndex*/, c_defaultBringIntoViewUIStackPanelChildrenCount / 2 /*newCount*/, "outer" /*namePrefix*/);

		stackPanel.Children.Add(innerScrollViewer);

		InsertStackPanelChild(stackPanel, 0 /*operationCount*/, c_defaultBringIntoViewUIStackPanelChildrenCount / 2 + 1 /*newIndex*/, c_defaultBringIntoViewUIStackPanelChildrenCount / 2 /*newCount*/, "outer" /*namePrefix*/);

		if (outerScrollViewerLoadedEvent != null)
		{
			outerScrollViewer.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("Outer ScrollViewer.Loaded event handler");
				outerScrollViewerLoadedEvent.Set();
			};
		}

		Log.Comment("Setting window content");
		Content = outerScrollViewer;
	}

	private void AddSnapPoints(ScrollPresenter scrollPresenter, StackPanel stackPanel)
	{
		Verify.IsNotNull(scrollPresenter);

		if (stackPanel == null || stackPanel.Children.Count == 0)
		{
			return;
		}

		Log.Comment("Populating snap points for " + scrollPresenter.Name + ":");

		ScrollSnapPoint scrollSnapPoint;
		GeneralTransform gt = stackPanel.TransformToVisual(scrollPresenter.Content);
		Point stackPanelOriginPoint = new Point();
		stackPanelOriginPoint = gt.TransformPoint(stackPanelOriginPoint);

		if (stackPanel.Orientation == Orientation.Horizontal)
		{
			scrollSnapPoint = new ScrollSnapPoint(stackPanelOriginPoint.X, ScrollSnapPointsAlignment.Near);
			Log.Comment("Adding horizontal snap point to " + scrollPresenter.Name + " at value " + stackPanelOriginPoint.X);
			scrollPresenter.HorizontalSnapPoints.Add(scrollSnapPoint);
		}
		else
		{
			scrollSnapPoint = new ScrollSnapPoint(stackPanelOriginPoint.Y, ScrollSnapPointsAlignment.Near);
			Log.Comment("Adding vertical snap point to " + scrollPresenter.Name + " at value " + stackPanelOriginPoint.Y);
			scrollPresenter.VerticalSnapPoints.Add(scrollSnapPoint);
		}

		foreach (UIElement child in stackPanel.Children)
		{
			FrameworkElement childAsFE = child as FrameworkElement;

			if (childAsFE != null)
			{
				gt = childAsFE.TransformToVisual(stackPanel);
				Point childOriginPoint = new Point();
				childOriginPoint = gt.TransformPoint(childOriginPoint);

				double snapPointValue = 0.0;
				Thickness margin = childAsFE.Margin;

				if (stackPanel.Orientation == Orientation.Horizontal)
				{
					snapPointValue = margin.Right + childAsFE.ActualWidth + childOriginPoint.X;
					if (snapPointValue <= scrollPresenter.ScrollableWidth)
					{
						scrollSnapPoint = new ScrollSnapPoint(snapPointValue, ScrollSnapPointsAlignment.Near);
						Log.Comment("Adding horizontal snap point to " + scrollPresenter.Name + " at value " + snapPointValue);
						scrollPresenter.HorizontalSnapPoints.Add(scrollSnapPoint);
					}
					else
					{
						break;
					}
				}
				else
				{
					snapPointValue = margin.Bottom + childAsFE.ActualHeight + childOriginPoint.Y;
					if (snapPointValue <= scrollPresenter.ScrollableHeight)
					{
						scrollSnapPoint = new ScrollSnapPoint(snapPointValue, ScrollSnapPointsAlignment.Near);
						Log.Comment("Adding vertical snap point to " + scrollPresenter.Name + " at value " + snapPointValue);
						scrollPresenter.VerticalSnapPoints.Add(scrollSnapPoint);
					}
					else
					{
						break;
					}
				}
			}

			AddSnapPoints(scrollPresenter, child as StackPanel);
		}
	}
}
#endif
