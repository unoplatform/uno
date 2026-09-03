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
using System.Numerics;
using Microsoft.UI.Xaml.Media;
using Uno;

namespace Microsoft.UI.Xaml.Media
{
	[TypeConverter(typeof(BrushConverter))]
	public partial class Brush : DependencyObject, IMultiParentShareableDependencyObject
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

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == XamlCompositionBrushBase.CompositionBrushProperty)
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

#nullable disable
		private protected CompositionBrush _compositionBrush;

		internal delegate void BrushSetterHandler(CompositionBrush brush);

		internal virtual CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
		{
			if (_compositionBrush is null)
			{
				_compositionBrush = compositor.CreateColorBrush(Colors.Transparent);
				SynchronizeCompositionBrush();
			}

			return _compositionBrush;
		}

		internal virtual void SynchronizeCompositionBrush()
		{
		}

		private protected static void ConvertGradientColorStops(Compositor compositor, CompositionGradientBrush compositionBrush, IEnumerable<GradientStop> gradientStops, double opacity)
		{
			compositionBrush.ColorStops.Clear();

			foreach (var stop in gradientStops)
			{
				compositionBrush.ColorStops.Add(compositor.CreateColorGradientStop((float)stop.Offset, stop.Color.WithOpacity(opacity)));
			}
		}

		private protected static CompositionGradientExtendMode ConvertGradientExtendMode(GradientSpreadMethod spreadMethod)
		{
			switch (spreadMethod)
			{
				case GradientSpreadMethod.Repeat:
					return CompositionGradientExtendMode.Wrap;
				case GradientSpreadMethod.Reflect:
					return CompositionGradientExtendMode.Mirror;
				case GradientSpreadMethod.Pad:
				default:
					return CompositionGradientExtendMode.Clamp;
			}
		}

		private protected static CompositionMappingMode ConvertBrushMappingMode(BrushMappingMode mappingMode)
		{
			switch (mappingMode)
			{
				case BrushMappingMode.Absolute:
					return CompositionMappingMode.Absolute;
				case BrushMappingMode.RelativeToBoundingBox:
				default:
					return CompositionMappingMode.Relative;
			}
		}
#nullable enable
	}


	internal static class BrushExtensions
	{
		internal static void TrySetColorFromBrush(this CompositionBrush brush, XamlCompositionBrushBase srcBrush)
		{
			if (brush is CompositionColorBrush colorBrush)
			{
				colorBrush.Color = srcBrush.FallbackColor;
			}
			else if (brush is CompositionBrushWrapper wrapper)
			{
				TrySetColorFromBrush(wrapper.WrappedBrush, srcBrush);
			}
		}
	}
}
