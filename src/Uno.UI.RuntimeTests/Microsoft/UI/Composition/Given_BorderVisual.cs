using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;

#if __SKIA__
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Uno.UI.Helpers;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_BorderVisual
{
	// A rounded border culled out of the viewport only builds its paths when it first paints. In a long list
	// scrolled a little each frame a row enters every few frames, so if that build invalidated the ancestors,
	// the panel would never stay stable long enough to be collapsed into a cached picture.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Rows_Enter_Viewport_While_Scrolling_Then_Content_Is_Collapsed()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();

		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);
		root.Clip = compositor.CreateRectangleClip(top: 0, left: 0, bottom: 200, right: 200);

		var scroller = compositor.CreateContainerVisual();
		root.Children.InsertAtTop(scroller);

		var panel = compositor.CreateContainerVisual();
		scroller.Children.InsertAtTop(panel);

		for (var i = 0; i < Visual.PictureCollapsingOptimizationVisualCountThreshold * 3; i++)
		{
			var row = compositor.CreateBorderVisual();
			row.Size = new Vector2(200, 10);
			row.Offset = new Vector3(0, i * 10, 0);
			row.CornerRadius = new CornerRadius(2);
			row.BackgroundBrush = compositor.CreateColorBrush(Colors.Magenta);
			panel.Children.InsertAtTop(row);
		}

		using var damage = new DamageRegion();
		for (var frame = 0; frame < Visual.PictureCollapsingOptimizationFrameThreshold * 3; frame++)
		{
			// Only the scroller moves: a new row comes into view every few frames.
			scroller.Offset = new Vector3(0, -3 * frame, 0);
			damage.Reset();
			var recording = DrawingFactory.Current.CreateRecording();
			FrameRenderHelper.RecordFrame(recording, 200, 200, root, invertPath: false, damage: damage);
			recording.Finish()?.Dispose();
		}

		// The collapse lands on the highest stable ancestor of the rows: the scrolled content.
		var childrenContent = typeof(Visual).GetField("_childrenContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
		Assert.IsNotNull(
			childrenContent.GetValue(scroller) ?? childrenContent.GetValue(panel),
			"The rows were never collapsed into a cached picture while they were only scrolled.");
#else
		await Task.CompletedTask;
#endif
	}
}
