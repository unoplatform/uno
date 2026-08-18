using Windows.Foundation;
using System.Runtime.InteropServices;

namespace Windows.Graphics.Interop.Direct2D;

[Guid("2CD906A2-12E2-11DC-9FED-001143A055F9")]
internal interface ID2D1GeometrySink : ID2D1SimplifiedGeometrySink
{
	void AddLine(Point point);

	void AddBezier(D2D1BezierSegment bezier);

	void AddQuadraticBezier(D2D1QuadraticBezierSegment bezier);

	void AddQuadraticBeziers(D2D1QuadraticBezierSegment[] beziers);

	void AddArc(D2D1ArcSegment arc);
}
