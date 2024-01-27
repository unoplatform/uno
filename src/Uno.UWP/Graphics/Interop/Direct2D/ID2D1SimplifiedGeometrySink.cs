using Windows.Foundation;
using System.Runtime.InteropServices;

namespace Windows.Graphics.Interop.Direct2D;

[Guid("2CD906A2-12E2-11DC-9FED-001143A055F9")]
internal interface ID2D1SimplifiedGeometrySink
{
	void SetFillMode(D2D1FillMode fillMode);

	void SetSegmentFlags(D2D1PathSegment vertexFlags);

	void BeginFigure(Point startPoint, D2D1FigureBegin figureBegin);

	void AddLines(Point[] points);

	void AddBeziers(D2D1BezierSegment[] beziers);

	void EndFigure(D2D1FigureEnd figureEnd);

	void Close();
}
