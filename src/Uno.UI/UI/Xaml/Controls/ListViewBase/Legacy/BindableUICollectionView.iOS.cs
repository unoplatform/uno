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
