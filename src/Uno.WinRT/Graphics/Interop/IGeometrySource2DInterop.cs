using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Interop.Direct2D;
using Windows.Foundation;

namespace Windows.Graphics.Interop;

[Guid("0657AF73-53FD-47CF-84FF-C8492D2A80A3")]
internal interface IGeometrySource2DInterop
{
	ID2D1Geometry GetGeometry();

	ID2D1Geometry TryGetGeometryUsingFactory(/*ID2D1Factory*/ object factory);
}
