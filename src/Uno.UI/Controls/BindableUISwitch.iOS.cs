using System;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Uno.UI.Views.Controls
{
	public partial class BindableUISwitch : UISwitch, DependencyObject
	{
		public BindableUISwitch()
		{
			InitializeBinder();
			ValueChanged += OnValueChanged;
		}

		private void OnValueChanged(object sender, EventArgs e)
		{
			IsOn = On;
		}

		public BindableUISwitch(RectangleF frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public BindableUISwitch(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public BindableUISwitch(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		public BindableUISwitch(IntPtr handle)
			: base(handle)
		{
			InitializeBinder();
		}

		public Brush Foreground
		{
			get { return (Brush)this.GetValue(ForegroundProperty); }
			set { this.SetValue(ForegroundProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ForegroundProperty =
			DependencyProperty.Register("Foreground", typeof(Brush), typeof(BindableUISwitch), new PropertyMetadata(null, (s, e) => ((BindableUISwitch)s).OnForegroundChanged((Brush)e.NewValue)));

		private void OnForegroundChanged(Brush newValue)
		{
			var asColorBrush = newValue as SolidColorBrush;
			if (asColorBrush != null)
			{
				ThumbTintColor = asColorBrush.Color;
			}
		}

		public Brush Background
		{
			get { return (Brush)this.GetValue(BackgroundProperty); }
			set { this.SetValue(BackgroundProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BackgroundProperty =
			DependencyProperty.Register("Background", typeof(Brush), typeof(BindableUISwitch), new PropertyMetadata(null, (s, e) => ((BindableUISwitch)s).OnBackgroundChanged((Brush)e.NewValue)));

		private void OnBackgroundChanged(Brush newValue)
		{
			var asColorBrush = newValue as SolidColorBrush;
			if (asColorBrush != null)
			{
				OnTintColor = asColorBrush.Color;
			}
		}

		#region IsOn DependencyProperty

		public bool IsOn
		{
			get { return (bool)GetValue(IsOnProperty); }
			set { SetValue(IsOnProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsOn.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsOnProperty =
			DependencyProperty.Register("IsOn", typeof(bool), typeof(BindableUISwitch), new PropertyMetadata(default(bool), (s, e) => ((BindableUISwitch)s)?.OnIsOnChanged(e)));


		private void OnIsOnChanged(DependencyPropertyChangedEventArgs e)
		{
			this.SetState((bool)e.NewValue, true);
		}

		#endregion

	}
}

