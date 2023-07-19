using System;
using System.Collections.Generic;
using System.Text;
using Foundation;
using UIKit;
using Windows.UI.Xaml;
using CoreGraphics;
using ObjCRuntime;

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

		public BindableUIActivityIndicatorView(NativeHandle handle)
			: base(handle)
		{
			InitializeBinder();
		}
	}
}
