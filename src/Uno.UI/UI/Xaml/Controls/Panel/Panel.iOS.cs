using CoreGraphics;
using UIKit;
using Uno.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Panel
	{
		public override void SubviewAdded(UIView uiview)
		{
			base.SubviewAdded(uiview);

			var element = uiview as IFrameworkElement;
			if (element != null)
			{
				OnChildAdded(element);
			}
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
		{
			SetNeedsLayout();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			SetNeedsLayout();
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args)
		{
			// TODO:MZ!!!!!
			// Ignore the background changes provided from base, we're rendering it using the CALayer.
			// base.OnBackgroundChanged(e);

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
		}

		private void OnBackgroundImageBrushChanged(UIImage backgroundImage)
		{
			UpdateBorder(backgroundImage);
		}

		private void UpdateBorder(UIImage backgroundImage = null)
		{
			// Checking for Window avoids re-creating the layer until it is actually used.
			if (IsLoaded)
			{
				if (backgroundImage == null)
				{
					ImageData backgroundImageData = default;
					(Background as ImageBrush)?.ImageSource?.TryOpenSync(out backgroundImageData);

					if (backgroundImageData.Kind == ImageDataKind.NativeImage)
					{
						backgroundImage = backgroundImageData.NativeImage;
					}
				}

				_borderRenderer.UpdateLayer(
					Background,
					InternalBackgroundSizing,
					BorderThicknessInternal,
					BorderBrushInternal,
					CornerRadiusInternal,
					backgroundImage
				);
			}
		}

		protected virtual void OnChildrenChanged()
		{
			SetNeedsLayout();
		}

		protected override void OnAfterArrange()
		{
			base.OnAfterArrange();

			//We trigger all layoutUpdated animations
			_transitionHelper?.LayoutUpdatedTransition();
		}

		protected override void OnBeforeArrange()
		{
			base.OnBeforeArrange();

			//We set childrens position for the animations before the arrange
			_transitionHelper?.SetInitialChildrenPositions();

			UpdateBackground();
		}

		public bool HitTestOutsideFrame
		{
			get;
			set;
		}

		public override UIView HitTest(CGPoint point, UIEvent uievent)
		{
			// All touches that are on this view (and not its subviews) are ignored
			return HitTestOutsideFrame ? this.HitTestOutsideFrame(point, uievent) : base.HitTest(point, uievent);
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadiusInternal == CornerRadius.None;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
