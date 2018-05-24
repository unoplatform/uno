using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
using Path = UIKit.UIBezierPath;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.Windows.Foundation.Rect;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.Windows.Foundation.Size;
using Path = MonoTouch.UIKit.UIBezierPath;
#elif XAMARIN_ANDROID
using Android.Graphics;
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

#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS
		public override UIImage ToUIImage ()
		{
			return (bezierPath == null) ? null : ToUIImage (bezierPath.Bounds.Size);
		}

		public override UIImage ToUIImage (CGSize targetSize, UIColor color = default(UIColor), Thickness margin = default(Thickness))
		{
			if (bezierPath == null) {
				return null;
			}

			CGSize imageSize = bezierPath.Bounds.Size;
			if ((int)imageSize.Width <= 0 && (int)imageSize.Height <= 0) {
				return null;
			}

			if (nfloat.IsNaN (targetSize.Width) && nfloat.IsNaN (targetSize.Height)) {
				targetSize = new CGSize(0.85f * imageSize.Width, 0.85f * imageSize.Height);
			} else if (nfloat.IsNaN (targetSize.Width)) {
				targetSize.Width = (imageSize.Width / imageSize.Height) * targetSize.Height;
			} else if (nfloat.IsNaN (targetSize.Height)) {
				targetSize.Height = (imageSize.Height / imageSize.Width) * targetSize.Width;
			}

			if ((int)targetSize.Width <= 0 && (int)targetSize.Height <= 0) {
				return null;
			}

			nfloat scale = 1f;
			var translate = CGPoint.Empty;

			if (!imageSize.Equals (targetSize)) {
				var scaleX = targetSize.Width / imageSize.Width;
				var scaleY = targetSize.Height / imageSize.Height;
				scale = (nfloat)Math.Min (targetSize.Width / imageSize.Width, targetSize.Height / imageSize.Height);

				if (scaleX > scaleY) {
					translate.X = (targetSize.Width - (imageSize.Width * scale)) * 0.5f;
				} else if (scaleX < scaleY) {
					translate.Y = (targetSize.Height - (imageSize.Height * scale)) * 0.5f;
				}
			}

			UIImage image;

			UIGraphics.BeginImageContextWithOptions (targetSize, false, 0);
			using (var context = UIGraphics.GetCurrentContext ()) {
				context.TranslateCTM (translate.X, translate.Y);
				context.ScaleCTM (scale, scale);

				context.InterpolationQuality = CGInterpolationQuality.High;
				context.SetFillColor ((color ?? UIColor.Black).CGColor);
				bezierPath.UsesEvenOddFillRule = (FillRule == FillRule.EvenOdd);
				bezierPath.Fill ();

				image = UIGraphics.GetImageFromCurrentImageContext ();
				UIGraphics.EndImageContext ();
			}

			return image;
		}

		public override CGPath ToCGPath ()
		{
			return(bezierPath != null) ?  new CGPath(bezierPath.CGPath) :  new CGPath();
		}
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
