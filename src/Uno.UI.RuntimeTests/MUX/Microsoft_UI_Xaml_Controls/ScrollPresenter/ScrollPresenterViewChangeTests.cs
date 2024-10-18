// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Windows.UI.Composition;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Windows.Foundation;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

partial class ScrollPresenterTests : MUXApiTestBase
{
	// Enum used in tests that use InterruptViewChange
	private enum ViewChangeInterruptionKind
	{
		OffsetsChangeByOffsetsChange,
		OffsetsChangeByZoomFactorChange,
		ZoomFactorChangeByOffsetsChange,
		ZoomFactorChangeByZoomFactorChange,
	}

	private const int c_MaxWaitDuration = 5000;
	private const int c_MaxStockOffsetsChangeDuration = 1000;
	private const int c_MaxStockZoomFactorChangeDuration = 1000;

	private uint viewChangedCount = 0u;

	[TestMethod]
	[TestProperty("Description", "Changes ScrollPresenter offsets using ScrollTo, ScrollBy, AddScrollVelocity and AnimationMode/SnapPointsMode enum values.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task BasicOffsetChanges()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Jump to absolute offsets
		await ScrollTo(scrollPresenter, 11.0, 22.0, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);
		await ScrollTo(scrollPresenter, 22.0, 11.0, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: false);

		// Jump to relative offsets
		await ScrollBy(scrollPresenter, -4.0, 15.0, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);
		await ScrollBy(scrollPresenter, 15.0, 4.0, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: false);

		// Animate to absolute offsets
		await ScrollTo(scrollPresenter, 55.0, 25.0, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);
		await ScrollTo(scrollPresenter, 5.0, 75.0, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: true);

		// Jump or animate to absolute offsets based on UISettings.AnimationsEnabled
		await ScrollTo(scrollPresenter, 55.0, 25.0, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);

		// Animate to relative offsets
		await ScrollBy(scrollPresenter, 700.0, -8.0, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);
		await ScrollBy(scrollPresenter, -80.0, 200.0, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: true);

		// Jump or animate to relative offsets based on UISettings.AnimationsEnabled
		await ScrollBy(scrollPresenter, 80.0, -200.0, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);

		// Flick with additional offsets velocity
		await AddScrollVelocity(scrollPresenter, horizontalVelocity: -65.0f, verticalVelocity: 80.0f, horizontalInertiaDecayRate: null, verticalInertiaDecayRate: null, hookViewChanged: false);

		// Flick with additional offsets velocity and custom scroll inertia decay rate
		await AddScrollVelocity(scrollPresenter, horizontalVelocity: 65.0f, verticalVelocity: -80.0f, horizontalInertiaDecayRate: 0.7f, verticalInertiaDecayRate: 0.8f, hookViewChanged: false);

		// Do it all again while respecting snap points
		await ScrollTo(scrollPresenter, 11.0, 22.0, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Default, hookViewChanged: false);
		await ScrollBy(scrollPresenter, -4.0, 15.0, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Default, hookViewChanged: false);
		await ScrollTo(scrollPresenter, 55.0, 25.0, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Default, hookViewChanged: false);
		await ScrollBy(scrollPresenter, 700.0, -8.0, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Default, hookViewChanged: false);
	}

	[TestMethod]
	[TestProperty("Description", "Changes ScrollPresenter zoomFactor using ZoomTo, ZoomBy, AddZoomVelocity and AnimationMode enum values.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task BasicZoomFactorChanges()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Jump to absolute zoomFactor
		await ZoomTo(scrollPresenter, 2.0f, 22.0f, 33.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);
		await ZoomTo(scrollPresenter, 5.0f, 33.0f, 22.0f, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: false);

		// Jump to relative zoomFactor
		await ZoomBy(scrollPresenter, 1.0f, 55.0f, 66.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);
		await ZoomBy(scrollPresenter, 1.0f, 66.0f, 55.0f, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: false);

		// Animate to absolute zoomFactor
		await ZoomTo(scrollPresenter, 4.0f, -40.0f, -25.0f, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);
		await ZoomTo(scrollPresenter, 6.0f, 25.0f, 40.0f, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: true);

		// Jump or animate to absolute zoomFactor based on UISettings.AnimationsEnabled
		await ZoomTo(scrollPresenter, 3.0f, 10.0f, 20.0f, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);

		// Animate to relative zoomFactor
		await ZoomBy(scrollPresenter, -2.0f, 100.0f, 200.0f, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);
		await ZoomBy(scrollPresenter, 1.0f, 100.0f, 200.0f, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false, isAnimationsEnabledOverride: true);

		// Jump or animate to relative zoomFactor based on UISettings.AnimationsEnabled
		await ZoomBy(scrollPresenter, 2.0f, 200.0f, 100.0f, ScrollingAnimationMode.Auto, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);

		// Flick with additional zoomFactor velocity
		await AddZoomVelocity(scrollPresenter, zoomFactorVelocity: 2.0f, inertiaDecayRate: null, centerPointX: -50.0f, centerPointY: 800.0f, waitForViewChangeCompletion: true, hookViewChanged: false);

		// Flick with additional zoomFactor velocity and custom zoomFactor inertia decay rate
		await AddZoomVelocity(scrollPresenter, zoomFactorVelocity: -2.0f, inertiaDecayRate: 0.75f, centerPointX: -50.0f, centerPointY: 800.0f, waitForViewChangeCompletion: true, hookViewChanged: false);
	}

	[TestMethod]
	[TestProperty("Description", "Changes ScrollPresenter offsets using AddScrollVelocity multiple times with various inertia decay rates.")]
	[Ignore("Fails in Uno, waiting forever on ViewChanged")]
	public async Task SuccessiveAdditionalScrollVelocities()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent1 = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent2 = new UnoAutoResetEvent(false);
		bool viewChanged = false;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");

				if (!viewChanged)
				{
					viewChanged = true;

					Log.Comment("Setting view changed event");
					scrollPresenterViewChangedEvent.Set();
				}
			};

			scrollPresenter.ScrollCompleted += (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args) =>
			{
				ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);

				Log.Comment("ScrollCompleted: AddScrollVelocity ScrollChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);

				if (args.CorrelationId == 1)
				{
					Log.Comment("Setting first view change completion event");
					scrollPresenterViewChangeOperationEvent1.Set();
				}
				else if (args.CorrelationId == 2)
				{
					Log.Comment("Setting second view change completion event");
					scrollPresenterViewChangeOperationEvent2.Set();
				}
			};
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Add scroll velocity with very small inertia decay rate
		await AddScrollVelocity(scrollPresenter, horizontalVelocity: 50.0f, verticalVelocity: 60.0f, horizontalInertiaDecayRate: 0.0007f, verticalInertiaDecayRate: 0.0008f, waitForViewChangeCompletion: false, hookViewChanged: false);

		// Waiting for first view changed event
		await WaitForEvent("Waiting for first ViewChanged event", scrollPresenterViewChangedEvent);

		// Add scroll velocity with default inertia decay rate
		await AddScrollVelocity(scrollPresenter, horizontalVelocity: 170.0f, verticalVelocity: 180.0f, horizontalInertiaDecayRate: null, verticalInertiaDecayRate: null, waitForViewChangeCompletion: false, hookViewChanged: false);

		// Waiting for first view change completion
		await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvent1);

		// Waiting for second view change completion
		await WaitForEvent("Waiting for second view change completion", scrollPresenterViewChangeOperationEvent2);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");

			Verify.IsLessThan(Math.Abs(scrollPresenter.HorizontalOffset - 72.0), 10.0);
			Verify.IsLessThan(Math.Abs(scrollPresenter.VerticalOffset - 80.0), 10.0);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Changes ScrollPresenter zoomFactor using AddZoomVelocity multiple times with various inertia decay rates.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task SuccessiveAdditionalZoomVelocities()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent1 = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent2 = new UnoAutoResetEvent(false);
		bool viewChanged = false;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");

