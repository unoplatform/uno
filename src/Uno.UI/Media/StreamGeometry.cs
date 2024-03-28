using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.UI.Extensions;

#if __IOS__
using Foundation;
using UIKit;
using CoreGraphics;
using Path = UIKit.UIBezierPath;
using ObjCRuntime;
#elif __MACOS__
using AppKit;
using CoreGraphics;
using UIImage = AppKit.NSImage;
using UIColor = AppKit.NSColor;
using UIGraphics = AppKit.NSGraphics;
using Path = AppKit.NSBezierPath;
using ObjCRuntime;
#elif __ANDROID__
using Android.Graphics;
#elif __SKIA__
using Path = Windows.UI.Composition.SkiaGeometrySource2D;
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
		internal override Path GetGeometrySource2D()
		{
			bezierPath.Geometry.FillType = FillRule.ToSkiaFillType();
			return bezierPath;
		}

		internal override SKPath GetSKPath()
		{
			bezierPath.Geometry.FillType = FillRule.ToSkiaFillType();
			return bezierPath.Geometry;
		}
#endif

#if __IOS__ || __MACOS__
		public override UIImage ToNativeImage()
		{
			return (bezierPath == null) ? null : ToNativeImage(bezierPath.Bounds.Size);
		}

		public override UIImage ToNativeImage(CGSize targetSize, UIColor color = default(UIColor), Thickness margin = default(Thickness))
		{
			if (bezierPath == null)
			{
				return null;
			}

			CGSize imageSize = bezierPath.Bounds.Size;
			if ((int)imageSize.Width <= 0 && (int)imageSize.Height <= 0)
			{
				return null;
			}

			if (nfloat.IsNaN(targetSize.Width) && nfloat.IsNaN(targetSize.Height))
			{
				targetSize = new CGSize(0.85f * imageSize.Width, 0.85f * imageSize.Height);
			}
			else if (nfloat.IsNaN(targetSize.Width))
			{
				targetSize.Width = (imageSize.Width / imageSize.Height) * targetSize.Height;
			}
			else if (nfloat.IsNaN(targetSize.Height))
			{
				targetSize.Height = (imageSize.Height / imageSize.Width) * targetSize.Width;
			}

			if ((int)targetSize.Width <= 0 && (int)targetSize.Height <= 0)
			{
				return null;
			}

			nfloat scale = 1f;
			var translate = CGPoint.Empty;

			if (!imageSize.Equals(targetSize))
			{
				var scaleX = targetSize.Width / imageSize.Width;
				var scaleY = targetSize.Height / imageSize.Height;
				scale = (nfloat)Math.Min(targetSize.Width / imageSize.Width, targetSize.Height / imageSize.Height);

				if (scaleX > scaleY)
				{
					translate.X = (targetSize.Width - (imageSize.Width * scale)) * 0.5f;
				}
				else if (scaleX < scaleY)
				{
					translate.Y = (targetSize.Height - (imageSize.Height * scale)) * 0.5f;
				}
			}

			UIImage image;

#if __IOS__
			UIGraphics.BeginImageContextWithOptions(targetSize, false, 0);
			using (var context = UIGraphics.GetCurrentContext())
			{
				context.TranslateCTM(translate.X, translate.Y);
				context.ScaleCTM(scale, scale);

				context.InterpolationQuality = CGInterpolationQuality.High;
				context.SetFillColor((color ?? UIColor.Black).CGColor);
				bezierPath.UsesEvenOddFillRule = (FillRule == FillRule.EvenOdd);
				bezierPath.Fill();

				image = UIGraphics.GetImageFromCurrentImageContext();
				UIGraphics.EndImageContext();
			}
#elif __MACOS__
			// macOS TODO
			image = null;
#endif

			return image;
		}

		public override CGPath ToCGPath()
			=> bezierPath != null ? bezierPath.ToCGPath() : new CGPath();
#endif

		#region implemented abstract members of Geometry

		public override void Dispose()
		{
#if XAMARIN
			if (bezierPath != null)
			{
				bezierPath.Dispose();
			}
#endif
		}

		#endregion
	}
}
