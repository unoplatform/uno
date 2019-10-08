using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Uno.UI.Controls
{
	public partial class BindableUISlider : UISlider, DependencyObject, INotifyPropertyChanged
	{
		public BindableUISlider()
		{
			Initialize();
		}

		public BindableUISlider(CGRect frame)
			: base(frame)
		{
			Initialize();
		}

		public BindableUISlider(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}

		public BindableUISlider(NSObjectFlag t)
			: base(t)
		{
			Initialize();
		}

		public BindableUISlider(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}

		private void Initialize()
		{
			this.ValueChanged += (s, e) =>
			{
				SetBindingValue(Value, nameof(Value));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
			};
		}

		public override float Value
		{
			get { return base.Value; }
			set
			{
				base.Value = value;

			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
