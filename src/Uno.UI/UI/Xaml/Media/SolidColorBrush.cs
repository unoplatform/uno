using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Windows.UI;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#endif

namespace Microsoft.UI.Xaml.Media
{
	public partial class SolidColorBrush : Brush, IEquatable<SolidColorBrush>
		, IShareableDependencyObject //TODO: should be implemented on Brush
	{
		/// <summary>
		/// Blends the Color set on the SolidColorBrush with its Opacity. Should generally be used for rendering rather than the Color property itself.
		/// </summary>
		internal Windows.UI.Color ColorWithOpacity
		{
			get; private set;
		}

		private readonly bool _isClone;
		bool IShareableDependencyObject.IsClone => _isClone;

		public SolidColorBrush()
		{
			IsAutoPropertyInheritanceEnabled = false;
		}

		public SolidColorBrush(Windows.UI.Color color) : this()
		{
			Color = color;
			UpdateColorWithOpacity(color);
		}

		private SolidColorBrush(SolidColorBrush original) : this()
		{
			_isClone = true;

			Color = original.Color;
			UpdateColorWithOpacity(Color);

			Opacity = original.Opacity;
			Transform = original.Transform;
			RelativeTransform = original.RelativeTransform;
		}

		/// <remarks>
		/// This method is required for performance. Creating a native Color 
		/// requires a round-trip with Objective-C, so updating this value only when opacity
		/// and color changes is more efficient.
		/// </remarks>
		private void UpdateColorWithOpacity(Windows.UI.Color newColor)
		{
			ColorWithOpacity = GetColorWithOpacity(newColor);
		}

		partial void OnColorChanged(Windows.UI.Color oldValue, Windows.UI.Color newValue)
		{
			UpdateColorWithOpacity(newValue);
		}

		protected override void OnOpacityChanged(double oldValue, double newValue)
		{
			base.OnOpacityChanged(oldValue, newValue);

			UpdateColorWithOpacity(Color);
		}

		#region Color Dependency Property

		public Windows.UI.Color Color
		{
			get { return (Windows.UI.Color)this.GetValue(ColorProperty); }
			set { this.SetValue(ColorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static DependencyProperty ColorProperty { get; } =
			DependencyProperty.Register(
				"Color",
				typeof(Windows.UI.Color),
				typeof(SolidColorBrush),
				new FrameworkPropertyMetadata(
					Colors.Transparent,
					(s, e) => ((SolidColorBrush)s).OnColorChanged((Windows.UI.Color)e.OldValue, (Windows.UI.Color)e.NewValue)
				)
			);

		partial void OnColorChanged(Windows.UI.Color oldValue, Windows.UI.Color newValue);

		#endregion

		DependencyObject IShareableDependencyObject.Clone() => new SolidColorBrush(this);

		public override string ToString() => "[SolidColorBrush {0}]".InvariantCultureFormat(Color);

		public override bool Equals(object obj) => Equals(obj as SolidColorBrush);

		public bool Equals(SolidColorBrush other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			return ReferenceEquals(this, other)
				|| (ColorWithOpacity.Equals(other.ColorWithOpacity) && this._isClone == other._isClone);
		}

		public override int GetHashCode() => ColorWithOpacity.GetHashCode() ^ _isClone.GetHashCode();

		public static bool operator ==(SolidColorBrush left, SolidColorBrush right) => Equals(left, right);

		public static bool operator !=(SolidColorBrush left, SolidColorBrush right) => !Equals(left, right);
	}
}
