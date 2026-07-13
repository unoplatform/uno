using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

public partial class Visual
{
	private interface IPrivateSessionFactory
	{
		void CreateInstance(Visual visual, IDrawingSession drawingSession, ref Matrix4x4 rootTransform, float opacity, out PaintingSession session);
	}

	/// <summary>
	/// Represents the "context" in which a visual draws.
	/// </summary>
	internal readonly ref struct PaintingSession
	{
		// This dance is done to make it so that only Visual can create a PaintingSession
		public readonly struct SessionFactory : IPrivateSessionFactory
		{
			void IPrivateSessionFactory.CreateInstance(Visual visual, IDrawingSession drawingSession, ref Matrix4x4 rootTransform, float opacity, out PaintingSession session)
			{
				session = new PaintingSession(visual, drawingSession, ref rootTransform, opacity);
			}
		}

		private PaintingSession(Visual visual, IDrawingSession drawingSession, ref Matrix4x4 rootTransform, float opacity)
		{
			Session = drawingSession;
			Canvas = ((SkiaDrawingSession)drawingSession).Canvas;
			RootTransform = ref rootTransform;
			Opacity = opacity;

			_saveCount = Session.Save();
		}

		public void Dispose() => Session.RestoreToCount(_saveCount);

		/// <summary>
		/// The underlying SkiaSharp canvas. Transitional accessor for render code (clips, shadows) not yet
		/// migrated off direct SkiaSharp; new painting code should use <see cref="Session"/>.
		/// </summary>
		public readonly SKCanvas Canvas;

		/// <summary>The backend-neutral drawing surface for this session.</summary>
		public readonly IDrawingSession Session;

		/// <summary>The transform matrix to the root visual of this drawing session (which isn't necessarily the identity matrix due to scaling (DPI) and/or RenderTargetBitmap.</summary>
		public readonly ref Matrix4x4 RootTransform;

		public readonly float Opacity;

		private readonly int _saveCount;
	}
}
