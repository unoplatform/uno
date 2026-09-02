using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Uno.UI;
using Windows.UI;

#if __SKIA__
using Uno.UI.Composition;
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

		using var damage = new DamageRegion();
		RenderFrame(root, damage);

		damage.Reset();

		// The only change: the clip grows to reveal the bottom half.
		root.Clip = compositor.CreateRectangleClip(top: 0, left: 0, bottom: 100, right: 100);
		RenderFrame(root, damage);

		Assert.IsFalse(
			damage.IsEmpty,
			"Growing the clip reported no damage, so the revealed strip would keep the previous frame's pixels.");

		using var reported = SnapshotDamage(damage);

		Assert.IsTrue(
			reported.Bounds.Bottom >= 98,
			$"Damage does not cover the revealed strip down to y=100 (damage bounds: {reported.Bounds}).");

		// Reporting the whole surface would satisfy the assertions above while erasing the point of
		// partial repaint, so bound the reported region to the child plus antialiasing slack.
		Assert.IsTrue(
			reported.Bounds.Right <= 140,
			$"Damage is far wider than the revealed strip, partial repaint is being defeated (damage bounds: {reported.Bounds}).");
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

			using var damage = new DamageRegion();

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

	// A scroll damages every moved visual at both its old and its new position, frame after frame. Once it
	// stops, nothing moves any more and the region has to fall silent: a visual that kept reporting itself
	// would repaint the whole scroll port forever, and one that under-reported while moving would leave the
	// vacated pixels stale.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Damage-region rendering is specific to the Skia compositor.")]
#endif
	public async Task When_Visual_Stops_Moving_Then_Damage_Falls_Silent()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();

		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);

		// The moved visual is an ancestor, as it is when a scroll translates a content presenter: the child's
		// own paint is never invalidated, which is what puts it on the moved-but-unchanged damage path.
		var mover = compositor.CreateContainerVisual();
		mover.Size = new Vector2(200, 200);
		root.Children.InsertAtTop(mover);

		var child = compositor.CreateSpriteVisual();
		child.Brush = compositor.CreateColorBrush(Colors.Magenta);
		child.Size = new Vector2(100, 50);
		mover.Children.InsertAtTop(child);

		using var damage = new DamageRegion();
		RenderFrame(root, damage);
		damage.Reset();

		// Scrolling: the subtree moves every frame.
		for (var y = 20f; y <= 60f; y += 20f)
		{
			var previousTop = y - 20f;
			mover.Offset = new Vector3(0, y, 0);
			RenderFrame(root, damage);

			using var moving = SnapshotDamage(damage);
			Assert.IsFalse(moving.IsEmpty, $"A visual that moved to y={y} reported no damage.");

			// Both positions must be covered, or one of them keeps the previous frame's pixels.
			Assert.IsTrue(
				moving.Bounds.Top <= previousTop && moving.Bounds.Bottom >= y + 50,
				$"Damage does not span the vacated and the new position ([{previousTop}, {y + 50}] expected, got {moving.Bounds}).");

			// The two positions overlap here, and overlapping contributions are appended to one path under the
			// nonzero fill rule — a wrong contour direction would cancel them into a hole Bounds cannot see.
			Assert.IsTrue(moving.Contains(50, y), $"The overlap of the two positions is a hole (damage bounds: {moving.Bounds}).");
		}

		// Scrolling stopped. Nothing moves, so no frame from here on may report anything — and the region is
		// deliberately not reset between these frames, so a single stuck contribution would fail every one.
		for (var frame = 0; frame < 3; frame++)
		{
			RenderFrame(root, damage);
			Assert.IsTrue(
				damage.IsEmpty,
				$"Frame {frame} after the movement stopped still reported damage, so a still frame would keep repainting.");
		}
#else
		await Task.CompletedTask;
#endif
	}

	// BorderVisual is the visual that the moved-visual fast path actually applies to: it has an exact content
	// path (so it would otherwise take the expensive branch) and guarantees it paints within its Size (so the
	// cheap branch is allowed to answer for it). Moving it must still damage both positions in full.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Damage-region rendering is specific to the Skia compositor.")]
