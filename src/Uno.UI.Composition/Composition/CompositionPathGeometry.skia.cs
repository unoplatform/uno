#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Interop.Direct2D;
using static Uno.FoundationFeatureConfiguration;

namespace Microsoft.UI.Composition;

public partial class CompositionPathGeometry : CompositionGeometry, ID2D1GeometrySink
{
	private SkiaGeometrySource2D? _geometrySource2D;
	private List<CompositionPathCommand> _commands = new();

	internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

	private void InternalBuildGeometry()
	{
		SkiaGeometrySource2D? geometrySource = null;

		switch (Path?.GeometrySource.GetType())
		{
			case ID2D1RectangleGeometry rectangleGeometry:
				{
					var rect = rectangleGeometry.GetRect();
					geometrySource = new(BuildRectangleGeometry(rect.Location.ToVector2(), rect.Size.ToVector2()));
					break;
				}
			case ID2D1RoundedRectangleGeometry roundedRectangleGeometry:
				{
					var rect = roundedRectangleGeometry.GetRoundedRect();
					geometrySource = new(BuildRoundedRectangleGeometry(rect.Rect.Location.ToVector2(), rect.Rect.Size.ToVector2(), new(rect.RadiusX, rect.RadiusY)));
					break;
				}
			case ID2D1EllipseGeometry ellipseGeometry:
				{
					var ellipse = ellipseGeometry.GetEllipse();
					geometrySource = new(BuildEllipseGeometry(ellipse.Point.ToVector2(), new(ellipse.RadiusX, ellipse.RadiusY)));
					break;
				}
			case ID2D1PathGeometry pathGeometry:
				{
					geometrySource = InternalBuildPathGeometry(pathGeometry);
					break;
				}
			default:
				throw new InvalidOperationException($"Path geometry source type {Path?.GeometrySource.GetType()} is no supported");
		}

		_geometrySource2D?.Dispose();
		_geometrySource2D = geometrySource;
		_commands.Clear();
	}

	private SkiaGeometrySource2D? InternalBuildPathGeometry(ID2D1PathGeometry pathGeometry)
	{
		pathGeometry.Stream(this);

		SKPath path = new();
		foreach (var command in _commands)
		{
			switch (command.Type)
			{
				case CompositionPathCommandType.SetFillMode:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						path.FillType = ((D2D1FillMode)parameters[0]).ToSkia();
						break;
					}
				case CompositionPathCommandType.SetSegmentFlags:
					break; // TODO
				case CompositionPathCommandType.BeginFigure:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 2);
						var point = (Point)parameters[0];
						path.MoveTo(point.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddLine:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var point = (Point)parameters[0];
						path.LineTo(point.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddLines:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var points = (Point[])parameters[0];

						foreach (var point in points)
						{
							path.LineTo(point.ToSkia());
						}

						break;
					}
				case CompositionPathCommandType.AddBezier:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var bezier = (D2D1BezierSegment)parameters[0];
						path.CubicTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia(), bezier.Point3.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddBeziers:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var beziers = (D2D1BezierSegment[])parameters[0];

						foreach (var bezier in beziers)
						{
							path.CubicTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia(), bezier.Point3.ToSkia());
						}

						break;
					}
				case CompositionPathCommandType.AddQuadraticBezier:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var bezier = (D2D1QuadraticBezierSegment)parameters[0];
						path.QuadTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddQuadraticBeziers:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var beziers = (D2D1QuadraticBezierSegment[])parameters[0];

						foreach (var bezier in beziers)
						{
							path.QuadTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia());
						}

						break;
					}
				case CompositionPathCommandType.AddArc:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var arc = (D2D1ArcSegment)parameters[0];
						path.ArcTo(new((float)arc.Size.Width, (float)arc.Size.Height), arc.RotationAngle, arc.ArcSize.ToSkia(), arc.SweepDirection.ToSkia(), arc.Point.ToSkia());
						break;
					}
				case CompositionPathCommandType.EndFigure:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var end = (D2D1FigureEnd)parameters[0];

						if (end is D2D1FigureEnd.Closed)
						{
							path.Close();
						}

						break;
					}
				case CompositionPathCommandType.Close:
					break; // We don't actually have a sink to close, so we can ignore this
			}
		}

		return new SkiaGeometrySource2D(path);
	}

	private static object[] ValidateCommandParameters(CompositionPathCommand command, int expectedParameterCount)
	{
		if (command.Parameters is null || command.Parameters.Length != expectedParameterCount)
		{
			throw new InvalidOperationException("Unexpected path command parameters value");
		}

		return command.Parameters;
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		if (propertyName is nameof(Path))
		{
			_commands.Clear();
			InternalBuildGeometry();
		}

		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
	}

	void ID2D1GeometrySink.AddLine(Point point) => _commands.Add(CompositionPathCommand.Create(point));

	void ID2D1GeometrySink.AddBezier(D2D1BezierSegment bezier) => _commands.Add(CompositionPathCommand.Create(bezier));

	void ID2D1GeometrySink.AddQuadraticBezier(D2D1QuadraticBezierSegment bezier) => _commands.Add(CompositionPathCommand.Create(bezier));

	void ID2D1GeometrySink.AddQuadraticBeziers(D2D1QuadraticBezierSegment[] beziers) => _commands.Add(CompositionPathCommand.Create(beziers));

	void ID2D1GeometrySink.AddArc(D2D1ArcSegment arc) => _commands.Add(CompositionPathCommand.Create(arc));

	void ID2D1SimplifiedGeometrySink.SetFillMode(D2D1FillMode fillMode) => _commands.Add(CompositionPathCommand.Create(fillMode));

	void ID2D1SimplifiedGeometrySink.SetSegmentFlags(D2D1PathSegment vertexFlags) => _commands.Add(CompositionPathCommand.Create(vertexFlags));

	void ID2D1SimplifiedGeometrySink.BeginFigure(Point startPoint, D2D1FigureBegin figureBegin) => _commands.Add(CompositionPathCommand.Create(startPoint, figureBegin));

	void ID2D1SimplifiedGeometrySink.AddLines(Point[] points) => _commands.Add(CompositionPathCommand.Create(points));

	void ID2D1SimplifiedGeometrySink.AddBeziers(D2D1BezierSegment[] beziers) => _commands.Add(CompositionPathCommand.Create(beziers));

	void ID2D1SimplifiedGeometrySink.EndFigure(D2D1FigureEnd figureEnd) => _commands.Add(CompositionPathCommand.Create(figureEnd));

	void ID2D1SimplifiedGeometrySink.Close() => _commands.Add(CompositionPathCommand.Create());
}
