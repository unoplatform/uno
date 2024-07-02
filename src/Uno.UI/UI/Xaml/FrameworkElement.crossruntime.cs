#if __NETSTD_REFERENCE__
#pragma warning disable IDE0051
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using View = Windows.UI.Xaml.UIElement;
using System.Collections;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.UI;
using System.Dynamic;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
#pragma warning disable CS0067 // Unused only in reference API.
		public event SizeChangedEventHandler SizeChanged;
#pragma warning restore CS0067

		internal bool WantsSizeChanged => SizeChanged != null;

		public double ActualWidth => GetActualWidth();
		public double ActualHeight => GetActualHeight();

		#region HorizontalAlignment Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = Xaml.HorizontalAlignment.Stretch,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty HorizontalAlignmentProperty { get; } = CreateHorizontalAlignmentProperty();

		public HorizontalAlignment HorizontalAlignment
		{
			get => GetHorizontalAlignmentValue();
			set => SetHorizontalAlignmentValue(value);
		}
		#endregion

		#region VerticalAlignment Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = Xaml.VerticalAlignment.Stretch,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty VerticalAlignmentProperty { get; } = CreateVerticalAlignmentProperty();

		public VerticalAlignment VerticalAlignment
		{
			get => GetVerticalAlignmentValue();
			set => SetVerticalAlignmentValue(value);
		}
		#endregion


		#region Width Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.NaN,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty WidthProperty { get; } = CreateWidthProperty();

		public double Width
		{
			get => GetWidthValue();
			set => SetWidthValue(value);
		}
		#endregion

		#region Height Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.NaN,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty HeightProperty { get; } = CreateHeightProperty();

		public double Height
		{
			get => GetHeightValue();
			set => SetHeightValue(value);
		}
		#endregion

		#region MinWidth Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = 0.0d,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MinWidthProperty { get; } = CreateMinWidthProperty();

		public double MinWidth
		{
			get => GetMinWidthValue();
			set => SetMinWidthValue(value);
		}
		#endregion

		#region MinHeight Dependency Property

		[GeneratedDependencyProperty(
			DefaultValue = 0.0d,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MinHeightProperty { get; } = CreateMinHeightProperty();

		public double MinHeight
		{
			get => GetMinHeightValue();
			set => SetMinHeightValue(value);
		}
		#endregion

		#region MaxWidth Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.PositiveInfinity,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MaxWidthProperty { get; } = CreateMaxWidthProperty();

		public double MaxWidth
		{
			get => GetMaxWidthValue();
			set => SetMaxWidthValue(value);
		}
		#endregion

		#region MaxHeight Dependency Property

		[GeneratedDependencyProperty(
			DefaultValue = double.PositiveInfinity,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MaxHeightProperty { get; } = CreateMaxHeightProperty();

		public double MaxHeight
		{
			get => GetMaxHeightValue();
			set => SetMaxHeightValue(value);
		}
		#endregion

		#region Margin Dependency Property
		[GeneratedDependencyProperty(
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MarginProperty { get; } = CreateMarginProperty();

		public Thickness Margin
		{
			get => GetMarginValue();
			set => SetMarginValue(value);
		}
		private static Thickness GetMarginDefaultValue() => Thickness.Empty;
		#endregion

		public new bool IsLoaded => base.IsLoaded; // The IsLoaded state is managed by the UIElement, FrameworkElement only makes it publicly visible

		private protected virtual void OnLoaded() { }

		partial void OnLoadingPartial();

		private protected sealed override void OnFwEltUnloaded()
		{
			// TODO: Unloaded is fired asynchronously in WinUI.
			void InvokeUnloaded()
			{
				// Raise event after invoking base in order to raise them bottom to top
				OnUnloaded();
				_unloaded?.Invoke(this, new RoutedEventArgs(this));
				OnUnloadedPartial();
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeUnloadedWithTry()
				{
					try
					{
						InvokeUnloaded();
					}
					catch (Exception error)
					{
						_log.Error("OnElementUnloaded failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeUnloadedWithTry();
			}
			else
			{
				InvokeUnloaded();
			}
		}

		partial void OnUnloadedPartial();

		private protected virtual void OnUnloaded() { }

		public override string ToString()
		{
#if __WASM__
			if (FeatureConfiguration.UIElement.RenderToStringWithId && !Name.IsNullOrEmpty())
			{
				return $"{base.ToString()}\"{Name}\"";
			}
#endif

			return base.ToString();
		}
	}
}
