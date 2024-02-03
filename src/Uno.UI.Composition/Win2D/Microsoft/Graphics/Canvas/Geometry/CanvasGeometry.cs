#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Uno.UI.Composition;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Interop;
using Windows.Graphics.Interop.Direct2D;

namespace Microsoft.Graphics.Canvas.Geometry;

internal class CanvasGeometry : IDisposable, IGeometrySource2D, IGeometrySource2DInterop
{
	private ID2D1Geometry _geometry;

	private CanvasGeometry(ID2D1Geometry geometry) => _geometry = geometry ?? throw new ArgumentNullException("geometry");

	public ID2D1Geometry GetGeometry() => _geometry;

	public ID2D1Geometry? TryGetGeometryUsingFactory(object factory) => null;

	public void Dispose() { }


	public static CanvasGeometry CreateEllipse(ICanvasResourceCreator resourceCreator, Vector2 centerPoint, float radiusX, float radiusY) => new(new CanvasEllipseGeometry(new() { Point = centerPoint.ToPoint(), RadiusX = radiusX, RadiusY = radiusY }));

	public static CanvasGeometry CreateEllipse(ICanvasResourceCreator resourceCreator, float x, float y, float radiusX, float radiusY) => new(new CanvasEllipseGeometry(new() { Point = new(x, y), RadiusX = radiusX, RadiusY = radiusY }));

	public static CanvasGeometry CreateCircle(ICanvasResourceCreator resourceCreator, Vector2 centerPoint, float radius) => CreateEllipse(resourceCreator, centerPoint, radius, radius);

	public static CanvasGeometry CreateCircle(ICanvasResourceCreator resourceCreator, float x, float y, float radius) => CreateEllipse(resourceCreator, x, y, radius, radius);

	public static CanvasGeometry CreatePath(CanvasPathBuilder pathBuilder) => new(new CanvasPathGeometry(pathBuilder.Commands));

	public static CanvasGeometry CreatePolygon(ICanvasResourceCreator resourceCreator, Vector2[] points)
	{
		CanvasPathBuilder pathBuilder = new(resourceCreator);

		if (points.Length > 0)
		{
			pathBuilder.BeginFigure(points[0], CanvasFigureFill.Default);

			foreach (var point in points)
			{
				pathBuilder.AddLine(point);
			}

			pathBuilder.EndFigure(CanvasFigureLoop.Closed);
		}

		return CreatePath(pathBuilder);
	}

	public static CanvasGeometry CreateRectangle(ICanvasResourceCreator resourceCreator, Rect rect) => new(new CanvasRectangleGeometry(rect));

	public static CanvasGeometry CreateRectangle(ICanvasResourceCreator resourceCreator, float x, float y, float w, float h) => new(new CanvasRectangleGeometry(new(x, y, w, h)));

	public static CanvasGeometry CreateRoundedRectangle(ICanvasResourceCreator resourceCreator, Rect rect, float radiusX, float radiusY) => new(new CanvasRoundedRectangleGeometry(new() { Rect = rect, RadiusX = radiusX, RadiusY = radiusY }));

	public static CanvasGeometry CreateRoundedRectangle(ICanvasResourceCreator resourceCreator, float x, float y, float w, float h, float radiusX, float radiusY) => new(new CanvasRoundedRectangleGeometry(new() { Rect = new(x, y, w, h), RadiusX = radiusX, RadiusY = radiusY }));

	private class CanvasPathGeometry : ID2D1PathGeometry, ICompositionPathCommandsProvider
	{
		private List<CompositionPathCommand> _commands;

		public CanvasPathGeometry(List<CompositionPathCommand> commands) => _commands = commands ?? throw new ArgumentNullException("commands");

		public List<CompositionPathCommand> Commands => _commands;

		public uint GetFigureCount() => (uint)_commands.Count(x => x.Type is CompositionPathCommandType.EndFigure);

		public uint GetSegmentCount() => (uint)_commands.Count(x =>
			x.Type is not
				(CompositionPathCommandType.Close or
				CompositionPathCommandType.SetSegmentFlags or
				CompositionPathCommandType.SetFillMode or
				CompositionPathCommandType.BeginFigure or
				CompositionPathCommandType.EndFigure));

		public ID2D1GeometrySink Open() => throw new NotImplementedException();

		public void Stream(ID2D1GeometrySink geometrySink) => throw new NotImplementedException(); // TODO
	}

	private class CanvasRectangleGeometry : ID2D1RectangleGeometry
	{
		private Rect _rect;

		public CanvasRectangleGeometry(Rect rect) => _rect = rect;

		public Rect GetRect() => _rect;
	}

	private class CanvasRoundedRectangleGeometry : ID2D1RoundedRectangleGeometry
	{
		private D2D1RoundedRect _rect;

		public CanvasRoundedRectangleGeometry(D2D1RoundedRect rect) => _rect = rect;

		public D2D1RoundedRect GetRoundedRect() => _rect;
	}

	private class CanvasEllipseGeometry : ID2D1EllipseGeometry
	{
		private D2D1Ellipse _ellipse;

		public CanvasEllipseGeometry(D2D1Ellipse ellipse) => _ellipse = ellipse;

		public D2D1Ellipse GetEllipse() => _ellipse;
	}
}
