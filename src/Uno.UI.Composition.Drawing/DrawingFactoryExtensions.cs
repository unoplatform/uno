#nullable enable

using System;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Convenience helpers layered over <see cref="IDrawingFactory"/>'s primitive-geometry builder, so common
/// shapes don't need a dedicated backend-implemented factory method.
/// </summary>
internal static class DrawingFactoryExtensions
{
	// Rectangle geometry is by far the most common shape (per-visual/per-frame clips and bounds). Rather than a
	// factory method every backend must implement, reuse a cached primitive builder: Build() resets it, so a
	// single instance serves every call. Thread-static because rendering can happen off the UI thread (offscreen
	// passes); keyed on the factory so a backend swap (re-negotiation) rebuilds the cached builder.
	[ThreadStatic]
	private static IDrawingFactory? _rectangleBuilderFactory;
	[ThreadStatic]
	private static IPrimitiveGeometryBuilder? _rectangleBuilder;

	/// <summary>Creates a rectangular geometry using (and reusing) a cached primitive builder.</summary>
	public static IGeometry CreateRectangleGeometry(this IDrawingFactory factory, Rect rect)
	{
		if (_rectangleBuilder is null || !ReferenceEquals(_rectangleBuilderFactory, factory))
		{
			_rectangleBuilderFactory = factory;
			_rectangleBuilder = factory.CreatePrimitiveGeometryBuilder();
		}

		// AddRectangle + Build is atomic (no reentrancy between them), so the shared builder is safe to reuse.
		_rectangleBuilder.AddRectangle(rect);
		return _rectangleBuilder.Build();
	}
}
