#if !HAS_UI_TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Uno.Disposables;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using CoreGraphics;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

#if __IOS__
using UIKit;
using _Image = UIKit.UIImage;
#elif __MACOS__
using AppKit;
using _Image = AppKit.NSImage;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Border
	{
		private readonly BorderLayerRenderer _borderRenderer;

		public Border()
		{
			_borderRenderer = new BorderLayerRenderer(this);
		}

		partial void OnBorderBrushChangedPartial()
		{
			UpdateBorderLayer();
		}

		protected override void OnAfterArrange()
		{
			base.OnAfterArrange();
			UpdateBorderLayer();
		}

		private void UpdateBorderLayer(_Image backgroundImage = null)
		{
			if (IsLoaded)
			{
				if (backgroundImage == null)
				{
					ImageData backgroundImageData = default;
					if ((Background as ImageBrush)?.ImageSource?.TryOpenSync(out backgroundImageData) == true &&
						backgroundImageData.Kind == Uno.UI.Xaml.Media.ImageDataKind.NativeImage)
					{
						backgroundImage = backgroundImageData.NativeImage;
					}
				}

				_borderRenderer.Update();
				if (_borderRenderer.BoundsPath is CGPath updated) // UpdateLayer may return null if there is no update
				{
					BoundsPath = updated;
					BoundsPathUpdated?.Invoke(this, default);
				}
			}

			this.SetNeedsDisplay();
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
			// because we're overriding draw.

			var old = args.OldValue as ImageBrush;
			if (old != null)
			{
				old.ImageChanged -= OnBackgroundImageBrushChanged;
			}
			var imgBrush = args.NewValue as ImageBrush;
			if (imgBrush != null)
			{
				imgBrush.ImageChanged += OnBackgroundImageBrushChanged;
			}
			else
			{
				UpdateBorderLayer();
			}
		}

		private void OnBackgroundImageBrushChanged(_Image backgroundImage)
		{
			UpdateBorderLayer(backgroundImage);
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorderLayer();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorderLayer();
		}

		partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
		{
			previousValue?.RemoveFromSuperview();

			if (newValue != null)
			{
				AddSubview(newValue);
			}

			UpdateBorderLayer();
		}

		partial void OnCornerRadiusUpdatedPartial(CornerRadius oldValue, CornerRadius newValue)
		{
			UpdateBorderLayer();
		}
		bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None && (!(Child is UIElement ue) || ue.RenderTransform == null);
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

		internal event EventHandler BoundsPathUpdated;
		internal CGPath BoundsPath { get; private set; }
	}
}
#endif
