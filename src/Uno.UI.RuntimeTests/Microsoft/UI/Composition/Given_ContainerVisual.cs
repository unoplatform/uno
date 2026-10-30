#if __SKIA__
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SamplesApp.UITests;
using SkiaSharp;
using Uno.Disposables;
using Uno.Extensions.Specialized;
using Uno.UI.RuntimeTests.Helpers;
using Uno.WinUI.Graphics2DSK;
using Uno.UI.DevTools.Input;
using CollectionExtensions = Uno.Extensions.CollectionExtensions;
using RectExtensions = Uno.Extensions.RectExtensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_ContainerVisual
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Children_Change()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var containerVisual = compositor.CreateContainerVisual();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);

		var shape = compositor.CreateShapeVisual();
		containerVisual.Children.InsertAtTop(shape);
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		var children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.HasCount(1, children);

		containerVisual.Children.InsertAtTop(compositor.CreateShapeVisual());
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.HasCount(2, children);

		containerVisual.Children.Remove(shape);
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.HasCount(1, children);
	}

	[TestMethod]
	public async Task When_Child_Removed_TotalMatrix_Updated()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var parent1 = compositor.CreateContainerVisual();
		var parent2 = compositor.CreateContainerVisual();
		var child = compositor.CreateSpriteVisual();

		parent2.Offset = new Vector3(100, 0, 0);

		parent1.Children.InsertAtTop(child);
		Assert.IsTrue(child.TotalMatrix.IsIdentity);

		parent2.Children.InsertAtTop(child);
		Assert.IsFalse(child.TotalMatrix.IsIdentity);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaAndroid)] // passed locally, times out in CI waiting for UITestHelper.WaitFor
	public async Task When_SKPicture_Collapsing_Optimization()
	{
		var oldValues =
			(FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold);
		using var _ = Disposable.Create(() =>
		{
			(FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold) = oldValues;
		});
		(FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization,
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold,
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold) = (true, 50, 100);
		var popup = new Popup()
		{
			Child = new StackPanel().Apply(sp => Enumerable.Range(0, 100).ForEach(i =>
				sp.Children.Add(new Rectangle
				{
					Fill = new SolidColorBrush(Colors.Red),
					Width = 100,
					Height = 100
				})))
		};

		var skce = new FrameCounterSKCanvasElement() { Width = 100, Height = 100 };
		popup.PlacementTarget = skce;
		popup.DesiredPlacement = PopupPlacementMode.Bottom;
		await UITestHelper.Load(new StackPanel { Children = { skce, popup } });

		for (int i = 0; i < 200; i++)
		{
			popup.IsOpen = !popup.IsOpen;
			await UITestHelper.WaitForIdle();
			var count = skce.FrameCounter;
			// A frame per toggle, which on a software-rasterizing agent can take longer than the default second.
			await UITestHelper.WaitFor(() => skce.FrameCounter > count, timeoutMS: 5000);
		}

		popup.IsOpen = true;
		await UITestHelper.WaitForIdle();
		var p = (popup.Child as FrameworkElement).GetAbsoluteBoundsRect().GetLocation().Offset(50, 50);

		var screenShot1 = await UITestHelper.ScreenShot(((FrameworkElement)TestServices.WindowHelper.XamlRoot.VisualTree.RootElement)!);
		ImageAssert.HasColorAt(screenShot1, p, Colors.Red);

		popup.IsOpen = false;
		await UITestHelper.WaitForIdle();

		var screenShot2 = await UITestHelper.ScreenShot(((FrameworkElement)TestServices.WindowHelper.XamlRoot.VisualTree.RootElement)!);
		ImageAssert.DoesNotHaveColorAt(screenShot2, p, Colors.Red);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22416")]
	public void GetArrangeClipPathInElementCoordinateSpace_NullParentAncestorClip_MapsToElementLocalSpace()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var visual = compositor.CreateContainerVisual();
		visual.Offset = new Vector3(30, 20, 0);

		// An ancestor (Panel) layout clip is stored in the parent's coordinate space.
		// For an element at offset (30, 20), a local clip of (0, 0, 50, 40) is stored as (30, 20, 50, 40).
		visual.LayoutClip = (new Rect(30, 20, 50, 40), true);

		// No parent: mimics the composition root (RootVisual is a Panel) or a visual rendered standalone
		// via RenderRootVisual. This used to throw a NullReferenceException (issue #22416).
		Assert.IsNull(visual.Parent);

		// Rect? overload.
		var rect = visual.GetArrangeClipPathInElementCoordinateSpace();
		Assert.IsNotNull(rect);
		Assert.AreEqual(0, rect.Value.X, 0.01);
		Assert.AreEqual(0, rect.Value.Y, 0.01);
		Assert.AreEqual(50, rect.Value.Width, 0.01);
		Assert.AreEqual(40, rect.Value.Height, 0.01);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22416")]
	public void RenderRootVisual_NullParentAncestorClip_DoesNotThrow()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var visual = compositor.CreateContainerVisual();
		visual.Size = new Vector2(100, 100);
		visual.Offset = new Vector3(30, 20, 0);
		visual.LayoutClip = (new Rect(30, 20, 50, 40), true);

		Assert.IsNull(visual.Parent);

		using var surface = SKSurface.Create(new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul));

		// Rendering a parent-less Panel-like visual carrying an ancestor layout clip used to throw a
		// NullReferenceException inside GetArrangeClipPathInElementCoordinateSpace (issue #22416).
		visual.RenderRootVisual(new global::Uno.UI.Composition.Drawing.SkiaDrawingSession(surface.Canvas, new global::Uno.UI.Composition.Drawing.SkiaDrawingFactory()), Vector2.Zero);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Retained_Fallback_Renders_Identically()
	{
		// The backend's native retained path (SkiaSharp's SKPicture) and the command-list fallback a backend
		// without native retention gets must produce identical output. Render the same scene each way and compare.
		// The scene exercises per-visual paint, child rendering, corner clipping, and the non-analytic shadow
		// fallback (gradient content + ThemeShadow) — all of which record and replay through the retained seam.
		var retained = BuildRenderComparisonScene();
		await UITestHelper.Load(retained);
		var expected = await UITestHelper.ScreenShot(retained);

		try
		{
			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = true;
			var fallback = BuildRenderComparisonScene();
			await UITestHelper.Load(fallback);
			var actual = await UITestHelper.ScreenShot(fallback);

			// The retained and fallback renders take different GPU paths, which may round a pixel by one level.
			await ImageAssert.AreEqualAsync(actual, expected, tolerance: 1);
		}
		finally
		{
			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = false;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PictureCollapsing_Produces_Identical_Output()
	{
		// Force the picture-collapsing optimization (record a stable subtree into one SKPicture and replay it)
		// by dropping its frame/visual-count thresholds to zero, and verify the collapsed output still matches
		// the immediate (uncollapsed) render.
		var origFrame = global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold;
		var origCount = global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold;
		try
		{
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold = 0;
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold = 0;

			var collapsed = BuildRenderComparisonScene();
			await UITestHelper.Load(collapsed);
			// First render collapses + caches _childrenContent; the screenshot render then replays the cache.
			await TestServices.WindowHelper.WaitForIdle();
			var expected = await UITestHelper.ScreenShot(collapsed);

			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = true;
			var immediate = BuildRenderComparisonScene();
			await UITestHelper.Load(immediate);
			var actual = await UITestHelper.ScreenShot(immediate);

			// The collapsed and immediate renders take different GPU paths, which may round a pixel by one level.
			await ImageAssert.AreEqualAsync(actual, expected, tolerance: 1);
		}
		finally
		{
			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = false;
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold = origFrame;
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold = origCount;
		}
	}

	// Flat content renders the same under a transform that flattens Z (M33 = 0, as Lottie's Scale(x, y, 0) gives) as
	// under the same transform with M33 = 1. Each test compares the two pixel for pixel.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public async Task When_Children_Under_Flattening_Transform(bool collapse, bool commandList)
	{
		using var _ = UITestHelper.ResetWindowContent();
		var forceFallbackRetainedRendering = global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering;
		try
		{
			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = commandList;
			var flattened = CreateScaledSquares(flattensZ: true, collapse);
			var expected = CreateScaledSquares(flattensZ: false, collapse);
			await UITestHelper.Load(new StackPanel { Children = { flattened.Host, expected.Host } });

			await AssertRendersLike(flattened.Host, expected.Host, Colors.Red, Colors.Blue);

			// Repainting the offset square re-records the children, and the identity square's matrix is no longer dirty
			// this time, so it renders with the matrix the recording starts with.
			flattened.OffsetSquare.Brush = flattened.OffsetSquare.Compositor.CreateColorBrush(Colors.Green);
			expected.OffsetSquare.Brush = expected.OffsetSquare.Compositor.CreateColorBrush(Colors.Green);
			await AssertRendersLike(flattened.Host, expected.Host, Colors.Green, Colors.Blue);

			// Moving the container replays that recording under the new matrix.
			flattened.Container.Offset = expected.Container.Offset = new Vector3(4, 4, 0);
			await AssertRendersLike(flattened.Host, expected.Host, Colors.Green, Colors.Blue);
		}
		finally
		{
			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = forceFallbackRetainedRendering;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public async Task When_Redirect_Source_Under_Flattening_Transform()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var flattened = CreateScaledSquares(flattensZ: true, collapse: false);
		var expected = CreateScaledSquares(flattensZ: false, collapse: false);
		var flattenedRedirect = CreateRedirectHost(flattened.OffsetSquare);
		var expectedRedirect = CreateRedirectHost(expected.OffsetSquare);
		await UITestHelper.Load(new StackPanel { Children = { flattened.Host, expected.Host, flattenedRedirect, expectedRedirect } });

		await AssertRendersLike(flattenedRedirect, expectedRedirect, Colors.Red);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public async Task When_Shadow_Fallback_Under_Flattening_Transform()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var flattened = CreateShadowedCard(flattensZ: true);
		var expected = CreateShadowedCard(flattensZ: false);
		await UITestHelper.Load(new StackPanel { Children = { flattened, expected } });

		// Only the card's content is compared: Skia draws the drop shadow filter itself differently under M33 = 0.
		var expectedScreenshot = await UITestHelper.ScreenShot(expected);
		AssertHasColor(expectedScreenshot, Colors.LimeGreen);
		Assert.AreEqual(
			ImageAssert.GetColorBounds(expectedScreenshot, Colors.LimeGreen, tolerance: 10),
			ImageAssert.GetColorBounds(await UITestHelper.ScreenShot(flattened), Colors.LimeGreen, tolerance: 10));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public async Task When_Panel_Layout_Clip_Under_Flattening_Transform()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var flattened = CreateClippedPanel(flattensZ: true);
		var expected = CreateClippedPanel(flattensZ: false);
		await UITestHelper.Load(new StackPanel { Children = { flattened, expected } });

		await AssertRendersLike(flattened, expected, Colors.Red);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public void When_Ancestor_Clip_Under_Flattening_Transform_Then_Maps_To_Element_Space()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var parent = compositor.CreateContainerVisual();
		parent.TransformMatrix = new Matrix4x4(
			2, 0, 0, 0,
			0, 2, 0, 0,
			0, 0, 0, 0,
			0, 0, 0, 1);
		var visual = compositor.CreateContainerVisual();
		visual.Offset = new Vector3(30, 20, 0);
		visual.LayoutClip = (new Rect(30, 20, 50, 40), true);
		parent.Children.InsertAtTop(visual);

		var rect = visual.GetArrangeClipPathInElementCoordinateSpace();

		Assert.IsNotNull(rect);
		Assert.AreEqual(0, rect.Value.X, 0.01);
		Assert.AreEqual(0, rect.Value.Y, 0.01);
		Assert.AreEqual(50, rect.Value.Width, 0.01);
		Assert.AreEqual(40, rect.Value.Height, 0.01);
	}

	// Neither a projective transform nor one that collapses the visual onto a line fits in a 2D matrix, so the
	// clip isn't mapped at all rather than mapped wrongly.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	[DataRow(false)]
	[DataRow(true)]
	public void When_Ancestor_Clip_Under_Unmappable_Flattening_Transform_Then_Narrows_Nothing(bool projective)
	{
		var visual = TestServices.WindowHelper.XamlRoot.Compositor.CreateContainerVisual();
		visual.TransformMatrix = projective
			? new Matrix4x4(
				1, 0, 0, 0.01f,
				0, 1, 0, 0,
				0, 0, 0, 0,
				0, 0, 0, 1)
			: Matrix4x4.CreateScale(0, 1, 0);
		visual.LayoutClip = (new Rect(30, 20, 50, 40), true);

		Assert.IsNull(visual.GetArrangeClipPathInElementCoordinateSpace());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public void When_Redirect_Source_Under_Flattening_Transform_Then_Bounds()
	{
		var source = CreateLiftedSquare(flattensZ: true, collapse: false).Container;
		var redirect = TestServices.WindowHelper.XamlRoot.Compositor.CreateRedirectVisual(source);

		// Z reaches nothing else, so the parent's 2D part maps the bounds back.
		Assert.IsTrue(redirect.TryGetLocalContentBounds(out var bounds));
		Assert.AreEqual(new Rect(10, 20, 20, 20), bounds);

		// Perspective from Z divides the lifted square by its depth, which the 2D part can't undo.
		var parent = (global::Microsoft.UI.Composition.ContainerVisual)source.Parent!;
		parent.TransformMatrix = parent.TransformMatrix with { M34 = 0.01f };
		Assert.IsFalse(redirect.TryGetLocalContentBounds(out _));
	}

	// A visual surface renders its source as a root, from SourceOffset, so the source's parent must not reach it.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Visual_Surface_Source_Under_Flattening_Transform(bool projective)
	{
		using var _ = UITestHelper.ResetWindowContent();
		var flattened = CreateVisualSurfaceHost(flattensZ: true, projective);
		var expected = CreateVisualSurfaceHost(flattensZ: false, projective);
		await UITestHelper.Load(new StackPanel { Children = { flattened, expected } });

		// Under perspective the two paths round the matrix differently, which can shift an antialiased edge by a level.
		await AssertRendersLike(flattened, expected, tolerance: projective ? (byte)2 : (byte)0, Colors.Red, Colors.Blue);
	}

	// A collapsed container keeps its recording when its own transform changes, so the recording can't depend on the
	// flattening above it: tilting the container brings back the depth of a child lifted along Z.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	// WebGPU composes a replayed recording with its destination in 2D, which drops the depth even without flattening.
	[BackendCondition(ConditionMode.Exclude, RuntimeTestBackends.WebGpu)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Collapsed_Under_Flattening_Transform_Then_Tilted(bool commandList)
	{
		using var _ = UITestHelper.ResetWindowContent();
		var forceFallbackRetainedRendering = global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering;
		try
		{
			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = commandList;
			var collapsed = CreateLiftedSquare(flattensZ: true, collapse: true);
			var live = CreateLiftedSquare(flattensZ: true, collapse: false);
			await UITestHelper.Load(new StackPanel { Children = { collapsed.Host, live.Host } });

			await AssertRendersLike(collapsed.Host, live.Host, Colors.Red);

			collapsed.Container.TransformMatrix = live.Container.TransformMatrix = Matrix4x4.CreateRotationX(MathF.PI / 3);
			await AssertRendersLike(collapsed.Host, live.Host, Colors.Red);
		}
		finally
		{
			global::Microsoft.UI.Composition.Visual.ForceFallbackRetainedRendering = forceFallbackRetainedRendering;
		}
	}

	// A redirect draws its source without the source's ancestors, so a parent that flattens Z must not flatten the
	// source's own depth, which a tilted destination shows.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public async Task When_Redirect_Source_Under_Flattening_Transform_Then_Tilted()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var flattened = CreateLiftedSquare(flattensZ: true, collapse: false);
		var expected = CreateLiftedSquare(flattensZ: false, collapse: false);
		var flattenedRedirect = CreateRedirectHost(flattened.Container, tilted: true);
		var expectedRedirect = CreateRedirectHost(expected.Container, tilted: true);
		await UITestHelper.Load(new StackPanel { Children = { flattened.Host, expected.Host, flattenedRedirect, expectedRedirect } });

		await AssertRendersLike(flattenedRedirect, expectedRedirect, Colors.Red);
	}

	private static Task AssertRendersLike(FrameworkElement actual, FrameworkElement expected, params Color[] colors)
		=> AssertRendersLike(actual, expected, tolerance: 0, colors);

	private static async Task AssertRendersLike(FrameworkElement actual, FrameworkElement expected, byte tolerance, params Color[] colors)
	{
		var expectedScreenshot = await UITestHelper.ScreenShot(expected);
		foreach (var color in colors)
		{
			AssertHasColor(expectedScreenshot, color);
		}

		await ImageAssert.AreEqualAsync(await UITestHelper.ScreenShot(actual), expectedScreenshot, tolerance);
	}

	// The reference must render, or two blank screenshots would compare equal.
	private static void AssertHasColor(RawBitmap screenshot, Color color)
		=> ImageAssert.HasColorInRectangle(screenshot, new global::System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height), color, tolerance: 10);

	// A blue square with an identity local transform and a red one with an offset, under a 2x scale container.
	private static (Border Host, global::Microsoft.UI.Composition.ContainerVisual Container, global::Microsoft.UI.Composition.SpriteVisual OffsetSquare) CreateScaledSquares(bool flattensZ, bool collapse)
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var enable = global::Microsoft.UI.Composition.Visual.EnablePictureCollapsingOptimization;
		var frameThreshold = global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold;
		var countThreshold = global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold;
		global::Microsoft.UI.Composition.ContainerVisual container;
		global::Microsoft.UI.Composition.SpriteVisual offsetSquare;
		try
		{
			if (collapse)
			{
				// Visuals capture these when created, so only the ones built here collapse: the host, built after, doesn't.
				global::Microsoft.UI.Composition.Visual.EnablePictureCollapsingOptimization = true;
				global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold = 0;
				global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold = 0;
			}

			container = compositor.CreateContainerVisual();
			container.TransformMatrix = new Matrix4x4(
				2, 0, 0, 0,
				0, 2, 0, 0,
				0, 0, flattensZ ? 0 : 1, 0,
				10, 10, 0, 1);

			var identitySquare = compositor.CreateSpriteVisual();
			identitySquare.Size = new Vector2(20, 20);
			identitySquare.Brush = compositor.CreateColorBrush(Colors.Blue);
			container.Children.InsertAtTop(identitySquare);

			offsetSquare = compositor.CreateSpriteVisual();
			offsetSquare.Size = new Vector2(20, 20);
			offsetSquare.Offset = new Vector3(20, 20, 0);
			offsetSquare.Brush = compositor.CreateColorBrush(Colors.Red);
			container.Children.InsertAtTop(offsetSquare);
		}
		finally
		{
			global::Microsoft.UI.Composition.Visual.EnablePictureCollapsingOptimization = enable;
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold = frameThreshold;
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold = countThreshold;
		}

		var host = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		global::Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(host, container);
		return (host, container, offsetSquare);
	}

	private static Border CreateRedirectHost(global::Microsoft.UI.Composition.Visual source, bool tilted = false)
	{
		var redirect = TestServices.WindowHelper.XamlRoot.Compositor.CreateRedirectVisual(source);
		redirect.Size = new Vector2(100, 100);
		if (tilted)
		{
			redirect.Offset = new Vector3(0, 20, 0);
			redirect.TransformMatrix = Matrix4x4.CreateRotationX(MathF.PI / 3);
		}

		var host = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		global::Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(host, redirect);
		return host;
	}

	// Two squares in a source under a 2x scale, flattening Z or not and with perspective or not, shown through a visual
	// surface that starts at SourceOffset.
	private static Border CreateVisualSurfaceHost(bool flattensZ, bool projective)
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var parent = compositor.CreateContainerVisual();
		parent.TransformMatrix = new Matrix4x4(
			2, 0, 0, projective ? 0.002f : 0,
			0, 2, 0, 0,
			0, 0, flattensZ ? 0 : 1, 0,
			10, 10, 0, 1);

		var source = compositor.CreateContainerVisual();
		source.Offset = new Vector3(5, 5, 0);
		foreach (var (offset, color) in new[] { (new Vector3(10, 20, 0), Colors.Red), (new Vector3(40, 40, 0), Colors.Blue) })
		{
			var square = compositor.CreateSpriteVisual();
			square.Size = new Vector2(20, 20);
			square.Offset = offset;
			square.Brush = compositor.CreateColorBrush(color);
			source.Children.InsertAtTop(square);
		}

		parent.Children.InsertAtTop(source);

		var surface = compositor.CreateVisualSurface();
		surface.SourceVisual = source;
		surface.SourceOffset = new Vector2(10, 10);
		surface.SourceSize = new Vector2(80, 80);

		var sprite = compositor.CreateSpriteVisual();
		sprite.Size = new Vector2(80, 80);
		sprite.Brush = compositor.CreateSurfaceBrush(surface);

		var host = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		global::Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(host, sprite);
		return host;
	}

	// A red square lifted 10 along Z, in a container under a 2x scale that flattens Z or not.
	private static (Border Host, global::Microsoft.UI.Composition.ContainerVisual Container) CreateLiftedSquare(bool flattensZ, bool collapse)
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var scale = compositor.CreateContainerVisual();
		scale.TransformMatrix = new Matrix4x4(
			2, 0, 0, 0,
			0, 2, 0, 0,
			0, 0, flattensZ ? 0 : 1, 0,
			10, 10, 0, 1);

		var enable = global::Microsoft.UI.Composition.Visual.EnablePictureCollapsingOptimization;
		var frameThreshold = global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold;
		var countThreshold = global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold;
		global::Microsoft.UI.Composition.ContainerVisual container;
		try
		{
			if (collapse)
			{
				// Visuals capture these when created, so only the container collapses.
				global::Microsoft.UI.Composition.Visual.EnablePictureCollapsingOptimization = true;
				global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold = 0;
				global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold = 0;
			}

			container = compositor.CreateContainerVisual();
		}
		finally
		{
			global::Microsoft.UI.Composition.Visual.EnablePictureCollapsingOptimization = enable;
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationFrameThreshold = frameThreshold;
			global::Microsoft.UI.Composition.Visual.PictureCollapsingOptimizationVisualCountThreshold = countThreshold;
		}

		var square = compositor.CreateSpriteVisual();
		square.Size = new Vector2(20, 20);
		square.Offset = new Vector3(10, 20, 0);
		square.TransformMatrix = Matrix4x4.CreateTranslation(0, 0, 10);
		square.Brush = compositor.CreateColorBrush(Colors.Red);
		container.Children.InsertAtTop(square);
		scale.Children.InsertAtTop(container);

		var host = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		global::Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(host, scale);
		return (host, container);
	}

	// A translucent gradient can't take the analytic shadow, so the card records its subtree for the fallback one.
	private static FrameworkElement CreateShadowedCard(bool flattensZ)
	{
		var gradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
		gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0x80, 0xFF, 0xA5, 0x00), Offset = 0 });
		gradient.GradientStops.Add(new GradientStop { Color = Colors.Purple, Offset = 1 });

		return new Grid
		{
			Width = 180,
			Height = 180,
			Background = new SolidColorBrush(Colors.White),
			Projection = CreateZScale(flattensZ),
			Children =
			{
				new Border
				{
					Width = 100,
					Height = 100,
					Background = gradient,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Shadow = new ThemeShadow(),
					Translation = new Vector3(0, 0, 32),
					Child = new Ellipse { Width = 50, Height = 50, Fill = new SolidColorBrush(Colors.LimeGreen) },
				},
			},
		};
	}

	// The Grid is wider than its slot, so it gets a layout clip, which a Panel applies as an ancestor clip.
	private static FrameworkElement CreateClippedPanel(bool flattensZ)
		=> new Border
		{
			Width = 150,
			Height = 100,
			Background = new SolidColorBrush(Colors.White),
			Projection = CreateZScale(flattensZ),
			Child = new Border
			{
				Width = 100,
				HorizontalAlignment = HorizontalAlignment.Left,
				Child = new Grid
				{
					Width = 140,
					HorizontalAlignment = HorizontalAlignment.Left,
					Children = { new Rectangle { Fill = new SolidColorBrush(Colors.Red) } },
				},
			},
		};

	private static Matrix3DProjection CreateZScale(bool flattensZ)
		=> new() { ProjectionMatrix = new global::Microsoft.UI.Xaml.Media.Media3D.Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, flattensZ ? 0 : 1, 0, 0, 0, 0, 1) };

	private static FrameworkElement BuildRenderComparisonScene()
	{
		var gradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
		gradient.GradientStops.Add(new GradientStop { Color = Colors.Orange, Offset = 0 });
		gradient.GradientStops.Add(new GradientStop { Color = Colors.Purple, Offset = 1 });

		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = gradient,
			CornerRadius = new CornerRadius(20),
			BorderBrush = new SolidColorBrush(Colors.DarkBlue),
			BorderThickness = new Thickness(4),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Shadow = new ThemeShadow(),
			Translation = new Vector3(0, 0, 32),
			Child = new Ellipse { Width = 50, Height = 50, Fill = new SolidColorBrush(Colors.LimeGreen) }
		};

		var host = new Grid { Width = 180, Height = 180, Background = new SolidColorBrush(Colors.White) };
		host.Children.Add(border);
		return host;
	}

	private class FrameCounterSKCanvasElement : SKCanvasElement
	{
		public int FrameCounter { get; private set; }
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			FrameCounter++;
			Invalidate();
		}
	}
}
#endif
