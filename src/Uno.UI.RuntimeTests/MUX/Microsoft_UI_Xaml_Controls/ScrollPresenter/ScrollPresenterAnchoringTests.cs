// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using MUXControlsTestApp.Utilities;
using Windows.UI;
using Private.Infrastructure;
using System.Threading.Tasks;


//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

partial class ScrollPresenterTests : MUXApiTestBase
{
	private const double c_defaultAnchoringUIScrollPresenterNonConstrainedSize = 600.0;
	private const double c_defaultAnchoringUIScrollPresenterConstrainedSize = 300.0;
	private const int c_defaultAnchoringUIStackPanelChildrenCount = 16;
	private const int c_defaultAnchoringUIRepeaterChildrenCount = 16;

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset remains at 0 when inserting an item at the beginning (HorizontalAnchorRatio=0).")]
	public async Task AnchoringAtLeftEdge()
	{
		await AnchoringAtNearEdge(Orientation.Horizontal);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset remains at 0 when inserting an item at the beginning (VerticalAnchorRatio=0).")]
	public async Task AnchoringAtTopEdge()
	{
		await AnchoringAtNearEdge(Orientation.Vertical);
	}

	private async Task AnchoringAtNearEdge(Orientation orientation)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			ScrollPresenter scrollPresenter = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter = new ScrollPresenter();

				SetupDefaultAnchoringUI(orientation, scrollPresenter, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Inserting child at near edge");
				InsertStackPanelChild((scrollPresenter.Content as Border).Child as StackPanel, 1 /*operationCount*/, 0 /*newIndex*/, 1 /*newCount*/);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("No ScrollPresenter offset change expected");
				if (orientation == Orientation.Vertical)
				{
					Verify.AreEqual(0, scrollPresenter.VerticalOffset);
				}
				else
				{
					Verify.AreEqual(0, scrollPresenter.HorizontalOffset);
				}
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset growns to max value when inserting an item at the end (HorizontalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtRightEdgeWhileIncreasingContentWidth()
	{
		await AnchoringAtFarEdgeWhileIncreasingContent(Orientation.Horizontal, 0 /*viewportSizeChange*/, 3876 /*expectedFinalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset grows to max value when inserting an item at the end (VerticalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtBottomEdgeWhileIncreasingContentHeight()
	{
		await AnchoringAtFarEdgeWhileIncreasingContent(Orientation.Vertical, 0 /*viewportSizeChange*/, 3876 /*expectedFinalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset growns to max value when inserting an item at the end and growing viewport (HorizontalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtRightEdgeWhileIncreasingContentAndViewportWidth()
	{
		await AnchoringAtFarEdgeWhileIncreasingContent(Orientation.Horizontal, 10 /*viewportSizeChange*/, 3866 /*expectedFinalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset grows to max value when inserting an item at the end and growning viewport (VerticalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtBottomEdgeWhileIncreasingContentAndViewportHeight()
	{
		await AnchoringAtFarEdgeWhileIncreasingContent(Orientation.Vertical, 10 /*viewportSizeChange*/, 3866 /*expectedFinalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset growns to max value when inserting an item at the end and shrinking viewport (HorizontalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtRightEdgeWhileIncreasingContentAndDecreasingViewportWidth()
	{
		await AnchoringAtFarEdgeWhileIncreasingContent(Orientation.Horizontal, -10 /*viewportSizeChange*/, 3886 /*expectedFinalOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset grows to max value when inserting an item at the end and shrinking viewport (VerticalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtBottomEdgeWhileIncreasingContentAndDecreasingViewportHeight()
	{
		await AnchoringAtFarEdgeWhileIncreasingContent(Orientation.Vertical, -10 /*viewportSizeChange*/, 3886 /*expectedFinalOffset*/);
	}

	private async Task AnchoringAtFarEdgeWhileIncreasingContent(Orientation orientation, double viewportSizeChange, double expectedFinalOffset)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			ScrollPresenter scrollPresenter = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter = new ScrollPresenter();

				SetupDefaultAnchoringUI(orientation, scrollPresenter, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			await ZoomTo(scrollPresenter, 2.0f, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			double horizontalOffset = 0.0;
			double verticalOffset = 0.0;

			RunOnUIThread.Execute(() =>
			{
				if (orientation == Orientation.Vertical)
				{
					verticalOffset = scrollPresenter.ExtentHeight * 2.0 - scrollPresenter.Height;
					scrollPresenter.VerticalAnchorRatio = 1.0;
				}
				else
				{
					horizontalOffset = scrollPresenter.ExtentWidth * 2.0 - scrollPresenter.Width;
					scrollPresenter.HorizontalAnchorRatio = 1.0;
				}
			});

			await ScrollTo(scrollPresenter, horizontalOffset, verticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, false /*hookViewChanged*/);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
				{
					Log.Comment("ViewChanged - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
					Log.Comment("ViewChanged - CurrentAnchor is " + (sender.CurrentAnchor == null ? "null" : "non-null"));
					if ((orientation == Orientation.Vertical && expectedFinalOffset == sender.VerticalOffset) ||
						(orientation == Orientation.Horizontal && expectedFinalOffset == sender.HorizontalOffset))
					{
						scrollPresenterViewChangedEvent.Set();
					}
				};

				scrollPresenter.AnchorRequested += delegate (ScrollPresenter sender, ScrollingAnchorRequestedEventArgs args)
				{
					Log.Comment("AnchorRequested - AnchorCandidates.Count={0}", args.AnchorCandidates.Count);
				};

				Log.Comment("Inserting child at far edge");
				InsertStackPanelChild((scrollPresenter.Content as Border).Child as StackPanel, 1 /*operationCount*/, c_defaultAnchoringUIStackPanelChildrenCount /*newIndex*/, 1 /*newCount*/);

				if (viewportSizeChange != 0)
				{
					if (orientation == Orientation.Vertical)
					{
						Log.Comment("Changing viewport height");
						scrollPresenter.Height += viewportSizeChange;
					}
					else
					{
						Log.Comment("Changing viewport width");
						scrollPresenter.Width += viewportSizeChange;
					}
				}
			});

			await WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				if (orientation == Orientation.Vertical)
				{
					verticalOffset = scrollPresenter.ExtentHeight * 2.0 - scrollPresenter.Height;
				}
				else
				{
					horizontalOffset = scrollPresenter.ExtentWidth * 2.0 - scrollPresenter.Width;
				}

				Log.Comment("ScrollPresenter offset change expected");
				Verify.AreEqual(scrollPresenter.HorizontalOffset, horizontalOffset);
				Verify.AreEqual(scrollPresenter.VerticalOffset, verticalOffset);

				Log.Comment("ScrollPresenter CurrentAnchor is " + (scrollPresenter.CurrentAnchor == null ? "null" : "non-null"));
				Verify.IsNull(scrollPresenter.CurrentAnchor);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset shrinks to max value when decreasing viewport width (HorizontalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtRightEdgeWhileDecreasingViewportWidth()
	{
		await AnchoringAtFarEdgeWhileDecreasingViewport(Orientation.Horizontal);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset shrinks to max value when decreasing viewport height (VerticalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtBottomEdgeWhileDecreasingViewportHeight()
	{
		await AnchoringAtFarEdgeWhileDecreasingViewport(Orientation.Vertical);
	}

	private async Task AnchoringAtFarEdgeWhileDecreasingViewport(Orientation orientation)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			ScrollPresenter scrollPresenter = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter = new ScrollPresenter();

				SetupDefaultAnchoringUI(orientation, scrollPresenter, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			await ZoomTo(scrollPresenter, 2.0f, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			double horizontalOffset = 0.0;
			double verticalOffset = 0.0;

			RunOnUIThread.Execute(() =>
			{
				if (orientation == Orientation.Vertical)
				{
					verticalOffset = scrollPresenter.ExtentHeight * 2.0 - scrollPresenter.Height;
					scrollPresenter.VerticalAnchorRatio = 1.0;
				}
				else
				{
					horizontalOffset = scrollPresenter.ExtentWidth * 2.0 - scrollPresenter.Width;
					scrollPresenter.HorizontalAnchorRatio = 1.0;
				}
			});

			await ScrollTo(scrollPresenter, horizontalOffset, verticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, false /*hookViewChanged*/);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
				{
					Log.Comment("ViewChanged - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
					Log.Comment("ViewChanged - CurrentAnchor is " + (sender.CurrentAnchor == null ? "null" : "non-null"));
					scrollPresenterViewChangedEvent.Set();
				};

				scrollPresenter.AnchorRequested += delegate (ScrollPresenter sender, ScrollingAnchorRequestedEventArgs args)
				{
					Log.Comment("AnchorRequested - AnchorCandidates.Count={0}", args.AnchorCandidates.Count);
				};

				if (orientation == Orientation.Vertical)
				{
					Log.Comment("Decreasing viewport height");
					scrollPresenter.Height -= 100;
				}
				else
				{
					Log.Comment("Decreasing viewport width");
					scrollPresenter.Width -= 100;
				}
			});

			await WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				if (orientation == Orientation.Vertical)
				{
					verticalOffset = scrollPresenter.ExtentHeight * 2.0 - scrollPresenter.Height;
				}
				else
				{
					horizontalOffset = scrollPresenter.ExtentWidth * 2.0 - scrollPresenter.Width;
				}

				Log.Comment("ScrollPresenter offset change expected");
				Verify.AreEqual(scrollPresenter.HorizontalOffset, horizontalOffset);
				Verify.AreEqual(scrollPresenter.VerticalOffset, verticalOffset);

				Log.Comment("ScrollPresenter CurrentAnchor is " + (scrollPresenter.CurrentAnchor == null ? "null" : "non-null"));
				Verify.IsNull(scrollPresenter.CurrentAnchor);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset growns when inserting an item at the beginning (HorizontalAnchorRatio=0).")]
	public async Task AnchoringAtAlmostLeftEdge()
	{
		await AnchoringAtAlmostNearEdge(Orientation.Horizontal);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset grows when inserting an item at the beginning (VerticalAnchorRatio=0).")]
	public async Task AnchoringAtAlmostTopEdge()
	{
		await AnchoringAtAlmostNearEdge(Orientation.Vertical);
	}

	private async Task AnchoringAtAlmostNearEdge(Orientation orientation)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			ScrollPresenter scrollPresenter = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter = new ScrollPresenter();

				SetupDefaultAnchoringUI(orientation, scrollPresenter, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			double horizontalOffset = orientation == Orientation.Vertical ? 0.0 : 1.0;
			double verticalOffset = orientation == Orientation.Vertical ? 1.0 : 0.0;

			await ScrollTo(scrollPresenter, horizontalOffset, verticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
				{
					Log.Comment("ViewChanged - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
					Log.Comment("ViewChanged - CurrentAnchor is " + (sender.CurrentAnchor == null ? "null" : "non-null"));
					scrollPresenterViewChangedEvent.Set();
				};

				Log.Comment("Inserting child at near edge");
				InsertStackPanelChild((scrollPresenter.Content as Border).Child as StackPanel, 1 /*operationCount*/, 0 /*newIndex*/, 1 /*newCount*/);
			});

			await WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("ScrollPresenter offset change expected");
				if (orientation == Orientation.Vertical)
				{
					Verify.AreEqual(127.0, scrollPresenter.VerticalOffset);
				}
				else
				{
					Verify.AreEqual(127.0, scrollPresenter.HorizontalOffset);
				}

				Log.Comment("ScrollPresenter CurrentAnchor is " + (scrollPresenter.CurrentAnchor == null ? "null" : "non-null"));
				Verify.IsNotNull(scrollPresenter.CurrentAnchor);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset does not change when inserting an item at the end (HorizontalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtAlmostRightEdge()
	{
		await AnchoringAtAlmostFarEdge(Orientation.Horizontal);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset does not change when inserting an item at the end (VerticalAnchorRatio=1).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtAlmostBottomEdge()
	{
		await AnchoringAtAlmostFarEdge(Orientation.Vertical);
	}

	private async Task AnchoringAtAlmostFarEdge(Orientation orientation)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			ScrollPresenter scrollPresenter = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter = new ScrollPresenter();

				SetupDefaultAnchoringUI(orientation, scrollPresenter, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			await ZoomTo(scrollPresenter, 2.0f, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			double horizontalOffset = 0.0;
			double verticalOffset = 0.0;

			RunOnUIThread.Execute(() =>
			{
				if (orientation == Orientation.Vertical)
				{
					verticalOffset = scrollPresenter.ExtentHeight * 2.0 - scrollPresenter.Height - 1.0;
					scrollPresenter.VerticalAnchorRatio = 1.0;
				}
				else
				{
					horizontalOffset = scrollPresenter.ExtentWidth * 2.0 - scrollPresenter.Width - 1.0;
					scrollPresenter.HorizontalAnchorRatio = 1.0;
				}
			});

			await ScrollTo(scrollPresenter, horizontalOffset, verticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, false /*hookViewChanged*/);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Inserting child at far edge");
				InsertStackPanelChild((scrollPresenter.Content as Border).Child as StackPanel, 1 /*operationCount*/, c_defaultAnchoringUIStackPanelChildrenCount /*newIndex*/, 1 /*newCount*/);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("No ScrollPresenter offset change expected");
				if (orientation == Orientation.Vertical)
				{
					Verify.AreEqual(scrollPresenter.VerticalOffset, verticalOffset);
				}
				else
				{
					Verify.AreEqual(scrollPresenter.HorizontalOffset, horizontalOffset);
				}

				Log.Comment("ScrollPresenter CurrentAnchor is " + (scrollPresenter.CurrentAnchor == null ? "null" : "non-null"));
				Verify.IsNotNull(scrollPresenter.CurrentAnchor);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HorizontalOffset increases when shrinking the viewport width (HorizontalAnchorRatio=0.5).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringElementWithShrinkingViewport()
	{
		await AnchoringElementWithResizedViewport(Orientation.Horizontal, -100.0);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset decreases when growning the viewport height (VerticalAnchorRatio=0.5).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringElementWithGrowningViewport()
	{
		await AnchoringElementWithResizedViewport(Orientation.Vertical, 100.0);
	}

	private async Task AnchoringElementWithResizedViewport(Orientation orientation, double viewportSizeChange)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			ScrollPresenter scrollPresenter = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				scrollPresenter = new ScrollPresenter();

				SetupDefaultAnchoringUI(orientation, scrollPresenter, scrollPresenterLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			await ZoomTo(scrollPresenter, 2.0f, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			double horizontalOffset = 0.0;
			double verticalOffset = 0.0;

			RunOnUIThread.Execute(() =>
			{
				if (orientation == Orientation.Vertical)
				{
					verticalOffset = (scrollPresenter.ExtentHeight * 2.0 - scrollPresenter.Height) / 2.0;
					scrollPresenter.VerticalAnchorRatio = 0.5;
				}
				else
				{
					horizontalOffset = (scrollPresenter.ExtentWidth * 2.0 - scrollPresenter.Width) / 2.0;
					scrollPresenter.HorizontalAnchorRatio = 0.5;
				}
			});

			await ScrollTo(scrollPresenter, horizontalOffset, verticalOffset, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, false /*hookViewChanged*/);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("ScrollPresenter view prior to viewport size change: HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);

				scrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
				{
					Log.Comment("ViewChanged - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
					Log.Comment("ViewChanged - CurrentAnchor is " + (sender.CurrentAnchor == null ? "null" : "non-null"));
					scrollPresenterViewChangedEvent.Set();
				};

				if (orientation == Orientation.Vertical)
				{
					Log.Comment("Changing viewport height");
					scrollPresenter.Height += viewportSizeChange;
				}
				else
				{
					Log.Comment("Changing viewport width");
					scrollPresenter.Width += viewportSizeChange;
				}
			});

			await WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("ScrollPresenter view after viewport size change: HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						scrollPresenter.HorizontalOffset, scrollPresenter.VerticalOffset, scrollPresenter.ZoomFactor);
				Log.Comment("Expecting offset change equal to half the viewport size change");
				if (orientation == Orientation.Vertical)
				{
					Verify.AreEqual(scrollPresenter.VerticalOffset, verticalOffset - viewportSizeChange / 2.0);
				}
				else
				{
					Verify.AreEqual(scrollPresenter.HorizontalOffset, horizontalOffset - viewportSizeChange / 2.0);
				}

				Log.Comment("ScrollPresenter CurrentAnchor is " + (scrollPresenter.CurrentAnchor == null ? "null" : "non-null"));
				Verify.IsNotNull(scrollPresenter.CurrentAnchor);
			});
		}
	}

	private void SetupDefaultAnchoringUI(
		Orientation orientation,
		ScrollPresenter scrollPresenter,
		UnoAutoResetEvent scrollPresenterLoadedEvent)
	{
		Log.Comment("Setting up default anchoring UI with ScrollPresenter");

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
			scrollPresenter.Width = c_defaultAnchoringUIScrollPresenterConstrainedSize;
			scrollPresenter.Height = c_defaultAnchoringUIScrollPresenterNonConstrainedSize;
		}
		else
		{
			scrollPresenter.ContentOrientation = ScrollingContentOrientation.Horizontal;
			scrollPresenter.Width = c_defaultAnchoringUIScrollPresenterNonConstrainedSize;
			scrollPresenter.Height = c_defaultAnchoringUIScrollPresenterConstrainedSize;
		}
		scrollPresenter.Background = new SolidColorBrush(Windows.UI.Colors.AliceBlue);
		scrollPresenter.Content = border;

		InsertStackPanelChild(stackPanel, 0 /*operationCount*/, 0 /*newIndex*/, c_defaultAnchoringUIStackPanelChildrenCount /*newCount*/);

		if (scrollPresenterLoadedEvent != null)
		{
			scrollPresenter.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ScrollPresenter.Loaded event handler");
				scrollPresenterLoadedEvent.Set();
			};
		}

		scrollPresenter.AnchorRequested += (ScrollPresenter sender, ScrollingAnchorRequestedEventArgs args) =>
		{
			Log.Comment("ScrollPresenter.AnchorRequested event handler");

			Verify.IsNull(args.AnchorElement);
			Verify.AreEqual(0, args.AnchorCandidates.Count);

			StackPanel sp = (sender.Content as Border).Child as StackPanel;
			foreach (Border b in sp.Children)
			{
				args.AnchorCandidates.Add(b);
			}
		};

		Log.Comment("Setting window content");
		Content = scrollPresenter;
	}

	private void InsertStackPanelChild(StackPanel stackPanel, int operationCount, int newIndex, int newCount, string namePrefix = "")
	{
		if (newIndex < 0 || newIndex > stackPanel.Children.Count || newCount <= 0)
		{
			throw new ArgumentException();
		}

		SolidColorBrush chartreuseBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse);
		SolidColorBrush blanchedAlmondBrush = new SolidColorBrush(Windows.UI.Colors.BlanchedAlmond);

		for (int i = 0; i < newCount; i++)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Text = "TB#" + stackPanel.Children.Count + "_" + operationCount;
			textBlock.Name = namePrefix + "textBlock" + stackPanel.Children.Count + "_" + operationCount;
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			textBlock.VerticalAlignment = VerticalAlignment.Center;

			Border border = new Border();
			border.Name = namePrefix + "border" + stackPanel.Children.Count + "_" + operationCount;
			border.BorderThickness = border.Margin = new Thickness(3);
			border.BorderBrush = chartreuseBrush;
			border.Background = blanchedAlmondBrush;
			border.Width = stackPanel.Orientation == Orientation.Vertical ? 170 : 120;
			border.Height = stackPanel.Orientation == Orientation.Vertical ? 120 : 170;
			border.Child = textBlock;

			stackPanel.Children.Insert(newIndex + i, border);
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies vertical offset does not exceed its max value because of anchoring, when reducing the extent height.")]
	[Ignore("Fails in Uno, waiting forever on ViewChanged")]
	public async Task AnchoringWithReducedExtent()
	{
		await AnchoringWithOffsetCoercion(false /*reduceAnchorOffset*/);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies vertical offset does not exceed its max value because of anchoring, when reducing the extent height and anchor offset.")]
	[Ignore("Fails in Uno, waiting forever on ViewChanged")]
	public async Task AnchoringWithReducedExtentAndAnchorOffset()
	{
		await AnchoringWithOffsetCoercion(true /*reduceAnchorOffset*/);
	}

	private async Task AnchoringWithOffsetCoercion(bool reduceAnchorOffset)
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			ScrollPresenter scrollPresenter = null;
			Border anchorElement = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollPresenterAnchorRequestedEvent = new UnoAutoResetEvent(false);

			// This test validates that the ScrollPresenter accounts for maximum vertical offset (based on viewport and content extent) 
			// when calculating the vertical offset shift for anchoring. The vertical offset cannot exceed content extent - viewport.

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Visual tree setup");
				anchorElement = new Border
				{
					Width = 100,
					Height = 100,
					Background = new SolidColorBrush(Windows.UI.Colors.Red),
					Margin = new Thickness(0, 600, 0, 0),
					VerticalAlignment = VerticalAlignment.Top
				};

				Grid grid = new Grid();
				grid.Children.Add(anchorElement);
				grid.Width = 200;
				grid.Height = 1000;
				grid.Background = new SolidColorBrush(Windows.UI.Colors.Gray);

				scrollPresenter = new ScrollPresenter
				{
					Content = grid,
					Width = 200,
					Height = 200
				};

				scrollPresenter.Loaded += (object sender, RoutedEventArgs e) =>
				{
					Log.Comment("ScrollPresenter.Loaded event handler");
					scrollPresenterLoadedEvent.Set();
				};

				scrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
				{
					Log.Comment("ViewChanged - HorizontalOffset={0}, VerticalOffset={1}, ZoomFactor={2}",
						sender.HorizontalOffset, sender.VerticalOffset, sender.ZoomFactor);
					Log.Comment("ViewChanged - CurrentAnchor is " + (sender.CurrentAnchor == null ? "null" : "non-null"));
					if ((reduceAnchorOffset && sender.VerticalOffset == 400) ||
						(!reduceAnchorOffset && sender.VerticalOffset == 500))
					{
						scrollPresenterViewChangedEvent.Set();
					}
				};

				scrollPresenter.AnchorRequested += delegate (ScrollPresenter sender, ScrollingAnchorRequestedEventArgs args)
				{
					Log.Comment("AnchorRequested - Forcing the red Border to be the ScrollPresenter anchor.");
					args.AnchorElement = anchorElement;
					scrollPresenterAnchorRequestedEvent.Set();
				};

				Log.Comment("Setting window content");
				Content = scrollPresenter;
			});

			await WaitForEvent("Waiting for ScrollPresenter.Loaded event", scrollPresenterLoadedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			await ScrollTo(scrollPresenter, 0.0, 600.0, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(600, scrollPresenter.VerticalOffset);

				Log.Comment("ScrollPresenter.Content height is reduced by 300px. ScrollPresenter.VerticalOffset is expected to be reduced by 100px (600 -> 500).");
				(scrollPresenter.Content as Grid).Height = 700;
				if (reduceAnchorOffset)
				{
					Log.Comment("Tracked element is shifted up by 200px within the ScrollPresenter.Content (600 -> 400). Anchoring is expected to reduce the VerticalOffset by half of that (500 -> 400).");
					anchorElement.Margin = new Thickness(0, 400, 0, 0);
				}
				scrollPresenterViewChangedEvent.Reset();
			});

			await WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);
			await WaitForEvent("Waiting for ScrollPresenter.AnchorRequested event", scrollPresenterAnchorRequestedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(reduceAnchorOffset ? 400 : 500, scrollPresenter.VerticalOffset);

				Log.Comment("ScrollPresenter CurrentAnchor is " + (scrollPresenter.CurrentAnchor == null ? "null" : "non-null"));
				Verify.IsNotNull(scrollPresenter.CurrentAnchor);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies VerticalOffset adjusts when inserting and removing items at the beginning (VerticalAnchorRatio=0.5).")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task AnchoringAtRepeaterMiddle()
	{
		using (ScrollPresenterTestHooksHelper scrollPresenterTestHooksHelper = new ScrollPresenterTestHooksHelper(
			enableAnchorNotifications: true,
			enableInteractionSourcesNotifications: true,
			enableExpressionAnimationStatusNotifications: false))
		{
			//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
			{
				ScrollPresenter scrollPresenter = null;
				UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
				UnoAutoResetEvent scrollPresenterViewChangedEvent = new UnoAutoResetEvent(false);

				RunOnUIThread.Execute(() =>
				{
					scrollPresenter = new ScrollPresenter();

					SetupRepeaterAnchoringUI(scrollPresenter, scrollPresenterLoadedEvent);

					scrollPresenter.HorizontalAnchorRatio = double.NaN;
					scrollPresenter.VerticalAnchorRatio = 0.5;
				});

				await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

				await ZoomTo(scrollPresenter, 2.0f, 0.0f, 0.0f, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore);
				await ScrollTo(scrollPresenter, 0.0, 250.0, ScrollingAnimationMode.Enabled, ScrollingSnapPointsMode.Ignore, false /*hookViewChanged*/);

				ItemsRepeater repeater = null;
				TestDataSource dataSource = null;

				RunOnUIThread.Execute(() =>
				{
					repeater = (scrollPresenter.Content as Border).Child as ItemsRepeater;
					dataSource = repeater.ItemsSource as TestDataSource;

					scrollPresenter.ViewChanged += delegate (ScrollPresenter sender, object args)
					{
						scrollPresenterViewChangedEvent.Set();
					};

					Log.Comment("Inserting items at the beginning");
					dataSource.Insert(0 /*index*/, 2 /*count*/);
				});

				await WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);

				RunOnUIThread.Execute(() =>
				{
					Log.Comment("ScrollPresenter offset change expected");
					Verify.AreEqual(520.0, scrollPresenter.VerticalOffset);
				});

				RunOnUIThread.Execute(() =>
				{
					scrollPresenterViewChangedEvent.Reset();

					Log.Comment("Removing items from the beginning");
					dataSource.Remove(0 /*index*/, 2 /*count*/);
				});

				await WaitForEvent("Waiting for ScrollPresenter.ViewChanged event", scrollPresenterViewChangedEvent);

				RunOnUIThread.Execute(() =>
				{
					Log.Comment("ScrollPresenter offset change expected");
					Verify.AreEqual(250.0, scrollPresenter.VerticalOffset);

					Log.Comment("ScrollPresenter CurrentAnchor is " + (scrollPresenter.CurrentAnchor == null ? "null" : "non-null"));
					Verify.IsNotNull(scrollPresenter.CurrentAnchor);
				});
			}
		}
	}

	private void SetupRepeaterAnchoringUI(
		ScrollPresenter scrollPresenter,
		UnoAutoResetEvent scrollPresenterLoadedEvent)
	{
		Log.Comment("Setting up ItemsRepeater anchoring UI with ScrollPresenter and ItemsRepeater");

		TestDataSource dataSource = new TestDataSource(
			Enumerable.Range(0, c_defaultAnchoringUIRepeaterChildrenCount).Select(i => string.Format("Item #{0}", i)).ToList());

		RecyclingElementFactory elementFactory = new RecyclingElementFactory();
		elementFactory.RecyclePool = new RecyclePool();
		elementFactory.Templates["Item"] = XamlReader.Load(
				@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
						<Border BorderThickness='3' BorderBrush='Chartreuse' Width='100' Height='100' Margin='3' Background='BlanchedAlmond'>
						  <TextBlock Text='{Binding}' HorizontalAlignment='Center' VerticalAlignment='Center'/>
						</Border>
					  </DataTemplate>") as DataTemplate;

		ItemsRepeater repeater = new ItemsRepeater()
		{
			Name = "repeater",
			ItemsSource = dataSource,
			ItemTemplate = elementFactory,
			Layout = new UniformGridLayout()
			{
				MinItemWidth = 125,
				MinItemHeight = 125,
				MinRowSpacing = 10,
				MinColumnSpacing = 10
			},
			Margin = new Thickness(30)
		};

		Border border = new Border()
		{
			Name = "border",
			BorderThickness = new Thickness(3),
			BorderBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse),
			Margin = new Thickness(15),
			Background = new SolidColorBrush(Windows.UI.Colors.Beige),
			Child = repeater
		};

		Verify.IsNotNull(scrollPresenter);
		scrollPresenter.Name = "scrollPresenter";
		scrollPresenter.ContentOrientation = ScrollingContentOrientation.Vertical;
		scrollPresenter.Width = 400;
		scrollPresenter.Height = 600;
		scrollPresenter.Background = new SolidColorBrush(Windows.UI.Colors.AliceBlue);
		scrollPresenter.Content = border;

		if (scrollPresenterLoadedEvent != null)
		{
			scrollPresenter.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ScrollPresenter.Loaded event handler");
				scrollPresenterLoadedEvent.Set();
			};
		}

		scrollPresenter.AnchorRequested += (ScrollPresenter sender, ScrollingAnchorRequestedEventArgs args) =>
		{
			Log.Comment("ScrollPresenter.AnchorRequested event handler");

			Verify.IsNull(args.AnchorElement);
			Verify.IsGreaterThan(args.AnchorCandidates.Count, 0);
		};

		Log.Comment("Setting window content");
		Content = scrollPresenter;
	}

	private class TestDataSource : CustomItemsSourceViewWithUniqueIdMapping
	{
		public TestDataSource(List<string> source)
		{
			Inner = source;
		}

		public List<string> Inner
		{
			get;
			set;
		}

		protected override int GetSizeCore()
		{
			return Inner.Count;
		}

		protected override object GetAtCore(int index)
		{
			return Inner[index];
		}

		protected override string KeyFromIndexCore(int index)
		{
			return Inner[index].ToString();
		}

		public void Insert(int index, int count)
		{
			for (int i = 0; i < count; i++)
			{
				Inner.Insert(index + i, string.Format("ItemI #{0}", Inner.Count));
			}

			OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(
				NotifyCollectionChangedAction.Add,
				oldStartingIndex: -1,
				oldItemsCount: 0,
				newStartingIndex: index,
				newItemsCount: count));
		}

		public void Remove(int index, int count)
		{
			for (int i = 0; i < count; i++)
			{
				Inner.RemoveAt(index);
			}

			OnItemsSourceChanged(CollectionChangeEventArgsConverters.CreateNotifyArgs(
				NotifyCollectionChangedAction.Remove,
				oldStartingIndex: index,
				oldItemsCount: count,
				newStartingIndex: -1,
				newItemsCount: 0));
		}
	}
}
