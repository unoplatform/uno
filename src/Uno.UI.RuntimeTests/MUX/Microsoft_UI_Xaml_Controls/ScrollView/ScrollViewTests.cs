// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Common;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Windows.Foundation;
using Windows.UI.ViewManagement;

using ScrollView = Windows.UI.Xaml.Controls.ScrollView;
using ScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollingScrollBarVisibility;
using ScrollPresenter = Windows.UI.Xaml.Controls.Primitives.ScrollPresenter;
using ScrollingContentOrientation = Windows.UI.Xaml.Controls.ScrollingContentOrientation;
using ScrollingScrollMode = Windows.UI.Xaml.Controls.ScrollingScrollMode;
using ScrollingInputKinds = Windows.UI.Xaml.Controls.ScrollingInputKinds;
using ScrollingChainMode = Windows.UI.Xaml.Controls.ScrollingChainMode;
using ScrollingRailMode = Windows.UI.Xaml.Controls.ScrollingRailMode;
using ScrollingZoomMode = Windows.UI.Xaml.Controls.ScrollingZoomMode;
using ScrollingAnchorRequestedEventArgs = Windows.UI.Xaml.Controls.ScrollingAnchorRequestedEventArgs;
//using MUXControlsTestHooksLoggingMessageEventArgs = Microsoft.UI.Private.Controls.MUXControlsTestHooksLoggingMessageEventArgs;
using ScrollViewTestHooks = Microsoft.UI.Private.Controls.ScrollViewTestHooks;
using Private.Infrastructure;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
#if !__SKIA__
[Ignore("Not well supported on platforms other than Skia")]
#endif
public class ScrollViewTests : MUXApiTestBase
{
	private const int c_MaxWaitDuration = 5000;
	private const double c_epsilon = 0.0000001;

	private const ScrollingInputKinds c_defaultIgnoredInputKinds = ScrollingInputKinds.None;
	private const ScrollingChainMode c_defaultHorizontalScrollChainMode = ScrollingChainMode.Auto;
	private const ScrollingChainMode c_defaultVerticalScrollChainMode = ScrollingChainMode.Auto;
	private const ScrollingRailMode c_defaultHorizontalScrollRailMode = ScrollingRailMode.Enabled;
	private const ScrollingRailMode c_defaultVerticalScrollRailMode = ScrollingRailMode.Enabled;
	private const ScrollingScrollMode c_defaultComputedHorizontalScrollMode = ScrollingScrollMode.Disabled;
	private const ScrollingScrollMode c_defaultComputedVerticalScrollMode = ScrollingScrollMode.Disabled;
	private const ScrollingScrollMode c_defaultHorizontalScrollMode = ScrollingScrollMode.Auto;
	private const ScrollingScrollMode c_defaultVerticalScrollMode = ScrollingScrollMode.Auto;
	private const ScrollingChainMode c_defaultZoomChainMode = ScrollingChainMode.Auto;
	private const ScrollingZoomMode c_defaultZoomMode = ScrollingZoomMode.Disabled;
	private const ScrollingContentOrientation c_defaultContentOrientation = ScrollingContentOrientation.Vertical;
	private const double c_defaultMinZoomFactor = 0.1;
	private const double c_defaultMaxZoomFactor = 10.0;
	private const double c_defaultAnchorRatio = 0.0;

	private const double c_defaultUIScrollViewContentWidth = 1200.0;
	private const double c_defaultUIScrollViewContentHeight = 600.0;
	private const double c_defaultUIScrollViewWidth = 300.0;
	private const double c_defaultUIScrollViewHeight = 200.0;

#pragma warning disable CS0414 // The field 'ScrollViewTests.m_trackScrollViewStateChanges' is assigned but its value is never used
	private bool m_trackScrollViewStateChanges = false;
#pragma warning restore CS0414

	private ScrollViewVisualStateCounts m_scrollViewVisualStateCounts;

	[TestMethod]
	[TestProperty("Description", "Verifies the ScrollView default properties.")]
	public void VerifyDefaultPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			ScrollView scrollView = new ScrollView();
			Verify.IsNotNull(scrollView);

