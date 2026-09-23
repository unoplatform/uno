#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		/// <summary>
		/// Paints this brush onto a backend-neutral <see cref="IDrawingSession"/>. Returns true when the brush
		/// handled the paint (including "nothing to paint"); false only when the brush cannot paint at all.
		/// </summary>
		internal virtual bool TryPaint(IDrawingSession session, float opacity, Rect bounds) => false;

		/// <summary>
		/// Realizes any offscreen content this brush needs (e.g. an effect graph's rasterized sources) BEFORE it is
		/// painted into a caller-owned offscreen. Called by <c>EffectGraphParser.RasterizeSource</c> ahead of opening
		/// its <see cref="IDrawingFactory.RenderOffscreen"/> so a nested effect brush's own offscreen passes run first,
		/// sequentially, rather than re-entering <c>RenderOffscreen</c> while the outer one is still open. Nested
		/// (re-entrant) offscreen rendering is not part of the drawing contract — a backend may hold per-pass scratch
		/// that a nested pass corrupts — so it must be avoided. No-op for brushes that don't rasterize offscreen.
		/// <para><paramref name="scale"/> is the device pixels per logical unit the caller will rasterize at; it must
		/// match the scale of the session the brush is then painted into, or the prepared content is rebuilt (nested)
		/// during that paint.</para>
		/// </summary>
		internal virtual void PrepareForOffscreenRasterization(IDrawingFactory factory, Rect bounds, Vector2 scale) { }

		// Beyond this, a magnified element's offscreen costs more memory than the sharpness is worth.
		private const float MaxRasterizationScale = 8f;

		/// <summary>
		/// The device pixels per logical unit of <paramref name="session"/>'s current transform, per axis. Content
		/// rasterized offscreen has to be sized by this: rasterizing at logical size and letting the session magnify
		/// the result is a visible softening at any scale above 1.
		/// </summary>
		private protected static Vector2 GetRasterizationScale(IDrawingSession session)
		{
			var matrix = session.TotalMatrix;

			// Row norms of the 2x2 linear part, so a rotated (or skewed) transform still reports what it magnifies by.
			var x = MathF.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12));
			var y = MathF.Sqrt((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22));

			return new Vector2(Clamp(x), Clamp(y));

			static float Clamp(float scale)
				=> !float.IsFinite(scale) || scale <= 0f ? 1f : MathF.Min(scale, MaxRasterizationScale);
		}

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;

		internal virtual float DamageRegionSamplingMargin => 0;
	}
}