				if (!viewChanged)
				{
					viewChanged = true;

					Log.Comment("Setting view changed event");
					scrollPresenterViewChangedEvent.Set();
				}
			};

			scrollPresenter.ZoomCompleted += (ScrollPresenter sender, ScrollingZoomCompletedEventArgs args) =>
			{
				ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetZoomCompletedResult(args);

				Log.Comment("ZoomCompleted: AddZoomVelocity ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);

				if (args.CorrelationId == 1)
				{
					Log.Comment("Setting first view change completion event");
					scrollPresenterViewChangeOperationEvent1.Set();
				}
				else if (args.CorrelationId == 2)
				{
					Log.Comment("Setting second view change completion event");
					scrollPresenterViewChangeOperationEvent2.Set();
				}
			};
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Add zoomFactor velocity with very small inertia decay rate
		await AddZoomVelocity(scrollPresenter, zoomFactorVelocity: 0.4f, inertiaDecayRate: 0.0001f, centerPointX: 0.0f, centerPointY: 0.0f, waitForViewChangeCompletion: false, hookViewChanged: false);

		// Waiting for first view changed event
		await WaitForEvent("Waiting for first ViewChanged event", scrollPresenterViewChangedEvent);

		// Add zoomFactor velocity with default inertia decay rate
		await AddZoomVelocity(scrollPresenter, zoomFactorVelocity: 0.6f, inertiaDecayRate: null, centerPointX: 0.0f, centerPointY: 0.0f, waitForViewChangeCompletion: false, hookViewChanged: false);

		// Waiting for first view change completion
		await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvent1);

		// Waiting for second view change completion
		await WaitForEvent("Waiting for second view change completion", scrollPresenterViewChangeOperationEvent2);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");

			Verify.AreEqual(0.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(0.0, scrollPresenter.VerticalOffset);
			Verify.IsLessThan(Math.Abs(scrollPresenter.ZoomFactor - 1.26f), 0.1f);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Cancels an animated offsets change.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task BasicOffsetsChangeCancelation()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
		ScrollPresenterOperation operation = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			operation = StartScrollTo(
				scrollPresenter,
				600.0,
				400.0,
				ScrollingAnimationMode.Enabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvent);

			bool operationCanceled = false;

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");

				if ((sender.HorizontalOffset >= 150.0 || sender.VerticalOffset >= 100.0) && !operationCanceled)
				{
					Log.Comment("Canceling view change");
					operationCanceled = true;
					sender.ScrollBy(0, 0, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
				}
			};
		});

		await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.IsTrue(scrollPresenter.HorizontalOffset < 600.0);
			Verify.IsTrue(scrollPresenter.VerticalOffset < 400.0);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Interrupted, operation.Result);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Cancels an animated zoomFactor change.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task BasicZoomFactorChangeCancelation()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			ScrollPresenter scrollPresenter = null;
			Rectangle rectangleScrollPresenterContent = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				rectangleScrollPresenterContent = new Rectangle();
				scrollPresenter = new ScrollPresenter();
				scrollPresenter.Name = "scr";

				SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				operation = StartZoomTo(
					scrollPresenter,
					8.0f,
					100.0f,
					150.0f,
					ScrollingAnimationMode.Enabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvent);

				bool operationCanceled = false;

				scrollPresenter.ViewChanged += (sender, args) =>
				{
					Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");

					if (sender.ZoomFactor >= 2.0 && !operationCanceled)
					{
						Log.Comment("Canceling view change");
						operationCanceled = true;
						sender.ZoomBy(0, Vector2.Zero, new ScrollingZoomOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
					}
				};
			});

			await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
					scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

				Verify.IsTrue(scrollPresenter.ZoomFactor < 8.0f);
				Verify.AreEqual(ScrollPresenterViewChangeResult.Interrupted, operation.Result);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Checks to make sure the exposed startPosition, endPosition, StartZoomFactor, and EndZoomFactor " +
		"on ScrollingScrollAnimationStartingEventArgs and ScrollingZoomAnimationStartingEventArgs respectively are accurate.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ValidateScrollAnimationStartingAndZoomFactorEventArgsHaveValidStartAndEndPositions()
	{
		int numOffsetChanges = 0;
		int numZoomFactorChanges = 0;
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		Point newPosition1 = new Point(100, 150);
		Point newPosition2 = new Point(50, 100);
		float newZoomFactor1 = 2.0f;
		float newZoomFactor2 = 0.5f;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(
				scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Attach to ScrollAnimationStarting");

			scrollPresenter.ZoomAnimationStarting += (ScrollPresenter sender, ScrollingZoomAnimationStartingEventArgs e) =>
			{
				Log.Comment("ScrollPresenter.ZoomAnimationStarting event handler");
				if (numZoomFactorChanges == 0)
				{
					Verify.AreEqual(2.0f, e.EndZoomFactor);
					Verify.AreEqual(1.0f, e.StartZoomFactor);
					numZoomFactorChanges++;
				}
				else
				{
					Verify.AreEqual(0.5f, e.EndZoomFactor);
					Verify.AreEqual(2.0f, e.StartZoomFactor);
				}
			};

			scrollPresenter.ScrollAnimationStarting += (ScrollPresenter sender, ScrollingScrollAnimationStartingEventArgs e) =>
			{
				Log.Comment("ScrollPresenter.ScrollAnimationStarting event handler");
				if (numOffsetChanges == 0)
				{
					Verify.AreEqual(100.0f, e.EndPosition.X);
					Verify.AreEqual(150.0f, e.EndPosition.Y);
					Verify.AreEqual(0.0f, e.StartPosition.X);
					Verify.AreEqual(0.0f, e.StartPosition.Y);

					numOffsetChanges++;
				}
				else
				{
					Verify.AreEqual(50.0f, e.EndPosition.X);
					Verify.AreEqual(100.0f, e.EndPosition.Y);
					Verify.AreEqual(100.0f, e.StartPosition.X);
					Verify.AreEqual(150.0f, e.StartPosition.Y);

					numOffsetChanges++;
				}
			};
		});

		Log.Comment("Animating to absolute Offset");
		await ScrollTo(scrollPresenter, newPosition1.X, newPosition1.Y, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore);

		Log.Comment("Animating to absolute Offset");
		await ScrollTo(scrollPresenter, newPosition2.X, newPosition2.Y, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore);

		Log.Comment("Animating to absolute zoomFactor");
		await ZoomTo(scrollPresenter, newZoomFactor1, 100.0f, 200.0f, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore);

		Log.Comment("Animating to absolute zoomFactor");
		await ZoomTo(scrollPresenter, newZoomFactor2, 100.0f, 200.0f, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore);

	}

	[TestMethod]
	[TestProperty("Description", "Performs an animated offsets change with an overridden duration.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task OffsetsChangeWithCustomDuration()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			ScrollPresenter scrollPresenter = null;
			Rectangle rectangleScrollPresenterContent = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				rectangleScrollPresenterContent = new Rectangle();
				scrollPresenter = new ScrollPresenter();

				SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter.ViewChanged += (sender, args) =>
				{
					Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
				};

				scrollPresenter.ScrollAnimationStarting += (sender, args) =>
				{
					Log.Comment("ScrollAnimationStarting - OffsetsChangeCorrelationId={0}", args.CorrelationId);
					Verify.IsNotNull(args.Animation);
					Vector3KeyFrameAnimation stockKeyFrameAnimation = args.Animation as Vector3KeyFrameAnimation;
					Verify.IsNotNull(stockKeyFrameAnimation);
					Log.Comment("Stock duration={0} msec.", stockKeyFrameAnimation.Duration.TotalMilliseconds);
					Verify.AreEqual(c_MaxStockOffsetsChangeDuration, stockKeyFrameAnimation.Duration.TotalMilliseconds);
					stockKeyFrameAnimation.Duration = TimeSpan.FromMilliseconds(10);
				};

				operation = StartScrollTo(
					scrollPresenter,
					600.0,
					400.0,
					ScrollingAnimationMode.Enabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvent);
			});

			await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
				Log.Comment($"Final viewChangedCount={viewChangedCount}");

				Verify.AreEqual(600.0, scrollPresenter.HorizontalOffset);
				Verify.AreEqual(400.0, scrollPresenter.VerticalOffset);
				Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);

				Verify.IsLessThanOrEqual(viewChangedCount, 2u);

				Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Performs an animated zoomFactor change with an overridden duration.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ZoomFactorChangeWithCustomDuration()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			ScrollPresenter scrollPresenter = null;
			Rectangle rectangleScrollPresenterContent = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				rectangleScrollPresenterContent = new Rectangle();
				scrollPresenter = new ScrollPresenter();

				SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter.ViewChanged += (sender, args) =>
				{
					Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
				};

				scrollPresenter.ZoomAnimationStarting += (sender, args) =>
				{
					Log.Comment("ZoomAnimationStarting - ZoomFactorChangeCorrelationId={0}", args.CorrelationId);
					Verify.IsNotNull(args.Animation);
					ScalarKeyFrameAnimation stockKeyFrameAnimation = args.Animation as ScalarKeyFrameAnimation;
					Verify.IsNotNull(stockKeyFrameAnimation);
					Log.Comment("Stock duration={0} msec.", stockKeyFrameAnimation.Duration.TotalMilliseconds);
					Verify.AreEqual(c_MaxStockZoomFactorChangeDuration, stockKeyFrameAnimation.Duration.TotalMilliseconds);
					stockKeyFrameAnimation.Duration = TimeSpan.FromMilliseconds(10);
				};

				operation = StartZoomTo(
					scrollPresenter,
					8.0f,
					100.0f,
					150.0f,
					ScrollingAnimationMode.Enabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvent);
			});

			await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
				Log.Comment($"Final viewChangedCount={viewChangedCount}");

				Verify.IsLessThan(Math.Abs(scrollPresenter.HorizontalOffset - 700.0), 0.01);
				Verify.IsLessThan(Math.Abs(scrollPresenter.VerticalOffset - 1050.0), 0.01);
				Verify.AreEqual(8.0f, scrollPresenter.ZoomFactor);

				Verify.IsLessThanOrEqual(viewChangedCount, 2u);
				Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Requests a view change just before unloading scrollPresenter.")]

	public async Task InterruptViewChangeWithUnloading()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
		ScrollPresenterOperation operation = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent, setAsContentRoot: true);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
			};

			operation = StartScrollTo(
				scrollPresenter,
				600.0,
				400.0,
				ScrollingAnimationMode.Disabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvent);

			Log.Comment("Resetting window content to unparent ScrollPresenter");
			Content = null;
		});

		await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(0.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(0.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Interrupted, operation.Result);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Interrupts an animated offsets change with another one.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task InterruptOffsetsChangeWithOffsetsChange()
	{
		await InterruptViewChange(ViewChangeInterruptionKind.OffsetsChangeByOffsetsChange);
	}

	[TestMethod]
	[TestProperty("Description", "Interrupts an animated offsets change with a zoomFactor change.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task InterruptOffsetsChangeWithZoomFactorChange()
	{
		await InterruptViewChange(ViewChangeInterruptionKind.OffsetsChangeByZoomFactorChange);
	}

	[TestMethod]
	[TestProperty("Description", "Interrupts an animated zoomFactor change with an offsets change.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task InterruptZoomFactorChangeWithOffsetsChange()
	{
		await InterruptViewChange(ViewChangeInterruptionKind.ZoomFactorChangeByOffsetsChange);
	}

	[TestMethod]
	[TestProperty("Description", "Interrupts an animated zoomFactor change with another one.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task InterruptZoomFactorChangeWithZoomFactorChange()
	{
		await InterruptViewChange(ViewChangeInterruptionKind.ZoomFactorChangeByZoomFactorChange);
	}

	[TestMethod]
	[TestProperty("Description", "Attempts an offsets change while there is no content.")]
	public async Task OffsetsChangeWithNoContent()
	{
		ScrollPresenter scrollPresenter = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
		ScrollPresenterOperation operation = null;

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, null /*rectangleScrollPresenterContent*/, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			operation = StartScrollTo(
				scrollPresenter,
				600.0,
				400.0,
				ScrollingAnimationMode.Enabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvent);
		});

		await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(0.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(0.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Ignored, operation.Result);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Attempts a zoomFactor change while there is no content.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ZoomFactorChangeWithNoContent()
	{
		ScrollPresenter scrollPresenter = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
		ScrollPresenterOperation operation = null;

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, null /*rectangleScrollPresenterContent*/, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			operation = StartZoomTo(
				scrollPresenter,
				8.0f,
				100.0f,
				150.0f,
				ScrollingAnimationMode.Enabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvent);
		});

		await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(0.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(0.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Ignored, operation.Result);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Performs consecutive non-animated offsets changes.")]
	public async Task ConsecutiveOffsetJumps()
	{
		await ConsecutiveOffsetJumps(waitForFirstCompletion: true);
		await ConsecutiveOffsetJumps(waitForFirstCompletion: false);
	}

	private async Task ConsecutiveOffsetJumps(bool waitForFirstCompletion)
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent[] scrollPresenterViewChangeOperationEvents = null;
		ScrollPresenterOperation[] operations = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			scrollPresenterViewChangeOperationEvents = new UnoAutoResetEvent[3];
			scrollPresenterViewChangeOperationEvents[0] = new UnoAutoResetEvent(false);
			scrollPresenterViewChangeOperationEvents[1] = new UnoAutoResetEvent(false);
			scrollPresenterViewChangeOperationEvents[2] = new UnoAutoResetEvent(false);

			operations = new ScrollPresenterOperation[3];

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
			};

			operations[0] = StartScrollTo(
				scrollPresenter,
				600.0,
				400.0,
				ScrollingAnimationMode.Disabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvents[0]);

			if (!waitForFirstCompletion)
			{
				operations[1] = StartScrollTo(
					scrollPresenter,
					500.0,
					300.0,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			}
		});

		await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);

		if (waitForFirstCompletion)
		{
			RunOnUIThread.Execute(() =>
			{
				operations[1] = StartScrollTo(
					scrollPresenter,
					500.0,
					300.0,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			});
		}

		await WaitForEvent("Waiting for second view change completion", scrollPresenterViewChangeOperationEvents[1]);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(500.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(300.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[0].Result);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[1].Result);

			// Jump to the same offsets.
			operations[2] = StartScrollTo(
				scrollPresenter,
				500.0,
				300.0,
				ScrollingAnimationMode.Disabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvents[2]);
		});

		await WaitForEvent("Waiting for third view change completion", scrollPresenterViewChangeOperationEvents[2]);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(500.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(300.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[2].Result);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Performs consecutive non-animated zoomFactor changes.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ConsecutiveZoomFactorJumps()
	{
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: false, isSecondZoomRelative: false, waitForFirstCompletion: true);
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: false, isSecondZoomRelative: true, waitForFirstCompletion: true);
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: true, isSecondZoomRelative: false, waitForFirstCompletion: true);
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: true, isSecondZoomRelative: true, waitForFirstCompletion: true);
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: false, isSecondZoomRelative: false, waitForFirstCompletion: false);
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: false, isSecondZoomRelative: true, waitForFirstCompletion: false);
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: true, isSecondZoomRelative: false, waitForFirstCompletion: false);
		await ConsecutiveZoomFactorJumps(isFirstZoomRelative: true, isSecondZoomRelative: true, waitForFirstCompletion: false);
	}

	private async Task ConsecutiveZoomFactorJumps(bool isFirstZoomRelative, bool isSecondZoomRelative, bool waitForFirstCompletion)
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent[] scrollPresenterViewChangeOperationEvents = null;
		ScrollPresenterOperation[] operations = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			scrollPresenterViewChangeOperationEvents = new UnoAutoResetEvent[3];
			scrollPresenterViewChangeOperationEvents[0] = new UnoAutoResetEvent(false);
			scrollPresenterViewChangeOperationEvents[1] = new UnoAutoResetEvent(false);
			scrollPresenterViewChangeOperationEvents[2] = new UnoAutoResetEvent(false);

			operations = new ScrollPresenterOperation[3];

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
			};

			if (isFirstZoomRelative)
			{
				operations[0] = StartZoomBy(
					scrollPresenter,
					7.0f,
					150.0f,
					120.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
			else
			{
				operations[0] = StartZoomTo(
					scrollPresenter,
					8.0f,
					150.0f,
					120.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
		});

		if (waitForFirstCompletion)
		{
			await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);
		}

		RunOnUIThread.Execute(() =>
		{
			if (isFirstZoomRelative)
			{
				operations[1] = StartZoomBy(
					scrollPresenter,
					-1.0f,
					10.0f,
					90.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			}
			else
			{
				operations[1] = StartZoomTo(
					scrollPresenter,
					7.0f,
					10.0f,
					90.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			}
		});

		if (!waitForFirstCompletion)
		{
			await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);
		}
		await WaitForEvent("Waiting for second view change completion", scrollPresenterViewChangeOperationEvents[1]);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(917.5, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(723.75, scrollPresenter.VerticalOffset);
			Verify.AreEqual(7.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[0].Result);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[1].Result);

			// Jump to the same zoomFactor
			operations[2] = StartZoomTo(
				scrollPresenter,
				7.0f,
				10.0f,
				90.0f,
				ScrollingAnimationMode.Disabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvents[2]);
		});

		await WaitForEvent("Waiting for third view change completion", scrollPresenterViewChangeOperationEvents[2]);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(917.5, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(723.75, scrollPresenter.VerticalOffset);
			Verify.AreEqual(7.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[2].Result);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Performs consecutive non-animated offsets and zoom factor changes.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ConsecutiveScrollAndZoomJumps()
	{
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: false, isZoomRelative: false, waitForFirstCompletion: true);
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: false, isZoomRelative: true, waitForFirstCompletion: true);
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: true, isZoomRelative: false, waitForFirstCompletion: true);
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: true, isZoomRelative: true, waitForFirstCompletion: true);
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: false, isZoomRelative: false, waitForFirstCompletion: false);
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: false, isZoomRelative: true, waitForFirstCompletion: false);
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: true, isZoomRelative: false, waitForFirstCompletion: false);
		await ConsecutiveScrollAndZoomJumps(isScrollRelative: true, isZoomRelative: true, waitForFirstCompletion: false);
	}

	private async Task ConsecutiveScrollAndZoomJumps(bool isScrollRelative, bool isZoomRelative, bool waitForFirstCompletion)
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent[] scrollPresenterViewChangeOperationEvents = null;
		ScrollPresenterOperation[] operations = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			scrollPresenterViewChangeOperationEvents = new UnoAutoResetEvent[2];
			scrollPresenterViewChangeOperationEvents[0] = new UnoAutoResetEvent(false);
			scrollPresenterViewChangeOperationEvents[1] = new UnoAutoResetEvent(false);

			operations = new ScrollPresenterOperation[2];

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
			};

			if (isScrollRelative)
			{
				operations[0] = StartScrollBy(
					scrollPresenter,
					80.0,
					35.0,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
			else
			{
				operations[0] = StartScrollTo(
					scrollPresenter,
					80.0,
					35.0,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
		});

		if (waitForFirstCompletion)
		{
			await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);
		}

		RunOnUIThread.Execute(() =>
		{
			if (isZoomRelative)
			{
				operations[1] = StartZoomBy(
					scrollPresenter,
					2.0f,
					10.0f,
					90.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			}
			else
			{
				operations[1] = StartZoomTo(
					scrollPresenter,
					3.0f,
					10.0f,
					90.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			}
		});

		if (!waitForFirstCompletion)
		{
			await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);
		}
		await WaitForEvent("Waiting for second view change completion", scrollPresenterViewChangeOperationEvents[1]);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(260.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(285.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(3.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[0].Result);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[1].Result);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Performs consecutive non-animated zoom factor and offsets changes.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ConsecutiveZoomAndScrollJumps()
	{
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: false, isScrollRelative: false, waitForFirstCompletion: true);
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: false, isScrollRelative: true, waitForFirstCompletion: true);
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: true, isScrollRelative: false, waitForFirstCompletion: true);
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: true, isScrollRelative: true, waitForFirstCompletion: true);
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: false, isScrollRelative: false, waitForFirstCompletion: false);
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: false, isScrollRelative: true, waitForFirstCompletion: false);
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: true, isScrollRelative: false, waitForFirstCompletion: false);
		await ConsecutiveZoomAndScrollJumps(isZoomRelative: true, isScrollRelative: true, waitForFirstCompletion: false);
	}

	private async Task ConsecutiveZoomAndScrollJumps(bool isZoomRelative, bool isScrollRelative, bool waitForFirstCompletion)
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent[] scrollPresenterViewChangeOperationEvents = null;
		ScrollPresenterOperation[] operations = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			scrollPresenterViewChangeOperationEvents = new UnoAutoResetEvent[2];
			scrollPresenterViewChangeOperationEvents[0] = new UnoAutoResetEvent(false);
			scrollPresenterViewChangeOperationEvents[1] = new UnoAutoResetEvent(false);

			operations = new ScrollPresenterOperation[2];

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
			};

			if (isZoomRelative)
			{
				operations[0] = StartZoomBy(
					scrollPresenter,
					2.0f,
					10.0f,
					90.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
			else
			{
				operations[0] = StartZoomTo(
					scrollPresenter,
					3.0f,
					10.0f,
					90.0f,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
		});

		if (waitForFirstCompletion)
		{
			await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);
		}

		RunOnUIThread.Execute(() =>
		{
			if (isScrollRelative)
			{
				operations[1] = StartScrollBy(
					scrollPresenter,
					80.0,
					35.0,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			}
			else
			{
				operations[1] = StartScrollTo(
					scrollPresenter,
					80.0,
					35.0,
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[1]);
			}
		});

		if (!waitForFirstCompletion)
		{
			await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);
		}
		await WaitForEvent("Waiting for second view change completion", scrollPresenterViewChangeOperationEvents[1]);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			if (isScrollRelative)
			{
				Verify.AreEqual(100.0, scrollPresenter.HorizontalOffset);
				Verify.AreEqual(215.0, scrollPresenter.VerticalOffset);
			}
			else
			{
				Verify.AreEqual(80.0, scrollPresenter.HorizontalOffset);
				Verify.AreEqual(35.0, scrollPresenter.VerticalOffset);
			}
			Verify.AreEqual(3.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[0].Result);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[1].Result);
		});
	}

	[TestProperty("Description", "Requests a non-animated offsets change before loading scrollPresenter.")]
	public async Task SetOffsetsBeforeLoading()
	{
		await ChangeOffsetsBeforeLoading(false /*animate*/);
	}

	[TestMethod]
	[TestProperty("Description", "Requests an animated offsets change before loading scrollPresenter.")]
	[Ignore("ScrollingAnimationMode.Enabled requires InteractionTracker's CustomAnimation state")]
	public async Task AnimateOffsetsBeforeLoading()
	{
		await ChangeOffsetsBeforeLoading(true /*animate*/);
	}

	[TestMethod]
	[TestProperty("Description", "Requests a non-animated zoomFactor change before loading scrollPresenter.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task SetZoomFactorBeforeLoading()
	{
		await ChangeZoomFactorBeforeLoading(false /*animate*/);
	}

	[TestMethod]
	[TestProperty("Description", "Requests an animated zoomFactor change before loading scrollPresenter.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnimateZoomFactorBeforeLoading()
	{
		await ChangeZoomFactorBeforeLoading(true /*animate*/);
	}

	[TestMethod]
	[TestProperty("Description", "Requests a non-animated offset change immediately after increasing content size.")]
	public async Task OffsetJumpAfterContentResizing()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent.Width = c_defaultUIScrollPresenterContentWidth + 200.0;
		});

		// Jump to absolute offsets
		await ScrollTo(
			scrollPresenter,
			c_defaultUIScrollPresenterContentWidth + 200.0 - c_defaultUIScrollPresenterWidth,
			c_defaultVerticalOffset,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore);
	}

	private async Task ChangeOffsetsBeforeLoading(bool animate)
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
		ScrollPresenterOperation operation = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent, false /*setAsContentRoot*/);

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
			};

			operation = StartScrollTo(
				scrollPresenter,
				600.0,
				400.0,
				animate ? ScrollingAnimationMode.Enabled : ScrollingAnimationMode.Disabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvent);

			Log.Comment("Setting window content");
			Content = scrollPresenter;
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.AreEqual(600.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(400.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);
		});
	}

	private async Task ChangeZoomFactorBeforeLoading(bool animate)
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
		ScrollPresenterOperation operation = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent, false /*setAsContentRoot*/);

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
			};

			operation = StartZoomTo(
				scrollPresenter,
				8.0f,
				100.0f,
				150.0f,
				animate ? ScrollingAnimationMode.Enabled : ScrollingAnimationMode.Disabled,
				ScrollingSnapPointsMode.Ignore,
				scrollPresenterViewChangeOperationEvent);

			Log.Comment("Setting window content");
			Content = scrollPresenter;
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			Verify.IsLessThan(Math.Abs(scrollPresenter.HorizontalOffset - 700.0), 0.01);
			Verify.IsLessThan(Math.Abs(scrollPresenter.VerticalOffset - 1050.0), 0.01);
			Verify.AreEqual(8.0f, scrollPresenter.ZoomFactor);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);
		});
	}

	private async Task ScrollTo(
		ScrollPresenter scrollPresenter,
		double horizontalOffset,
		double verticalOffset,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		bool hookViewChanged = true,
		bool? isAnimationsEnabledOverride = null,
		double? expectedFinalHorizontalOffset = null,
		double? expectedFinalVerticalOffset = null)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: false,
			enableInteractionSourcesNotifications: false,
			enableExpressionAnimationStatusNotifications: true,
			isAnimationsEnabledOverride: isAnimationsEnabledOverride))
		{
			Log.Comment("Waiting for any pending ExpressionAnimation start/stop notifications to occur");
			await CompositionPropertySpy.SynchronouslyTickUIThread(6);

			float originalZoomFactor = 1.0f;
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				scrollPresenterTestHooksHelper.ResetExpressionAnimationStatusChanges(scrollPresenter);

				if (hookViewChanged)
				{
					scrollPresenter.ViewChanged += (sender, args) =>
					{
						Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
					};
				}

				originalZoomFactor = scrollPresenter.ZoomFactor;

				if (expectedFinalHorizontalOffset == null)
				{
					expectedFinalHorizontalOffset = horizontalOffset;
				}

				if (expectedFinalVerticalOffset == null)
				{
					expectedFinalVerticalOffset = verticalOffset;
				}

				operation = StartScrollTo(
					scrollPresenter,
					horizontalOffset,
					verticalOffset,
					animationMode,
					snapPointsMode,
					scrollPresenterViewChangeOperationEvent);
			});

			await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
				Log.Comment($"Final viewChangedCount={viewChangedCount}");

				Verify.AreEqual(expectedFinalHorizontalOffset, scrollPresenter.HorizontalOffset);
				Verify.AreEqual(expectedFinalVerticalOffset, scrollPresenter.VerticalOffset);
				Verify.AreEqual(originalZoomFactor, scrollPresenter.ZoomFactor);
				Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);

				if (GetEffectiveIsAnimationEnabled(animationMode, isAnimationsEnabledOverride))
				{
					Verify.IsFalse(viewChangedCount == 1u);
				}
				else
				{
					Verify.IsLessThanOrEqual(viewChangedCount, 1u);
				}
			});

			Log.Comment("Waiting for any ExpressionAnimation start/stop notification");
			await CompositionPropertySpy.SynchronouslyTickUIThread(6);

			RunOnUIThread.Execute(() =>
			{
				List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges = scrollPresenterTestHooksHelper.GetExpressionAnimationStatusChanges(scrollPresenter);
				ScrollPresenterTestHooksHelper.LogExpressionAnimationStatusChanges(expressionAnimationStatusChanges);
				Verify.IsNull(expressionAnimationStatusChanges);
			});
		}
	}

	private async Task ScrollBy(
		ScrollPresenter scrollPresenter,
		double horizontalOffsetDelta,
		double verticalOffsetDelta,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		bool hookViewChanged = true,
		bool? isAnimationsEnabledOverride = null,
		double? expectedFinalHorizontalOffset = null,
		double? expectedFinalVerticalOffset = null)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: false,
			enableInteractionSourcesNotifications: false,
			enableExpressionAnimationStatusNotifications: true,
			isAnimationsEnabledOverride: isAnimationsEnabledOverride))
		{
			Log.Comment("Waiting for any pending ExpressionAnimation start/stop notifications to occur");
			await CompositionPropertySpy.SynchronouslyTickUIThread(6);

			double originalHorizontalOffset = 0.0;
			double originalVerticalOffset = 0.0;
			float originalZoomFactor = 1.0f;
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				scrollPresenterTestHooksHelper.ResetExpressionAnimationStatusChanges(scrollPresenter);

				if (hookViewChanged)
				{
					scrollPresenter.ViewChanged += (sender, args) =>
					{
						Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
					};
				}

				originalHorizontalOffset = scrollPresenter.HorizontalOffset;
				originalVerticalOffset = scrollPresenter.VerticalOffset;
				originalZoomFactor = scrollPresenter.ZoomFactor;

				Log.Comment($"Original HorizontalOffset={originalHorizontalOffset}, VerticalOffset={originalVerticalOffset}, ZoomFactor={originalZoomFactor}");

				if (expectedFinalHorizontalOffset == null)
				{
					expectedFinalHorizontalOffset = horizontalOffsetDelta + originalHorizontalOffset;
				}

				if (expectedFinalVerticalOffset == null)
				{
					expectedFinalVerticalOffset = verticalOffsetDelta + originalVerticalOffset;
				}

				operation = StartScrollBy(
					scrollPresenter,
					horizontalOffsetDelta,
					verticalOffsetDelta,
					animationMode,
					snapPointsMode,
					scrollPresenterViewChangeOperationEvent);
			});

			await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
				Log.Comment($"Final viewChangedCount={viewChangedCount}");

				Verify.AreEqual(expectedFinalHorizontalOffset, scrollPresenter.HorizontalOffset);
				Verify.AreEqual(expectedFinalVerticalOffset, scrollPresenter.VerticalOffset);
				Verify.AreEqual(originalZoomFactor, scrollPresenter.ZoomFactor);
				Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);

				if (GetEffectiveIsAnimationEnabled(animationMode, isAnimationsEnabledOverride))
				{
					Verify.IsGreaterThan(viewChangedCount, 1u);
				}
				else
				{
					Verify.AreEqual(1u, viewChangedCount);
				}
			});

			Log.Comment("Waiting for any ExpressionAnimation start/stop notification");
			await CompositionPropertySpy.SynchronouslyTickUIThread(6);

			RunOnUIThread.Execute(() =>
			{
				List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges = scrollPresenterTestHooksHelper.GetExpressionAnimationStatusChanges(scrollPresenter);
				ScrollPresenterTestHooksHelper.LogExpressionAnimationStatusChanges(expressionAnimationStatusChanges);
				Verify.IsNull(expressionAnimationStatusChanges);
			});
		}
	}

	private async Task AddScrollVelocity(
		ScrollPresenter scrollPresenter,
		float horizontalVelocity,
		float verticalVelocity,
		float? horizontalInertiaDecayRate,
		float? verticalInertiaDecayRate,
		bool waitForViewChangeCompletion = true,
		bool hookViewChanged = true)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: false,
			enableInteractionSourcesNotifications: false,
			enableExpressionAnimationStatusNotifications: true))
		{
			Log.Comment("Waiting for any pending ExpressionAnimation start/stop notifications to occur");
			await CompositionPropertySpy.SynchronouslyTickUIThread(6);

			double originalHorizontalOffset = 0.0;
			double originalVerticalOffset = 0.0;
			float originalZoomFactor = 1.0f;
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				scrollPresenterTestHooksHelper.ResetExpressionAnimationStatusChanges(scrollPresenter);

				if (hookViewChanged)
				{
					scrollPresenter.ViewChanged += (sender, args) =>
					{
						Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
					};
				}

				originalHorizontalOffset = scrollPresenter.HorizontalOffset;
				originalVerticalOffset = scrollPresenter.VerticalOffset;
				originalZoomFactor = scrollPresenter.ZoomFactor;

				Log.Comment($"Original HorizontalOffset={originalHorizontalOffset}, VerticalOffset={originalVerticalOffset}, ZoomFactor={originalZoomFactor}");

				operation = StartAddScrollVelocity(
					scrollPresenter,
					horizontalVelocity,
					verticalVelocity,
					horizontalInertiaDecayRate,
					verticalInertiaDecayRate,
					scrollPresenterViewChangeOperationEvent);
			});

			if (waitForViewChangeCompletion)
			{
				await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

				RunOnUIThread.Execute(() =>
				{
					Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

					if (horizontalVelocity > 0)
						Verify.IsTrue(originalHorizontalOffset < scrollPresenter.HorizontalOffset);
					else if (horizontalVelocity < 0)
						Verify.IsTrue(originalHorizontalOffset > scrollPresenter.HorizontalOffset);
					else
						Verify.IsTrue(originalHorizontalOffset == scrollPresenter.HorizontalOffset);
					if (verticalVelocity > 0)
						Verify.IsTrue(originalVerticalOffset < scrollPresenter.VerticalOffset);
					else if (verticalVelocity < 0)
						Verify.IsTrue(originalVerticalOffset > scrollPresenter.VerticalOffset);
					else
						Verify.IsTrue(originalVerticalOffset == scrollPresenter.VerticalOffset);
					Verify.AreEqual(originalZoomFactor, scrollPresenter.ZoomFactor);
					Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);
				});

				Log.Comment("Waiting for any ExpressionAnimation start/stop notification");
				await CompositionPropertySpy.SynchronouslyTickUIThread(6);

				RunOnUIThread.Execute(() =>
				{
					List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges = scrollPresenterTestHooksHelper.GetExpressionAnimationStatusChanges(scrollPresenter);
					ScrollPresenterTestHooksHelper.LogExpressionAnimationStatusChanges(expressionAnimationStatusChanges);
					Verify.IsNull(expressionAnimationStatusChanges);
				});
			}
		}
	}

	private ScrollPresenterOperation StartScrollTo(
		ScrollPresenter scrollPresenter,
		double horizontalOffset,
		double verticalOffset,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent)
	{
		Log.Comment("ScrollTo - horizontalOffset={0}, verticalOffset={1}, animationMode={2}, snapPointsMode={3}",
			horizontalOffset, verticalOffset, animationMode, snapPointsMode);

		viewChangedCount = 0u;
		ScrollPresenterOperation operation = new ScrollPresenterOperation();

		operation.CorrelationId = scrollPresenter.ScrollTo(
			horizontalOffset,
			verticalOffset,
			new ScrollingScrollOptions(animationMode, snapPointsMode));

		if (operation.CorrelationId == -1)
		{
			scrollPresenterViewChangeOperationEvent.Set();
		}
		else
		{
			scrollPresenter.ScrollCompleted += (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args) =>
			{
				if (args.CorrelationId == operation.CorrelationId)
				{
					ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);

					Log.Comment("ScrollCompleted: ScrollTo OffsetsChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
					operation.Result = result;

					Log.Comment("Setting completion event");
					scrollPresenterViewChangeOperationEvent.Set();
				}
			};
		}

		return operation;
	}

	private ScrollPresenterOperation StartScrollBy(
		ScrollPresenter scrollPresenter,
		double horizontalOffsetDelta,
		double verticalOffsetDelta,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent)
	{
		Log.Comment("ScrollBy - horizontalOffsetDelta={0}, verticalOffsetDelta={1}, animationMode={2}, snapPointsMode={3}",
			horizontalOffsetDelta, verticalOffsetDelta, animationMode, snapPointsMode);

		viewChangedCount = 0u;
		ScrollPresenterOperation operation = new ScrollPresenterOperation();

		operation.CorrelationId = scrollPresenter.ScrollBy(
			horizontalOffsetDelta,
			verticalOffsetDelta,
			new ScrollingScrollOptions(animationMode, snapPointsMode));

		if (operation.CorrelationId == -1)
		{
			scrollPresenterViewChangeOperationEvent.Set();
		}
		else
		{
			scrollPresenter.ScrollCompleted += (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args) =>
			{
				if (args.CorrelationId == operation.CorrelationId)
				{
					ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);

					Log.Comment("ScrollCompleted: ScrollBy OffsetsChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
					operation.Result = result;

					Log.Comment("Setting completion event");
					scrollPresenterViewChangeOperationEvent.Set();
				}
			};
		}

		return operation;
	}

	private ScrollPresenterOperation StartAddScrollVelocity(
		ScrollPresenter scrollPresenter,
		float horizontalVelocity,
		float verticalVelocity,
		float? horizontalInertiaDecayRate,
		float? verticalInertiaDecayRate,
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent)
	{
		Log.Comment("AddScrollVelocity - horizontalVelocity={0}, verticalVelocity={1}, horizontalInertiaDecayRate={2}, verticalInertiaDecayRate={3}",
			horizontalVelocity, verticalVelocity, horizontalInertiaDecayRate, verticalInertiaDecayRate);

		Vector2? inertiaDecayRate = null;

		if (horizontalInertiaDecayRate != null && verticalInertiaDecayRate != null)
		{
			inertiaDecayRate = new Vector2((float)horizontalInertiaDecayRate, (float)verticalInertiaDecayRate);
		}

		viewChangedCount = 0u;
		ScrollPresenterOperation operation = new ScrollPresenterOperation();

		operation.CorrelationId = scrollPresenter.AddScrollVelocity(
				new Vector2(horizontalVelocity, verticalVelocity),
				inertiaDecayRate);

		if (operation.CorrelationId == -1)
		{
			scrollPresenterViewChangeOperationEvent.Set();
		}
		else
		{
			scrollPresenter.ScrollCompleted += (ScrollPresenter sender, ScrollingScrollCompletedEventArgs args) =>
			{
				if (args.CorrelationId == operation.CorrelationId)
				{
					ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);

					Log.Comment("ScrollCompleted: AddScrollVelocity OffsetsChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
					operation.Result = result;

					Log.Comment("Setting completion event");
					scrollPresenterViewChangeOperationEvent.Set();
				}
			};
		}

		return operation;
	}

	private async Task ZoomTo(
		ScrollPresenter scrollPresenter,
		float zoomFactor,
		float centerPointX,
		float centerPointY,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		bool hookViewChanged = true,
		bool? isAnimationsEnabledOverride = null)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: false,
			enableInteractionSourcesNotifications: false,
			enableExpressionAnimationStatusNotifications: true,
			isAnimationsEnabledOverride: isAnimationsEnabledOverride))
		{
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				if (hookViewChanged)
				{
					scrollPresenter.ViewChanged += (sender, args) =>
					{
						Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
					};
				}

				operation = StartZoomTo(
					scrollPresenter,
					zoomFactor,
					centerPointX,
					centerPointY,
					animationMode,
					snapPointsMode,
					scrollPresenterViewChangeOperationEvent);
			});

			await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
				Log.Comment($"Final viewChangedCount={viewChangedCount}");

				Verify.AreEqual(zoomFactor, scrollPresenter.ZoomFactor);
				Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);

				if (GetEffectiveIsAnimationEnabled(animationMode, isAnimationsEnabledOverride))
				{
					Verify.IsGreaterThan(viewChangedCount, 1u);
				}
				else
				{
					Verify.AreEqual(1u, viewChangedCount);
				}
			});

			Log.Comment("Waiting for any ExpressionAnimation start/stop notification");
			await CompositionPropertySpy.SynchronouslyTickUIThread(6);

			RunOnUIThread.Execute(() =>
			{
				List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges = scrollPresenterTestHooksHelper.GetExpressionAnimationStatusChanges(scrollPresenter);
				ScrollPresenterTestHooksHelper.LogExpressionAnimationStatusChanges(expressionAnimationStatusChanges);
				VerifyExpressionAnimationStatusChangesForTranslationAndZoomFactorSuspension(expressionAnimationStatusChanges);
			});
		}
	}

	private async Task ZoomBy(
		ScrollPresenter scrollPresenter,
		float zoomFactorDelta,
		float centerPointX,
		float centerPointY,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		bool hookViewChanged = true,
		bool? isAnimationsEnabledOverride = null)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: false,
			enableInteractionSourcesNotifications: false,
			enableExpressionAnimationStatusNotifications: true,
			isAnimationsEnabledOverride: isAnimationsEnabledOverride))
		{
			float originalZoomFactor = 1.0f;
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				if (hookViewChanged)
				{
					scrollPresenter.ViewChanged += (sender, args) =>
					{
						Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
					};
				}

				originalZoomFactor = scrollPresenter.ZoomFactor;

				Log.Comment($"Original HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={originalZoomFactor}");

				operation = StartZoomBy(
					scrollPresenter,
					zoomFactorDelta,
					centerPointX,
					centerPointY,
					animationMode,
					snapPointsMode,
					scrollPresenterViewChangeOperationEvent);
			});

			await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment($"Final HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
				Log.Comment($"Final viewChangedCount={viewChangedCount}");

				Verify.AreEqual(zoomFactorDelta + originalZoomFactor, scrollPresenter.ZoomFactor);
				Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);

				if (GetEffectiveIsAnimationEnabled(animationMode, isAnimationsEnabledOverride))
				{
					Verify.IsGreaterThan(viewChangedCount, 1u);
				}
				else
				{
					Verify.AreEqual(1u, viewChangedCount);
				}
			});

			Log.Comment("Waiting for any ExpressionAnimation start/stop notification");
			await CompositionPropertySpy.SynchronouslyTickUIThread(6);

			RunOnUIThread.Execute(() =>
			{
				List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges = scrollPresenterTestHooksHelper.GetExpressionAnimationStatusChanges(scrollPresenter);
				ScrollPresenterTestHooksHelper.LogExpressionAnimationStatusChanges(expressionAnimationStatusChanges);
				VerifyExpressionAnimationStatusChangesForTranslationAndZoomFactorSuspension(expressionAnimationStatusChanges);
			});
		}
	}

	private async Task AddZoomVelocity(
		ScrollPresenter scrollPresenter,
		float zoomFactorVelocity,
		float? inertiaDecayRate,
		float centerPointX,
		float centerPointY,
		bool waitForViewChangeCompletion = true,
		bool hookViewChanged = true)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: false,
			enableInteractionSourcesNotifications: false,
			enableExpressionAnimationStatusNotifications: true))
		{
			float originalZoomFactor = 1.0f;
			UnoAutoResetEvent scrollPresenterViewChangeOperationEvent = new UnoAutoResetEvent(false);
			ScrollPresenterOperation operation = null;

			RunOnUIThread.Execute(() =>
			{
				if (hookViewChanged)
				{
					scrollPresenter.ViewChanged += (sender, args) =>
					{
						Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
					};
				}

				originalZoomFactor = scrollPresenter.ZoomFactor;

				Log.Comment($"Original HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={originalZoomFactor}");

				operation = StartAddZoomVelocity(
					scrollPresenter,
					zoomFactorVelocity,
					inertiaDecayRate,
					centerPointX,
					centerPointY,
					scrollPresenterViewChangeOperationEvent);
			});

			if (waitForViewChangeCompletion)
			{
				await WaitForEvent("Waiting for view change completion", scrollPresenterViewChangeOperationEvent);

				RunOnUIThread.Execute(() =>
				{
					Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

					if (zoomFactorVelocity > 0)
						Verify.IsTrue(originalZoomFactor < scrollPresenter.ZoomFactor);
					else if (zoomFactorVelocity < 0)
						Verify.IsTrue(originalZoomFactor > scrollPresenter.ZoomFactor);
					else
						Verify.IsTrue(originalZoomFactor == scrollPresenter.ZoomFactor);
					Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operation.Result);
				});

				Log.Comment("Waiting for any ExpressionAnimation start/stop notification");
				await CompositionPropertySpy.SynchronouslyTickUIThread(6);

				RunOnUIThread.Execute(() =>
				{
					List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges = scrollPresenterTestHooksHelper.GetExpressionAnimationStatusChanges(scrollPresenter);
					ScrollPresenterTestHooksHelper.LogExpressionAnimationStatusChanges(expressionAnimationStatusChanges);
					VerifyExpressionAnimationStatusChangesForTranslationAndZoomFactorSuspension(expressionAnimationStatusChanges);
				});
			}
		}
	}

	private ScrollPresenterOperation StartZoomTo(
		ScrollPresenter scrollPresenter,
		float zoomFactor,
		float centerPointX,
		float centerPointY,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent)
	{
		Log.Comment("ZoomTo - zoomFactor={0}, centerPoint=({1},{2}), animationMode={3}, snapPointsMode={4}",
			zoomFactor, centerPointX, centerPointY, animationMode, snapPointsMode);

		viewChangedCount = 0u;
		ScrollPresenterOperation operation = new ScrollPresenterOperation();

		operation.CorrelationId = scrollPresenter.ZoomTo(
			zoomFactor,
			new Vector2(centerPointX, centerPointY),
			new ScrollingZoomOptions(animationMode, snapPointsMode));

		if (operation.CorrelationId == -1)
		{
			scrollPresenterViewChangeOperationEvent.Set();
		}
		else
		{
			scrollPresenter.ZoomCompleted += (ScrollPresenter sender, ScrollingZoomCompletedEventArgs args) =>
			{
				if (args.CorrelationId == operation.CorrelationId)
				{
					ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetZoomCompletedResult(args);

					Log.Comment("ZoomCompleted: ZoomTo ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
					operation.Result = result;

					Log.Comment("Setting completion event");
					scrollPresenterViewChangeOperationEvent.Set();
				}
			};
		}

		return operation;
	}

	private ScrollPresenterOperation StartZoomBy(
		ScrollPresenter scrollPresenter,
		float zoomFactorDelta,
		float centerPointX,
		float centerPointY,
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode,
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent)
	{
		Log.Comment("ZoomBy - zoomFactorDelta={0}, centerPoint=({1},{2}), animationMode={3}, snapPointsMode={4}",
			zoomFactorDelta, centerPointX, centerPointY, animationMode, snapPointsMode);

		viewChangedCount = 0u;
		ScrollPresenterOperation operation = new ScrollPresenterOperation();

		operation.CorrelationId = scrollPresenter.ZoomBy(
			zoomFactorDelta,
			new Vector2(centerPointX, centerPointY),
			new ScrollingZoomOptions(animationMode, snapPointsMode));

		if (operation.CorrelationId == -1)
		{
			scrollPresenterViewChangeOperationEvent.Set();
		}
		else
		{
			scrollPresenter.ZoomCompleted += (ScrollPresenter sender, ScrollingZoomCompletedEventArgs args) =>
			{
				if (args.CorrelationId == operation.CorrelationId)
				{
					ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetZoomCompletedResult(args);

					Log.Comment("ZoomCompleted: ZoomBy ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
					operation.Result = result;

					Log.Comment("Setting completion event");
					scrollPresenterViewChangeOperationEvent.Set();
				}
			};
		}

		return operation;
	}

	private ScrollPresenterOperation StartAddZoomVelocity(
		ScrollPresenter scrollPresenter,
		float zoomFactorVelocity,
		float? inertiaDecayRate,
		float centerPointX,
		float centerPointY,
		UnoAutoResetEvent scrollPresenterViewChangeOperationEvent)
	{
		Log.Comment("AddZoomVelocity - zoomFactorVelocity={0}, inertiaDecayRate={1}, centerPoint=({2},{3})",
			zoomFactorVelocity, inertiaDecayRate, centerPointX, centerPointY);

		viewChangedCount = 0u;
		ScrollPresenterOperation operation = new ScrollPresenterOperation();

		operation.CorrelationId = scrollPresenter.AddZoomVelocity(
			zoomFactorVelocity, new Vector2(centerPointX, centerPointY), inertiaDecayRate);

		if (operation.CorrelationId == -1)
		{
			scrollPresenterViewChangeOperationEvent.Set();
		}
		else
		{
			scrollPresenter.ZoomCompleted += (ScrollPresenter sender, ScrollingZoomCompletedEventArgs args) =>
			{
				if (args.CorrelationId == operation.CorrelationId)
				{
					ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetZoomCompletedResult(args);

					Log.Comment("ZoomCompleted: AddZoomVelocity ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
					operation.Result = result;

					Log.Comment("Setting completion event");
					scrollPresenterViewChangeOperationEvent.Set();
				}
			};
		}
		return operation;
	}

	private async Task InterruptViewChange(
		ViewChangeInterruptionKind viewChangeInterruptionKind)
	{
		bool viewChangeInterruptionDone = false;
		bool changeOffsetsFirst =
			viewChangeInterruptionKind == ViewChangeInterruptionKind.OffsetsChangeByOffsetsChange || viewChangeInterruptionKind == ViewChangeInterruptionKind.OffsetsChangeByZoomFactorChange;
		bool changeOffsetsSecond =
			viewChangeInterruptionKind == ViewChangeInterruptionKind.OffsetsChangeByOffsetsChange || viewChangeInterruptionKind == ViewChangeInterruptionKind.ZoomFactorChangeByOffsetsChange;
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent[] scrollPresenterViewChangeOperationEvents = null;
		ScrollPresenterOperation[] operations = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			scrollPresenterViewChangeOperationEvents = new UnoAutoResetEvent[2];
			scrollPresenterViewChangeOperationEvents[0] = new UnoAutoResetEvent(false);
			scrollPresenterViewChangeOperationEvents[1] = new UnoAutoResetEvent(false);

			operations = new ScrollPresenterOperation[2];
			operations[0] = null;
			operations[1] = null;

			scrollPresenter.ViewChanged += (sender, args) =>
			{
				Log.Comment($"ViewChanged viewChangedCount={++viewChangedCount} - HorizontalOffset={sender.HorizontalOffset}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");

				if (viewChangedCount == 3u && !viewChangeInterruptionDone)
				{
					viewChangeInterruptionDone = true;
					if (changeOffsetsSecond)
					{
						operations[1] = StartScrollTo(
							scrollPresenter,
							500.0,
							300.0,
							ScrollingAnimationMode.Enabled,
							ScrollingSnapPointsMode.Ignore,
							scrollPresenterViewChangeOperationEvents[1]);
					}
					else
					{
						operations[1] = StartZoomTo(
							scrollPresenter,
							7.0f,
							70.0f,
							50.0f,
							ScrollingAnimationMode.Enabled,
							ScrollingSnapPointsMode.Ignore,
							scrollPresenterViewChangeOperationEvents[1]);
					}
				}
			};

			if (changeOffsetsFirst)
			{
				operations[0] = StartScrollTo(
					scrollPresenter,
					600.0,
					400.0,
					ScrollingAnimationMode.Enabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
			else
			{
				operations[0] = StartZoomTo(
					scrollPresenter,
					8.0f,
					100.0f,
					150.0f,
					ScrollingAnimationMode.Enabled,
					ScrollingSnapPointsMode.Ignore,
					scrollPresenterViewChangeOperationEvents[0]);
			}
		});

		await WaitForEvent("Waiting for first view change completion", scrollPresenterViewChangeOperationEvents[0]);

		await WaitForEvent("Waiting for second view change completion", scrollPresenterViewChangeOperationEvents[1]);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Final HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
				scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

			if (changeOffsetsFirst && changeOffsetsSecond)
			{
				Verify.AreEqual(500.0, scrollPresenter.HorizontalOffset);
				Verify.AreEqual(300.0, scrollPresenter.VerticalOffset);
				Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
			}
			if (changeOffsetsFirst && !changeOffsetsSecond)
			{
				Verify.IsGreaterThanOrEqual(scrollPresenter.HorizontalOffset, 600.0);
				Verify.IsGreaterThanOrEqual(scrollPresenter.VerticalOffset, 400.0);
				Verify.AreEqual(7.0f, scrollPresenter.ZoomFactor);
			}
			if (!changeOffsetsFirst && changeOffsetsSecond)
			{
				Verify.IsGreaterThanOrEqual(scrollPresenter.HorizontalOffset, 500.0);
				Verify.IsGreaterThanOrEqual(scrollPresenter.VerticalOffset, 300.0);
				Verify.AreEqual(8.0f, scrollPresenter.ZoomFactor);
			}
			if (!changeOffsetsFirst && !changeOffsetsSecond)
			{
				Verify.AreEqual(7.0f, scrollPresenter.ZoomFactor);
			}

			Verify.AreEqual(ScrollPresenterViewChangeResult.Interrupted, operations[0].Result);
			Verify.AreEqual(ScrollPresenterViewChangeResult.Completed, operations[1].Result);
		});
	}

	private async Task WaitForEvent(string logComment, UnoManualOrAutoResetEvent eventWaitHandle)
	{
		Log.Comment(logComment);
		if (Debugger.IsAttached)
		{
			await eventWaitHandle.WaitOne();
		}
		else
		{
			if (!await eventWaitHandle.WaitOne(TimeSpan.FromMilliseconds(c_MaxWaitDuration)))
			{
				throw new Exception("Timeout expiration in WaitForEvent.");
			}
		}
	}

	private void VerifyExpressionAnimationStatusChangesForTranslationAndZoomFactorSuspension(List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges)
	{
		// Facades are enabled. The translation and zoom factor animations are expected to be interrupted momentarily.
		Verify.IsNotNull(expressionAnimationStatusChanges);
		Verify.AreEqual(4, expressionAnimationStatusChanges.Count);

		Verify.IsFalse(expressionAnimationStatusChanges[0].IsExpressionAnimationStarted);
		Verify.IsFalse(expressionAnimationStatusChanges[1].IsExpressionAnimationStarted);
		Verify.IsTrue(expressionAnimationStatusChanges[2].IsExpressionAnimationStarted);
		Verify.IsTrue(expressionAnimationStatusChanges[3].IsExpressionAnimationStarted);

		Verify.AreEqual("Translation", expressionAnimationStatusChanges[0].PropertyName);
		Verify.AreEqual("Scale", expressionAnimationStatusChanges[1].PropertyName);
		Verify.AreEqual("Translation", expressionAnimationStatusChanges[2].PropertyName);
		Verify.AreEqual("Scale", expressionAnimationStatusChanges[3].PropertyName);
	}

	private bool GetEffectiveIsAnimationEnabled(ScrollingAnimationMode animationMode, bool? isAnimationsEnabledOverride)
	{
		switch (animationMode)
		{
			case ScrollingAnimationMode.Auto:
				switch (isAnimationsEnabledOverride)
				{
					case null:
						return new UISettings().AnimationsEnabled;
					default:
						return isAnimationsEnabledOverride.Value;
				}
			case ScrollingAnimationMode.Enabled:
				return true;
		}

		return false;
	}

	private class ScrollPresenterOperation
	{
		public ScrollPresenterOperation()
		{
			Result = ScrollPresenterViewChangeResult.Ignored;
			CorrelationId = -1;
		}

		public ScrollPresenterViewChangeResult Result
		{
			get;
			set;
		}

		public int CorrelationId
		{
			get;
			set;
		}
	}
}