			Log.Comment("Verifying ScrollView default property values");
			Verify.IsNull(scrollView.Content);
			Verify.IsNull(scrollView.CurrentAnchor);
			Verify.IsNull(scrollView.ScrollPresenter);
			Verify.AreEqual(c_defaultComputedHorizontalScrollMode, scrollView.ComputedHorizontalScrollMode);
			Verify.AreEqual(c_defaultComputedVerticalScrollMode, scrollView.ComputedVerticalScrollMode);
			Verify.AreEqual(c_defaultIgnoredInputKinds, scrollView.IgnoredInputKinds);
			Verify.AreEqual(c_defaultContentOrientation, scrollView.ContentOrientation);
			Verify.AreEqual(c_defaultHorizontalScrollChainMode, scrollView.HorizontalScrollChainMode);
			Verify.AreEqual(c_defaultVerticalScrollChainMode, scrollView.VerticalScrollChainMode);
			Verify.AreEqual(c_defaultHorizontalScrollRailMode, scrollView.HorizontalScrollRailMode);
			Verify.AreEqual(c_defaultVerticalScrollRailMode, scrollView.VerticalScrollRailMode);
			Verify.AreEqual(c_defaultHorizontalScrollMode, scrollView.HorizontalScrollMode);
			Verify.AreEqual(c_defaultVerticalScrollMode, scrollView.VerticalScrollMode);
			Verify.AreEqual(c_defaultZoomMode, scrollView.ZoomMode);
			Verify.AreEqual(c_defaultZoomChainMode, scrollView.ZoomChainMode);
			Verify.IsGreaterThan(scrollView.MinZoomFactor, c_defaultMinZoomFactor - c_epsilon);
			Verify.IsLessThan(scrollView.MinZoomFactor, c_defaultMinZoomFactor + c_epsilon);
			Verify.IsGreaterThan(scrollView.MaxZoomFactor, c_defaultMaxZoomFactor - c_epsilon);
			Verify.IsLessThan(scrollView.MaxZoomFactor, c_defaultMaxZoomFactor + c_epsilon);
			Verify.AreEqual(c_defaultAnchorRatio, scrollView.HorizontalAnchorRatio);
			Verify.AreEqual(c_defaultAnchorRatio, scrollView.VerticalAnchorRatio);
			Verify.AreEqual(0.0, scrollView.ExtentWidth);
			Verify.AreEqual(0.0, scrollView.ExtentHeight);
			Verify.AreEqual(0.0, scrollView.ViewportWidth);
			Verify.AreEqual(0.0, scrollView.ViewportHeight);
			Verify.AreEqual(0.0, scrollView.ScrollableWidth);
			Verify.AreEqual(0.0, scrollView.ScrollableHeight);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ScrollView properties after template application.")]
	public async Task VerifyScrollPresenterAttachedProperties()
	{
		//using (PrivateLoggingHelper privateSVLoggingHelper = new PrivateLoggingHelper("ScrollView", "ScrollPresenter"))
		{
			ScrollView scrollView = null;
			Rectangle rectangleScrollViewContent = null;
			UnoAutoResetEvent scrollViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				rectangleScrollViewContent = new Rectangle();
				scrollView = new ScrollView();

				SetupDefaultUI(scrollView, rectangleScrollViewContent, scrollViewLoadedEvent, scrollViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ScrollPresenter-cloned properties to non-default values");
				scrollView.IgnoredInputKinds = ScrollingInputKinds.MouseWheel | ScrollingInputKinds.Pen;
				scrollView.ContentOrientation = ScrollingContentOrientation.Horizontal;
				scrollView.HorizontalScrollChainMode = ScrollingChainMode.Always;
				scrollView.VerticalScrollChainMode = ScrollingChainMode.Never;
				scrollView.HorizontalScrollRailMode = ScrollingRailMode.Disabled;
				scrollView.VerticalScrollRailMode = ScrollingRailMode.Disabled;
				scrollView.HorizontalScrollMode = ScrollingScrollMode.Enabled;
				scrollView.VerticalScrollMode = ScrollingScrollMode.Disabled;
				scrollView.ZoomMode = ScrollingZoomMode.Enabled;
				scrollView.ZoomChainMode = ScrollingChainMode.Never;
				scrollView.MinZoomFactor = 2.0;
				scrollView.MaxZoomFactor = 8.0;

				Log.Comment("Verifying ScrollPresenter-cloned non-default properties");
				Verify.AreEqual(ScrollingInputKinds.MouseWheel | ScrollingInputKinds.Pen, scrollView.IgnoredInputKinds);
				Verify.AreEqual(ScrollingContentOrientation.Horizontal, scrollView.ContentOrientation);
				Verify.AreEqual(ScrollingChainMode.Always, scrollView.HorizontalScrollChainMode);
				Verify.AreEqual(ScrollingChainMode.Never, scrollView.VerticalScrollChainMode);
				Verify.AreEqual(ScrollingRailMode.Disabled, scrollView.HorizontalScrollRailMode);
				Verify.AreEqual(ScrollingRailMode.Disabled, scrollView.VerticalScrollRailMode);
				Verify.AreEqual(ScrollingScrollMode.Enabled, scrollView.HorizontalScrollMode);
				Verify.AreEqual(ScrollingScrollMode.Disabled, scrollView.VerticalScrollMode);
				Verify.AreEqual(ScrollingScrollMode.Enabled, scrollView.ComputedHorizontalScrollMode);
				Verify.AreEqual(ScrollingScrollMode.Disabled, scrollView.ComputedVerticalScrollMode);
				Verify.AreEqual(ScrollingZoomMode.Enabled, scrollView.ZoomMode);
				Verify.AreEqual(ScrollingChainMode.Never, scrollView.ZoomChainMode);
				Verify.IsGreaterThan(scrollView.MinZoomFactor, 2.0 - c_epsilon);
				Verify.IsLessThan(scrollView.MinZoomFactor, 2.0 + c_epsilon);
				Verify.IsGreaterThan(scrollView.MaxZoomFactor, 8.0 - c_epsilon);
				Verify.IsLessThan(scrollView.MaxZoomFactor, 8.0 + c_epsilon);

				Log.Comment("Resetting window content and ScrollView");
				Content = null;
				scrollView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", scrollViewUnloadedEvent);

			await TestServices.WindowHelper.WaitForIdle();
			Log.Comment("Garbage collecting...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ScrollPresenter attached properties.")]
	public async Task VerifyPropertyValuesAfterTemplateApplication()
	{
		//using (PrivateLoggingHelper privateSVLoggingHelper = new PrivateLoggingHelper("ScrollView", "ScrollPresenter"))
		{
			ScrollView scrollView = null;
			Rectangle rectangleScrollViewContent = null;
			UnoAutoResetEvent scrollViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				rectangleScrollViewContent = new Rectangle();
				scrollView = new ScrollView();

				SetupDefaultUI(scrollView, rectangleScrollViewContent, scrollViewLoadedEvent, scrollViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", scrollViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Verifying ScrollView property values after Loaded event");
				Verify.AreEqual(rectangleScrollViewContent, scrollView.Content);
				Verify.IsNull(scrollView.CurrentAnchor);
				Verify.IsNotNull(scrollView.ScrollPresenter);
				Verify.AreEqual(rectangleScrollViewContent, scrollView.ScrollPresenter.Content);
				Verify.AreEqual(c_defaultUIScrollViewContentWidth, scrollView.ExtentWidth);
				Verify.AreEqual(c_defaultUIScrollViewContentHeight, scrollView.ExtentHeight);
				Verify.AreEqual(c_defaultUIScrollViewWidth, scrollView.ViewportWidth);
				Verify.AreEqual(c_defaultUIScrollViewHeight, scrollView.ViewportHeight);
				Verify.AreEqual(c_defaultUIScrollViewContentWidth - c_defaultUIScrollViewWidth, scrollView.ScrollableWidth);
				Verify.AreEqual(c_defaultUIScrollViewContentHeight - c_defaultUIScrollViewHeight, scrollView.ScrollableHeight);

				Log.Comment("Resetting window content and ScrollView");
				Content = null;
				scrollView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", scrollViewUnloadedEvent);

			await TestServices.WindowHelper.WaitForIdle();
			Log.Comment("Garbage collecting...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ScrollView visual state changes based on the AutoHideScrollBars, IsEnabled and ScrollBarVisibility settings.")]
	[TestProperty("Ignore", "True")] // Disabled due to #41343576
	public async Task VerifyVisualStates()
	{
		UISettings settings = new UISettings();
		if (!settings.AnimationsEnabled)
		{
			Log.Warning("Test is disabled when animations are turned off.");
			return;
		}

		//using (PrivateLoggingHelper privateSVLoggingHelper = new PrivateLoggingHelper("ScrollView"))
		{
			//MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessageForVisualStateChange;

			await VerifyVisualStates(ScrollBarVisibility.Auto, autoHideScrollControllers: true);
			await VerifyVisualStates(ScrollBarVisibility.Visible, autoHideScrollControllers: true);

			await VerifyVisualStates(ScrollBarVisibility.Auto, autoHideScrollControllers: false);
			await VerifyVisualStates(ScrollBarVisibility.Visible, autoHideScrollControllers: false);

			//MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessageForVisualStateChange;
		}
	}

	private async Task VerifyVisualStates(ScrollBarVisibility scrollBarVisibility, bool autoHideScrollControllers)
	{
		ScrollView scrollView = null;

		RunOnUIThread.Execute(() =>
		{
			m_trackScrollViewStateChanges = true;
			m_scrollViewVisualStateCounts = new ScrollViewVisualStateCounts();
			scrollView = new ScrollView();
		});

		using (ScrollViewTestHooksHelper scrollViewTestHooksHelper = new ScrollViewTestHooksHelper(scrollView, autoHideScrollControllers))
		{
			Rectangle rectangleScrollViewContent = null;
			UnoAutoResetEvent scrollViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				rectangleScrollViewContent = new Rectangle();
				scrollView.HorizontalScrollBarVisibility = scrollBarVisibility;
				scrollView.VerticalScrollBarVisibility = scrollBarVisibility;

				SetupDefaultUI(
					scrollView: scrollView,
					rectangleScrollViewContent: rectangleScrollViewContent,
					scrollViewLoadedEvent: scrollViewLoadedEvent,
					scrollViewUnloadedEvent: scrollViewUnloadedEvent,
					setAsContentRoot: true,
					useParentGrid: true);
			});

			await WaitForEvent("Waiting for Loaded event", scrollViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				m_trackScrollViewStateChanges = false;
				Log.Comment($"VerifyVisualStates: isEnabled:True, scrollBarVisibility:{scrollBarVisibility}, autoHideScrollControllers:{autoHideScrollControllers}");

				VerifyVisualStates(
					expectedMouseIndicatorStateCount: autoHideScrollControllers ? 0u : (scrollBarVisibility == ScrollBarVisibility.Auto ? 2u : 3u),
					expectedTouchIndicatorStateCount: 0,
					expectedNoIndicatorStateCount: autoHideScrollControllers ? (scrollBarVisibility == ScrollBarVisibility.Auto ? 2u : 3u) : 0u,
					expectedScrollBarsSeparatorCollapsedStateCount: autoHideScrollControllers ? (scrollBarVisibility == ScrollBarVisibility.Auto ? 1u : 3u) : 0u,
					expectedScrollBarsSeparatorCollapsedDisabledStateCount: 0,
					expectedScrollBarsSeparatorExpandedStateCount: autoHideScrollControllers ? 0u : (scrollBarVisibility == ScrollBarVisibility.Auto ? 1u : 3u),
					expectedScrollBarsSeparatorDisplayedWithoutAnimationStateCount: 0,
					expectedScrollBarsSeparatorExpandedWithoutAnimationStateCount: 0,
					expectedScrollBarsSeparatorCollapsedWithoutAnimationStateCount: 0);

				m_scrollViewVisualStateCounts.ResetStateCounts();
				m_trackScrollViewStateChanges = true;

				Log.Comment("Disabling ScrollView");
				scrollView.IsEnabled = false;
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				m_trackScrollViewStateChanges = false;
				Log.Comment($"VerifyVisualStates: isEnabled:False, scrollBarVisibility:{scrollBarVisibility}, autoHideScrollControllers:{autoHideScrollControllers}");

				VerifyVisualStates(
					expectedMouseIndicatorStateCount: autoHideScrollControllers ? 0u : (scrollBarVisibility == ScrollBarVisibility.Auto ? 1u : 3u),
					expectedTouchIndicatorStateCount: 0,
					expectedNoIndicatorStateCount: autoHideScrollControllers ? (scrollBarVisibility == ScrollBarVisibility.Auto ? 1u : 3u) : 0u,
					expectedScrollBarsSeparatorCollapsedStateCount: 0,
					expectedScrollBarsSeparatorCollapsedDisabledStateCount: scrollBarVisibility == ScrollBarVisibility.Auto ? 0u : 3u,
					expectedScrollBarsSeparatorExpandedStateCount: 0,
					expectedScrollBarsSeparatorDisplayedWithoutAnimationStateCount: 0,
					expectedScrollBarsSeparatorExpandedWithoutAnimationStateCount: 0,
					expectedScrollBarsSeparatorCollapsedWithoutAnimationStateCount: 0);

				m_scrollViewVisualStateCounts.ResetStateCounts();
				m_trackScrollViewStateChanges = true;

				Log.Comment("Enabling ScrollView");
				scrollView.IsEnabled = true;
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				m_trackScrollViewStateChanges = false;
				Log.Comment($"VerifyVisualStates: isEnabled:True, scrollBarVisibility:{scrollBarVisibility}, autoHideScrollControllers:{autoHideScrollControllers}");

				VerifyVisualStates(
					expectedMouseIndicatorStateCount: autoHideScrollControllers ? 0u : 3u,
					expectedTouchIndicatorStateCount: 0,
					expectedNoIndicatorStateCount: autoHideScrollControllers ? 3u : 0u,
					expectedScrollBarsSeparatorCollapsedStateCount: autoHideScrollControllers ? (scrollBarVisibility == ScrollBarVisibility.Auto ? 2u : 3u) : 0u,
					expectedScrollBarsSeparatorCollapsedDisabledStateCount: 0,
					expectedScrollBarsSeparatorExpandedStateCount: autoHideScrollControllers ? 0u : (scrollBarVisibility == ScrollBarVisibility.Auto ? 2u : 3u),
					expectedScrollBarsSeparatorDisplayedWithoutAnimationStateCount: 0,
					expectedScrollBarsSeparatorExpandedWithoutAnimationStateCount: 0,
					expectedScrollBarsSeparatorCollapsedWithoutAnimationStateCount: 0);

				Log.Comment("Resetting window content");
				Content = null;
				m_scrollViewVisualStateCounts = null;
			});

			await WaitForEvent("Waiting for Unloaded event", scrollViewUnloadedEvent);
		}
	}

