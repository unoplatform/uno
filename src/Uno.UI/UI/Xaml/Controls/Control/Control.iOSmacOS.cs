using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using System.Linq;
using Windows.UI.Xaml.Input;

#if XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using UIKit;
using CoreGraphics;
#elif __MACOS__
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using AppKit;
using CoreGraphics;
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

		partial void RegisterSubView(View child)
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
#if __IOS__
				child.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
#elif __MACOS__
				child.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
#endif
			}

			AddSubview(child);
		}

#if __IOS__
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
#endif

		protected virtual bool RequestFocus(FocusState state)
		{
			FocusState = state;

			return true;
		}
	}
}

