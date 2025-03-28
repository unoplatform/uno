using System.Numerics;
using SkiaSharp;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media;

partial class EllipseGeometry
{
	internal override SKPath GetSKPath() => CompositionGeometry.BuildEllipseGeometry(Center.ToVector2(), new Vector2((float)RadiusX, (float)RadiusY));
}