	//private void MUXControlsTestHooks_LoggingMessageForVisualStateChange(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
	//{
	//	if (m_trackScrollViewStateChanges)
	//	{
	//		if (args.IsVerboseLevel)
	//		{
	//			if (args.Message.Contains("ScrollView::GoToState"))
	//			{
	//				if (args.Message.Contains("NoIndicator"))
	//				{
	//					m_scrollViewVisualStateCounts.NoIndicatorStateCount++;
	//				}
	//				else if (args.Message.Contains("TouchIndicator"))
	//				{
	//					m_scrollViewVisualStateCounts.TouchIndicatorStateCount++;
	//				}
	//				else if (args.Message.Contains("MouseIndicator"))
	//				{
	//					m_scrollViewVisualStateCounts.MouseIndicatorStateCount++;
	//				}
	//				else if (args.Message.Contains("ScrollBarsSeparatorCollapsedDisabled"))
	//				{
	//					m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedDisabledStateCount++;
	//				}
	//				else if (args.Message.Contains("ScrollBarsSeparatorCollapsedWithoutAnimation"))
	//				{
	//					m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedWithoutAnimationStateCount++;
	//				}
	//				else if (args.Message.Contains("ScrollBarsSeparatorDisplayedWithoutAnimation"))
	//				{
	//					m_scrollViewVisualStateCounts.ScrollBarsSeparatorDisplayedWithoutAnimationStateCount++;
	//				}
	//				else if (args.Message.Contains("ScrollBarsSeparatorExpandedWithoutAnimation"))
	//				{
	//					m_scrollViewVisualStateCounts.ScrollBarsSeparatorExpandedWithoutAnimationStateCount++;
	//				}
	//				else if (args.Message.Contains("ScrollBarsSeparatorCollapsed"))
	//				{
	//					m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedStateCount++;
	//				}
	//				else if (args.Message.Contains("ScrollBarsSeparatorExpanded"))
	//				{
	//					m_scrollViewVisualStateCounts.ScrollBarsSeparatorExpandedStateCount++;
	//				}
	//			}
	//		}
	//	}
	//}