#endif
	public async Task When_Visual_With_Content_Path_Moves_Then_Both_Positions_Are_Damaged()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();

		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);

		var mover = compositor.CreateContainerVisual();
		mover.Size = new Vector2(200, 200);
		root.Children.InsertAtTop(mover);

		var border = compositor.CreateBorderVisual();
		border.Size = new Vector2(100, 50);
		border.BackgroundBrush = compositor.CreateColorBrush(Colors.Magenta);
		mover.Children.InsertAtTop(border);

		using var damage = new DamageRegion();
		RenderFrame(root, damage);
		damage.Reset();

		// Only the ancestor moves, so the border's own paint is untouched — the moved-but-unchanged path.
		mover.Offset = new Vector3(0, 100, 0);
		RenderFrame(root, damage);

		using var moved = SnapshotDamage(damage);

		Assert.IsTrue(
			moved.Bounds.Top <= 0 && moved.Bounds.Bottom >= 150,
			$"Damage does not span the vacated (0-50) and the new (100-150) position (got {moved.Bounds}).");

		// Interiors, not just the bounding box: contributions are appended to one path under the nonzero fill
		// rule, so a wrong contour direction would union into a hole that Bounds cannot see.
		Assert.IsTrue(moved.Contains(50, 25), $"The vacated region has a hole (damage bounds: {moved.Bounds}).");
		Assert.IsTrue(moved.Contains(50, 125), $"The new region has a hole (damage bounds: {moved.Bounds}).");

		// Still a partial repaint, not the whole surface.
		Assert.IsTrue(
			moved.Bounds.Right <= 140,
			$"Damage is far wider than the moved visual, partial repaint is being defeated (got {moved.Bounds}).");
#else
		await Task.CompletedTask;
#endif
	}

	// Rect contributions are merged into a running rect to keep the clip cheap, which is free while scrolling
	// because every moved visual lands in the same port. A frame that also damages something elsewhere — a
	// progress ring, a caret — must not have the two merged, or everything between them repaints as well.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Damage-region rendering is specific to the Skia compositor.")]
#endif
	public async Task When_Damage_Is_Far_Apart_Then_It_Is_Not_Merged()
	{
#if __SKIA__
		using var damage = new DamageRegion();

		// A scroll port's worth of overlapping contributions, as a moved subtree produces.
		for (var y = 0f; y < 200; y += 10)
		{
			damage.UnionRect(new SKRect(0, y, 100, y + 20));
		}

		// Something small animating in the opposite corner.
		damage.UnionRect(new SKRect(900, 900, 920, 920));

		using var reported = SnapshotDamage(damage, frameSize: 1000);

		Assert.IsTrue(reported.Contains(50, 100), $"The scroll port is not damaged (damage bounds: {reported.Bounds}).");
		Assert.IsTrue(reported.Contains(910, 910), $"The far region is not damaged (damage bounds: {reported.Bounds}).");
		Assert.IsFalse(
			reported.Contains(500, 500),
			$"The empty space between the two regions was damaged, so they were merged into one box (damage bounds: {reported.Bounds}).");
#else
		await Task.CompletedTask;
#endif
	}

	// A ShapeVisual is bounded by its shapes, not by its Size, so it answers the moved-visual question through
	// its own TryGetLocalContentBounds override. Its content is deliberately smaller than the visual here: the
	// reported region has to cover what is painted without being widened to the whole visual or to the clip.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Damage-region rendering is specific to the Skia compositor.")]
