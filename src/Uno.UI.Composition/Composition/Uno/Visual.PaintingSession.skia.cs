#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

public partial class Visual
{
	private interface IPrivateSessionFactory
	{
		void CreateInstance(Visual visual, IDrawingSession drawingSession, ref Matrix4x4 rootTransform, float opacity, DamageRegion? damage, out PaintingSession session);
	}

	/// <summary>
	/// Represents the "context" in which a visual draws.
	/// </summary>
	internal readonly ref struct PaintingSession
	{
		// This dance is done to make it so that only Visual can create a PaintingSession
		public readonly struct SessionFactory : IPrivateSessionFactory
		{
			void IPrivateSessionFactory.CreateInstance(Visual visual, IDrawingSession drawingSession, ref Matrix4x4 rootTransform, float opacity, DamageRegion? damage, out PaintingSession session)
			{
				session = new PaintingSession(visual, drawingSession, ref rootTransform, opacity, damage);
			}
		}

		private PaintingSession(Visual visual, IDrawingSession drawingSession, ref Matrix4x4 rootTransform, float opacity, DamageRegion? damage)
		{
			Session = drawingSession;
			RootTransform = ref rootTransform;
			Opacity = opacity;
			Damage = damage;

			_saveCount = Session.Save();
		}

		public void Dispose() => Session.RestoreToCount(_saveCount);

		/// <summary>The backend-neutral drawing surface for this session.</summary>
		public readonly IDrawingSession Session;

		/// <summary>The transform matrix to the root visual of this drawing session (which isn't necessarily the identity matrix due to scaling (DPI) and/or RenderTargetBitmap.</summary>
		public readonly ref Matrix4x4 RootTransform;

		public readonly float Opacity;

		/// <summary>The per-frame damage (dirty-region) accumulator threaded through the render walk, or null when
		/// damage tracking is disabled for this pass (e.g. offscreen surfaces / RenderTargetBitmap that repaint fully).</summary>
		public readonly DamageRegion? Damage;

		private readonly int _saveCount;
	}
}
