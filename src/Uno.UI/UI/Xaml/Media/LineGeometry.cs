using System.Numerics;
using Uno.UI.Composition.Drawing;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media;

partial class LineGeometry
{
	internal override IGeometry GetGeometry() => CompositionGeometry.BuildLineGeometry(StartPoint.ToVector2(), EndPoint.ToVector2());
}