#endif
	public async Task When_Shape_Visual_Moves_Then_Damage_Covers_Its_Shapes()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();

		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);

		var mover = compositor.CreateContainerVisual();
		mover.Size = new Vector2(200, 200);
		root.Children.InsertAtTop(mover);

		var shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Size = new Vector2(160, 60);

		var geometry = compositor.CreateRoundedRectangleGeometry();
		geometry.Offset = new Vector2(20, 10);
		geometry.Size = new Vector2(60, 30);

		var shape = compositor.CreateSpriteShape(geometry);
		shape.FillBrush = compositor.CreateColorBrush(Colors.Magenta);
		shapeVisual.Shapes.Add(shape);
		mover.Children.InsertAtTop(shapeVisual);

		using var damage = new DamageRegion();
		RenderFrame(root, damage);
		damage.Reset();

		mover.Offset = new Vector3(0, 100, 0);
		RenderFrame(root, damage);

		using var reported = SnapshotDamage(damage);

		// The shape sits at (20,10)-(80,40) locally, so its centre is (50,25) before the move and (50,125) after.
		Assert.IsTrue(reported.Contains(50, 25), $"The vacated shape is not covered (damage bounds: {reported.Bounds}).");
		Assert.IsTrue(reported.Contains(50, 125), $"The moved shape is not covered (damage bounds: {reported.Bounds}).");

		// Falling back to the visual's Size, or worse to the clip, would reach well past the shape's 80px right edge.
		Assert.IsTrue(
			reported.Bounds.Right <= 120,
			$"Damage covers far more than the shape, so it was not bounded by the shape (damage bounds: {reported.Bounds}).");
#else
		await Task.CompletedTask;
#endif
	}

	// A real scroll contributes two rects per moved visual, so a list frame runs well past the point where the
	// region stops keeping every contribution as its own contour and starts collapsing them to a bounding rect.
	// Collapsing is only ever allowed to report more, never less: every vacated and every new position has to
	// survive it.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Damage-region rendering is specific to the Skia compositor.")]
#endif
	public async Task When_Many_Visuals_Move_Then_Collapsed_Damage_Is_A_Superset()
	{
#if __SKIA__
		const int ItemCount = 8;
		const float ItemHeight = 20;
		const float Delta = 5;

		var compositor = Compositor.GetSharedCompositor();

		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);

		var mover = compositor.CreateContainerVisual();
		mover.Size = new Vector2(200, 200);
		root.Children.InsertAtTop(mover);

		for (var i = 0; i < ItemCount; i++)
		{
			var item = compositor.CreateSpriteVisual();
			item.Brush = compositor.CreateColorBrush(Colors.Magenta);
			item.Size = new Vector2(100, ItemHeight);
			item.Offset = new Vector3(0, i * ItemHeight, 0);
			mover.Children.InsertAtTop(item);
		}

		using var damage = new DamageRegion();
		RenderFrame(root, damage);
		damage.Reset();

		mover.Offset = new Vector3(0, Delta, 0);
		RenderFrame(root, damage);

		using var reported = SnapshotDamage(damage);

		for (var i = 0; i < ItemCount; i++)
		{
			var centre = i * ItemHeight + ItemHeight / 2;
			Assert.IsTrue(
				reported.Contains(50, centre),
				$"Item {i} vacated y={centre}, which is not covered (damage bounds: {reported.Bounds}).");
			Assert.IsTrue(
				reported.Contains(50, centre + Delta),
				$"Item {i} moved to y={centre + Delta}, which is not covered (damage bounds: {reported.Bounds}).");
		}

		// The items are 100 wide, so collapsing must not have grown the region across the whole frame.
		Assert.IsTrue(
			reported.Bounds.Right <= 140,
			$"Collapsing widened the damage far beyond the moved items (damage bounds: {reported.Bounds}).");
#else
		await Task.CompletedTask;
#endif
	}

#if __SKIA__
	private static void RenderFrame(ContainerVisual root, DamageRegion damage)
	{
		var (picture, _, _) = SkiaRenderHelper.RecordPictureAndReturnPath(200, 200, root, invertPath: false, damage: damage);
		picture.Dispose();
	}

	// The region keeps rect and exact-path contributions apart; snapshotting materialises them into the
	// single path the frame is actually clipped to, which is what these assertions inspect.
	private static SKPath SnapshotDamage(DamageRegion damage, float frameSize = 200)
	{
		var path = new SKPath();
		damage.SnapshotAndReset(path, new SKRect(0, 0, frameSize, frameSize));
		return path;
	}
#endif
}
