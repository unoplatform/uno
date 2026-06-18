#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class Visual
{
	private interface IPrivateSessionFactory
	{
		void CreateInstance(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity, DamageRegion? damage, out PaintingSession session);
	}

	/// <summary>
	/// Represents the "context" in which a visual draws.
	/// </summary>
	internal readonly ref struct PaintingSession
	{
		// This dance is done to make it so that only Visual can create a PaintingSession
		public readonly struct SessionFactory : IPrivateSessionFactory
		{
			void IPrivateSessionFactory.CreateInstance(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity, DamageRegion? damage, out PaintingSession session)
			{
				session = new PaintingSession(visual, canvas, ref rootTransform, opacity, damage);
			}
		}

		private PaintingSession(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity, DamageRegion? damage)
		{
			Canvas = canvas;
			RootTransform = ref rootTransform;
			Opacity = opacity;
			Damage = damage;

			_saveCount = canvas.Save();
		}

		public void Dispose() => Canvas.RestoreToCount(_saveCount);

		public readonly SKCanvas Canvas;

		/// <summary>The transform matrix to the root visual of this drawing session (which isn't necessarily the identity matrix due to scaling (DPI) and/or RenderTargetBitmap.</summary>
		public readonly ref Matrix4x4 RootTransform;

		public readonly float Opacity;

		/// <summary>
		/// The per-frame damage-region accumulator for the on-screen render pass, threaded through the whole
		/// visual walk so each visual adds the region it (re)paints as the walk proceeds. Null for off-screen
		/// renders (RenderTargetBitmap, visual surfaces), which don't track damage.
		/// </summary>
		public readonly DamageRegion? Damage;

		private readonly int _saveCount;
	}
}
