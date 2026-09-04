using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;

#if __SKIA__
using Microsoft.UI.Composition;
using Path = SkiaSharp.SKPath;
using SkiaSharp;
using Uno.UI.UI.Xaml.Media;
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

		internal void Close(Path bezierPath_)
		{
			bezierPath = bezierPath_;
		}

#if __SKIA__
		internal override SKPath GetSKPath()
		{
			bezierPath.FillType = FillRule.ToSkiaFillType();
			return bezierPath;
		}

		private protected override Windows.Foundation.Rect ComputeBounds()
		{
			if (bezierPath is null)
			{
				return default;
			}

			var path = GetSKPath();
			if (path.IsEmpty)
			{
				return default;
			}

			var b = path.Bounds;
			var rect = new Windows.Foundation.Rect(b.Left, b.Top, b.Width, b.Height);
			return Transform is { } transform ? transform.TransformBounds(rect) : rect;
		}
#endif

		#region implemented abstract members of Geometry

		public override void Dispose()
		{
		}

		#endregion
	}
}
