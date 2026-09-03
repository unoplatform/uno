#nullable enable

using System;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Convenience helpers layered over <see cref="IGeometryFactory"/>'s primitive-geometry builder, so common
/// shapes don't need a dedicated factory method.
/// </summary>
internal static class GeometryFactoryExtensions
{
	// Rectangle geometry is the most common shape (per-visual/per-frame clips and bounds), so reuse a cached builder
	// (Build() resets it). Thread-static because rendering can happen off the UI thread; keyed on the factory so a
	// backend swap rebuilds the cached builder.
	[ThreadStatic]
	private static IGeometryFactory? _rectangleBuilderFactory;
	[ThreadStatic]
	private static IPrimitiveGeometryBuilder? _rectangleBuilder;

	/// <summary>Creates a rectangular geometry using (and reusing) a cached primitive builder.</summary>
	public static IGeometry CreateRectangleGeometry(this IGeometryFactory factory, Rect rect)
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
