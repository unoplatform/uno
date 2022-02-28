#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Android.Graphics.Drawables.Shapes;
using System.Linq;
using Uno.Disposables;
using Microsoft.UI.Xaml.Media;
using Uno.UI;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using Android.Views;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#else
using Color = System.Drawing.Color;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Border
	{
		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayoutCore(changed, left, top, right, bottom, localIsLayoutRequested);

			UpdateBorder();
		}

		partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
		{
			if (previousValue != null)
			{
				RemoveView(previousValue);
			}

			if (newValue != null)
			{
				AddView(newValue);
			}
		}

		protected override void OnDraw(Android.Graphics.Canvas canvas)
		{
			AdjustCornerRadius(canvas, CornerRadius);
		}

		private void UpdateBorder()
		{
			UpdateBorder(false);
		}

		private void UpdateBorder(bool willUpdateMeasures)
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					Background,
					BackgroundSizing,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					Padding,
					willUpdateMeasures
				);
			}
		}

		partial void OnBorderBrushChangedPartial()
		{
			UpdateBorder();
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateBorder();
		}

		partial void OnBackgroundSizingChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			UpdateBorder();
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder(true);
		}

		partial void OnCornerRadiusUpdatedPartial(CornerRadius oldValue, CornerRadius newValue)
		{
			UpdateBorder();
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => !(Child is UIElement ue) || ue.RenderTransform == null;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
	}
}
#endif
