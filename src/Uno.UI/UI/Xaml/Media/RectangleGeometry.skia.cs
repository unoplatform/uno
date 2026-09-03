using System.Numerics;
using Uno.UI.Composition.Drawing;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media
{
	partial class RectangleGeometry
	{
		internal override IGeometry GetGeometry() =>
			CompositionGeometry.BuildRectangleGeometry(offset: new Vector2((float)Rect.X, (float)Rect.Y), size: new Vector2((float)Rect.Width, (float)Rect.Height));
	}
}
