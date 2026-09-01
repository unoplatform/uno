using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Uno.UI;
using Windows.UI;

#if __SKIA__
using Uno.UI.Helpers;
using SkiaSharp;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_Visual_Damage
{
	// Damage-region rendering only repaints the regions reported as damaged, so every change that alters
	// what a visual contributes to the frame has to report one. Growing an ancestor's clip reveals part of
	// a child whose own content and transform are untouched; if that isn't reported, the revealed pixels
	// keep the previous frame's content until something else happens to damage them (e.g. a hover state).
	// Controls that re-clip on every arrange (a virtualizing rows panel sizing its clip to the content
	// height) hit this whenever the clip grows.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Damage-region rendering is specific to the Skia compositor.")]
#endif
	public async Task When_Ancestor_Clip_Grows_Then_Revealed_Region_Is_Damaged()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();

		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);

		var child = compositor.CreateSpriteVisual();
		child.Brush = compositor.CreateColorBrush(Colors.Magenta);
		child.Size = new Vector2(100, 100);
		root.Children.InsertAtTop(child);

		// Clip away the bottom half of the child.
		root.Clip = compositor.CreateRectangleClip(top: 0, left: 0, bottom: 50, right: 100);

		using var damage = new SKPath();
		RenderFrame(root, damage);

		damage.Reset();

		// The only change: the clip grows to reveal the bottom half.
		root.Clip = compositor.CreateRectangleClip(top: 0, left: 0, bottom: 100, right: 100);
		RenderFrame(root, damage);

		Assert.IsFalse(
			damage.IsEmpty,
			"Growing the clip reported no damage, so the revealed strip would keep the previous frame's pixels.");

		Assert.IsTrue(
			damage.Bounds.Bottom >= 98,
			$"Damage does not cover the revealed strip down to y=100 (damage bounds: {damage.Bounds}).");

		// Reporting the whole surface would satisfy the assertions above while erasing the point of
		// partial repaint, so bound the reported region to the child plus antialiasing slack.
		Assert.IsTrue(
			damage.Bounds.Right <= 140,
			$"Damage is far wider than the revealed strip, partial repaint is being defeated (damage bounds: {damage.Bounds}).");
#else
		await Task.CompletedTask;
#endif
	}

	// Same bounds, different clip shape: only the corner radii change, so a comparison of the clip's
	// bounding box sees nothing while the corners must in fact be repainted. The subtree is also forced to
	// be collapsed into a cached children picture first, since descendants are not walked at all then and
	// nothing would get the chance to report the damage.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Damage-region rendering is specific to the Skia compositor.")]
#endif
	public async Task When_Clip_Shape_Changes_Within_Same_Bounds_Then_It_Is_Damaged()
	{
#if __SKIA__
		var frameThreshold = FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold;
		var countThreshold = FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold;
		var enabled = FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization;

		try
		{
			// Force the collapsing optimization to apply to this small tree immediately.
			FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization = true;
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold = 1;
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold = 1;

			var compositor = Compositor.GetSharedCompositor();

			var root = compositor.CreateContainerVisual();
			root.Size = new Vector2(200, 200);

			var child = compositor.CreateSpriteVisual();
			child.Brush = compositor.CreateColorBrush(Colors.Magenta);
			child.Size = new Vector2(100, 100);
			root.Children.InsertAtTop(child);

			root.Clip = compositor.CreateRectangleClip(left: 0, top: 0, right: 100, bottom: 100);

			using var damage = new SKPath();

			// Render enough unchanged frames for the subtree to be cached.
			for (var i = 0; i < 5; i++)
			{
				RenderFrame(root, damage);
				damage.Reset();
			}

			// Identical bounds, rounded corners: the corner pixels are no longer inside the clip.
			var radius = new Vector2(50, 50);
			root.Clip = compositor.CreateRectangleClip(0, 0, 100, 100, radius, radius, radius, radius);
			RenderFrame(root, damage);

			Assert.IsFalse(
				damage.IsEmpty,
				"A clip whose shape changed within unchanged bounds reported no damage, so the clipped-away corners would keep the previous frame's pixels.");
		}
		finally
		{
			FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization = enabled;
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold = frameThreshold;
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold = countThreshold;
		}
#else
		await Task.CompletedTask;
#endif
	}

#if __SKIA__
	private static void RenderFrame(ContainerVisual root, SKPath damage)
	{
		var (picture, _, _) = SkiaRenderHelper.RecordPictureAndReturnPath(200, 200, root, invertPath: false, damage: damage);
		picture.Dispose();
	}
#endif
}
