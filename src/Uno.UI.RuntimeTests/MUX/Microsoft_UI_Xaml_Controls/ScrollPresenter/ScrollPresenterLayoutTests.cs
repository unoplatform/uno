// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Numerics;
using System.Threading;
using Common;
using Windows.UI.Composition;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
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
	private enum BiDirectionalAlignment
	{
		Near,
		Center,
		Stretch,
		Far
	}

	[TestMethod]
	[TestProperty("Description", "Sets ScrollPresenter.Content.Margin and verifies InteractionTracker.MaxPosition.")]
	public async Task BasicMargin()
	{
		const double c_Margin = 50.0;
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
			Log.Comment("Adding positive Margin to ScrollPresenter.Content");
			rectangleScrollPresenterContent.Margin = new Thickness(c_Margin);
		});

		// Try to jump beyond maximum offsets
		await ScrollTo(
			scrollPresenter,
			c_defaultUIScrollPresenterContentWidth + 2 * c_Margin - c_defaultUIScrollPresenterWidth + 10.0,
			c_defaultUIScrollPresenterContentHeight + 2 * c_Margin - c_defaultUIScrollPresenterHeight + 10.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: true,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: c_defaultUIScrollPresenterContentWidth + 2 * c_Margin - c_defaultUIScrollPresenterWidth,
			expectedFinalVerticalOffset: c_defaultUIScrollPresenterContentHeight + 2 * c_Margin - c_defaultUIScrollPresenterHeight);

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Adding negative Margin to ScrollPresenter.Content");
			rectangleScrollPresenterContent.Margin = new Thickness(-c_Margin);
		});

		// Try to jump beyond maximum offsets
		await ScrollTo(
			scrollPresenter,
			c_defaultUIScrollPresenterContentWidth - 2 * c_Margin - c_defaultUIScrollPresenterWidth + 10.0,
			c_defaultUIScrollPresenterContentHeight - 2 * c_Margin - c_defaultUIScrollPresenterHeight + 10.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: c_defaultUIScrollPresenterContentWidth - 2 * c_Margin - c_defaultUIScrollPresenterWidth,
			expectedFinalVerticalOffset: c_defaultUIScrollPresenterContentHeight - 2 * c_Margin - c_defaultUIScrollPresenterHeight);
	}

	[TestMethod]
	[TestProperty("Description", "Sets ScrollPresenter.Content.Padding and verifies InteractionTracker.MaxPosition.")]
	public async Task BasicPadding()
	{
		const double c_Padding = 50.0;
		ScrollPresenter scrollPresenter = null;
		Border borderScrollPresenterContent = null;
		Rectangle rectangle = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			borderScrollPresenterContent = new Border();
			rectangle = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			borderScrollPresenterContent.Width = c_defaultUIScrollPresenterContentWidth;
			borderScrollPresenterContent.Height = c_defaultUIScrollPresenterContentHeight;
			borderScrollPresenterContent.Child = rectangle;
			scrollPresenter.Content = borderScrollPresenterContent;

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Adding Padding to ScrollPresenter.Content");
			borderScrollPresenterContent.Padding = new Thickness(c_Padding);
		});

		// Try to jump beyond maximum offsets
		await ScrollTo(
			scrollPresenter,
			c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth + 10.0,
			c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight + 10.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: true,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: c_defaultUIScrollPresenterContentWidth - c_defaultUIScrollPresenterWidth,
			expectedFinalVerticalOffset: c_defaultUIScrollPresenterContentHeight - c_defaultUIScrollPresenterHeight);
	}

	[TestMethod]
	[TestProperty("Description", "Sets ScrollPresenter.Content.HorizontalAlignment/VerticalAlignment and verifies content positioning.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task BasicAlignment()
	{
		const float c_smallZoomFactor = 0.15f;
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		float horizontalOffset = 0.0f;
		float verticalOffset = 0.0f;
		float zoomFactor = 1.0f;
		Compositor compositor = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
			compositor = Compositor.GetSharedCompositor(); //CompositionTarget.GetCompositorForCurrentThread(); - UNO Specific: GetCompositorForcurrentThread() isn't implemented.
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		// Jump to absolute small zoomFactor to make the content smaller than the viewport.
		await ZoomTo(scrollPresenter, c_smallZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Stretch/Strech alignments");
			Verify.AreEqual(HorizontalAlignment.Stretch, rectangleScrollPresenterContent.HorizontalAlignment);
			Verify.AreEqual(VerticalAlignment.Stretch, rectangleScrollPresenterContent.VerticalAlignment);
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.IsTrue(Math.Abs(horizontalOffset - (float)(c_defaultUIScrollPresenterWidth - c_defaultUIScrollPresenterContentWidth * c_smallZoomFactor) / 2.0f) < c_defaultOffsetResultTolerance);
		Verify.IsTrue(Math.Abs(verticalOffset - (float)(c_defaultUIScrollPresenterHeight - c_defaultUIScrollPresenterContentHeight * c_smallZoomFactor) / 2.0f) < c_defaultOffsetResultTolerance);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Left/Top alignments");
			rectangleScrollPresenterContent.HorizontalAlignment = HorizontalAlignment.Left;
			rectangleScrollPresenterContent.VerticalAlignment = VerticalAlignment.Top;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.AreEqual(0.0f, horizontalOffset);
		Verify.AreEqual(0.0f, verticalOffset);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Right/Bottom alignments");
			rectangleScrollPresenterContent.HorizontalAlignment = HorizontalAlignment.Right;
			rectangleScrollPresenterContent.VerticalAlignment = VerticalAlignment.Bottom;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.IsTrue(Math.Abs(horizontalOffset - (float)(c_defaultUIScrollPresenterWidth - c_defaultUIScrollPresenterContentWidth * c_smallZoomFactor)) < c_defaultOffsetResultTolerance);
		Verify.IsTrue(Math.Abs(verticalOffset - (float)(c_defaultUIScrollPresenterHeight - c_defaultUIScrollPresenterContentHeight * c_smallZoomFactor)) < c_defaultOffsetResultTolerance);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Center/Center alignments");
			rectangleScrollPresenterContent.HorizontalAlignment = HorizontalAlignment.Center;
			rectangleScrollPresenterContent.VerticalAlignment = VerticalAlignment.Center;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.IsTrue(Math.Abs(horizontalOffset - (float)(c_defaultUIScrollPresenterWidth - c_defaultUIScrollPresenterContentWidth * c_smallZoomFactor) / 2.0f) < c_defaultOffsetResultTolerance);
		Verify.IsTrue(Math.Abs(verticalOffset - (float)(c_defaultUIScrollPresenterHeight - c_defaultUIScrollPresenterContentHeight * c_smallZoomFactor) / 2.0f) < c_defaultOffsetResultTolerance);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);
	}

	[TestMethod]
	[TestProperty("Description", "Validates ScrollPresenter.ViewportHeight for various layouts.")]
	public void ViewportHeight()
	{
		for (double scrollPresenterContentHeight = 50.0; scrollPresenterContentHeight <= 350.0; scrollPresenterContentHeight += 300.0)
		{
			ViewportHeight(
				isScrollPresenterParentSizeSet: false,
				isScrollPresenterParentMaxSizeSet: false,
				scrollPresenterVerticalAlignment: VerticalAlignment.Top,
				scrollPresenterContentHeight: scrollPresenterContentHeight);
			ViewportHeight(
				isScrollPresenterParentSizeSet: true,
				isScrollPresenterParentMaxSizeSet: false,
				scrollPresenterVerticalAlignment: VerticalAlignment.Top,
				scrollPresenterContentHeight: scrollPresenterContentHeight);
			ViewportHeight(
				isScrollPresenterParentSizeSet: false,
				isScrollPresenterParentMaxSizeSet: true,
				scrollPresenterVerticalAlignment: VerticalAlignment.Top,
				scrollPresenterContentHeight: scrollPresenterContentHeight);

			ViewportHeight(
				isScrollPresenterParentSizeSet: false,
				isScrollPresenterParentMaxSizeSet: false,
				scrollPresenterVerticalAlignment: VerticalAlignment.Center,
				scrollPresenterContentHeight: scrollPresenterContentHeight);
			ViewportHeight(
				isScrollPresenterParentSizeSet: true,
				isScrollPresenterParentMaxSizeSet: false,
				scrollPresenterVerticalAlignment: VerticalAlignment.Center,
				scrollPresenterContentHeight: scrollPresenterContentHeight);
			ViewportHeight(
				isScrollPresenterParentSizeSet: false,
				isScrollPresenterParentMaxSizeSet: true,
				scrollPresenterVerticalAlignment: VerticalAlignment.Center,
				scrollPresenterContentHeight: scrollPresenterContentHeight);

			ViewportHeight(
				isScrollPresenterParentSizeSet: false,
				isScrollPresenterParentMaxSizeSet: false,
				scrollPresenterVerticalAlignment: VerticalAlignment.Stretch,
				scrollPresenterContentHeight: scrollPresenterContentHeight);
			ViewportHeight(
				isScrollPresenterParentSizeSet: true,
				isScrollPresenterParentMaxSizeSet: false,
				scrollPresenterVerticalAlignment: VerticalAlignment.Stretch,
				scrollPresenterContentHeight: scrollPresenterContentHeight);
			ViewportHeight(
				isScrollPresenterParentSizeSet: false,
				isScrollPresenterParentMaxSizeSet: true,
				scrollPresenterVerticalAlignment: VerticalAlignment.Stretch,
				scrollPresenterContentHeight: scrollPresenterContentHeight);
		}
	}

	private void ViewportHeight(
		bool isScrollPresenterParentSizeSet,
		bool isScrollPresenterParentMaxSizeSet,
		VerticalAlignment scrollPresenterVerticalAlignment,
		double scrollPresenterContentHeight)
	{
		Log.Comment($"ViewportHeight test case - isScrollPresenterParentSizeSet: {isScrollPresenterParentSizeSet}, isScrollPresenterParentMaxSizeSet: {isScrollPresenterParentMaxSizeSet}, scrollPresenterVerticalAlignment: {scrollPresenterVerticalAlignment}, scrollPresenterContentHeight: {scrollPresenterContentHeight}");

		RunOnUIThread.Execute(() =>
		{
			var rectangle = new Rectangle()
			{
				Width = 30,
				Height = scrollPresenterContentHeight
			};

			var stackPanel = new StackPanel()
			{
				BorderThickness = new Thickness(5),
				Margin = new Thickness(7),
				VerticalAlignment = VerticalAlignment.Top
			};
			stackPanel.Children.Add(rectangle);

			var scrollPresenter = new ScrollPresenter()
			{
				Content = stackPanel,
				ContentOrientation = ScrollingContentOrientation.Vertical,
				VerticalAlignment = scrollPresenterVerticalAlignment
			};

			var border = new Border()
			{
				BorderThickness = new Thickness(2),
				Margin = new Thickness(3),
				VerticalAlignment = VerticalAlignment.Center,
				Child = scrollPresenter
			};
			if (isScrollPresenterParentSizeSet)
			{
				border.Width = 300.0;
				border.Height = 200.0;
			}
			if (isScrollPresenterParentMaxSizeSet)
			{
				border.MaxWidth = 300.0;
				border.MaxHeight = 200.0;
			}

			Log.Comment("Setting window content");
			Content = border;
			Content.UpdateLayout();

			double expectedViewportHeight =
				rectangle.Height + stackPanel.BorderThickness.Top + stackPanel.BorderThickness.Bottom +
				stackPanel.Margin.Top + stackPanel.Margin.Bottom;

			double borderChildAvailableHeight = border.ActualHeight - border.BorderThickness.Top - border.BorderThickness.Bottom;

			if (expectedViewportHeight > borderChildAvailableHeight || scrollPresenterVerticalAlignment == VerticalAlignment.Stretch)
			{
				expectedViewportHeight = borderChildAvailableHeight;
			}

			Log.Comment($"border.ActualWidth: {border.ActualWidth}, border.ActualHeight: {border.ActualHeight}");
			Log.Comment($"Checking ViewportWidth - scrollPresenter.ViewportWidth: {scrollPresenter.ViewportWidth}, scrollPresenter.ActualWidth: {scrollPresenter.ActualWidth}");
			Verify.AreEqual(scrollPresenter.ViewportWidth, scrollPresenter.ActualWidth);

			Log.Comment($"Checking ViewportHeight - expectedViewportHeight: {expectedViewportHeight}, scrollPresenter.ViewportHeight: {scrollPresenter.ViewportHeight}, scrollPresenter.ActualHeight: {scrollPresenter.ActualHeight}");
			Verify.AreEqual(expectedViewportHeight, scrollPresenter.ViewportHeight);
			Verify.AreEqual(scrollPresenter.ViewportHeight, scrollPresenter.ActualHeight);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Uses a StackPanel with Stretch alignment as ScrollPresenter.Content to verify it stretched to the size of the ScrollPresenter.")]
	public void StretchAlignment()
	{
		RunOnUIThread.Execute(() =>
		{
			var rectangle = new Rectangle();
			rectangle.Height = c_defaultUIScrollPresenterContentHeight;
			var stackPanel = new StackPanel();
			stackPanel.Children.Add(rectangle);
			var scrollPresenter = new ScrollPresenter();
			scrollPresenter.Width = c_defaultUIScrollPresenterWidth;
			scrollPresenter.Height = c_defaultUIScrollPresenterHeight;
			scrollPresenter.Content = stackPanel;

			Log.Comment("Setting window content");
			Content = scrollPresenter;
			Content.UpdateLayout();


			Log.Comment("Checking Stretch/Strech alignments");
			Verify.AreEqual(HorizontalAlignment.Stretch, stackPanel.HorizontalAlignment);
			Verify.AreEqual(VerticalAlignment.Stretch, stackPanel.VerticalAlignment);

			Log.Comment("Checking StackPanel size");
			Verify.AreEqual(c_defaultUIScrollPresenterWidth, stackPanel.ActualWidth);
			Verify.AreEqual(c_defaultUIScrollPresenterContentHeight, stackPanel.ActualHeight);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Sets ScrollPresenter.Content.HorizontalAlignment/VerticalAlignment and ScrollPresenter.Content.Margin and verifies content positioning.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task BasicMarginAndAlignment()
	{
		const float c_smallZoomFactor = 0.15f;
		const double c_Margin = 40.0;
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		float horizontalOffset = 0.0f;
		float verticalOffset = 0.0f;
		float zoomFactor = 1.0f;
		Compositor compositor = null;

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);

			Log.Comment("Adding positive Margin to ScrollPresenter.Content");
			rectangleScrollPresenterContent.Margin = new Thickness(c_Margin);
			compositor = Compositor.GetSharedCompositor(); //CompositionTarget.GetCompositorForCurrentThread(); - UNO Specific: GetCompositorForcurrentThread() isn't implemented.
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		// Jump to absolute small zoomFactor to make the content smaller than the viewport.
		await ZoomTo(scrollPresenter, c_smallZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Stretch/Strech alignments");
			Verify.AreEqual(HorizontalAlignment.Stretch, rectangleScrollPresenterContent.HorizontalAlignment);
			Verify.AreEqual(VerticalAlignment.Stretch, rectangleScrollPresenterContent.VerticalAlignment);

		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.IsTrue(Math.Abs(horizontalOffset - 20.0f) < c_defaultOffsetResultTolerance);
		Verify.IsTrue(Math.Abs(verticalOffset - 15.0f) < c_defaultOffsetResultTolerance);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Left/Top alignments");
			rectangleScrollPresenterContent.HorizontalAlignment = HorizontalAlignment.Left;
			rectangleScrollPresenterContent.VerticalAlignment = VerticalAlignment.Top;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.IsTrue(Math.Abs(horizontalOffset + 34.0f) < c_defaultOffsetResultTolerance);
		Verify.IsTrue(Math.Abs(verticalOffset + 34.0f) < c_defaultOffsetResultTolerance);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Right/Bottom alignments");
			rectangleScrollPresenterContent.HorizontalAlignment = HorizontalAlignment.Right;
			rectangleScrollPresenterContent.VerticalAlignment = VerticalAlignment.Bottom;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.IsTrue(Math.Abs(horizontalOffset - 74.0f) < c_defaultOffsetResultTolerance);
		Verify.IsTrue(Math.Abs(verticalOffset - 64.0f) < c_defaultOffsetResultTolerance);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Covering Center/Center alignments");
			rectangleScrollPresenterContent.HorizontalAlignment = HorizontalAlignment.Center;
			rectangleScrollPresenterContent.VerticalAlignment = VerticalAlignment.Center;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		Verify.IsTrue(Math.Abs(horizontalOffset - 20.0f) < c_defaultOffsetResultTolerance);
		Verify.IsTrue(Math.Abs(verticalOffset - 15.0f) < c_defaultOffsetResultTolerance);
		Verify.AreEqual(c_smallZoomFactor, zoomFactor);
	}

	[TestMethod]
	[TestProperty("Description", "Sets ScrollPresenter.Content to an unsized and stretched Image, verifies resulting ScrollPresenter extents.")]
	[Ignore("Fails for missing InteractionTracker features")]
	public async Task StretchedImage()
	{
		ScrollPresenter scrollPresenter = null;
		Image imageScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		const double margin = 10.0;

		RunOnUIThread.Execute(() =>
		{
			Uri uri = new Uri("ms-appx:/Assets/ingredient8.png");
			Verify.IsNotNull(uri);
			imageScrollPresenterContent = new Image();
			imageScrollPresenterContent.Source = new BitmapImage(uri);
			imageScrollPresenterContent.Margin = new Thickness(margin);

			scrollPresenter = new ScrollPresenter();
			scrollPresenter.Content = imageScrollPresenterContent;

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(scrollPresenter.ContentOrientation, ScrollingContentOrientation.Both);
			// Image is unconstrained and stretches to largest square contained in the 300 x 200 viewport: 200 x 200.
			ValidateStretchedImageSize(
				scrollPresenter,
				imageScrollPresenterContent,
				desiredSize: 80.0 + 2.0 * margin /*natural size + margins*/,
				actualSize: c_defaultUIScrollPresenterHeight - 2.0 * margin,
				extentSize: c_defaultUIScrollPresenterHeight);

			Log.Comment("Changing ScrollPresenter.ContentOrientation to Vertical.");
			scrollPresenter.ContentOrientation = ScrollingContentOrientation.Vertical;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(scrollPresenter.ContentOrientation, ScrollingContentOrientation.Vertical);
			// Image is constrained horizontally to 300 and stretches to the 300 x 300 square.
			ValidateStretchedImageSize(
				scrollPresenter,
				imageScrollPresenterContent,
				desiredSize: c_defaultUIScrollPresenterWidth,
				actualSize: c_defaultUIScrollPresenterWidth - 2.0 * margin,
				extentSize: c_defaultUIScrollPresenterWidth);

			Log.Comment("Changing ScrollPresenter.ContentOrientation to Horizontal.");
			scrollPresenter.ContentOrientation = ScrollingContentOrientation.Horizontal;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(scrollPresenter.ContentOrientation, ScrollingContentOrientation.Horizontal);
			// Image is constrained vertically to 200 and stretches to the 200 x 200 square.
			ValidateStretchedImageSize(
				scrollPresenter,
				imageScrollPresenterContent,
				desiredSize: c_defaultUIScrollPresenterHeight,
				actualSize: c_defaultUIScrollPresenterHeight - 2.0 * margin,
				extentSize: c_defaultUIScrollPresenterHeight);
		});
	}

	private void ValidateStretchedImageSize(
		ScrollPresenter scrollPresenter,
		Image imageScrollPresenterContent,
		double desiredSize,
		double actualSize,
		double extentSize)
	{
		Log.Comment($"Sizes with ScrollPresenter.ContentOrientation={scrollPresenter.ContentOrientation}");
		Log.Comment($"Image DesiredSize=({imageScrollPresenterContent.DesiredSize.Width} x {imageScrollPresenterContent.DesiredSize.Height})");
		Log.Comment($"Image RenderSize=({imageScrollPresenterContent.RenderSize.Width} x {imageScrollPresenterContent.RenderSize.Height})");
		Log.Comment($"Image ActualSize=({imageScrollPresenterContent.ActualWidth} x {imageScrollPresenterContent.ActualHeight})");
		Log.Comment($"ScrollPresenter ExtentSize=({scrollPresenter.ExtentWidth} x {scrollPresenter.ExtentHeight})");

		Verify.AreEqual(imageScrollPresenterContent.DesiredSize.Width, desiredSize);
		Verify.AreEqual(imageScrollPresenterContent.DesiredSize.Height, desiredSize);
		Verify.AreEqual(imageScrollPresenterContent.RenderSize.Width, actualSize);
		Verify.AreEqual(imageScrollPresenterContent.RenderSize.Height, actualSize);
		Verify.AreEqual(imageScrollPresenterContent.ActualWidth, actualSize);
		Verify.AreEqual(imageScrollPresenterContent.ActualHeight, actualSize);
		Verify.AreEqual(scrollPresenter.ExtentWidth, extentSize);
		Verify.AreEqual(scrollPresenter.ExtentHeight, extentSize);
	}

	[TestMethod]
	[TestProperty("Description", "Sets ScrollPresenter.Content to Image with unnatural size and verifies InteractionTracker.MaxPosition.")]
	public async Task ImageWithUnnaturalSize()
	{
		const double c_UnnaturalImageWidth = 1200.0;
		const double c_UnnaturalImageHeight = 1000.0;
		ScrollPresenter scrollPresenter = null;
		Image imageScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			imageScrollPresenterContent = new Image();
			scrollPresenter = new ScrollPresenter();

			Uri uri = new Uri("ms-appx:/Assets/ingredient3.png");
			Verify.IsNotNull(uri);
			imageScrollPresenterContent.Source = new BitmapImage(uri);
			imageScrollPresenterContent.Width = c_UnnaturalImageWidth;
			imageScrollPresenterContent.Height = c_UnnaturalImageHeight;
			scrollPresenter.Content = imageScrollPresenterContent;

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Try to jump beyond maximum offsets
		await ScrollTo(
			scrollPresenter,
			c_UnnaturalImageWidth - c_defaultUIScrollPresenterWidth + 10.0,
			c_UnnaturalImageHeight - c_defaultUIScrollPresenterHeight + 10.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: true,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: c_UnnaturalImageWidth - c_defaultUIScrollPresenterWidth,
			expectedFinalVerticalOffset: c_UnnaturalImageHeight - c_defaultUIScrollPresenterHeight);
	}

	[TestMethod]
	[TestProperty("Description",
		"Sets ScrollPresenter.ContentOrientation to Vertical and verifies Image positioning for Stretch alignment.")]
	[Ignore("Relies on CompositionAnimation.SetExpressionReferenceParameter which is not yet implemented.")]
	public async Task BasicImageWithConstrainedWidth()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			const double c_imageHeight = 300.0;
			const double c_scrollPresenterWidth = 200.0;
			ScrollPresenter scrollPresenter = null;
			Image imageScrollPresenterContent = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			Compositor compositor = null;

			RunOnUIThread.Execute(() =>
			{
				imageScrollPresenterContent = new Image();
				scrollPresenter = new ScrollPresenter();

				Uri uri = new Uri("ms-appx:/Assets/ingredient3.png");
				Verify.IsNotNull(uri);
				imageScrollPresenterContent.Source = new BitmapImage(uri);
				scrollPresenter.Content = imageScrollPresenterContent;
				scrollPresenter.Background = new Media.SolidColorBrush(Windows.UI.Colors.Chartreuse);

				SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);

				// Constraining the Image width and making the ScrollPresenter smaller than the Image
				imageScrollPresenterContent.Height = c_imageHeight;
				scrollPresenter.ContentOrientation = ScrollingContentOrientation.Vertical;
				scrollPresenter.Width = c_scrollPresenterWidth;
				compositor = Compositor.GetSharedCompositor(); //CompositionTarget.GetCompositorForCurrentThread(); - UNO Specific: GetCompositorForcurrentThread() isn't implemented.
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			await ValidateContentWithConstrainedWidth(
				compositor,
				scrollPresenter,
				content: imageScrollPresenterContent,
				horizontalAlignment: HorizontalAlignment.Stretch,
				leftMargin: 0.0,
				rightMargin: 0.0,
				expectedMinPosition: 0.0f,
				expectedZoomFactor: 1.0f);
		}
	}

	[TestMethod]
	[TestProperty("Description",
		"Sets ScrollPresenter.ContentOrientation to Vertical and verifies Image positioning for various alignments and zoom factors.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ImageWithConstrainedWidth()
	{
		const float c_smallZoomFactor = 0.5f;
		const float c_largeZoomFactor = 2.0f;
		const double c_imageHeight = 300.0;
		const double c_scrollPresenterWidth = 200.0;
		const double c_leftMargin = 20.0;
		const double c_rightMargin = 30.0;
		ScrollPresenter scrollPresenter = null;
		Image imageScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		Compositor compositor = null;

		RunOnUIThread.Execute(() =>
		{
			imageScrollPresenterContent = new Image();
			scrollPresenter = new ScrollPresenter();

			Uri uri = new Uri("ms-appx:/Assets/ingredient3.png");
			Verify.IsNotNull(uri);
			imageScrollPresenterContent.Source = new BitmapImage(uri);
			imageScrollPresenterContent.Margin = new Thickness(c_leftMargin, 0, c_rightMargin, 0);
			scrollPresenter.Content = imageScrollPresenterContent;
			scrollPresenter.Background = new Media.SolidColorBrush(Windows.UI.Colors.Chartreuse);

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);

			// Constraining the Image width and making the ScrollPresenter smaller than the Image
			imageScrollPresenterContent.Height = c_imageHeight;
			scrollPresenter.ContentOrientation = ScrollingContentOrientation.Vertical;
			scrollPresenter.Width = c_scrollPresenterWidth;
			compositor = Compositor.GetSharedCompositor(); //CompositionTarget.GetCompositorForCurrentThread(); - UNO Specific: GetCompositorForcurrentThread() isn't implemented.
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Stretch,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: 1.0f);

		// Jump to absolute small zoomFactor to make the content smaller than the viewport.
		await ZoomTo(scrollPresenter, c_smallZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Stretch,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: (float)(-c_scrollPresenterWidth * c_smallZoomFactor / 2.0), // -50
			expectedZoomFactor: c_smallZoomFactor);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Left,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_smallZoomFactor);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Right,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: (float)(-c_scrollPresenterWidth * c_smallZoomFactor), // -100
			expectedZoomFactor: c_smallZoomFactor);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Center,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: (float)(-c_scrollPresenterWidth * c_smallZoomFactor / 2.0), // -50
			expectedZoomFactor: c_smallZoomFactor);

		// Jump to absolute large zoomFactor to make the content larger than the viewport.
		await ZoomTo(scrollPresenter, c_largeZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Stretch,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Left,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Right,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);

		await ValidateContentWithConstrainedWidth(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			horizontalAlignment: HorizontalAlignment.Center,
			leftMargin: c_leftMargin,
			rightMargin: c_rightMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);
	}

	[TestMethod]
	[TestProperty("Description",
		"Sets ScrollPresenter.ContentOrientation to Horizontal and verifies Image positioning for Stretch alignment.")]
	[Ignore("Relies on CompositionAnimation.SetExpressionReferenceParameter which is not yet implemented.")]
	public async Task BasicImageWithConstrainedHeight()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			const float c_smallZoomFactor = 0.5f;
			const double c_imageWidth = 250.0;
			ScrollPresenter scrollPresenter = null;
			Image imageScrollPresenterContent = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			Compositor compositor = null;

			RunOnUIThread.Execute(() =>
			{
				imageScrollPresenterContent = new Image();
				scrollPresenter = new ScrollPresenter();

				Uri uri = new Uri("ms-appx:/Assets/ingredient3.png");
				Verify.IsNotNull(uri);
				imageScrollPresenterContent.Source = new BitmapImage(uri);
				scrollPresenter.Content = imageScrollPresenterContent;
				scrollPresenter.Background = new Media.SolidColorBrush(Windows.UI.Colors.Chartreuse);

				SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);

				// Constraining the Image height and making the ScrollPresenter smaller than the Image
				imageScrollPresenterContent.Width = c_imageWidth;
				scrollPresenter.ContentOrientation = ScrollingContentOrientation.Horizontal;
				compositor = Compositor.GetSharedCompositor(); //CompositionTarget.GetCompositorForCurrentThread(); - UNO Specific: GetCompositorForcurrentThread() isn't implemented.
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			await ValidateContentWithConstrainedHeight(
				compositor,
				scrollPresenter,
				content: imageScrollPresenterContent,
				verticalAlignment: VerticalAlignment.Stretch,
				topMargin: 0.0,
				bottomMargin: 0.0,
				expectedMinPosition: 0f,
				expectedZoomFactor: 1.0f);

			// Jump to absolute small zoomFactor to make the content smaller than the viewport.
			await ZoomTo(scrollPresenter, c_smallZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			await ValidateContentWithConstrainedHeight(
				compositor,
				scrollPresenter,
				content: imageScrollPresenterContent,
				verticalAlignment: VerticalAlignment.Stretch,
				topMargin: 0.0,
				bottomMargin: 0.0,
				expectedMinPosition: (float)(-c_defaultUIScrollPresenterHeight * c_smallZoomFactor / 2.0), // -50
				expectedZoomFactor: c_smallZoomFactor);
		}
	}

	[TestMethod]
	[TestProperty("Description",
		"Sets ScrollPresenter.ContentOrientation to Horizontal and verifies Image positioning for various alignments and zoom factors.")]
	[Ignore("Relies on CompositionAnimation.SetExpressionReferenceParameter which is not yet implemented.")]
	public async Task ImageWithConstrainedHeight()
	{
		const float c_smallZoomFactor = 0.5f;
		const float c_largeZoomFactor = 2.0f;
		const double c_imageWidth = 250.0;
		const double c_topMargin = 40.0;
		const double c_bottomMargin = 10.0;
		ScrollPresenter scrollPresenter = null;
		Image imageScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
		Compositor compositor = null;

		RunOnUIThread.Execute(() =>
		{
			imageScrollPresenterContent = new Image();
			scrollPresenter = new ScrollPresenter();

			Uri uri = new Uri("ms-appx:/Assets/ingredient3.png");
			Verify.IsNotNull(uri);
			imageScrollPresenterContent.Source = new BitmapImage(uri);
			imageScrollPresenterContent.Margin = new Thickness(0, c_topMargin, 0, c_bottomMargin);
			scrollPresenter.Content = imageScrollPresenterContent;
			scrollPresenter.Background = new Media.SolidColorBrush(Windows.UI.Colors.Chartreuse);

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);

			// Constraining the Image height and making the ScrollPresenter smaller than the Image
			imageScrollPresenterContent.Width = c_imageWidth;
			scrollPresenter.ContentOrientation = ScrollingContentOrientation.Horizontal;
			compositor = Compositor.GetSharedCompositor(); //CompositionTarget.GetCompositorForCurrentThread(); - UNO Specific: GetCompositorForcurrentThread() isn't implemented.
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Stretch,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: 0f,
			expectedZoomFactor: 1.0f);

		// Jump to absolute small zoomFactor to make the content smaller than the viewport.
		await ZoomTo(scrollPresenter, c_smallZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Stretch,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: (float)(-c_defaultUIScrollPresenterHeight * c_smallZoomFactor / 2.0), // -50
			expectedZoomFactor: c_smallZoomFactor);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Top,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_smallZoomFactor);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Bottom,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: (float)(-c_defaultUIScrollPresenterHeight * c_smallZoomFactor), // -100
			expectedZoomFactor: c_smallZoomFactor);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Center,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: (float)(-c_defaultUIScrollPresenterHeight * c_smallZoomFactor / 2.0), // -50
			expectedZoomFactor: c_smallZoomFactor);

		// Jump to absolute large zoomFactor to make the content larger than the viewport.
		await ZoomTo(scrollPresenter, c_largeZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Stretch,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Top,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Bottom,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);

		await ValidateContentWithConstrainedHeight(
			compositor,
			scrollPresenter,
			content: imageScrollPresenterContent,
			verticalAlignment: VerticalAlignment.Center,
			topMargin: c_topMargin,
			bottomMargin: c_bottomMargin,
			expectedMinPosition: 0.0f,
			expectedZoomFactor: c_largeZoomFactor);
	}

	[TestMethod]
	[TestProperty("Description",
		"Sets ScrollPresenter.ContentOrientation to Both and verifies Image positioning for various alignments and zoom factors.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task ImageWithConstrainedSize()
	{
		//using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("ScrollPresenter"))
		{
			const float c_smallZoomFactor = 0.5f;
			const float c_largeZoomFactor = 2.0f;
			const double c_imageWidth = 2400;
			const double c_imageHeight = 1400.0;
			const double c_scrollPresenterWidth = 314.0;
			const double c_scrollPresenterHeight = 210.0;
			const double c_leftMargin = 20.0;
			const double c_rightMargin = 30.0;
			const double c_topMargin = 40.0;
			const double c_bottomMargin = 10.0;
			ScrollPresenter scrollPresenter = null;
			Image imageScrollPresenterContent = null;
			UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);
			Compositor compositor = null;

			RunOnUIThread.Execute(() =>
			{
				imageScrollPresenterContent = new Image();
				scrollPresenter = new ScrollPresenter();

				Uri uri = new Uri("ms-appx:/Assets/LargeWisteria.jpg");
				Verify.IsNotNull(uri);
				imageScrollPresenterContent.Source = new BitmapImage(uri);
				imageScrollPresenterContent.Margin = new Thickness(c_leftMargin, c_topMargin, c_rightMargin, c_bottomMargin);
				scrollPresenter.Content = imageScrollPresenterContent;
				scrollPresenter.Background = new Media.SolidColorBrush(Windows.UI.Colors.Chartreuse);

				SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent: null, scrollPresenterLoadedEvent);

				// Constraining the Image width and height, and making the ScrollPresenter smaller than the Image
				scrollPresenter.ContentOrientation = ScrollingContentOrientation.None;
				scrollPresenter.Width = c_scrollPresenterWidth;
				scrollPresenter.Height = c_scrollPresenterHeight;
				compositor = Compositor.GetSharedCompositor(); //CompositionTarget.GetCompositorForCurrentThread(); - UNO Specific: GetCompositorForcurrentThread() isn't implemented.
			});

			await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

			await ValidateContentWithConstrainedSize(
				compositor,
				scrollPresenter,
				content: imageScrollPresenterContent,
				horizontalAlignment: HorizontalAlignment.Stretch,
				verticalAlignment: VerticalAlignment.Stretch,
				expectedMinPositionX: 0.0f,
				expectedMinPositionY: (float)-(c_scrollPresenterHeight - (c_scrollPresenterWidth - c_leftMargin - c_rightMargin) * c_imageHeight / c_imageWidth - c_topMargin - c_bottomMargin) / 2.0f, //-3
				expectedZoomFactor: 1.0f);

			// Jump to absolute small zoomFactor to make the content smaller than the viewport.
			await ZoomTo(scrollPresenter, c_smallZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);

			await ValidateContentWithConstrainedSize(
				compositor,
				scrollPresenter,
				content: imageScrollPresenterContent,
				horizontalAlignment: HorizontalAlignment.Stretch,
				verticalAlignment: VerticalAlignment.Stretch,
				expectedMinPositionX: (float)-c_scrollPresenterWidth / 4.0f, // -78.5
				expectedMinPositionY: (float)-(c_scrollPresenterHeight - ((c_scrollPresenterWidth - c_leftMargin - c_rightMargin) * c_imageHeight / c_imageWidth + c_topMargin + c_bottomMargin) * c_smallZoomFactor) / 2.0f, //-54
				expectedZoomFactor: c_smallZoomFactor);

			// Jump to absolute large zoomFactor to make the content larger than the viewport.
			await ZoomTo(scrollPresenter, c_largeZoomFactor, 0.0f, 0.0f, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore, hookViewChanged: false);

			await ValidateContentWithConstrainedSize(
				compositor,
				scrollPresenter,
				content: imageScrollPresenterContent,
				horizontalAlignment: HorizontalAlignment.Stretch,
				verticalAlignment: VerticalAlignment.Stretch,
				expectedMinPositionX: 0.0f,
				expectedMinPositionY: 0.0f,
				expectedZoomFactor: c_largeZoomFactor);
		}
	}

	[TestMethod]
	[TestProperty("Description", "Tests ScrollPresenter's view properties and methods with RightToLeft flow direction.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task RightToLeftFlowDirection()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle()
			{
				Margin = new Thickness(25.0)
			};
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Jump to absolute offsets
		await ScrollTo(
			scrollPresenter,
			11.0,
			17.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: true,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: 11.0,
			expectedFinalVerticalOffset: 17.0);

		// Jump to absolute zoom factor
		await ZoomTo(
			scrollPresenter,
			2.0f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false);

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"After view changes: HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
			Verify.AreEqual(22.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(34.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(2.0f, scrollPresenter.ZoomFactor);
		});

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter.FlowDirection = FlowDirection.RightToLeft;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"After FlowDirection change: HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
			Verify.AreEqual(22.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(34.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(2.0f, scrollPresenter.ZoomFactor);
		});

		// Jump to absolute offsets in RightToLeft flow direction
		await ScrollTo(
			scrollPresenter,
			7.0,
			13.0,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false,
			isAnimationsEnabledOverride: null,
			expectedFinalHorizontalOffset: 7.0,
			expectedFinalVerticalOffset: 13.0);

		// Jump to absolute zoom factor in RightToLeft flow direction
		await ZoomTo(
			scrollPresenter,
			4.0f,
			0.0f,
			0.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore,
			hookViewChanged: false);

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"After view changes in RightToLeft flow direction: HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
			Verify.AreEqual(14.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(26.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(4.0f, scrollPresenter.ZoomFactor);
		});
	}

	private async Task ValidateContentWithConstrainedWidth(
		Compositor compositor,
		ScrollPresenter scrollPresenter,
		FrameworkElement content,
		HorizontalAlignment horizontalAlignment,
		double leftMargin,
		double rightMargin,
		float expectedMinPosition,
		float expectedZoomFactor)
	{
		const double c_scrollPresenterWidth = 200.0;
		float horizontalOffset = 0.0f;
		float verticalOffset = 0.0f;
		float zoomFactor = 1.0f;
		float expectedHorizontalOffset = 0.0f;
		float expectedVerticalOffset = 0.0f;

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Covering alignment {horizontalAlignment.ToString()}");
			content.HorizontalAlignment = horizontalAlignment;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Vector2 minPosition = ScrollPresenterTestHooks.GetMinPosition(scrollPresenter);
			Vector2 arrangeRenderSizesDelta = ScrollPresenterTestHooks.GetArrangeRenderSizesDelta(scrollPresenter);
			Log.Comment($"MinPosition {minPosition.ToString()}");
			Log.Comment($"ArrangeRenderSizesDelta {arrangeRenderSizesDelta.ToString()}");
			Log.Comment($"Content.DesiredSize {content.DesiredSize.ToString()}");
			Log.Comment($"Content.RenderSize {content.RenderSize.ToString()}");

			Verify.AreEqual(expectedMinPosition, minPosition.X);
			Verify.AreEqual(horizontalAlignment, content.HorizontalAlignment);
			Verify.AreEqual(c_scrollPresenterWidth, content.DesiredSize.Width); // 200
			Verify.AreEqual(c_scrollPresenterWidth - leftMargin - rightMargin, content.RenderSize.Width);
			double arrangeRenderSizesDeltaX = GetArrangeRenderSizesDelta(
				BiDirectionalAlignmentFromHorizontalAlignment(horizontalAlignment),
				extentSize: c_scrollPresenterWidth,
				renderSize: c_scrollPresenterWidth - leftMargin - rightMargin,
				nearMargin: leftMargin,
				farMargin: rightMargin);
			Verify.AreEqual(leftMargin, arrangeRenderSizesDeltaX);
			Verify.AreEqual(arrangeRenderSizesDeltaX, arrangeRenderSizesDelta.X);
			expectedHorizontalOffset = -minPosition.X + (expectedZoomFactor - 1.0f) * arrangeRenderSizesDelta.X;
			expectedVerticalOffset = -minPosition.Y + (expectedZoomFactor - 1.0f) * arrangeRenderSizesDelta.Y;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"horizontalOffset={horizontalOffset}, verticalOffset={verticalOffset}, zoomFactor={zoomFactor}");
			Log.Comment($"expectedHorizontalOffset={expectedHorizontalOffset}, expectedVerticalOffset={expectedVerticalOffset}, expectedZoomFactor={expectedZoomFactor}");
			Verify.AreEqual(expectedHorizontalOffset, horizontalOffset);
			Verify.AreEqual(expectedVerticalOffset, verticalOffset);
			Verify.AreEqual(expectedZoomFactor, zoomFactor);
		});
	}

	private async Task ValidateContentWithConstrainedHeight(
		Compositor compositor,
		ScrollPresenter scrollPresenter,
		FrameworkElement content,
		VerticalAlignment verticalAlignment,
		double topMargin,
		double bottomMargin,
		float expectedMinPosition,
		float expectedZoomFactor)
	{
		float horizontalOffset = 0.0f;
		float verticalOffset = 0.0f;
		float zoomFactor = 1.0f;
		float expectedHorizontalOffset = 0.0f;
		float expectedVerticalOffset = 0.0f;

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Covering alignment {verticalAlignment.ToString()}");
			content.VerticalAlignment = verticalAlignment;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Vector2 minPosition = ScrollPresenterTestHooks.GetMinPosition(scrollPresenter);
			Vector2 arrangeRenderSizesDelta = ScrollPresenterTestHooks.GetArrangeRenderSizesDelta(scrollPresenter);
			Log.Comment($"MinPosition {minPosition.ToString()}");
			Log.Comment($"ArrangeRenderSizesDelta {arrangeRenderSizesDelta.ToString()}");
			Log.Comment($"Content.DesiredSize {content.DesiredSize.ToString()}");
			Log.Comment($"Content.RenderSize {content.RenderSize.ToString()}");

			Verify.AreEqual(expectedMinPosition, minPosition.Y);
			Verify.AreEqual(verticalAlignment, content.VerticalAlignment);
			Verify.AreEqual(c_defaultUIScrollPresenterHeight, content.DesiredSize.Height);
			Verify.AreEqual(c_defaultUIScrollPresenterHeight - topMargin - bottomMargin, content.RenderSize.Height);
			double arrangeRenderSizesDeltaY = GetArrangeRenderSizesDelta(
				BiDirectionalAlignmentFromVerticalAlignment(verticalAlignment),
				extentSize: c_defaultUIScrollPresenterHeight,
				renderSize: c_defaultUIScrollPresenterHeight - topMargin - bottomMargin,
				nearMargin: topMargin,
				farMargin: bottomMargin);
			Verify.AreEqual(topMargin, arrangeRenderSizesDeltaY);
			Verify.AreEqual(arrangeRenderSizesDeltaY, arrangeRenderSizesDelta.Y);
			expectedVerticalOffset = -minPosition.Y + (expectedZoomFactor - 1.0f) * arrangeRenderSizesDelta.Y;
			expectedHorizontalOffset = -minPosition.X + (expectedZoomFactor - 1.0f) * arrangeRenderSizesDelta.X;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"horizontalOffset={horizontalOffset}, verticalOffset={verticalOffset}, zoomFactor={zoomFactor}");
			Log.Comment($"expectedHorizontalOffset={expectedHorizontalOffset}, expectedVerticalOffset={expectedVerticalOffset}, expectedZoomFactor={expectedZoomFactor}");
			Verify.AreEqual(expectedHorizontalOffset, horizontalOffset);
			Verify.AreEqual(expectedVerticalOffset, verticalOffset);
			Verify.AreEqual(expectedZoomFactor, zoomFactor);
		});
	}

	private async Task ValidateContentWithConstrainedSize(
		Compositor compositor,
		ScrollPresenter scrollPresenter,
		FrameworkElement content,
		HorizontalAlignment horizontalAlignment,
		VerticalAlignment verticalAlignment,
		float expectedMinPositionX,
		float expectedMinPositionY,
		float expectedZoomFactor)
	{
		const double c_leftMargin = 20.0;
		const double c_rightMargin = 30.0;
		const double c_topMargin = 40.0;
		const double c_bottomMargin = 10.0;
		const double c_scrollPresenterWidth = 314.0;
		const double c_scrollPresenterHeight = 210.0;
		const double c_imageWidth = 2400;
		const double c_imageHeight = 1400.0;

		float horizontalOffset = 0.0f;
		float verticalOffset = 0.0f;
		float zoomFactor = 1.0f;
		double horizontalArrangeRenderSizesDelta = 0.0;
		double verticalArrangeRenderSizesDelta = 0.0;
		double expectedHorizontalOffset = 0.0;
		double expectedVerticalOffset = 0.0;

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Covering alignments {horizontalAlignment.ToString()} and {verticalAlignment.ToString()}");
			content.HorizontalAlignment = horizontalAlignment;
			content.VerticalAlignment = verticalAlignment;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(horizontalAlignment, content.HorizontalAlignment);
			Verify.AreEqual(verticalAlignment, content.VerticalAlignment);

			Vector2 minPosition = ScrollPresenterTestHooks.GetMinPosition(scrollPresenter);
			Vector2 arrangeRenderSizesDelta = ScrollPresenterTestHooks.GetArrangeRenderSizesDelta(scrollPresenter);
			Log.Comment($"MinPosition {minPosition.ToString()}");
			Log.Comment($"ArrangeRenderSizesDelta {arrangeRenderSizesDelta.ToString()}");
			Log.Comment($"Content.DesiredSize {content.DesiredSize.ToString()}");
			Log.Comment($"Content.RenderSize {content.RenderSize.ToString()}");

			Verify.AreEqual(expectedMinPositionX, minPosition.X);
			Verify.AreEqual(expectedMinPositionY, minPosition.Y);
			Verify.AreEqual(c_scrollPresenterWidth, content.DesiredSize.Width); // 314
			Verify.AreEqual((c_scrollPresenterWidth - c_leftMargin - c_rightMargin) * c_imageHeight / c_imageWidth + c_topMargin + c_bottomMargin, content.DesiredSize.Height); // 204
			Verify.AreEqual(c_scrollPresenterWidth - c_leftMargin - c_rightMargin, content.RenderSize.Width); // 264
			Verify.AreEqual((c_scrollPresenterWidth - c_leftMargin - c_rightMargin) * c_imageHeight / c_imageWidth, content.RenderSize.Height); // 154

			horizontalArrangeRenderSizesDelta = GetArrangeRenderSizesDelta(
				BiDirectionalAlignmentFromHorizontalAlignment(horizontalAlignment),
				extentSize: c_scrollPresenterWidth,
				renderSize: c_scrollPresenterWidth - c_leftMargin - c_rightMargin,
				nearMargin: c_leftMargin,
				farMargin: c_rightMargin);
			Log.Comment($"horizontalArrangeRenderSizesDelta {horizontalArrangeRenderSizesDelta}");
			Verify.AreEqual(c_leftMargin, horizontalArrangeRenderSizesDelta);
			Verify.AreEqual(arrangeRenderSizesDelta.X, horizontalArrangeRenderSizesDelta);
			expectedHorizontalOffset = -minPosition.X + (expectedZoomFactor - 1.0f) * horizontalArrangeRenderSizesDelta;

			verticalArrangeRenderSizesDelta = GetArrangeRenderSizesDelta(
				BiDirectionalAlignmentFromVerticalAlignment(verticalAlignment),
				extentSize: c_scrollPresenterHeight,
				renderSize: c_scrollPresenterHeight - c_topMargin - c_bottomMargin,
				nearMargin: c_topMargin,
				farMargin: c_bottomMargin);
			Log.Comment($"verticalArrangeRenderSizesDelta {verticalArrangeRenderSizesDelta}");
			Verify.AreEqual(c_topMargin, verticalArrangeRenderSizesDelta);
			Verify.AreEqual(arrangeRenderSizesDelta.Y, verticalArrangeRenderSizesDelta);
			expectedVerticalOffset = -minPosition.Y + (expectedZoomFactor - 1.0f) * verticalArrangeRenderSizesDelta;
		});

		(horizontalOffset, verticalOffset, zoomFactor) = await SpyTranslationAndScale(scrollPresenter, compositor);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"horizontalOffset={horizontalOffset}, verticalOffset={verticalOffset}, zoomFactor={zoomFactor}");
			Log.Comment($"expectedHorizontalOffset={expectedHorizontalOffset}, expectedVerticalOffset={expectedVerticalOffset}, expectedZoomFactor={expectedZoomFactor}");
			Verify.AreEqual(expectedHorizontalOffset, horizontalOffset);
			Verify.AreEqual(expectedVerticalOffset, verticalOffset);
			Verify.AreEqual(expectedZoomFactor, zoomFactor);
		});
	}

	private double GetArrangeRenderSizesDelta(
		BiDirectionalAlignment alignment,
		double extentSize,
		double renderSize,
		double nearMargin,
		double farMargin)
	{
		double delta = extentSize - renderSize;

		if (alignment == BiDirectionalAlignment.Near)
		{
			delta = 0.0;
		}
		else
		{
			delta -= nearMargin + farMargin;
		}

		if (alignment == BiDirectionalAlignment.Center ||
			alignment == BiDirectionalAlignment.Stretch)
		{
			delta /= 2.0;
		}

		delta += nearMargin;

		Log.Comment($"GetArrangeRenderSizesDelta returns {delta}.");
		return delta;
	}

	private BiDirectionalAlignment BiDirectionalAlignmentFromHorizontalAlignment(HorizontalAlignment horizontalAlignment)
	{
		switch (horizontalAlignment)
		{
			case HorizontalAlignment.Stretch:
				return BiDirectionalAlignment.Stretch;
			case HorizontalAlignment.Left:
				return BiDirectionalAlignment.Near;
			case HorizontalAlignment.Right:
				return BiDirectionalAlignment.Far;
			default:
				return BiDirectionalAlignment.Center;
		}
	}

	private BiDirectionalAlignment BiDirectionalAlignmentFromVerticalAlignment(VerticalAlignment verticalAlignment)
	{
		switch (verticalAlignment)
		{
			case VerticalAlignment.Stretch:
				return BiDirectionalAlignment.Stretch;
			case VerticalAlignment.Top:
				return BiDirectionalAlignment.Near;
			case VerticalAlignment.Bottom:
				return BiDirectionalAlignment.Far;
			default:
				return BiDirectionalAlignment.Center;
		}
	}
}
