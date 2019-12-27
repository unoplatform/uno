#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Android.Graphics.Drawables.Shapes;
using System.Linq;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
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

namespace Windows.UI.Xaml.Controls
{
	public partial class Border
	{
		private SerialDisposable _brushChanged = new SerialDisposable();
		private BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();

		public Border()
		{
			this.RegisterLoadActions(UpdateBorder, () => _borderRenderer.Clear());
		}

		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayoutCore(changed, left, top, right, bottom);

			UpdateBorder();
		}

		partial void OnChildChangedPartial(View previousValue, View newValue)
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
				_borderRenderer.UpdateLayers(
					this,
					Background,
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
			// Don't call base, just update the filling color.
			_brushChanged.Disposable = Brush.AssignAndObserveBrush(e.NewValue as Brush, c => UpdateBorder(), UpdateBorder);

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