	private void VerifyVisualStates(
		uint expectedMouseIndicatorStateCount,
		uint expectedTouchIndicatorStateCount,
		uint expectedNoIndicatorStateCount,
		uint expectedScrollBarsSeparatorCollapsedStateCount,
		uint expectedScrollBarsSeparatorCollapsedDisabledStateCount,
		uint expectedScrollBarsSeparatorExpandedStateCount,
		uint expectedScrollBarsSeparatorDisplayedWithoutAnimationStateCount,
		uint expectedScrollBarsSeparatorExpandedWithoutAnimationStateCount,
		uint expectedScrollBarsSeparatorCollapsedWithoutAnimationStateCount)
	{
		//Log.Comment($"expectedMouseIndicatorStateCount:{expectedMouseIndicatorStateCount}, mouseIndicatorStateCount:{m_scrollViewVisualStateCounts.MouseIndicatorStateCount}");
		//Log.Comment($"expectedNoIndicatorStateCount:{expectedNoIndicatorStateCount}, noIndicatorStateCount:{m_scrollViewVisualStateCounts.NoIndicatorStateCount}");
		//Log.Comment($"expectedScrollBarsSeparatorCollapsedStateCount:{expectedScrollBarsSeparatorCollapsedStateCount}, scrollBarsSeparatorCollapsedStateCount:{m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedStateCount}");
		//Log.Comment($"expectedScrollBarsSeparatorCollapsedDisabledStateCount:{expectedScrollBarsSeparatorCollapsedDisabledStateCount}, scrollBarsSeparatorCollapsedDisabledStateCount:{m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedDisabledStateCount}");
		//Log.Comment($"expectedScrollBarsSeparatorExpandedStateCount:{expectedScrollBarsSeparatorExpandedStateCount}, scrollBarsSeparatorExpandedStateCount:{m_scrollViewVisualStateCounts.ScrollBarsSeparatorExpandedStateCount}");

		//Verify.AreEqual(expectedMouseIndicatorStateCount, m_scrollViewVisualStateCounts.MouseIndicatorStateCount, "MouseIndicatorStateCount");
		//Verify.AreEqual(expectedTouchIndicatorStateCount, m_scrollViewVisualStateCounts.TouchIndicatorStateCount, "TouchIndicatorStateCount");
		//Verify.AreEqual(expectedNoIndicatorStateCount, m_scrollViewVisualStateCounts.NoIndicatorStateCount, "NoIndicatorStateCount");
		//Verify.AreEqual(expectedScrollBarsSeparatorCollapsedStateCount, m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedStateCount, "ScrollBarsSeparatorCollapsedStateCount");
		//Verify.AreEqual(expectedScrollBarsSeparatorCollapsedDisabledStateCount, m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedDisabledStateCount, "ScrollBarsSeparatorCollapsedDisabledStateCount");
		//Verify.AreEqual(expectedScrollBarsSeparatorExpandedStateCount, m_scrollViewVisualStateCounts.ScrollBarsSeparatorExpandedStateCount, "ScrollBarsSeparatorExpandedStateCount");
		//Verify.AreEqual(expectedScrollBarsSeparatorDisplayedWithoutAnimationStateCount, m_scrollViewVisualStateCounts.ScrollBarsSeparatorDisplayedWithoutAnimationStateCount, "ScrollBarsSeparatorDisplayedWithoutAnimationStateCount");
		//Verify.AreEqual(expectedScrollBarsSeparatorExpandedWithoutAnimationStateCount, m_scrollViewVisualStateCounts.ScrollBarsSeparatorExpandedWithoutAnimationStateCount, "ScrollBarsSeparatorExpandedWithoutAnimationStateCount");
		//Verify.AreEqual(expectedScrollBarsSeparatorCollapsedWithoutAnimationStateCount, m_scrollViewVisualStateCounts.ScrollBarsSeparatorCollapsedWithoutAnimationStateCount, "ScrollBarsSeparatorCollapsedWithoutAnimationStateCount");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies anchor candidate registration and unregistration.")]
	public async Task VerifyAnchorCandidateRegistration()
	{
		//using (PrivateLoggingHelper privateSVLoggingHelper = new PrivateLoggingHelper("ScrollView", "ScrollPresenter"))
		{
			int expectedAnchorCandidatesCount = 0;
			ScrollPresenter scrollPresenter = null;
			ScrollView scrollView = null;
			Rectangle rectangleScrollViewContent = null;
			UnoAutoResetEvent scrollViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewAnchorRequestedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				rectangleScrollViewContent = new Rectangle();
				scrollView = new ScrollView();
				scrollView.HorizontalAnchorRatio = 0.1;

				SetupDefaultUI(scrollView, rectangleScrollViewContent, scrollViewLoadedEvent);

				scrollView.AnchorRequested += (ScrollView sender, ScrollingAnchorRequestedEventArgs args) =>
				{
					Log.Comment("ScrollView.AnchorRequested event handler. args.AnchorCandidates.Count: " + args.AnchorCandidates.Count);
					Verify.IsNull(args.AnchorElement);
					Verify.IsNull(sender.CurrentAnchor);
					Verify.AreEqual(expectedAnchorCandidatesCount, args.AnchorCandidates.Count);
					scrollViewAnchorRequestedEvent.Set();
				};
			});

			await WaitForEvent("Waiting for Loaded event", scrollViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Accessing inner ScrollPresenter control");
				scrollPresenter = scrollView.ScrollPresenter;

				Log.Comment("Registering Rectangle as anchor candidate");
				scrollView.RegisterAnchorCandidate(rectangleScrollViewContent);
				expectedAnchorCandidatesCount = 1;

				Log.Comment("Forcing ScrollPresenter layout");
				scrollPresenter.InvalidateArrange();
			});

