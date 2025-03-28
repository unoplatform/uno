// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;
using System.Threading;
using Common;
using MUXControlsTestApp;
using MUXControlsTestApp.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Private.Infrastructure;
using System.Threading.Tasks;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

partial class ScrollPresenterTests : MUXApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Sets the ScrollPresenter.HorizontalScrollController and ScrollPresenter.VerticalScrollController properties.")]
	public async Task SettingScrollControllerProperties()
	{
		ScrollPresenter scrollPresenter = null;
		CompositionScrollController horizontalScrollController = null;
		CompositionScrollController verticalScrollController = null;

		RunOnUIThread.Execute(() =>
		{
			// We need the styles of the CompositionScrollController, so let's load them
			App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

			scrollPresenter = new ScrollPresenter();
			Verify.IsNotNull(scrollPresenter);

			horizontalScrollController = new CompositionScrollController();
			Verify.IsNotNull(horizontalScrollController);

			verticalScrollController = new CompositionScrollController();
			Verify.IsNotNull(verticalScrollController);

			Log.Comment("Setting ScrollPresenter.HorizontalScrollController and ScrollPresenter.VerticalScrollController properties.");
			scrollPresenter.HorizontalScrollController = horizontalScrollController;
			scrollPresenter.VerticalScrollController = verticalScrollController;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Verifying ScrollPresenter properties.");
			Verify.AreEqual(scrollPresenter.HorizontalScrollController, horizontalScrollController);
			Verify.AreEqual(scrollPresenter.VerticalScrollController, verticalScrollController);

			Verify.IsFalse(horizontalScrollController.CanScroll); // horizontalScrollController.isScrollable is False
			Verify.IsFalse(horizontalScrollController.IsScrollingWithMouse);
			Verify.IsNull(horizontalScrollController.PanningInfo.PanningElementAncestor);
			Verify.IsFalse(verticalScrollController.CanScroll); // verticalScrollController.isScrollable is False
			Verify.IsFalse(verticalScrollController.IsScrollingWithMouse);
			Verify.IsNull(verticalScrollController.PanningInfo.PanningElementAncestor);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Change ScrollPresenter view while scroll controllers are attached.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ChangeOffsetsWhileScrollControllersAreAttached()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		CompositionScrollController horizontalScrollController = null;
		CompositionScrollController verticalScrollController = null;
		UnoAutoResetEvent loadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			// We need the styles of the CompositionScrollController, so let's load them
			App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();
			horizontalScrollController = new CompositionScrollController();
			verticalScrollController = new CompositionScrollController();

			horizontalScrollController.Orientation = Orientation.Horizontal;

			horizontalScrollController.LogMessage += (CompositionScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			verticalScrollController.LogMessage += (CompositionScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			scrollPresenter.HorizontalScrollController = horizontalScrollController;
			scrollPresenter.VerticalScrollController = verticalScrollController;

			SetupUIWithScrollControllers(
				scrollPresenter,
				rectangleScrollPresenterContent,
				horizontalScrollController,
				verticalScrollController,
				loadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", loadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("HorizontalScrollController size={0}, {1}", horizontalScrollController.ActualWidth, horizontalScrollController.ActualHeight);
			Log.Comment("VerticalScrollController size={0}, {1}", verticalScrollController.ActualWidth, verticalScrollController.ActualHeight);
		});

		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Jump to offsets");
		await ScrollTo(
			scrollPresenter,
			(c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 2.0,
			(c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 2.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: true,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: (c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 2.0,
			expectedFinalVerticalOffset: (c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 2.0);

		Log.Comment("Animate to offsets");
		await ScrollTo(
			scrollPresenter,
			(c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 4.0,
			(c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 4.0,
			ScrollingAnimationMode.Enabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: (c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 4.0,
			expectedFinalVerticalOffset: (c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 4.0);

		Log.Comment("Jump to zoomFactor 2.0");
		await ZoomTo(
			scrollPresenter,
			2.0f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false);

		Log.Comment("Animate to zoomFactor 1.5");
		await ZoomTo(
			scrollPresenter,
			1.5f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Enabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false);
	}

	[TestMethod]
	[TestProperty("Description", "Change ScrollPresenter view via attached scroll controllers.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ChangeOffsetsWithAttachedScrollControllers()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		CompositionScrollController horizontalScrollController = null;
		CompositionScrollController verticalScrollController = null;
		UnoAutoResetEvent loadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollCompletedEvent = new UnoAutoResetEvent(false);
		int hOffsetChangeCorrelationId = -1;
		int vOffsetChangeCorrelationId = -1;

		RunOnUIThread.Execute(() =>
		{
			// We need the styles of the CompositionScrollController, so let's load them
			App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();
			horizontalScrollController = new CompositionScrollController();
			verticalScrollController = new CompositionScrollController();

			horizontalScrollController.Orientation = Orientation.Horizontal;

			horizontalScrollController.LogMessage += (CompositionScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			verticalScrollController.LogMessage += (CompositionScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			scrollPresenter.HorizontalScrollController = horizontalScrollController;
			scrollPresenter.VerticalScrollController = verticalScrollController;

			SetupUIWithScrollControllers(
				scrollPresenter,
				rectangleScrollPresenterContent,
				horizontalScrollController,
				verticalScrollController,
				loadedEvent);

			horizontalScrollController.OffsetChangeCompleted += (CompositionScrollController sender, CompositionScrollControllerOffsetChangeCompletedEventArgs args) =>
			{
				Log.Comment("ChangeOffset completed (horizontal). OffsetChangeCorrelationId=" + args.OffsetChangeCorrelationId);

				Log.Comment("Setting completion event");
				scrollCompletedEvent.Set();
			};

			verticalScrollController.OffsetChangeCompleted += (CompositionScrollController sender, CompositionScrollControllerOffsetChangeCompletedEventArgs args) =>
			{
				Log.Comment("ChangeOffset completed (vertical). OffsetChangeCorrelationId=" + args.OffsetChangeCorrelationId);
			};
		});

		await WaitForEvent("Waiting for Loaded event", loadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("HorizontalScrollController size={0}, {1}", horizontalScrollController.ActualWidth, horizontalScrollController.ActualHeight);
			Log.Comment("VerticalScrollController size={0}, {1}", verticalScrollController.ActualWidth, verticalScrollController.ActualHeight);
		});

		Log.Comment("Jump to zoomFactor 0.75");
		await ZoomTo(
			scrollPresenter,
			0.75f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Jumping to horizontal offset");
			hOffsetChangeCorrelationId = horizontalScrollController.ScrollTo(
				(c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 4.0,
				ScrollingAnimationMode.Disabled);

			Log.Comment("Jumping to vertical offset");
			vOffsetChangeCorrelationId = verticalScrollController.ScrollTo(
				(c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 4.0,
				ScrollingAnimationMode.Disabled);

			Verify.AreEqual(hOffsetChangeCorrelationId, vOffsetChangeCorrelationId);
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual((c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 4.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual((c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 4.0, scrollPresenter.VerticalOffset);

			Log.Comment("Animating to horizontal offset");
			hOffsetChangeCorrelationId = horizontalScrollController.ScrollTo(
				(c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 2.0,
				ScrollingAnimationMode.Enabled);

			Log.Comment("Animating to vertical offset");
			vOffsetChangeCorrelationId = verticalScrollController.ScrollTo(
				(c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 2.0,
				ScrollingAnimationMode.Enabled);

			Verify.AreEqual(hOffsetChangeCorrelationId, vOffsetChangeCorrelationId);

			scrollCompletedEvent.Reset();
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual((c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 2.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual((c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 2.0, scrollPresenter.VerticalOffset);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Change ScrollPresenter view with additional velocity via attached scroll controllers.")]
	[Ignore("Fails for missing InteractionTracker/ExpressionAnimation features.")]
	public async Task ChangeOffsetsWithAdditionalVelocityAndAttachedScrollControllers()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		CompositionScrollController horizontalScrollController = null;
		CompositionScrollController verticalScrollController = null;
		UnoAutoResetEvent loadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollCompletedEvent = new UnoAutoResetEvent(false);
		int hOffsetChangeCorrelationId = -1;
		int vOffsetChangeCorrelationId = -1;

		RunOnUIThread.Execute(() =>
		{
			// We need the styles of the CompositionScrollController, so let's load them
			App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();
			horizontalScrollController = new CompositionScrollController();
			verticalScrollController = new CompositionScrollController();

			horizontalScrollController.Orientation = Orientation.Horizontal;

			horizontalScrollController.LogMessage += (CompositionScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			verticalScrollController.LogMessage += (CompositionScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			scrollPresenter.HorizontalScrollController = horizontalScrollController;
			scrollPresenter.VerticalScrollController = verticalScrollController;

			SetupUIWithScrollControllers(
				scrollPresenter,
				rectangleScrollPresenterContent,
				horizontalScrollController,
				verticalScrollController,
				loadedEvent);

			horizontalScrollController.OffsetChangeCompleted += (CompositionScrollController sender, CompositionScrollControllerOffsetChangeCompletedEventArgs args) =>
			{
				Log.Comment("ChangeOffset completed. OffsetChangeCorrelationId=" + args.OffsetChangeCorrelationId);

				Log.Comment("Setting completion event");
				scrollCompletedEvent.Set();
			};
		});

		await WaitForEvent("Waiting for Loaded event", loadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("HorizontalScrollController size={0}, {1}", horizontalScrollController.ActualWidth, horizontalScrollController.ActualHeight);
			Log.Comment("VerticalScrollController size={0}, {1}", verticalScrollController.ActualWidth, verticalScrollController.ActualHeight);
		});

		Log.Comment("Jump to zoomFactor 0.75");
		await ZoomTo(
			scrollPresenter,
			0.75f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Adding velocity to horizontal offset, with default inertia decay rate");
			hOffsetChangeCorrelationId = horizontalScrollController.AddScrollVelocity(
				100.0f /*offsetVelocity*/, null /*inertiaDecayRate*/);

			Log.Comment("Adding velocity to vertical offset, with default inertia decay rate");
			vOffsetChangeCorrelationId = verticalScrollController.AddScrollVelocity(
				100.0f /*offsetVelocity*/, null /*inertiaDecayRate*/);

			Verify.AreEqual(hOffsetChangeCorrelationId, vOffsetChangeCorrelationId);
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("scrollPresenter.HorizontalOffset={0}", scrollPresenter.HorizontalOffset);
			Log.Comment("scrollPresenter.VerticalOffset={0}", scrollPresenter.VerticalOffset);

			Verify.IsTrue(scrollPresenter.HorizontalOffset > 20.0);
			Verify.IsTrue(scrollPresenter.VerticalOffset > 20.0);

			Log.Comment("Adding negative velocity to horizontal offset, with custom inertia decay rate");
			hOffsetChangeCorrelationId = horizontalScrollController.AddScrollVelocity(
				-50.0f /*offsetVelocity*/, 0.9f /*inertiaDecayRate*/);

			Log.Comment("Adding negative velocity to vertical offset, with custom inertia decay rate");
			vOffsetChangeCorrelationId = verticalScrollController.AddScrollVelocity(
				-50.0f /*offsetVelocity*/, 0.9f /*inertiaDecayRate*/);

			Verify.AreEqual(hOffsetChangeCorrelationId, vOffsetChangeCorrelationId);

			scrollCompletedEvent.Reset();
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("scrollPresenter.HorizontalOffset={0}", scrollPresenter.HorizontalOffset);
			Log.Comment("scrollPresenter.VerticalOffset={0}", scrollPresenter.VerticalOffset);

			Verify.IsTrue(scrollPresenter.HorizontalOffset < 20.0);
			Verify.IsTrue(scrollPresenter.VerticalOffset < 20.0);

			Log.Comment("Adding velocity to horizontal offset, with no inertia decay rate");
			hOffsetChangeCorrelationId = horizontalScrollController.AddScrollVelocity(
				200.0f /*offsetVelocity*/, 0.0f /*inertiaDecayRate*/);

			Log.Comment("Adding velocity to vertical offset, with no inertia decay rate");
			vOffsetChangeCorrelationId = verticalScrollController.AddScrollVelocity(
				200.0f /*offsetVelocity*/, 0.0f /*inertiaDecayRate*/);

			Verify.AreEqual(hOffsetChangeCorrelationId, vOffsetChangeCorrelationId);

			scrollCompletedEvent.Reset();
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("scrollPresenter.HorizontalOffset={0}", scrollPresenter.HorizontalOffset);
			Log.Comment("scrollPresenter.VerticalOffset={0}", scrollPresenter.VerticalOffset);

			Verify.AreEqual(600.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(250.0, scrollPresenter.VerticalOffset);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Change ScrollPresenter view while a bi-directional scroll controller is attached.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ChangeOffsetsWhileBiDirectionalScrollControllerIsAttached()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		BiDirectionalScrollController biDirectionalScrollController = null;
		UnoAutoResetEvent loadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			// We need the styles of the CompositionScrollController, so let's load them
			App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();
			biDirectionalScrollController = new BiDirectionalScrollController();

			biDirectionalScrollController.LogMessage += (BiDirectionalScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			scrollPresenter.HorizontalScrollController = biDirectionalScrollController.HorizontalScrollController;
			scrollPresenter.VerticalScrollController = biDirectionalScrollController.VerticalScrollController;

			SetupUIWithBiDirectionalScrollController(
				scrollPresenter,
				rectangleScrollPresenterContent,
				biDirectionalScrollController,
				loadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", loadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("BiDirectionalScrollController size={0}, {1}", biDirectionalScrollController.ActualWidth, biDirectionalScrollController.ActualHeight);
		});

		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Jump to offsets");
		await ScrollTo(
			scrollPresenter,
			(c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 2.0,
			(c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 2.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: true,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: (c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 2.0,
			expectedFinalVerticalOffset: (c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 2.0);

		Log.Comment("Animate to offsets");
		await ScrollTo(
			scrollPresenter,
			(c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 4.0,
			(c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 4.0,
			ScrollingAnimationMode.Enabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: (c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth) / 4.0,
			expectedFinalVerticalOffset: (c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight) / 4.0);

		Log.Comment("Jump to zoomFactor 2.0");
		await ZoomTo(
			scrollPresenter,
			2.0f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false);

		Log.Comment("Animate to zoomFactor 1.5");
		await ZoomTo(
			scrollPresenter,
			1.5f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Enabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false);
	}

	[TestMethod]
	[TestProperty("Description", "Change ScrollPresenter view via attached bi-directional scroll controller.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ChangeOffsetsWithAttachedBiDirectionalScrollController()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		BiDirectionalScrollController biDirectionalScrollController = null;
		UnoAutoResetEvent loadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollCompletedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			// We need the styles of the CompositionScrollController, so let's load them
			App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();
			biDirectionalScrollController = new BiDirectionalScrollController();

			biDirectionalScrollController.LogMessage += (BiDirectionalScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			scrollPresenter.HorizontalScrollController = biDirectionalScrollController.HorizontalScrollController;
			scrollPresenter.VerticalScrollController = biDirectionalScrollController.VerticalScrollController;

			SetupUIWithBiDirectionalScrollController(
				scrollPresenter,
				rectangleScrollPresenterContent,
				biDirectionalScrollController,
				loadedEvent);

			biDirectionalScrollController.ScrollCompleted += (BiDirectionalScrollController sender, BiDirectionalScrollControllerScrollingScrollCompletedEventArgs args) =>
			{
				Log.Comment("ChangeOffset completed. OffsetsChangeCorrelationId=" + args);

				Log.Comment("Setting completion event");
				scrollCompletedEvent.Set();
			};
		});

		await WaitForEvent("Waiting for Loaded event", loadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("BiDirectionalScrollController size={0}, {1}", biDirectionalScrollController.ActualWidth, biDirectionalScrollController.ActualHeight);
		});

		Log.Comment("Jump to zoomFactor 0.75");
		await ZoomTo(
			scrollPresenter,
			0.75f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Jumping to offsets");
			biDirectionalScrollController.ScrollTo(
				(c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 4.0,
				(c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 4.0,
				ScrollingAnimationMode.Disabled);
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual((c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 4.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual((c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 4.0, scrollPresenter.VerticalOffset);

			Log.Comment("Animating to offsets");
			biDirectionalScrollController.ScrollTo(
				(c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 2.0,
				(c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 2.0,
				ScrollingAnimationMode.Enabled);

			scrollCompletedEvent.Reset();
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual((c_defaultUIScrollPresenterContentWidth * 0.75 - c_defaultUIScrollPresenterWidth) / 2.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual((c_defaultUIScrollPresenterContentHeight * 0.75 - c_defaultUIScrollPresenterHeight) / 2.0, scrollPresenter.VerticalOffset);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Change ScrollPresenter view with additional velocity via attached bi-directional scroll controller.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ChangeOffsetsWithAdditionalVelocityAndAttachedBiDirectionalScrollController()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		BiDirectionalScrollController biDirectionalScrollController = null;
		UnoAutoResetEvent loadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollCompletedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			// We need the styles of the CompositionScrollController, so let's load them
			App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();
			biDirectionalScrollController = new BiDirectionalScrollController();

			biDirectionalScrollController.LogMessage += (BiDirectionalScrollController sender, string args) =>
			{
				Log.Comment(args);
			};

			scrollPresenter.HorizontalScrollController = biDirectionalScrollController.HorizontalScrollController;
			scrollPresenter.VerticalScrollController = biDirectionalScrollController.VerticalScrollController;

			SetupUIWithBiDirectionalScrollController(
				scrollPresenter,
				rectangleScrollPresenterContent,
				biDirectionalScrollController,
				loadedEvent);

			biDirectionalScrollController.ScrollCompleted += (BiDirectionalScrollController sender, BiDirectionalScrollControllerScrollingScrollCompletedEventArgs args) =>
			{
				Log.Comment("AddScrollVelocity completed. OffsetsChangeCorrelationId=" + args);

				Log.Comment("Setting completion event");
				scrollCompletedEvent.Set();
			};
		});

		await WaitForEvent("Waiting for Loaded event", loadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("BiDirectionalScrollController size={0}, {1}", biDirectionalScrollController.ActualWidth, biDirectionalScrollController.ActualHeight);
		});

		Log.Comment("Jump to zoomFactor 0.75");
		await ZoomTo(
			scrollPresenter,
			0.75f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Adding velocity to offsets, with default inertia decay rates");
			biDirectionalScrollController.AddScrollVelocity(
				new Vector2(100.0f) /*offsetsVelocity*/, null /*inertiaDecayRate*/);
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("scrollPresenter.HorizontalOffset={0}", scrollPresenter.HorizontalOffset);
			Log.Comment("scrollPresenter.VerticalOffset={0}", scrollPresenter.VerticalOffset);

			Verify.IsTrue(scrollPresenter.HorizontalOffset > 20.0);
			Verify.IsTrue(scrollPresenter.VerticalOffset > 20.0);

			Log.Comment("Adding negative velocity to offsets, with custom inertia decay rates");
			biDirectionalScrollController.AddScrollVelocity(
				new Vector2(-50.0f) /*offsetsVelocity*/, new Vector2(0.9f) /*inertiaDecayRate*/);

			scrollCompletedEvent.Reset();
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("scrollPresenter.HorizontalOffset={0}", scrollPresenter.HorizontalOffset);
			Log.Comment("scrollPresenter.VerticalOffset={0}", scrollPresenter.VerticalOffset);

			Verify.IsTrue(scrollPresenter.HorizontalOffset < 20.0);
			Verify.IsTrue(scrollPresenter.VerticalOffset < 20.0);

			Log.Comment("Adding velocity to offsets, with no inertia decay rates");
			biDirectionalScrollController.AddScrollVelocity(
				new Vector2(200.0f) /*offsetsVelocity*/, new Vector2(0.0f) /*inertiaDecayRate*/);

			scrollCompletedEvent.Reset();
		});

		await WaitForEvent("Waiting for operation completion", scrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("scrollPresenter.HorizontalOffset={0}", scrollPresenter.HorizontalOffset);
			Log.Comment("scrollPresenter.VerticalOffset={0}", scrollPresenter.VerticalOffset);

			Verify.AreEqual(600.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(250.0, scrollPresenter.VerticalOffset);
		});
	}

	private void SetupUIWithScrollControllers(
		ScrollPresenter scrollPresenter,
		Rectangle rectangleScrollPresenterContent,
		CompositionScrollController horizontalScrollController,
		CompositionScrollController verticalScrollController,
		UnoAutoResetEvent loadedEvent)
	{
		Log.Comment("Setting up UI with ScrollPresenter and scroll controlllers" + (rectangleScrollPresenterContent == null ? "" : " and Rectangle"));

		LinearGradientBrush twoColorLGB = new LinearGradientBrush() { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };

		GradientStop brownGS = new GradientStop() { Color = Windows.UI.Colors.Brown, Offset = 0.0 };
		twoColorLGB.GradientStops.Add(brownGS);

		GradientStop orangeGS = new GradientStop() { Color = Windows.UI.Colors.Orange, Offset = 1.0 };
		twoColorLGB.GradientStops.Add(orangeGS);

		if (rectangleScrollPresenterContent != null)
		{
			rectangleScrollPresenterContent.Width = c_defaultUIScrollPresenterContentWidth;
			rectangleScrollPresenterContent.Height = c_defaultUIScrollPresenterContentHeight;
			rectangleScrollPresenterContent.Fill = twoColorLGB;
		}

		Verify.IsNotNull(scrollPresenter);
		scrollPresenter.Width = c_defaultUIScrollPresenterWidth;
		scrollPresenter.Height = c_defaultUIScrollPresenterHeight;
		if (rectangleScrollPresenterContent != null)
		{
			scrollPresenter.Content = rectangleScrollPresenterContent;
		}

		horizontalScrollController.Width = c_defaultUIScrollPresenterWidth;
		horizontalScrollController.HorizontalAlignment = HorizontalAlignment.Left;

		StackPanel horizontalStackPanel = new StackPanel();
		horizontalStackPanel.Orientation = Orientation.Horizontal;
		horizontalStackPanel.Children.Add(scrollPresenter);
		horizontalStackPanel.Children.Add(verticalScrollController);

		StackPanel verticalStackPanel = new StackPanel();
		verticalStackPanel.Children.Add(horizontalStackPanel);
		verticalStackPanel.Children.Add(horizontalScrollController);
		verticalStackPanel.Width = c_defaultUIScrollPresenterWidth + c_defaultUIScrollControllerThickness;

		if (loadedEvent != null)
		{
			verticalStackPanel.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("Loaded event handler");
				loadedEvent.Set();
			};
		}

		Log.Comment("Setting window content");
		Content = verticalStackPanel;
	}

	private void SetupUIWithBiDirectionalScrollController(
		ScrollPresenter scrollPresenter,
		Rectangle rectangleScrollPresenterContent,
		BiDirectionalScrollController biDirectionalScrollController,
		UnoAutoResetEvent loadedEvent)
	{
		Log.Comment("Setting up UI with ScrollPresenter and bi-directional scroll controller" + (rectangleScrollPresenterContent == null ? "" : " and Rectangle"));

		LinearGradientBrush twoColorLGB = new LinearGradientBrush() { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };

		GradientStop brownGS = new GradientStop() { Color = Windows.UI.Colors.Brown, Offset = 0.0 };
		twoColorLGB.GradientStops.Add(brownGS);

		GradientStop orangeGS = new GradientStop() { Color = Windows.UI.Colors.Orange, Offset = 1.0 };
		twoColorLGB.GradientStops.Add(orangeGS);

		if (rectangleScrollPresenterContent != null)
		{
			rectangleScrollPresenterContent.Width = c_defaultUIScrollPresenterContentWidth;
			rectangleScrollPresenterContent.Height = c_defaultUIScrollPresenterContentHeight;
			rectangleScrollPresenterContent.Fill = twoColorLGB;
		}

		Verify.IsNotNull(scrollPresenter);
		scrollPresenter.Width = c_defaultUIScrollPresenterWidth;
		scrollPresenter.Height = c_defaultUIScrollPresenterHeight;
		if (rectangleScrollPresenterContent != null)
		{
			scrollPresenter.Content = rectangleScrollPresenterContent;
		}

		biDirectionalScrollController.Width = c_defaultUIScrollPresenterHeight;
		biDirectionalScrollController.Height = c_defaultUIScrollPresenterHeight;

		StackPanel horizontalStackPanel = new StackPanel();
		horizontalStackPanel.Orientation = Orientation.Horizontal;
		horizontalStackPanel.Children.Add(scrollPresenter);
		horizontalStackPanel.Children.Add(biDirectionalScrollController);

		if (loadedEvent != null)
		{
			horizontalStackPanel.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("Loaded event handler");
				loadedEvent.Set();
			};
		}

		Log.Comment("Setting window content");
		Content = horizontalStackPanel;
	}
}
