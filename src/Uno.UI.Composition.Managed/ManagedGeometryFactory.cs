#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-free <see cref="IGeometryFactory"/> built on the managed geometry engine
/// (<see cref="ManagedPathBuilder"/> → <see cref="ManagedGeometry"/>). Register it (e.g. via the host builder's
/// <c>.GeometryFactory</c>) for a Skia-less setup: any render backend consumes the neutral <see cref="IGeometry"/>
/// it produces (the Skia backend converts it to an <c>SKPath</c>; WebGPU flattens it).
/// </summary>
public sealed class ManagedGeometryFactory : IGeometryFactory
{
	public IPathBuilder CreatePathBuilder() => new ManagedPathBuilder();

	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => new ManagedPathBuilder();
}
