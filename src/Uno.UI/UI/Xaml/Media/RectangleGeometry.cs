﻿using System;
using Windows.Foundation;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml.Media
{
	public partial class RectangleGeometry : Geometry
	{
		public RectangleGeometry()
		{
			InitPartials();
		}

		partial void InitPartials();

		#region Rect DependencyProperty

		public Rect Rect
		{
			get => (Rect)this.GetValue(RectProperty);
			set => this.SetValue(RectProperty, value);
		}

		public static DependencyProperty RectProperty { get; } =
			DependencyProperty.Register(
				"Rect",
				typeof(Rect), typeof(RectangleGeometry),
				new FrameworkPropertyMetadata(
					default(Rect),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: OnRectChanged));

		private static void OnRectChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			((RectangleGeometry)dependencyObject).RaiseGeometryChanged();
		}

		#endregion

		#region Geometry implementation (not implemented)

#if __ANDROID__
		public override APath ToPath()
		{
			throw new NotImplementedException();
		}
#endif

		public override void Dispose()
		{
			throw new NotImplementedException();
		}

#if __APPLE_UIKIT__
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

		private protected override Rect ComputeBounds()
		{
			if (Transform is { } transform)
			{
				return transform.TransformBounds(Rect);
			}
			else
			{
				return Rect;
			}
		}
	}
}
