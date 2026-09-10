using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace Windows.Graphics.Interop.Direct2D;

[Guid("2CD906A2-12E2-11DC-9FED-001143A055F9")]
internal interface ID2D1PathGeometry : ID2D1Geometry
{
	ID2D1GeometrySink Open();

	void Stream(ID2D1GeometrySink geometrySink);

	uint GetSegmentCount();

	uint GetFigureCount();
}
