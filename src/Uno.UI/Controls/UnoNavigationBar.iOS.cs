using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Windows.UI.Xaml;
using Foundation;
using UIKit;
using ObjCRuntime;

namespace Uno.UI.Controls
{
	public partial class UnoNavigationBar : UINavigationBar, DependencyObject
	{
		internal event Action SizeChanged;

		public UnoNavigationBar()
		{
			InitializeBinder();
		}

		public UnoNavigationBar(CGRect frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public UnoNavigationBar(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public UnoNavigationBar(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		public UnoNavigationBar(NativeHandle handle)
			: base(handle)
		{
			InitializeBinder();
		}

		public override CGRect Frame
		{
			get => base.Frame;
			set
			{
				if (value != Frame)
				{
					base.Frame = value;
					SizeChanged?.Invoke();
				}
			}
		}

		public override CGRect Bounds
		{
			get => base.Bounds;
			set
			{
				if (value != Bounds)
				{
					base.Bounds = value;
					SizeChanged?.Invoke();
				}
			}
		}
	}
}
