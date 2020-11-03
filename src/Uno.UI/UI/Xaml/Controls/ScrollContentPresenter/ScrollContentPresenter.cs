using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Drawing;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, ILayoutConstraints
	{
		private void InitializeScrollContentPresenter()
		{
			this.RegisterParentChangedCallback(this, OnParentChanged);
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			// Set Content to null when we are removed from TemplatedParent. Otherwise Content.RemoveFromSuperview() may
			// be called when it is attached to a different view.
			if (args.NewParent == null)
			{
				Content = null;
			}
		}

#if XAMARIN
		private NativeScrollContentPresenter Native => Content as NativeScrollContentPresenter;
		public ScrollBarVisibility HorizontalScrollBarVisibility => Native?.HorizontalScrollBarVisibility ?? default;
		public ScrollBarVisibility VerticalScrollBarVisibility => Native?.VerticalScrollBarVisibility ?? default;
#endif

		bool ILayoutConstraints.IsWidthConstrained(View requester)
		{
			if (requester != null && HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
			{
				return false;
			}

#if __MACOS__
			return false;
#else
			return this.IsWidthConstrainedSimple() ?? (Parent as ILayoutConstraints)?.IsWidthConstrained(this) ?? false;
#endif
		}

		bool ILayoutConstraints.IsHeightConstrained(View requester)
		{
			if (requester != null && VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
			{
				return false;
			}

#if __MACOS__
			return false;
#else
			return this.IsHeightConstrainedSimple() ?? (Parent as ILayoutConstraints)?.IsHeightConstrained(this) ?? false;
#endif
		}

		public double ExtentHeight
		{
			get => Content is FrameworkElement fe ? fe.DesiredSize.Height : 0;
		}

		public double ExtentWidth
		{
			get => Content is FrameworkElement fe ? fe.DesiredSize.Width : 0;
		}

		public double ViewportHeight => DesiredSize.Height;

		public double ViewportWidth => DesiredSize.Width;
	}
}
