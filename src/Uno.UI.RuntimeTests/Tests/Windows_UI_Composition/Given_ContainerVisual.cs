#if __SKIA__
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
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
using Uno.UI.Toolkit.DevTools.Input;
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
			await UITestHelper.WaitFor(() => skce.FrameCounter > count);
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

		// SKPath overload (the one used by GetPrePaintingClipping during rendering).
		using var path = new SKPath();
		Assert.IsTrue(visual.GetArrangeClipPathInElementCoordinateSpace(path));
		Assert.AreEqual(0f, path.Bounds.Left, 0.01f);
		Assert.AreEqual(0f, path.Bounds.Top, 0.01f);
		Assert.AreEqual(50f, path.Bounds.Right, 0.01f);
		Assert.AreEqual(40f, path.Bounds.Bottom, 0.01f);

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
		visual.RenderRootVisual(surface.Canvas, Vector2.Zero);
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
