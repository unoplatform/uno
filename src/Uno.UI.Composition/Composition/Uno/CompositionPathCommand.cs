#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Interop.Direct2D;

namespace Uno.UI.Composition;

internal class CompositionPathCommand
{
	public CompositionPathCommandType Type { get; private set; }

	public object[]? Parameters { get; private set; }

	private CompositionPathCommand(CompositionPathCommandType type, object[]? parameters = null)
	{
		Type = type;
		Parameters = parameters;
	}

	public static CompositionPathCommand Create(D2D1FillMode fillMode) => new(CompositionPathCommandType.SetFillMode, [fillMode]);

	public static CompositionPathCommand Create(D2D1PathSegment vertexFlags) => new(CompositionPathCommandType.SetSegmentFlags, [vertexFlags]);

	public static CompositionPathCommand Create(Point startPoint, D2D1FigureBegin figureBegin) => new(CompositionPathCommandType.BeginFigure, [startPoint, figureBegin]);

	public static CompositionPathCommand Create(Point point) => new(CompositionPathCommandType.AddLine, [point]);

	public static CompositionPathCommand Create(Point[] points) => new(CompositionPathCommandType.AddLines, [points]);

	public static CompositionPathCommand Create(D2D1BezierSegment bezier) => new(CompositionPathCommandType.AddBezier, [bezier]);

	public static CompositionPathCommand Create(D2D1BezierSegment[] beziers) => new(CompositionPathCommandType.AddBeziers, [beziers]);

	public static CompositionPathCommand Create(D2D1QuadraticBezierSegment bezier) => new(CompositionPathCommandType.AddQuadraticBezier, [bezier]);

	public static CompositionPathCommand Create(D2D1QuadraticBezierSegment[] beziers) => new(CompositionPathCommandType.AddQuadraticBeziers, [beziers]);

	public static CompositionPathCommand Create(D2D1ArcSegment arc) => new(CompositionPathCommandType.AddArc, [arc]);

	public static CompositionPathCommand Create(D2D1FigureEnd figureEnd) => new(CompositionPathCommandType.EndFigure, [figureEnd]);

	public static CompositionPathCommand Create() => new(CompositionPathCommandType.Close);
}
