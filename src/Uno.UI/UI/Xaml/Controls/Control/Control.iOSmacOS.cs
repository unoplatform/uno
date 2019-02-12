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

		protected virtual bool RequestFocus(FocusState state)
		{
			FocusState = state;

			return true;
		}
	}
}

