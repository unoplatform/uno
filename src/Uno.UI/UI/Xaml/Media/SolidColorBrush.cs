using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.UI.Xaml;
using Windows.UI;

using Color = Windows.UI.Color;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __IOS__
using View = UIKit.UIView;
using Font = UIKit.UIFont;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class SolidColorBrush : Brush, IEquatable<SolidColorBrush>
		, IShareableDependencyObject //TODO: should be implemented on Brush
	{
		/// <summary>
		/// Blends the Color set on the SolidColorBrush with its Opacity. Should generally be used for rendering rather than the Color property itself.
		/// </summary>
		internal Color ColorWithOpacity
		{
			get; private set;
		}

		private readonly bool _isClone;
		bool IShareableDependencyObject.IsClone => _isClone;

		public SolidColorBrush()
		{
			IsAutoPropertyInheritanceEnabled = false;
		}

		public SolidColorBrush(Color color) : this()
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
		private void UpdateColorWithOpacity(Color newColor)
		{
			ColorWithOpacity = GetColorWithOpacity(newColor);
		}

		partial void OnColorChanged(Color oldValue, Color newValue)
		{
			UpdateColorWithOpacity(newValue);
		}

		protected override void OnOpacityChanged(double oldValue, double newValue)
		{
			base.OnOpacityChanged(oldValue, newValue);

			UpdateColorWithOpacity(Color);
		}

		#region Color Dependency Property

		public Color Color
		{
			get => GetColorValue();
			set => SetColorValue(value);
		}

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnColorChanged))]
		public static DependencyProperty ColorProperty { get; } = CreateColorProperty();

		private static Color GetColorDefaultValue() => Colors.Transparent;

		partial void OnColorChanged(Color oldValue, Color newValue);

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
