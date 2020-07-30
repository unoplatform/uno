using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using Windows.Foundation;
#if XAMARIN_IOS
using UIKit;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class RectangleGeometry : Geometry
	{
		public RectangleGeometry() { }

		#region Rect DependencyProperty

		public Rect Rect
		{
			get { return (Rect)this.GetValue(RectProperty); }
			set { this.SetValue(RectProperty, value); }
		}

		public static DependencyProperty RectProperty { get ; } =
			DependencyProperty.Register(
				"Rect",
				typeof(Rect), typeof(RectangleGeometry),
				new FrameworkPropertyMetadata(
					null,
					(s, e) => ((RectangleGeometry)s)?.OnRectChanged(e)
				)
			);

		private void OnRectChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region Geometry implementation (not implemented)

#if XAMARIN_ANDROID
		public override Android.Graphics.Path ToPath()
		{
			throw new NotImplementedException();
		}
#endif

		public override void Dispose()
		{
			throw new NotImplementedException();
		}

#if XAMARIN_IOS
		public override UIImage ToNativeImage()
		{
			throw new NotImplementedException();
		}

		public override UIImage ToNativeImage(CoreGraphics.CGSize targetSize, UIColor color = null, Thickness margin = default(Thickness))
		{
			throw new NotImplementedException();
		}

		public override CoreGraphics.CGPath ToCGPath()
		{
			throw new NotImplementedException();
		}
#endif

		#endregion
	}
}
