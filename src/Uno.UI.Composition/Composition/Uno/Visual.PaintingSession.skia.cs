using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;
namespace Windows.UI.Composition;

public partial class Visual
{
	private interface IPrivateSessionFactory
	{
		PaintingSession CreateInstance(in Visual visual, in SKSurface surface, in SKCanvas canvas, in DrawingFilters filters, in Matrix4x4 rootTransform);
	}

	/// <summary>
	/// Represents the "context" in which a visual draws.
	/// </summary>
	/// <remarks>
	/// Accessing Surface.Canvas is slow due to SkiaSharp interop.
	/// Avoid using .Surface.Canvas and use .Canvas right away.
	/// </remarks>
	internal readonly struct PaintingSession : IDisposable
	{
		// This dance is done to make it so that only Visual can create a PaintingSession
		public readonly struct SessionFactory : IPrivateSessionFactory
		{
			PaintingSession IPrivateSessionFactory.CreateInstance(in Visual visual, in SKSurface surface, in SKCanvas canvas, in DrawingFilters filters, in Matrix4x4 rootTransform)
			{
				return new PaintingSession(visual, surface, canvas, filters, rootTransform);
			}
		}

		private readonly int _saveCount;

		private PaintingSession(Visual visual, SKSurface surface, SKCanvas canvas, in DrawingFilters filters, in Matrix4x4 rootTransform)
		{
			_saveCount = visual.ShadowState is { } shadow ? canvas.SaveLayer(shadow.Paint) : canvas.Save();
			Surface = surface;
			Canvas = canvas;
			Filters = filters;
			RootTransform = rootTransform;
		}

		public SKSurface Surface { get; }
		public SKCanvas Canvas { get; }
		public DrawingFilters Filters { get; }

		/// <summary>The transform matrix to the root visual of this drawing session (which isn't necessarily the identity matrix due to scaling (DPI) and/or RenderTargetBitmap.</summary>
		public Matrix4x4 RootTransform { get; }

		public void Dispose() => Canvas.RestoreToCount(_saveCount);
	}
}
