#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using ObjCRuntime;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
using ObjCRuntime;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal partial class NativeScrollContentPresenter : IScrollContentPresenter, INativeScrollContentPresenter
	{
		private View _content;

		ScrollBarVisibility IScrollContentPresenter.NativeHorizontalScrollBarVisibility { set => HorizontalScrollBarVisibility = value; }
		ScrollBarVisibility IScrollContentPresenter.NativeVerticalScrollBarVisibility { set => VerticalScrollBarVisibility = value; }

		public bool CanHorizontallyScroll
		{
			get => HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
			set { }
		}

		public bool CanVerticallyScroll
		{
			get => VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
			set { }
		}

		private double _extentWidth;
		double INativeScrollContentPresenter.ExtentWidth
		{
			get => _extentWidth;
			set => _extentWidth = value;
		}

		private double _extentHeight;
		double INativeScrollContentPresenter.ExtentHeight
		{
			get => _extentHeight;
			set => _extentHeight = value;
		}

		public object Content
		{
			get => _content;
			set
			{
				var previousView = _content;
				_content = value as View;

				OnContentChanged(previousView, value as View);
			}
		}

		public Size? CustomContentExtent => null;

		partial void OnContentChanged(View previousView, View newView);

		void IScrollContentPresenter.OnMinZoomFactorChanged(float newValue)
		{
			MinimumZoomScale = newValue;
		}

		void IScrollContentPresenter.OnMaxZoomFactorChanged(float newValue)
		{
			MaximumZoomScale = newValue;
		}
	}
}
#endif
