using System;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Uno.UI.Views.Controls
{
	public partial class BindableUICollectionView : UICollectionView, DependencyObject
	{
		public BindableUICollectionView (RectangleF frame, UICollectionViewLayout layout)
			: base (frame, layout)
		{
            Initialize();
		}

		public BindableUICollectionView (NSCoder coder)
			 : base (coder)
        {
            Initialize();
		}

		public BindableUICollectionView (NSObjectFlag t)
			 : base (t)
        {
            Initialize();
		}

		public BindableUICollectionView (IntPtr handle)
			 : base (handle)
        {
            Initialize();
		}

        void Initialize()
        {
            DelaysContentTouches = true;
		}
	}
}
