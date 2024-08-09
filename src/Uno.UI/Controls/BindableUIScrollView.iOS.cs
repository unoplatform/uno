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

