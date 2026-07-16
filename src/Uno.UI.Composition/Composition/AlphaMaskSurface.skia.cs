#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

/// <summary>
/// A composition surface that renders a visual's content as an alpha mask.
/// The alpha mask is rendered as white content on transparent background,
/// where the alpha channel represents the shape of the original content.
/// </summary>
internal class AlphaMaskSurface : CompositionObject, ICompositionSurface, ISkiaSurface
{
	private readonly WeakReference<Visual> _visual;

	// Color filter that converts any color to white while preserving alpha.
	// This is the matrix for: output.rgb = white (1,1,1), output.a = input.a
	// The color matrix is a 4x5 matrix in row-major order.
	private static readonly float[] AlphaMaskColorMatrix = new float[]
	{
		0, 0, 0, 0, 1,    // R: output = 1 (white)
		0, 0, 0, 0, 1,    // G: output = 1 (white)
		0, 0, 0, 0, 1,    // B: output = 1 (white)
		0, 0, 0, 1, 0,    // A: output = input alpha
	};

	private static IColorFilter? _alphaMaskColorFilter;

	internal AlphaMaskSurface(Compositor compositor, Visual visual) : base(compositor)
	{
		_visual = new WeakReference<Visual>(visual);
	}

	Vector2 ISkiaSurface.Size => _visual.TryGetTarget(out var visual) ? visual.Size : Vector2.Zero;

	void ISkiaSurface.Paint(IDrawingSession session, float opacity)
	{
		if (!_visual.TryGetTarget(out var visual))
		{
			return;
		}

		_alphaMaskColorFilter ??= DrawingBackend.Current.CreateColorMatrixColorFilter(AlphaMaskColorMatrix);

		// Apply the alpha mask color filter to convert all colors to white while preserving the alpha channel.
		session.SaveLayer(_alphaMaskColorFilter!, antialias: true);

		// Render the visual at its natural position
		visual.RenderRootVisual(session, Vector2.Zero);

		session.Restore();
	}
}
