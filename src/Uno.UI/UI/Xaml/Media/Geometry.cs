using System.Drawing;
using Windows.UI.Xaml;
using System;
using System.ComponentModel;
using Uno.Media;
using Windows.Foundation;

using Rect = Windows.Foundation.Rect;

#if __IOS__
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
#elif __ANDROID__
using Android.Graphics;
#endif

namespace Windows.UI.Xaml.Media
{
	[TypeConverter(typeof(GeometryConverter))]
	public partial class Geometry : DependencyObject, IDisposable
	{
		internal Geometry()
		{
			InitializeBinder();
		}

		internal event Action GeometryChanged;

		private protected void RaiseGeometryChanged()
			=> GeometryChanged?.Invoke();

		public static implicit operator Geometry(string data)
		{
#if __WASM__
			return new GeometryData(data);
#else
			return Parsers.ParseGeometry(data);
#endif
		}

		public Rect Bounds => ComputeBounds();

		private protected virtual Rect ComputeBounds()
		{
			throw new NotImplementedException($"Bounds property is not implemented on {GetType().Name}.");
		}

		#region Transform

		public Transform Transform
		{
			get => (Transform)this.GetValue(TransformProperty);
			set => this.SetValue(TransformProperty, value);
		}

		public static DependencyProperty TransformProperty { get; } =
			DependencyProperty.Register(
				"Transform",
				typeof(Transform),
				typeof(Geometry),
				new FrameworkPropertyMetadata(default(Transform), propertyChangedCallback: (s, args) => ((Geometry)s).OnTransformChanged(args))
			);

		private void OnTransformChanged(DependencyPropertyChangedEventArgs args)
		{
			RaiseGeometryChanged();

			if (args.OldValue is Transform oldValue)
			{
				oldValue.Changed -= OnTransformSubChanged;
			}

			if (args.NewValue is Transform newValue)
			{
				newValue.Changed += OnTransformSubChanged;
			}
		}

		private void OnTransformSubChanged(object sender, EventArgs e)
			=> RaiseGeometryChanged();

		#endregion

#if __IOS__ || __MACOS__
		public static implicit operator UIImage(Geometry g)
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

#elif __ANDROID__
		public virtual Path ToPath() { throw new InvalidOperationException(); }
#endif
		public virtual void Dispose() { throw new InvalidOperationException(); }
	}
}
