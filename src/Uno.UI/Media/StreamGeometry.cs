using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;

#if __SKIA__
using Microsoft.UI.Composition;
using Path = Uno.UI.Composition.Drawing.IGeometry;
using Uno.UI.Composition.Drawing;
#else
using Path = System.Object;
#endif

namespace Uno.Media
{
	[TypeConverter(typeof(GeometryConverter))]
	public sealed partial class StreamGeometry : Geometry
	{
		Path bezierPath;

		public FillRule FillRule { get; set; }

		public StreamGeometryContext Open()
		{
			return new PathStreamGeometryContext(this);
		}

#if __SKIA__
		private IPathBuilder _pendingBuilder;

		// The winding rule is baked in at Build time, so the build waits for the first read: the rule is only
		// known from the markup after the context has closed (Uno.Media.Parsers, and any caller assigning
		// FillRule after its using block), and a built geometry can no longer be re-ruled.
		internal void Close(IPathBuilder builder)
		{
			_pendingBuilder = builder;
			bezierPath = null;
		}

		private Path EnsureGeometry()
		{
			if (bezierPath is null && _pendingBuilder is { } builder)
			{
				builder.FillRule = FillRule == FillRule.EvenOdd ? GeometryFillRule.EvenOdd : GeometryFillRule.NonZero;
				bezierPath = builder.Build();
				_pendingBuilder = null;
			}

			return bezierPath;
		}

		internal override IGeometry GetGeometry() => EnsureGeometry();

		private protected override Windows.Foundation.Rect ComputeBounds()
		{
			if (EnsureGeometry() is not { IsEmpty: false } geometry)
			{
				return default;
			}

			var rect = geometry.Bounds;
			return Transform is { } transform ? transform.TransformBounds(rect) : rect;
		}
#else
		internal void Close(Path bezierPath_)
		{
			bezierPath = bezierPath_;
		}
#endif

		#region implemented abstract members of Geometry

		public override void Dispose()
		{
		}

		#endregion
	}
}
