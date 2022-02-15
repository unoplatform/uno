using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Android.Graphics.Drawables.Shapes;
using System.Linq;
using Uno.Disposables;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Helpers;

using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using Android.Views;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Border
	{
		private SerialDisposable _brushChanged = new SerialDisposable();
		private BorderLayerRenderer _borderRenderer;

		public Border()
		{
			_borderRenderer = new BorderLayerRenderer(this);

			this.RegisterLoadActions(UpdateBorder, () => _borderRenderer.Clear());
		}

		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom, bool localIsLayoutRequested)
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
			// Don't call base, just update the filling color.
			var newOnInvalidateRender = _brushChanged ?? (() => UpdateBorder());
			Brush.SetupBrushChanged(e.OldValue as Brush, e.NewValue as Brush, ref _brushChanged, newOnInvalidateRender);
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
