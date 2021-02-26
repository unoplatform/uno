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
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal partial class NativeScrollContentPresenter : IScrollContentPresenter
	{
		private View _content;

		public object Content
		{
			get { return _content; }
			set
			{
				var previousView = _content;
				_content = value as View;

				OnContentChanged(previousView, value as View);
			}
		}

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