			await WaitForEvent("Waiting for AnchorRequested event", scrollViewAnchorRequestedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Unregistering Rectangle as anchor candidate");
				scrollView.UnregisterAnchorCandidate(rectangleScrollViewContent);
				expectedAnchorCandidatesCount = 0;

				Log.Comment("Forcing ScrollPresenter layout");
				scrollPresenter.InvalidateArrange();
			});

			await WaitForEvent("Waiting for AnchorRequested event", scrollViewAnchorRequestedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("ScrollView CurrentAnchor is " + (scrollView.CurrentAnchor == null ? "null" : "non-null"));
				Verify.IsNull(scrollView.CurrentAnchor);
				Verify.AreEqual(scrollView.CurrentAnchor, scrollPresenter.CurrentAnchor);
			});
		}
	}

	private void SetupDefaultUI(
		ScrollView scrollView,
		Rectangle rectangleScrollViewContent = null,
		UnoAutoResetEvent scrollViewLoadedEvent = null,
		UnoAutoResetEvent scrollViewUnloadedEvent = null,
		bool setAsContentRoot = true,
		bool useParentGrid = false)
	{
		Log.Comment("Setting up default UI with ScrollView" + (rectangleScrollViewContent == null ? "" : " and Rectangle"));

		LinearGradientBrush twoColorLGB = new LinearGradientBrush() { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };

		GradientStop brownGS = new GradientStop() { Color = Colors.Brown, Offset = 0.0 };
		twoColorLGB.GradientStops.Add(brownGS);

		GradientStop orangeGS = new GradientStop() { Color = Colors.Orange, Offset = 1.0 };
		twoColorLGB.GradientStops.Add(orangeGS);

		if (rectangleScrollViewContent != null)
		{
			rectangleScrollViewContent.Width = c_defaultUIScrollViewContentWidth;
			rectangleScrollViewContent.Height = c_defaultUIScrollViewContentHeight;
			rectangleScrollViewContent.Fill = twoColorLGB;
		}

		Verify.IsNotNull(scrollView);
		scrollView.Name = "scrollView";
		scrollView.Width = c_defaultUIScrollViewWidth;
		scrollView.Height = c_defaultUIScrollViewHeight;
		if (rectangleScrollViewContent != null)
		{
			scrollView.Content = rectangleScrollViewContent;
		}

		if (scrollViewLoadedEvent != null)
		{
			scrollView.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ScrollView.Loaded event handler");
				scrollViewLoadedEvent.Set();
			};
		}

		if (scrollViewUnloadedEvent != null)
		{
			scrollView.Unloaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ScrollView.Unloaded event handler");
				scrollViewUnloadedEvent.Set();
			};
		}

		Grid parentGrid = null;

		if (useParentGrid)
		{
			parentGrid = new Grid();
			parentGrid.Width = c_defaultUIScrollViewWidth * 3;
			parentGrid.Height = c_defaultUIScrollViewHeight * 3;

			scrollView.HorizontalAlignment = HorizontalAlignment.Left;
			scrollView.VerticalAlignment = VerticalAlignment.Top;

			parentGrid.Children.Add(scrollView);
		}

		if (setAsContentRoot)
		{
			Log.Comment("Setting window content");
			if (useParentGrid)
			{
				Content = parentGrid;
			}
			else
			{
				Content = scrollView;
			}
		}
	}

	private async Task WaitForEvent(string logComment, UnoManualOrAutoResetEvent eventWaitHandle)
	{
		Log.Comment(logComment);
		if (!UnoIsDebuggerPresent())
		{
			if (!await eventWaitHandle.WaitOne(TimeSpan.FromMilliseconds(c_MaxWaitDuration)))
			{
				throw new Exception("Timeout expiration in WaitForEvent.");
			}
		}
		else
		{
			await eventWaitHandle.WaitOne();
		}
	}

	[DllImport("kernel32.dll")]
	private static extern bool IsDebuggerPresent();

	// Uno specific. Avoid calling IsDebuggerPresent on non-Windows since we are cross-platform :)
	private static bool UnoIsDebuggerPresent()
	{
		if (OperatingSystem.IsWindows())
			return IsDebuggerPresent();
		return false;
	}
}

