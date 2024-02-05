#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Interop;
using Windows.Graphics.Interop.Direct2D;

namespace Microsoft.Graphics.Canvas.Geometry;

internal class CanvasPathBuilder : IDisposable
{
	private List<CompositionPathCommand> _commands = new List<CompositionPathCommand>();

	public CanvasPathBuilder(ICanvasResourceCreator? resourceCreator) { }

	internal List<CompositionPathCommand> Commands => _commands;

	public void AddArc(Vector2 centerPoint, float radiusX, float radiusY, float startAngle, float sweepAngle)
	{
		bool isFullCircle = MathF.Abs(sweepAngle) >= (MathF.PI * 2.0f) - float.Epsilon;

		if (isFullCircle)
		{
			sweepAngle = (sweepAngle < 0) ? -MathF.PI : MathF.PI;
		}

		Point startPoint = new()
		{
			X = centerPoint.X + MathF.Cos(startAngle) * radiusX,
			Y = centerPoint.Y + MathF.Sin(startAngle) * radiusY
		};

		Point endPoint = new()
		{
			X = centerPoint.X + Math.Cos(startAngle + sweepAngle) * radiusX,
			Y = centerPoint.Y + MathF.Sin(startAngle + sweepAngle) * radiusY
		};

		D2D1ArcSegment segment = new D2D1ArcSegment()
		{
			Point = endPoint,
			Size = new(radiusX, radiusY),
			RotationAngle = 0,
			SweepDirection = (sweepAngle >= 0) ? D2D1SweepDirection.Clockwise : D2D1SweepDirection.CounterClockwise,
			ArcSize = (MathF.Abs(sweepAngle) > MathF.PI) ? D2D1ArcSize.Large : D2D1ArcSize.Small
		};

		_commands.Add(CompositionPathCommand.Create(startPoint));
		_commands.Add(CompositionPathCommand.Create(segment));

		if (isFullCircle)
		{
			segment.Point = startPoint;
			_commands.Add(CompositionPathCommand.Create(segment));
		}
	}

	public void AddArc(Vector2 endPoint, float radiusX, float radiusY, float rotationAngle, CanvasSweepDirection sweepDirection, CanvasArcSize arcSize)
	{
		D2D1ArcSegment segment = new D2D1ArcSegment()
		{
			Point = endPoint.ToPoint(),
			Size = new(radiusX, radiusY),
			RotationAngle = Uno.Extensions.MathEx.ToDegree(rotationAngle),
			SweepDirection = (D2D1SweepDirection)sweepDirection,
			ArcSize = (D2D1ArcSize)arcSize
		};

		_commands.Add(CompositionPathCommand.Create(segment));
	}

	public void AddCubicBezier(Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint)
	{
		D2D1BezierSegment segment = new D2D1BezierSegment()
		{
			Point1 = controlPoint1.ToPoint(),
			Point2 = controlPoint2.ToPoint(),
			Point3 = endPoint.ToPoint()
		};

		_commands.Add(CompositionPathCommand.Create(segment));
	}

	// TODO: Implement rest of supported CanvasGeometry types, we might need something like ID2D1Geometry::Simplify for that
	public void AddGeometry(CanvasGeometry geometry)
	{
		if (geometry is IGeometrySource2DInterop geometryInterop
		&& geometryInterop.GetGeometry() is ICompositionPathCommandsProvider commandsProvider
		&& commandsProvider.Commands is List<CompositionPathCommand> { Count: > 0 } commands)
		{
			Commands.AddRange(commands);
		}
	}

	public void AddLine(Vector2 endPoint)
	{
		_commands.Add(CompositionPathCommand.Create(endPoint.ToPoint()));
	}

	public void AddLine(float x, float y)
	{
		AddLine(new(x, y));
	}

	public void AddQuadraticBezier(Vector2 controlPoint, Vector2 endPoint)
	{
		D2D1QuadraticBezierSegment segment = new D2D1QuadraticBezierSegment()
		{
			Point1 = controlPoint.ToPoint(),
			Point2 = endPoint.ToPoint()
		};

		_commands.Add(CompositionPathCommand.Create(segment));
	}

	public void BeginFigure(Vector2 startPoint, CanvasFigureFill figureFill)
	{
		_commands.Add(CompositionPathCommand.Create(startPoint.ToPoint(), (D2D1FigureBegin)figureFill));
	}

	public void BeginFigure(float startX, float startY, CanvasFigureFill figureFill)
	{
		BeginFigure(new(startX, startY), figureFill);
	}

	public void BeginFigure(Vector2 startPoint)
	{
		BeginFigure(startPoint, (CanvasFigureFill)D2D1FigureBegin.Hollow);
	}

	public void BeginFigure(float startX, float startY)
	{
		BeginFigure(new(startX, startY), (CanvasFigureFill)D2D1FigureBegin.Hollow);
	}

	public void EndFigure(CanvasFigureLoop figureLoop)
	{
		_commands.Add(CompositionPathCommand.Create((D2D1FigureEnd)figureLoop));
	}

	public void SetFilledRegionDetermination(CanvasFilledRegionDetermination filledRegionDetermination)
	{
		_commands.Add(CompositionPathCommand.Create((D2D1FillMode)filledRegionDetermination));
	}

	public void SetSegmentOptions(CanvasFigureSegmentOptions figureSegmentOptions)
	{
		_commands.Add(CompositionPathCommand.Create((D2D1PathSegment)figureSegmentOptions));
	}

	public void Dispose() { }
}
