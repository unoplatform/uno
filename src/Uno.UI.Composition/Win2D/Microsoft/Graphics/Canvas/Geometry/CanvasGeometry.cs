#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.Composition;
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


	public static CanvasGeometry CreatePath(CanvasPathBuilder pathBuilder) => new(new CanvasPathGeometry(pathBuilder.Commands));

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
}
