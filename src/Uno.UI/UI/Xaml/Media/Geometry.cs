using System.Drawing;
using Windows.UI.Xaml;
using System;
using System.ComponentModel;
using Uno.Media;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif __MACOS__
using AppKit;
using CoreGraphics;
using UIImage = AppKit.NSImage;
using UIColor = AppKit.NSColor;
using UIGraphics = AppKit.NSGraphics;
using Path = AppKit.NSBezierPath;
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

		public static implicit operator Geometry(string data)
		{
#if __WASM__
			return new GeometryData(data);
#else
			return Parsers.ParseGeometry(data);
#endif
		}

		#region Transform

		public Transform Transform
		{
			get => (Transform)this.GetValue(TransformProperty);
			set => this.SetValue(TransformProperty, value);
		}

		public static DependencyProperty TransformProperty { get ; } =
			DependencyProperty.Register(
				"Transform",
				typeof(Transform),
				typeof(Geometry),
				new FrameworkPropertyMetadata(default(Transform))
			);

		#endregion

#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS || __MACOS__
		public static implicit operator UIImage (Geometry g)
		{
			return g.ToNativeImage();
		}

		public static implicit operator CGPath(Geometry g)
		{
			return g.ToCGPath();
		}

		public virtual UIImage ToNativeImage() { throw new InvalidOperationException(); }

		public virtual UIImage ToNativeImage(CGSize targetSize, UIColor color = default(UIColor), Thickness margin = default(Thickness)) { throw new InvalidOperationException(); }

		public virtual CGPath ToCGPath() { throw new InvalidOperationException(); }

#elif XAMARIN_ANDROID
		public virtual Path ToPath() { throw new InvalidOperationException(); }
#endif
		public virtual void Dispose() { throw new InvalidOperationException(); }
	}
}
