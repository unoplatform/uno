using System.Drawing;
using System.Globalization;
using Windows.UI.Xaml;
using System;
using System.ComponentModel;
using Uno.Media;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.Windows.Foundation.Rect;
using nfloat = System.Single;
using nint = System.Int32;
using CGPoint = System.Drawing.PointF;
using CGSize = System.Drawing.Windows.Foundation.Size;
#elif XAMARIN_ANDROID
using Android.Graphics;
#endif

namespace Windows.UI.Xaml.Media
{
	[TypeConverter(typeof(GeometryConverter))]
	public partial class Geometry : DependencyObject, IDisposable
	{
		public Geometry()
		{
			InitializeBinder();
		}

		#region Transform

		public Transform Transform
		{
			get => (Transform)this.GetValue(TransformProperty);
			set => this.SetValue(TransformProperty, value);
		}

		public static readonly DependencyProperty TransformProperty =
			DependencyProperty.Register(
				"Transform",
				typeof(Transform),
				typeof(Geometry),
				new FrameworkPropertyMetadata(default(Transform))
			);

		#endregion

#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS
		public static implicit operator UIImage (Geometry g)
		{
			return g.ToUIImage();
		}

		public static implicit operator CGPath(Geometry g)
		{
			return g.ToCGPath();
		}

		public virtual UIImage ToUIImage() { throw new InvalidOperationException(); }

		public virtual UIImage ToUIImage(CGSize targetSize, UIColor color = default(UIColor), Thickness margin = default(Thickness)) { throw new InvalidOperationException(); }

		public virtual CGPath ToCGPath() { throw new InvalidOperationException(); }

#elif XAMARIN_ANDROID
		public virtual Path ToPath() { throw new InvalidOperationException(); }
#endif
		public virtual void Dispose() { throw new InvalidOperationException(); }
	}
}