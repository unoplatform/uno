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
	public partial class BindableUIAlertView : UIAlertView, DependencyObject
	{
		public BindableUIAlertView(string title, string message, UIAlertViewDelegate del, string cancelButtonTitle, params string[] otherButtons)
#pragma warning disable CS0618 // Type or member is obsolete
			: base(title, message, del, cancelButtonTitle, otherButtons)
#pragma warning restore CS0618 // Type or member is obsolete
		{
			InitializeBinder();
		}

		public BindableUIAlertView(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public BindableUIAlertView(RectangleF frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public BindableUIAlertView(NativeHandle handle)
			: base(handle)
		{
			InitializeBinder();
		}

		public BindableUIAlertView(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		public BindableUIAlertView()
		{
			InitializeBinder();
		}
	}
}

