using System;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml;
using ObjCRuntime;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

#if !NET6_0_OR_GREATER
using NativeHandle = System.IntPtr;
#endif

namespace Uno.UI.Views.Controls
{
	public partial class BindableUIScrollView : UIScrollView, DependencyObject
	{
		public BindableUIScrollView(NativeHandle handle)
			: base(handle)
		{
			InitializeBinder();
		}

		public BindableUIScrollView(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public BindableUIScrollView(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		public BindableUIScrollView(RectangleF frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public BindableUIScrollView()
		{
			InitializeBinder();
		}
	}
}

