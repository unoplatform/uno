using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using Uno.Collections;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media
{
	[TypeConverter(typeof(BrushConverter))]
	public partial class Brush : DependencyObject
	{
		private ImmutableList<BrushChangedCallback> _brushChangedCallbacks = ImmutableList<BrushChangedCallback>.Empty;

		public Brush()
		{
			InitializeBinder();
		}

		public static implicit operator Brush(Color uiColor) => new SolidColorBrush(uiColor);

		public static implicit operator Brush(string colorCode) => SolidColorBrushHelper.Parse(colorCode);

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

		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
		[GeneratedDependencyProperty(DefaultValue = null)]
		public static DependencyProperty TransformProperty { get; } = CreateTransformProperty();

		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		public static DependencyProperty RelativeTransformProperty { get; } = CreateRelativeTransformProperty();

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



		internal IDisposable SubscribeToChanges(BrushChangedCallback onChanged)
		{
			var weakOnChangedReference = WeakReferencePool.RentWeakReference(null, onChanged);

			BrushChangedCallback weakCallback = () => (!weakOnChangedReference.IsDisposed ?
				weakOnChangedReference.Target as BrushChangedCallback : null)?.Invoke();

			_brushChangedCallbacks = _brushChangedCallbacks.Add(weakCallback);

			return new BrushChangedDisposable(this, weakCallback, weakOnChangedReference);
		}

		/// <summary>
		/// Disposable that removes brush changed callback.
		/// </summary>
		internal struct BrushChangedDisposable : IDisposable
		{
			private readonly ManagedWeakReference _brushWeakReference;
			private readonly ManagedWeakReference _onChangedWeakReference;
			private readonly BrushChangedCallback _callbackWeak;

			public BrushChangedDisposable(Brush brush, BrushChangedCallback callbackWeak, ManagedWeakReference onChangedWeakReference)
			{
				_brushWeakReference = WeakReferencePool.RentWeakReference(brush, brush);
				_callbackWeak = callbackWeak;
				_onChangedWeakReference = onChangedWeakReference;
			}

			public void Dispose()
			{
				if (!_brushWeakReference.IsDisposed && _brushWeakReference.Target is Brush brush)
				{
					brush._brushChangedCallbacks = brush._brushChangedCallbacks.Remove(_callbackWeak);
					_onChangedWeakReference.Dispose();
				}
			}
		}
	}
}
