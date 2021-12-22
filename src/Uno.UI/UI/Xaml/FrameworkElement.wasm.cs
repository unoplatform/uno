using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using View = Microsoft.UI.Xaml.UIElement;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.UI;
using System.Dynamic;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		private BorderLayerRenderer _borderLayerRenderer;

		bool IFrameworkElementInternal.HasLayouter => true;

		/*
			About NativeOn** vs ManagedOn** methods:
				The flag FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded will configure which set of methods will be used
				but they are mutually exclusive: Only one of each is going to be invoked.

			For the managed methods: for perf consideration (avoid lots of casting) the loaded state is managed by the UI Element,
				the FrameworkElement only makes it publicly available by overriding methods from UIElement and raising events.
				The propagation of this loaded state is also made by the UIElement.
		 */

		private void NativeOnLoading(FrameworkElement sender, object args)
		{
			OnLoadingPartial();

			// Explicit propagation of the loading even must be performed
			// after the compiled bindings are applied, as there may be altered
			// properties that affect the visual tree.
			foreach (var child in _children)
			{
				(child as FrameworkElement)?.InternalDispatchEvent("loading", args as EventArgs);
			}
		}


		private void NativeOnLoaded(object sender, RoutedEventArgs args)
		{
			base.IsLoaded = true;

			foreach (var child in _children)
			{
				(child as FrameworkElement)?.InternalDispatchEvent("loaded", args);
			}

			RaiseOnLoadedSafe();
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RaiseOnLoadedSafe()
		{
			try
			{
				OnLoaded();
			}
			catch (Exception error)
			{
				_log.Error("NativeOnLoaded failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}
		}

		private void NativeOnUnloaded(object sender, RoutedEventArgs args)
		{
			base.IsLoaded = false;

			foreach (var child in _children)
			{
				(child as FrameworkElement)?.InternalDispatchEvent("unloaded", args);
			}

			RaiseOnUnloadedSafe();
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RaiseOnUnloadedSafe()
		{
			try
			{
				OnUnloaded();
			}
			catch (Exception error)
			{
				_log.Error("NativeOnUnloaded failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}
		}

		internal bool HasParent()
			=> Parent != null;

		public double ActualWidth => GetActualWidth();
		public double ActualHeight => GetActualHeight();

		public event SizeChangedEventHandler SizeChanged;

		private event TypedEventHandler<FrameworkElement, object> _loading;
		public event TypedEventHandler<FrameworkElement, object> Loading
		{
			add
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					_loading += value;
				}
				else
				{
					RegisterEventHandler("loading", value, GenericEventHandlers.RaiseRoutedEventHandler);
				}
			}
			remove
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					_loading -= value;
				}
				else
				{
					UnregisterEventHandler("loading", value, GenericEventHandlers.RaiseRoutedEventHandler);
				}
			}
		}

		private event RoutedEventHandler _loaded;
		public event RoutedEventHandler Loaded
		{
			add
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					_loaded += value;
				}
				else
				{
					RegisterEventHandler("loaded", value, GenericEventHandlers.RaiseRoutedEventHandler);
				}
			}
			remove
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					_loaded -= value;
				}
				else
				{
					UnregisterEventHandler("loaded", value, GenericEventHandlers.RaiseRoutedEventHandler);
				}
			}
		}

		private event RoutedEventHandler _unloaded;
		public event RoutedEventHandler Unloaded
		{
			add
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					_unloaded += value;
				}
				else
				{
					RegisterEventHandler("unloaded", value, GenericEventHandlers.RaiseRoutedEventHandler);
				}
			}
			remove
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					_unloaded -= value;
				}
				else
				{
					UnregisterEventHandler("unloaded", value, GenericEventHandlers.RaiseRoutedEventHandler);
				}
			}
		}

		private bool IsTopLevelXamlView() => throw new NotSupportedException();

		internal void SuspendRendering() => throw new NotSupportedException();

		internal void ResumeRendering() => throw new NotSupportedException();

		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		protected void SetCornerRadius(CornerRadius cornerRadius)
			=> (_borderLayerRenderer ??= new BorderLayerRenderer()).SetCornerRadius(this, cornerRadius);

		protected void SetBorder(Thickness thickness, Brush brush, CornerRadius cornerRadius)
			=> (_borderLayerRenderer ??= new BorderLayerRenderer()).SetBorder(this, thickness, brush, cornerRadius);

		partial void OnBackgroundSizingChangedPartial(DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
		{
			if (dependencyPropertyChangedEventArgs.NewValue is BackgroundSizing sizing)
			{
				SetStyle("background-clip", sizing == BackgroundSizing.InnerBorderEdge ? "padding-box" : "border-box");
			}
		}

		#region Name Dependency Property

		private void OnNameChanged(string oldValue, string newValue)
		{
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled)
			{
				Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(this, newValue);
			}

			if (FeatureConfiguration.UIElement.AssignDOMXamlName)
			{
				Uno.UI.Xaml.WindowManagerInterop.SetName(HtmlId, newValue);
			}
		}

		[GeneratedDependencyProperty(DefaultValue = "", ChangedCallback = true)]
		public static DependencyProperty NameProperty { get; } = CreateNameProperty();

		public string Name
		{
			get => GetNameValue();
			set => SetNameValue(value);
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

		#region HorizontalAlignment Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = Xaml.HorizontalAlignment.Stretch,
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

#if DEBUG
		private void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
			}
		}
#endif

		/// <summary>
		/// If corresponding feature flag is enabled, set layout properties as DOM attributes to aid in debugging.
		/// </summary>
		/// <remarks>
		/// Calls to this method should be wrapped in a check of the feature flag, to avoid the expense of a virtual method call
		/// that will most of the time do nothing in hot code paths.
		/// </remarks>
		private protected override void UpdateDOMProperties()
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties && IsLoaded)
			{
				UpdateDOMXamlProperty(nameof(Margin), Margin);
				UpdateDOMXamlProperty(nameof(HorizontalAlignment), HorizontalAlignment);
				UpdateDOMXamlProperty(nameof(VerticalAlignment), VerticalAlignment);
				UpdateDOMXamlProperty(nameof(Width), Width);
				UpdateDOMXamlProperty(nameof(Height), Height);
				UpdateDOMXamlProperty(nameof(MinWidth), MinWidth);
				UpdateDOMXamlProperty(nameof(MinHeight), MinHeight);
				UpdateDOMXamlProperty(nameof(MaxWidth), MaxWidth);
				UpdateDOMXamlProperty(nameof(MaxHeight), MaxHeight);

				if (this is Control control)
				{
					UpdateDOMXamlProperty(nameof(Control.IsEnabled), control.IsEnabled);
				}

				if (this.TryGetPadding(out var padding))
				{
					UpdateDOMXamlProperty("Padding", padding);
				}

				base.UpdateDOMProperties();
			}
		}
	}
}
