#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Composition;
using Uno.Disposables;
using Uno.UI.Helpers;
using Uno.UI.Xaml;
using Windows.UI.Core;

using Windows.UI;

namespace Microsoft.UI.Xaml.Media
{
	[TypeConverter(typeof(BrushConverter))]
	public partial class Brush : DependencyObject
	{
		private WeakEventHelper.WeakEventCollection? _invalidateRenderHandlers;

		internal IDisposable RegisterInvalidateRender(Action handler)
			=> WeakEventHelper.RegisterEvent(
				_invalidateRenderHandlers ??= new(),
				handler,
				(h, s, e) =>
					(h as Action)?.Invoke()
			);

		protected Brush()
		{
			InitializeBinder();
		}

#if __ANDROID__ || __APPLE_UIKIT__
		internal static Color GetFallbackColor(Brush brush)
		{
			return brush switch
			{
				SolidColorBrush scb => scb.ColorWithOpacity,
				GradientBrush gb => gb.FallbackColorWithOpacity,
				XamlCompositionBrushBase xamlCompositionBrushBase => xamlCompositionBrushBase.FallbackColorWithOpacity,
				_ => SolidColorBrushHelper.Transparent.Color,
			};
		}
#endif

		public static implicit operator Brush(Color uiColor) => new SolidColorBrush(uiColor);

		public static implicit operator Brush(string colorCode) => SolidColorBrushHelper.Parse(colorCode);

		internal static IDisposable? SetupBrushChanged(Brush? newValue, ref Action? onInvalidateRender, Action newOnInvalidateRender, bool initialInvoke = true)
		{
			if (initialInvoke)
			{
				newOnInvalidateRender();
			}

			if (newValue is not null)
			{
				onInvalidateRender = newOnInvalidateRender;
				return newValue.RegisterInvalidateRender(onInvalidateRender);
			}
			else
			{
				onInvalidateRender = null;
			}

			return null;
		}

		private protected void OnInvalidateRender()
		{
			_invalidateRenderHandlers?.Invoke(this, null);

#if __SKIA__
			SynchronizeCompositionBrush();
#endif
		}

		internal virtual void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == DataContextProperty || args.Property == XamlCompositionBrushBase.CompositionBrushProperty)
			{
				return;
			}

			OnInvalidateRender();

			if (args.Property == TransformProperty || args.Property == RelativeTransformProperty)
			{
				if (args.NewValue is Transform newTransform)
				{
					newTransform.Changed += OnTransformChange;
				}

				if (args.OldValue is Transform oldTransform)
				{
					oldTransform.Changed -= OnTransformChange;
				}
			}
		}

		private void OnTransformChange(object? sender, EventArgs args) => OnInvalidateRender();

		#region Opacity Dependency Property

		public double Opacity
		{
			get => GetOpacityValue();
			set => SetOpacityValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = 1d, ChangedCallback = true)]
		public static DependencyProperty OpacityProperty { get; } = CreateOpacityProperty();

		protected virtual void OnOpacityChanged(double oldValue, double newValue)
		{
		}

		#endregion

		[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
		[GeneratedDependencyProperty(DefaultValue = null)]
		public static DependencyProperty TransformProperty { get; } = CreateTransformProperty();

		[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
		public Microsoft.UI.Xaml.Media.Transform Transform
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
		public static DependencyProperty RelativeTransformProperty { get; } = CreateRelativeTransformProperty();

		protected virtual void OnRelativeTransformChanged(Transform oldValue, Transform newValue)
		{
		}

		private protected Color GetColorWithOpacity(Color referenceColor)
		{
			return Color.FromArgb((byte)(Opacity * referenceColor.A), referenceColor.R, referenceColor.G, referenceColor.B);
		}

		internal static Color? GetColorWithOpacity(Brush? brush, Color? defaultColor = null)
		{
			return TryGetColorWithOpacity(brush, out var c) ? c : defaultColor;
		}

		internal static bool TryGetColorWithOpacity(Brush? brush, out Color color)
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
	}
}
