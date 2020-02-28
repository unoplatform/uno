using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using UIKit;
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#else
using System.Drawing;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class SolidColorBrush : Brush, IEquatable<SolidColorBrush>
	{
		public SolidColorBrush()
		{
			IsAutoPropertyInheritanceEnabled = false;
			UpdateColorWithOpacity(Color, Opacity);
		}

		partial void UpdateColorWithOpacity(Color newColor, double opacity);

		public SolidColorBrush(Color color) : this()
		{
			Color = color;
		}

		partial void OnColorChanged(Color oldValue, Color newValue)
		{
			UpdateColorWithOpacity(newValue, Opacity);
		}

		protected override void OnOpacityChanged(double oldValue, double newValue)
		{
			base.OnOpacityChanged(oldValue, newValue);

			UpdateColorWithOpacity(Color, newValue);
		}

		#region Color Dependency Property

		public Color Color
		{
			get { return (Color)this.GetValue(ColorProperty); }
			set { this.SetValue(ColorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ColorProperty =
			DependencyProperty.Register(
				"Color", 
				typeof(Color),
				typeof(SolidColorBrush),
				new PropertyMetadata(
					Colors.Transparent,
					(s, e) => ((SolidColorBrush)s).OnColorChanged((Color)e.OldValue, (Color)e.NewValue)
				)
			);

		partial void OnColorChanged(Color oldValue, Color newValue);

		#endregion

		public override string ToString()
		{
			return "[SolidColorBrush {0}]".InvariantCultureFormat(Color);
		}

		public override bool Equals(object obj) => Equals(obj as SolidColorBrush);

		public bool Equals(SolidColorBrush other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			return ReferenceEquals(this, other) || ColorWithOpacity.Equals(other.ColorWithOpacity);
		}

		public override int GetHashCode() => ColorWithOpacity.GetHashCode();

		public static bool operator ==(SolidColorBrush left, SolidColorBrush right) => Equals(left, right);

		public static bool operator !=(SolidColorBrush left, SolidColorBrush right) => !Equals(left, right);
	}
}
