#if __SKIA__
using System;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

// A frame is recorded in logical units and the display scale applied when it is presented, so a brush that rasterizes
// its content offscreen while the frame records has to be told that scale: sized from the recording's transform
// alone, the texture is baked at 1x and magnified on a high-DPI display.
[TestClass]
[RunsOnUIThread]
public class Given_CompositionBrush_RasterizationScale
{
	private const float PresentScale = 2.625f;

	[TestMethod]
	public void When_NineGrid_Recorded_Without_Present_Scale()
	{
		var (nineGrid, probe) = CreateNineGrid();

		Paint(nineGrid, presentScale: null);

		Assert.AreEqual(1f, probe.SeenScale, 1e-3f);
	}

	[TestMethod]
	public void When_NineGrid_Recorded_For_Scaled_Present()
	{
		var (nineGrid, probe) = CreateNineGrid();

		Paint(nineGrid, PresentScale);

		// Rounded up to a power of two (the brushes cache on it), never coarser than the device.
		Assert.AreEqual(4f, probe.SeenScale, 1e-3f);
	}

	[TestMethod]
	public void When_Nested_NineGrid_Recorded_For_Scaled_Present()
	{
		var compositor = Compositor.GetSharedCompositor();
		var (inner, probe) = CreateNineGrid();
		var outer = compositor.CreateNineGridBrush();
		outer.Source = inner;

		Paint(outer, PresentScale);

		// The outer offscreen session already carries the scale: counting the present scale again would rasterize
		// the inner source at 4 x 2.625.
		Assert.AreEqual(4f, probe.SeenScale, 1e-3f);
	}

	[TestMethod]
	public void When_Effect_Recorded_For_Scaled_Present()
	{
		var compositor = Compositor.GetSharedCompositor();
		var probe = new ScaleProbeBrush(compositor);
		var effect = new ColorMatrixEffect { Source = new CompositionEffectSourceParameter("source"), ColorMatrix = Matrix5x4.Identity };
		var brush = compositor.CreateEffectFactory(effect).CreateBrush()!;
		brush.SetSourceParameter("source", probe);

		Paint(brush, PresentScale);

		Assert.AreEqual(4f, probe.SeenScale, 1e-3f);
	}

	private static (CompositionNineGridBrush NineGrid, ScaleProbeBrush Probe) CreateNineGrid()
	{
		var compositor = Compositor.GetSharedCompositor();
		var probe = new ScaleProbeBrush(compositor);
		var nineGrid = compositor.CreateNineGridBrush();
		nineGrid.Source = probe;
		nineGrid.SetInsets(4);
		return (nineGrid, probe);
	}

	private static void Paint(CompositionBrush brush, float? presentScale)
	{
		// Painted the way a frame records it: with the main window's backend, at identity, under the present scale.
		var factory = Private.Infrastructure.TestServices.WindowHelper.XamlRoot!.VisualTree.RootElement!.Visual.CompositionTarget!.Renderer!;
		var recording = factory.CreateRecording();
		using (CompositionBrush.SetPresentScale(presentScale ?? 1f))
		{
			brush.TryPaint(recording, 1f, new Rect(0, 0, 40, 40));
		}

		recording.Finish()?.Dispose();
	}

	// Records the device pixels per unit of the session it is painted into.
	private sealed class ScaleProbeBrush : CompositionBrush
	{
		public ScaleProbeBrush(Compositor compositor) : base(compositor)
		{
		}

		public float SeenScale { get; private set; }

		internal override bool CanPaint() => true;

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			var m = session.TotalMatrix;
			SeenScale = MathF.Sqrt((m.M11 * m.M11) + (m.M12 * m.M12));
			session.DrawRect(bounds, Colors.Red);
			return true;
		}
	}
}
#endif
