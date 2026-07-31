using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
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

		damage.Rewind();

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

#if __SKIA__
	private static void RenderFrame(ContainerVisual root, SKPath damage)
	{
		var (picture, _, _) = SkiaRenderHelper.RecordPictureAndReturnPath(200, 200, root, invertPath: false, damage: damage);
		picture.Dispose();
	}
#endif
}
