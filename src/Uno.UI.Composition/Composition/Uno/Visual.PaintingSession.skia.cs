using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class Visual
{
	private interface IPrivateSessionFactory
	{
		void CreateInstance(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity, out PaintingSession session);
	}

	/// <summary>
	/// Represents the "context" in which a visual draws.
	/// </summary>
	internal readonly ref struct PaintingSession
	{
		// This dance is done to make it so that only Visual can create a PaintingSession
		public readonly struct SessionFactory : IPrivateSessionFactory
		{
			void IPrivateSessionFactory.CreateInstance(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity, out PaintingSession session)
			{
				session = new PaintingSession(visual, canvas, ref rootTransform, opacity);
			}
		}

		private PaintingSession(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity)
		{
			Canvas = canvas;
			RootTransform = ref rootTransform;
			Opacity = opacity;

			_saveCount = canvas.Save();
		}

		public void Dispose() => Canvas.RestoreToCount(_saveCount);

		public readonly SKCanvas Canvas;

		/// <summary>The transform matrix to the root visual of this drawing session (which isn't necessarily the identity matrix due to scaling (DPI) and/or RenderTargetBitmap.</summary>
		public readonly ref Matrix4x4 RootTransform;

		public readonly float Opacity;

		public SKColorFilter OpacityColorFilter => Opacity == 1.0f ?
			null :
			_opacityToColorFilter[(byte)(0xFF * Opacity)] ??= SKColorFilter.CreateBlendMode(new SKColor(0xFF, 0xFF, 0xFF, (byte)(0xFF * Opacity)), SKBlendMode.Modulate);

		private static readonly SKColorFilter[] _opacityToColorFilter = new SKColorFilter[256];

		private readonly int _saveCount;
	}
}
