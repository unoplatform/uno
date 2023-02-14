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
	public partial class BindableUICollectionView : UICollectionView, DependencyObject
	{
		public BindableUICollectionView(RectangleF frame, UICollectionViewLayout layout)
			: base(frame, layout)
		{
			Initialize();
		}

		public BindableUICollectionView(NSCoder coder)
			 : base(coder)
		{
			Initialize();
		}

		public BindableUICollectionView(NSObjectFlag t)
			 : base(t)
		{
			Initialize();
		}

		public BindableUICollectionView(NativeHandle handle)
			 : base(handle)
		{
			Initialize();
		}

		void Initialize()
		{
			DelaysContentTouches = true;
		}
	}
}