// Custom ScrollView that records its visual state changes.
public class ScrollViewVisualStateCounts
{
	public uint NoIndicatorStateCount
	{
		get;
		set;
	}

	public uint TouchIndicatorStateCount
	{
		get;
		set;
	}

	public uint MouseIndicatorStateCount
	{
		get;
		set;
	}

	public uint ScrollBarsSeparatorExpandedStateCount
	{
		get;
		set;
	}

	public uint ScrollBarsSeparatorCollapsedStateCount
	{
		get;
		set;
	}

	public uint ScrollBarsSeparatorCollapsedDisabledStateCount
	{
		get;
		set;
	}

	public uint ScrollBarsSeparatorCollapsedWithoutAnimationStateCount
	{
		get;
		set;
	}

	public uint ScrollBarsSeparatorDisplayedWithoutAnimationStateCount
	{
		get;
		set;
	}

	public uint ScrollBarsSeparatorExpandedWithoutAnimationStateCount
	{
		get;
		set;
	}

	public void ResetStateCounts()
	{
		NoIndicatorStateCount = 0;
		TouchIndicatorStateCount = 0;
		MouseIndicatorStateCount = 0;
		ScrollBarsSeparatorExpandedStateCount = 0;
		ScrollBarsSeparatorCollapsedStateCount = 0;
		ScrollBarsSeparatorCollapsedDisabledStateCount = 0;
		ScrollBarsSeparatorCollapsedWithoutAnimationStateCount = 0;
		ScrollBarsSeparatorDisplayedWithoutAnimationStateCount = 0;
		ScrollBarsSeparatorExpandedWithoutAnimationStateCount = 0;
	}
}
