using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.UI.DataBinding;
using System.Linq;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using ViewGroup = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif


namespace Windows.UI.Xaml.Controls
{
	public partial class IconElement : FrameworkElement
    {
		partial void UnregisterSubView();
		partial void RegisterSubView(View child);

		//This field is never accessed. It just exists to create a reference, because the DP causes issues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
		private Brush _foregroundStrongref;

		public IconElement()
		{
		}

		public
#if __ANDROID_23__
		new
#endif
		Brush Foreground
		{
			get { return (Brush)this.GetValue(ForegroundProperty); }
			set
			{
				this.SetValue(ForegroundProperty, value);
				_foregroundStrongref = value;
			}
		}

		public static DependencyProperty ForegroundProperty { get ; } =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(IconElement),
				new FrameworkPropertyMetadata(
					SolidColorBrushHelper.White,
					FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((IconElement)s).OnForegroundChanged(e)
				)
			);

		protected virtual void OnForegroundChanged(DependencyPropertyChangedEventArgs e) { }

		internal void AddIconElementView(View child)
		{
			RegisterSubView(child);
        }

		public static implicit operator IconElement(string symbol)
		{
			return new SymbolIcon() { Symbol = (Symbol)Enum.Parse(typeof(Symbol), symbol, true) };
		}
	}
}
