using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;
namespace Microsoft.UI.Composition;

public partial class Visual
{
	/// <remarks>
	/// ONLY CONSTRUCT THIS IN A USING BLOCK. C# doesn't have RAII, so we can't enforce this.
	/// The closest thing we can do is make this PaintingSessionWrapper type only visible to the Visual
	/// and limit the areas where a new PaintingSessionWrapper needs to be created.
	/// </remarks>
	private readonly struct PaintingSessionWrapper(PaintingSession session, int saveCount) : IDisposable
	{
		public PaintingSession Session { get; } = session;
		public void Dispose() => Session.Canvas.RestoreToCount(saveCount);
	}

	private interface IPrivateSessionFactory
	{
		PaintingSessionWrapper CreateInstance(Visual visual, SKSurface surface, SKCanvas canvas, in DrawingFilters filters, in Matrix4x4 rootTransform);
	}

	/// <summary>
	/// Represents the "context" in which a visual draws.
	/// </summary>
	/// <remarks>
	/// Accessing Surface.Canvas is slow due to SkiaSharp interop.
	/// Avoid using .Surface.Canvas and use .Canvas right away.
	/// </remarks>
	internal readonly struct PaintingSession
	{
		// This dance is done to make it so that only Visual can create a PaintingSession
		public readonly struct SessionFactory : IPrivateSessionFactory
		{
			PaintingSessionWrapper IPrivateSessionFactory.CreateInstance(Visual visual, SKSurface surface, SKCanvas canvas, in DrawingFilters filters, in Matrix4x4 rootTransform)
			{
				var saveCount = visual.ShadowState is { } shadow ? canvas.SaveLayer(shadow.Paint) : canvas.Save();
				return new PaintingSessionWrapper(new PaintingSession(surface, canvas, filters, rootTransform), saveCount);
			}
		}

		private PaintingSession(SKSurface surface, SKCanvas canvas, in DrawingFilters filters, in Matrix4x4 rootTransform)
		{
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
	}
}
