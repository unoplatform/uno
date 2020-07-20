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
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class PathIcon : IconElement
	{
		private Shapes.Path _path;

		public PathIcon()
		{
			_path = new Shapes.Path();
			_path.Fill = Foreground;
			_path.Stretch = Stretch.None;
			AddIconElementView(_path);
		}

		public Geometry Data
		{
			get { return (Geometry)this.GetValue(DataProperty); }
			set { this.SetValue(DataProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
		public static DependencyProperty DataProperty { get ; } =
			DependencyProperty.Register("Data", typeof(Geometry), typeof(PathIcon), new FrameworkPropertyMetadata(null, propertyChangedCallback: (s, e) => ((PathIcon)s).OnDataChanged(e)));

		protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
		{
			_path.Fill = e.NewValue as Brush;
		}

		private void OnDataChanged(DependencyPropertyChangedEventArgs e)
		{
			_path.Data = e.NewValue as Geometry;
			_path.Fill = Foreground;
		}


	}
}
