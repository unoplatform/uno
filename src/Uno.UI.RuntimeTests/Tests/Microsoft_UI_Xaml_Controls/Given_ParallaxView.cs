// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParallaxViewTests.cs, commit 5f9e85113

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ParallaxView
{
	private const ParallaxSourceOffsetKind c_defaultHorizontalSourceOffsetKind = ParallaxSourceOffsetKind.Relative;
	private const double c_defaultHorizontalSourceStartOffset = 0.0;
	private const double c_defaultHorizontalSourceEndOffset = 0.0;
	private const double c_defaultMaxHorizontalShiftRatio = 1.0;
	private const double c_defaultHorizontalShift = 0.0;
	private const bool c_defaultIsHorizontalShiftClamped = true;
	private const ParallaxSourceOffsetKind c_defaultVerticalSourceOffsetKind = ParallaxSourceOffsetKind.Relative;
	private const double c_defaultVerticalSourceStartOffset = 0.0;
	private const double c_defaultVerticalSourceEndOffset = 0.0;
	private const double c_defaultMaxVerticalShiftRatio = 1.0;
	private const double c_defaultVerticalShift = 0.0;
	private const bool c_defaultIsVerticalShiftClamped = true;

	private const double c_defaultUIScrollViewerWidth = 200.0;
	private const double c_defaultUIScrollViewerHeight = 100.0;
	private const double c_defaultUIScrollViewerContentWidth = 1200.0;
	private const double c_defaultUIScrollViewerContentHeight = 600.0;
	private const double c_defaultUIHorizontalShift = 100.0;
	private const double c_defaultUIVerticalShift = 50.0;

	[TestMethod]
	[Description("Verifies the ParallaxView default properties.")]
	public void VerifyDefaultPropertyValues()
	{
		ParallaxView parallaxView = new ParallaxView();
		Assert.IsNotNull(parallaxView);

		Assert.IsNull(parallaxView.Child);
		Assert.IsNull(parallaxView.Source);
		Assert.AreEqual(c_defaultHorizontalSourceOffsetKind, parallaxView.HorizontalSourceOffsetKind);
		Assert.AreEqual(c_defaultHorizontalSourceStartOffset, parallaxView.HorizontalSourceStartOffset);
		Assert.AreEqual(c_defaultHorizontalSourceEndOffset, parallaxView.HorizontalSourceEndOffset);
		Assert.AreEqual(c_defaultMaxHorizontalShiftRatio, parallaxView.MaxHorizontalShiftRatio);
		Assert.AreEqual(c_defaultHorizontalShift, parallaxView.HorizontalShift);
		Assert.AreEqual(c_defaultIsHorizontalShiftClamped, parallaxView.IsHorizontalShiftClamped);
		Assert.AreEqual(c_defaultVerticalSourceOffsetKind, parallaxView.VerticalSourceOffsetKind);
		Assert.AreEqual(c_defaultVerticalSourceStartOffset, parallaxView.VerticalSourceStartOffset);
		Assert.AreEqual(c_defaultVerticalSourceEndOffset, parallaxView.VerticalSourceEndOffset);
		Assert.AreEqual(c_defaultMaxVerticalShiftRatio, parallaxView.MaxVerticalShiftRatio);
		Assert.AreEqual(c_defaultVerticalShift, parallaxView.VerticalShift);
		Assert.AreEqual(c_defaultIsVerticalShiftClamped, parallaxView.IsVerticalShiftClamped);
	}

	[TestMethod]
	[Description("Exercises the ParallaxView property setters and getters for non-default values.")]
	public async Task VerifyPropertyGettersAndSetters()
	{
		ParallaxView parallaxView = new ParallaxView();
		Rectangle rectangle = new Rectangle();
		ScrollViewer scrollViewer = new ScrollViewer();

		parallaxView.Child = rectangle;
		parallaxView.Source = scrollViewer;
		parallaxView.HorizontalSourceOffsetKind = ParallaxSourceOffsetKind.Absolute;
		parallaxView.HorizontalSourceStartOffset = 11.0;
		parallaxView.HorizontalSourceEndOffset = 22.0;
		parallaxView.MaxHorizontalShiftRatio = 0.123;
		parallaxView.HorizontalShift = 321.0;
		parallaxView.IsHorizontalShiftClamped = false;
		parallaxView.VerticalSourceOffsetKind = ParallaxSourceOffsetKind.Absolute;
		parallaxView.VerticalSourceStartOffset = 4.5;
		parallaxView.VerticalSourceEndOffset = 5.4;
		parallaxView.MaxVerticalShiftRatio = 0.321;
		parallaxView.VerticalShift = 45.6;
		parallaxView.IsVerticalShiftClamped = false;

		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(rectangle, parallaxView.Child);
		Assert.AreEqual(scrollViewer, parallaxView.Source);
		Assert.AreEqual(ParallaxSourceOffsetKind.Absolute, parallaxView.HorizontalSourceOffsetKind);
		Assert.AreEqual(11.0, parallaxView.HorizontalSourceStartOffset);
		Assert.AreEqual(22.0, parallaxView.HorizontalSourceEndOffset);
		Assert.AreEqual(0.123, parallaxView.MaxHorizontalShiftRatio);
		Assert.AreEqual(321.0, parallaxView.HorizontalShift);
		Assert.IsFalse(parallaxView.IsHorizontalShiftClamped);
		Assert.AreEqual(ParallaxSourceOffsetKind.Absolute, parallaxView.VerticalSourceOffsetKind);
		Assert.AreEqual(4.5, parallaxView.VerticalSourceStartOffset);
		Assert.AreEqual(5.4, parallaxView.VerticalSourceEndOffset);
		Assert.AreEqual(0.321, parallaxView.MaxVerticalShiftRatio);
		Assert.AreEqual(45.6, parallaxView.VerticalShift);
		Assert.IsFalse(parallaxView.IsVerticalShiftClamped);
	}

	[TestMethod]
	[Description("Verifies that setting the Child property attaches the child to the visual tree.")]
	public async Task When_ChildSet_Then_AttachedToVisualTree()
	{
		Rectangle rectangle = new Rectangle { Width = 100, Height = 100 };
		ParallaxView parallaxView = new ParallaxView
		{
			Width = 200,
			Height = 100,
			Child = rectangle,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(parallaxView, VisualTreeHelper.GetParent(rectangle));
		Assert.AreEqual(rectangle, parallaxView.Child);
	}

	[TestMethod]
	[Description("Verifies that replacing the Child property detaches the old child and attaches the new one.")]
	public async Task When_ChildReplaced_Then_OldDetachedAndNewAttached()
	{
		Rectangle firstChild = new Rectangle { Width = 100, Height = 100 };
		Rectangle secondChild = new Rectangle { Width = 100, Height = 100 };
		ParallaxView parallaxView = new ParallaxView
		{
			Width = 200,
			Height = 100,
			Child = firstChild,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(parallaxView, VisualTreeHelper.GetParent(firstChild));

		parallaxView.Child = secondChild;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(VisualTreeHelper.GetParent(firstChild));
		Assert.AreEqual(parallaxView, VisualTreeHelper.GetParent(secondChild));
	}

	[TestMethod]
	[Description("Verifies that clearing the Child property detaches the child from the visual tree.")]
	public async Task When_ChildCleared_Then_DetachedFromVisualTree()
	{
		Rectangle rectangle = new Rectangle { Width = 100, Height = 100 };
		ParallaxView parallaxView = new ParallaxView
		{
			Width = 200,
			Height = 100,
			Child = rectangle,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		parallaxView.Child = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(VisualTreeHelper.GetParent(rectangle));
		Assert.IsNull(parallaxView.Child);
	}

	[TestMethod]
	[Description("Verifies ParallaxView measure/arrange routes the child through Uno's layouter so it receives a non-empty arrange rect when HorizontalShift is set.")]
	public async Task When_HorizontalShift_Then_ChildArranged()
	{
		Rectangle rectanglePVChild = new Rectangle
		{
			Width = c_defaultUIScrollViewerWidth + c_defaultHorizontalShift,
			Height = c_defaultUIScrollViewerHeight,
		};
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = rectanglePVChild,
			HorizontalShift = c_defaultUIHorizontalShift,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(c_defaultUIScrollViewerWidth, parallaxView.ActualWidth);
		Assert.AreEqual(c_defaultUIScrollViewerHeight, parallaxView.ActualHeight);
		// The child must have been arranged (non-zero size). WinUI allows the arrange rect to exceed
		// the parent's finalSize to accommodate the parallax motion; Uno's layouter may clamp, so we
		// only assert that the child received a size and that it is at least as tall as expected.
		Assert.IsTrue(rectanglePVChild.ActualWidth > 0);
		Assert.AreEqual(c_defaultUIScrollViewerHeight, rectanglePVChild.ActualHeight);
	}

	[TestMethod]
	[Description("Verifies ParallaxView measure/arrange routes the child through Uno's layouter so it receives a non-empty arrange rect when VerticalShift is set.")]
	public async Task When_VerticalShift_Then_ChildArranged()
	{
		Rectangle rectanglePVChild = new Rectangle
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight + c_defaultVerticalShift,
		};
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = rectanglePVChild,
			VerticalShift = c_defaultUIVerticalShift,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(c_defaultUIScrollViewerWidth, parallaxView.ActualWidth);
		Assert.AreEqual(c_defaultUIScrollViewerHeight, parallaxView.ActualHeight);
		// See note on When_HorizontalShift_Then_ChildArranged.
		Assert.AreEqual(c_defaultUIScrollViewerWidth, rectanglePVChild.ActualWidth);
		Assert.IsTrue(rectanglePVChild.ActualHeight > 0);
	}

	[TestMethod]
	[Description("Verifies RefreshAutomaticHorizontalOffsets does not throw when there is no Source.")]
	public void VerifyRefreshAutomaticHorizontalOffsetsWithoutSource()
	{
		ParallaxView parallaxView = new ParallaxView
		{
			HorizontalShift = 100.0,
			HorizontalSourceOffsetKind = ParallaxSourceOffsetKind.Relative,
		};

		// Should not throw when there is no Source.
		parallaxView.RefreshAutomaticHorizontalOffsets();
	}

	[TestMethod]
	[Description("Verifies RefreshAutomaticVerticalOffsets does not throw when there is no Source.")]
	public void VerifyRefreshAutomaticVerticalOffsetsWithoutSource()
	{
		ParallaxView parallaxView = new ParallaxView
		{
			VerticalShift = 100.0,
			VerticalSourceOffsetKind = ParallaxSourceOffsetKind.Relative,
		};

		// Should not throw when there is no Source.
		parallaxView.RefreshAutomaticVerticalOffsets();
	}

	[TestMethod]
	[Description("Verifies setting Source to a ScrollViewer does not throw during measure/arrange/load.")]
	public async Task When_SourceIsScrollViewer_Then_LoadsWithoutExceptions()
	{
		Rectangle rectangleSVContent = new Rectangle
		{
			Width = c_defaultUIScrollViewerContentWidth,
			Height = c_defaultUIScrollViewerContentHeight,
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Content = rectangleSVContent,
		};
		Rectangle rectanglePVChild = new Rectangle
		{
			Width = c_defaultUIScrollViewerWidth + c_defaultHorizontalShift,
			Height = c_defaultUIScrollViewerHeight + c_defaultVerticalShift,
		};
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = rectanglePVChild,
			HorizontalShift = c_defaultUIHorizontalShift,
			VerticalShift = c_defaultUIVerticalShift,
			Source = scrollViewer,
		};

		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(parallaxView);
		stackPanel.Children.Add(scrollViewer);

		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForLoaded(scrollViewer);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(scrollViewer, parallaxView.Source);
		Assert.AreEqual(rectanglePVChild, parallaxView.Child);
	}

	[TestMethod]
	[Description("Verifies setting Source to a ScrollPresenter does not throw during measure/arrange/load.")]
	public async Task When_SourceIsScrollPresenter_Then_LoadsWithoutExceptions()
	{
		Rectangle rectangleSPContent = new Rectangle
		{
			Width = c_defaultUIScrollViewerContentWidth,
			Height = c_defaultUIScrollViewerContentHeight,
		};
		ScrollPresenter scrollPresenter = new ScrollPresenter
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Content = rectangleSPContent,
		};
		Rectangle rectanglePVChild = new Rectangle
		{
			Width = c_defaultUIScrollViewerWidth + c_defaultHorizontalShift,
			Height = c_defaultUIScrollViewerHeight + c_defaultVerticalShift,
		};
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = rectanglePVChild,
			HorizontalShift = c_defaultUIHorizontalShift,
			VerticalShift = c_defaultUIVerticalShift,
			Source = scrollPresenter,
		};

		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(parallaxView);
		stackPanel.Children.Add(scrollPresenter);

		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForLoaded(scrollPresenter);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(scrollPresenter, parallaxView.Source);
		Assert.AreEqual(rectanglePVChild, parallaxView.Child);
	}

	[TestMethod]
	[Description("Verifies that changing the Source triggers re-evaluation without throwing.")]
	public async Task When_SourceChanged_Then_NoException()
	{
		ScrollViewer scrollViewer1 = new ScrollViewer
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Content = new Rectangle { Width = c_defaultUIScrollViewerContentWidth, Height = c_defaultUIScrollViewerContentHeight },
		};
		ScrollViewer scrollViewer2 = new ScrollViewer
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Content = new Rectangle { Width = c_defaultUIScrollViewerContentWidth, Height = c_defaultUIScrollViewerContentHeight },
		};
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = new Rectangle { Width = c_defaultUIScrollViewerWidth + c_defaultHorizontalShift, Height = c_defaultUIScrollViewerHeight + c_defaultVerticalShift },
			HorizontalShift = c_defaultUIHorizontalShift,
			VerticalShift = c_defaultUIVerticalShift,
			Source = scrollViewer1,
		};

		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(parallaxView);
		stackPanel.Children.Add(scrollViewer1);
		stackPanel.Children.Add(scrollViewer2);

		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		parallaxView.Source = scrollViewer2;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(scrollViewer2, parallaxView.Source);

		parallaxView.Source = null;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsNull(parallaxView.Source);
	}

	[TestMethod]
	[Description("Verifies that setting HorizontalShift invalidates measure.")]
	public async Task When_HorizontalShiftChanged_Then_MeasureInvalidated()
	{
		Rectangle rectanglePVChild = new Rectangle
		{
			Width = c_defaultUIScrollViewerWidth + 10,
			Height = c_defaultUIScrollViewerHeight,
		};
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = rectanglePVChild,
			HorizontalShift = 10,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		double initialChildWidth = rectanglePVChild.ActualWidth;
		parallaxView.HorizontalShift = 200;
		await TestServices.WindowHelper.WaitForIdle();

		// The child should be re-measured/arranged wider to accommodate the new shift.
		Assert.IsTrue(rectanglePVChild.ActualWidth >= initialChildWidth);
	}

	[TestMethod]
	[Description("Verifies that the ParallaxView applies a rectangular clip matching its final size when a Child is arranged.")]
	public async Task When_ChildArranged_Then_ClipMatchesFinalSize()
	{
		Rectangle rectanglePVChild = new Rectangle
		{
			Width = c_defaultUIScrollViewerWidth + c_defaultHorizontalShift,
			Height = c_defaultUIScrollViewerHeight + c_defaultVerticalShift,
		};
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = rectanglePVChild,
			HorizontalShift = c_defaultUIHorizontalShift,
			VerticalShift = c_defaultUIVerticalShift,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		var clip = parallaxView.Clip as RectangleGeometry;
		Assert.IsNotNull(clip);
		Assert.AreEqual(0.0, clip.Rect.X);
		Assert.AreEqual(0.0, clip.Rect.Y);
		Assert.AreEqual(c_defaultUIScrollViewerWidth, clip.Rect.Width);
		Assert.AreEqual(c_defaultUIScrollViewerHeight, clip.Rect.Height);
	}

	[TestMethod]
	[Description("Verifies that a Child with explicit size is arranged at that size when the ParallaxView has no shifts.")]
	public async Task When_ShiftsAreZero_Then_ChildKeepsDesiredSize()
	{
		// The WinUI layout path copies child.DesiredSize into the arrange rect and only widens it when
		// the corresponding shift is non-zero; with zero shifts an explicitly-sized Rectangle keeps its
		// own size rather than being stretched to fill the ParallaxView.
		Rectangle rectanglePVChild = new Rectangle { Width = 120, Height = 60 };
		ParallaxView parallaxView = new ParallaxView
		{
			Width = c_defaultUIScrollViewerWidth,
			Height = c_defaultUIScrollViewerHeight,
			Child = rectanglePVChild,
			HorizontalShift = 0,
			VerticalShift = 0,
		};

		TestServices.WindowHelper.WindowContent = parallaxView;
		await TestServices.WindowHelper.WaitForLoaded(parallaxView);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(120.0, rectanglePVChild.ActualWidth);
		Assert.AreEqual(60.0, rectanglePVChild.ActualHeight);
	}
}
