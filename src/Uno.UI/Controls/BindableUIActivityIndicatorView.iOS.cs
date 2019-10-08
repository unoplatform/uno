using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using CoreGraphics;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Uno.UI.Views.Controls
{
	public partial class BindableUIActivityIndicatorView : UIActivityIndicatorView, DependencyObject
	{
		public BindableUIActivityIndicatorView()
		{
			InitializeBinder();
		}

		public BindableUIActivityIndicatorView(CGRect frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public BindableUIActivityIndicatorView(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public BindableUIActivityIndicatorView(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		public BindableUIActivityIndicatorView(IntPtr handle)
			: base(handle)
		{
			InitializeBinder();
		}
	}
}
