using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Media
{
	[TypeConverter(typeof(BrushConverter))]
	public partial class Brush : DependencyObject
	{
		public Brush()
		{
			InitializeBinder();
		}

		public static implicit operator Brush(Color uiColor) => SolidColorBrushHelper.FromARGB(uiColor.A, uiColor.R, uiColor.G, uiColor.B);

		public static implicit operator Brush(string colorCode) => SolidColorBrushHelper.Parse(colorCode);

		#region Opacity Dependency Property

		public double Opacity
		{
			get => GetOpacityValue();
			set => SetOpacityValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = 1d, ChangedCallback = true)]
		public static DependencyProperty OpacityProperty { get ; } = CreateOpacityProperty();

		protected virtual void OnOpacityChanged(double oldValue, double newValue)
		{
		}

		#endregion

		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
		[GeneratedDependencyProperty(DefaultValue = null)]
		public static DependencyProperty TransformProperty { get; } = CreateTransformProperty();

		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Windows.UI.Xaml.Media.Transform Transform
		{
			get => GetTransformValue();
			set => SetTransformValue(value);
		}

		public Transform RelativeTransform
		{
			get => GetRelativeTransformValue();
			set => SetRelativeTransformValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true)]
		public static DependencyProperty RelativeTransformProperty { get ; } = CreateRelativeTransformProperty();

		protected virtual void OnRelativeTransformChanged(Transform oldValue, Transform newValue)
		{
		}

		private protected Color GetColorWithOpacity(Color referenceColor)
		{
			return Color.FromArgb((byte)(Opacity * referenceColor.A), referenceColor.R, referenceColor.G, referenceColor.B);
		}

		[Pure]
		internal static Color? GetColorWithOpacity(Brush brush, Color? defaultColor = null)
		{
			return TryGetColorWithOpacity(brush, out var c) ? c : defaultColor;
		}

		[Pure]
		internal static bool TryGetColorWithOpacity(Brush brush, out Color color)
		{
			switch (brush)
			{
				case SolidColorBrush scb:
					color = scb.ColorWithOpacity;
					return true;
				case GradientBrush gb:
					color = gb.FallbackColorWithOpacity;
					return true;
				case XamlCompositionBrushBase ab:
					color = ab.FallbackColorWithOpacity;
					return true;
				default:
					color = default;
					return false;
			}
		}

#if !__WASM__
		// TODO: Refactor brush handling to a cleaner unified approach - https://github.com/unoplatform/uno/issues/5192
		internal bool SupportsAssignAndObserveBrush => true;
#endif
	}
}
