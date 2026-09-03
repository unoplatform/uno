using System.Numerics;
using Uno.UI.Composition.Drawing;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media;

partial class EllipseGeometry
{
	internal override IGeometry GetGeometry() => CompositionGeometry.BuildEllipseGeometry(Center.ToVector2(), new Vector2((float)RadiusX, (float)RadiusY));
}
