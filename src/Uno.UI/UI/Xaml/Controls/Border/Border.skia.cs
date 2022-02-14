using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using System.Linq;
using System.Drawing;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Uno.UI;

using View = Windows.UI.Xaml.UIElement;
using Color = System.Drawing.Color;
using Windows.UI.Composition;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	public partial class Border
	{
		private readonly BorderLayerRenderer _borderRenderer;

		public Border()
		{
			_borderRenderer = new BorderLayerRenderer(this);

			Loaded += (s, e) => UpdateBorder();
			Unloaded += (s, e) => _borderRenderer.Clear();
			LayoutUpdated += (s, e) => UpdateBorder();
		}

		partial void OnChildChangedPartial(View previousValue, View newValue)
		{
			if (previousValue != null)
			{
				RemoveChild(previousValue);
			}

			AddChild(newValue);
		}

		private void UpdateBorder()
		{
			if (Visual != null)
			{
				_borderRenderer.UpdateLayer(
					Background,
					BackgroundSizing,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					null
				);
			}
		}

		internal override void OnArrangeVisual(Rect rect, Rect? clip)
		{
			UpdateBorder();

			base.OnArrangeVisual(rect, clip);
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			UpdateBorder();
		}

		partial void OnBorderBrushChangedPartial()
		{
			UpdateBorder();
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}

		partial void OnCornerRadiusUpdatedPartial(CornerRadius oldValue, CornerRadius newValue)
		{
			UpdateBorder();
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args)
		{
			base.OnBackgroundChanged(args);

			UpdateBorder();
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => !(Child is UIElement ue) || ue.RenderTransform == null;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
	}
}
