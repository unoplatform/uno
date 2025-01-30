using System.Numerics;
using SkiaSharp;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media;

partial class EllipseGeometry
{
	internal override SKPath GetSKPath() => CompositionGeometry.BuildEllipseGeometry(Center.ToVector2(), new Vector2((float)RadiusX, (float)RadiusY));
}
