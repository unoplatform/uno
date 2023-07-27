using System;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using ObjCRuntime;

using Foundation;
using UIKit;

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

