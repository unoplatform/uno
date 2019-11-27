using System;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.Logging;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;
using Uno.UI.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;

using CoreGraphics;
using AppKit;

namespace Windows.UI.Xaml.Controls
{
	public partial class Panel
	{
		private BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();

		public Panel()
		{
			Initialize();

			this.RegisterLoadActions(() => UpdateBackground(), () => _borderRenderer.Clear());
		}

		partial void Initialize();

		public override void DidAddSubview(NSView nsView)
		{
			base.DidAddSubview(nsView);

			var element = nsView as IFrameworkElement;
			if (element != null)
			{
				OnChildAdded(element);
			}
		}

		partial void OnUnloadedPartial()
		{
			_borderRenderer.Clear();
		}

		partial void OnLoadedPartial()
		{
			UpdateBackground();
		}

		partial void OnBorderBrushChangedPartial(Brush oldValue, Brush newValue)
		{
			UpdateBackground();
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
		{
			NeedsLayout = true;
			UpdateBackground();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			NeedsLayout = true;
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args)
		{
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
			else
			{
				UpdateBackground();
			}
		}

		private void OnBackgroundImageBrushChanged(NSImage backgroundImage)
		{
			UpdateBackground(backgroundImage);
		}

		partial void OnCornerRadiusChangedPartial(CornerRadius oldValue, CornerRadius newValue)
		{
			UpdateBackground();
		}

		private void UpdateBackground(NSImage backgroundImage = null)
		{
			// Checking for Window avoids re-creating the layer until it is actually used.
			if (IsLoaded)
			{
				backgroundImage = backgroundImage ?? (Background as ImageBrush)?.ImageSource?.ImageData;

				_borderRenderer.UpdateLayer(
					this,
					Background,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					backgroundImage
				);
			}
		}

		protected virtual void OnChildrenChanged()
		{
			NeedsLayout = true;
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

		/// <summary>        
		/// Support for the C# collection initializer style.
		/// Allows items to be added like this 
		/// new Panel 
		/// {
		///    new Border()
		/// }
		/// </summary>
		/// <param name="view"></param>
		public void Add(NSView view)
		{
			Children.Add(view);
		}

		public bool HitTestOutsideFrame
		{
			get;
			set;
		}

		public override NSView HitTest(CGPoint point)
		{
			// All touches that are on this view (and not its subviews) are ignored
			return HitTestOutsideFrame ? this.HitTestOutsideFrame(point) : base.HitTest(point);
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
