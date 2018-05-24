using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Views.Controls;
using Uno.UI.DataBinding;
using System.Linq;
using Windows.UI.Xaml.Input;

#if XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
using MonoTouch.UIKit;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif


namespace Windows.UI.Xaml.Controls
{
	public partial class Control
	{
		public Control ()
		{
			InitializeControl();
			Initialize();
		}

		void Initialize()
		{
		}

		/// <summary>
		/// Gets the first sub-view of this control or null if there is none
		/// </summary>
		public IFrameworkElement GetTemplateRoot()
		{
			return Subviews.FirstOrDefault() as IFrameworkElement;
		}

		partial void UnregisterSubView()
		{
			if (Subviews.Length > 0)
			{
				Subviews[0].RemoveFromSuperview();
			}
		}

		partial void RegisterSubView(UIView child)
		{
			if(Subviews.Length != 0)
			{
				throw new Exception("A Xaml control may not contain more than one child.");
			}

			if (FeatureConfiguration.UIElement.UseLegacyClipping)
			{
				// This is no longer needed when using normal clipping.
				// Assigning the frame overrides the standard Uno layouter, which 
				// prevents the clipping to be set to the proper size.

				child.Frame = Bounds;
				child.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			}

			AddSubview(child);
		}

		internal override void OnPointerPressedInternal(object sender, PointerRoutedEventArgs args)
		{
			// Call virtual method first to give subclasses a chance to set handled to true
			OnPointerPressed(args);
			base.OnPointerPressedInternal(sender, args);
		}

		internal override void OnPointerReleasedInternal(object sender, PointerRoutedEventArgs args)
		{
			// Call virtual method first to give subclasses a chance to set handled to true
			OnPointerReleased(args);
			base.OnPointerReleasedInternal(sender, args);
		}

		internal override void OnPointerCaptureLostInternal(object sender, PointerRoutedEventArgs args)
		{
			// Call virtual method first to give subclasses a chance to set handled to true
			OnPointerCaptureLost(args);
			base.OnPointerCaptureLostInternal(sender, args);
		}

		internal override void OnPointerEnteredInternal(object sender, PointerRoutedEventArgs args)
		{
			OnPointerEntered(args);
			base.OnPointerEnteredInternal(sender, args);
		}

		internal override void OnPointerExitedInternal(object sender, PointerRoutedEventArgs args)
		{
			OnPointerExited(args);
			base.OnPointerExitedInternal(sender, args);
		}

		internal override void OnPointerMovedInternal(object sender, PointerRoutedEventArgs args)
		{
			OnPointerMoved(args);
			base.OnPointerMovedInternal(sender, args);
		}

		internal override void OnPointerCanceledInternal(object sender, PointerRoutedEventArgs args)
		{
			OnPointerCanceled(args);
			base.OnPointerCanceledInternal(sender, args);
		}

		protected virtual bool RequestFocus(FocusState state)
		{
			FocusState = state;

			return true;
		}
	}
}

