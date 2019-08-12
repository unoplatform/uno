using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Logging;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using View = Windows.UI.Xaml.UIElement;
using System.Collections;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		partial void OnLoadingPartial();

		/*
			About NativeOn** vs ManagedOn** methods:
				The flag FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded will configure which set of methods will be used
				but they are mutually exclusive: Only one of each is going to be invoked.

			For the managed methods: for perf consideration (avoid lots of casting) the loaded state is managed by the UI Element,
				the FrameworkElement only makes it publicly available by overriding methods from UIElement and raising events.
				The propagation of this loaded state is also made by the UIElement.
		 */

		internal sealed override void ManagedOnLoading()
		{
			OnLoadingPartial();
			ApplyCompiledBindings();

			try
			{
				// Raise event before invoking base in order to raise them top to bottom
				_loading?.Invoke(this, RoutedEventArgs.Empty);
			}
			catch (Exception error)
			{
				this.Log().Error("ManagedOnLoading failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}

			// Explicit propagation of the loading even must be performed
			// after the compiled bindings are applied (cf. OnLoading), as there may be altered
			// properties that affect the visual tree.
			base.ManagedOnLoading();
		}

		private void NativeOnLoading(object sender, RoutedEventArgs args)
		{
			OnLoadingPartial();
			ApplyCompiledBindings();

			// Explicit propagation of the loading even must be performed
			// after the compiled bindings are applied, as there may be altered
			// properties that affect the visual tree.
			foreach (var child in _children)
			{
				(child as FrameworkElement)?.InternalDispatchEvent("loading", args);
			}
		}

		internal sealed override void ManagedOnLoaded()
		{
			// Make sure to set the flag before raising the loaded event (duplicated with the base.ManagedOnLoaded)
			base.IsLoaded = true;

			try
			{
				// Raise event before invoking base in order to raise them top to bottom
				OnLoaded();
				_loaded?.Invoke(this, RoutedEventArgs.Empty);
			}
			catch (Exception error)
			{
				this.Log().Error("ManagedOnLoaded failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}

			base.ManagedOnLoaded();
		}

		private void NativeOnLoaded(object sender, RoutedEventArgs args)
		{
			base.IsLoaded = true;

			foreach (var child in _children)
			{
				(child as FrameworkElement)?.InternalDispatchEvent("loaded", args);
			}

			try
			{
				OnLoaded();
			}
			catch (Exception error)
			{
				this.Log().Error("NativeOnLoaded failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}
		}

		internal sealed override void ManagedOnUnloaded()
		{
			base.ManagedOnUnloaded(); // Will set flag IsLoaded to false

			try
			{
				// Raise event after invoking base in order to raise them bottom to top
				OnUnloaded();
				_unloaded?.Invoke(this, RoutedEventArgs.Empty);
			}
			catch (Exception error)
			{
				this.Log().Error("ManagedOnUnloaded failed in FrameworkElement", error);
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

			try
			{
				OnUnloaded();
			}
			catch (Exception error)
			{
				this.Log().Error("NativeOnUnloaded failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}
		}

		public bool HasParent()
		{
			return Parent != null;
		}
		
		public double ActualWidth { get; internal set; }
		public double ActualHeight { get; internal set; }

		public event SizeChangedEventHandler SizeChanged;

		internal void RaiseSizeChanged(SizeChangedEventArgs args)
		{
			SizeChanged?.Invoke(this, args);
			_renderTransform?.UpdateSize(args.NewSize);
		}

		static partial void OnGenericPropertyUpdatedPartial(object dependencyObject, DependencyPropertyChangedEventArgs args);

		private event RoutedEventHandler _loading;
		public event RoutedEventHandler Loading
		{
			add
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					_loading += value;
				}
				else
				{
					RegisterEventHandler("loading", value);
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
					UnregisterEventHandler("loading", value);
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
					RegisterEventHandler("loaded", value);
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
					UnregisterEventHandler("loaded", value);
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
					RegisterEventHandler("unloaded", value);
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
					UnregisterEventHandler("unloaded", value);
				}
			}
		}

		public new bool IsLoaded => base.IsLoaded; // The IsLoaded state is managed by the UIElement, FrameworkElement only makes it publicly visible

		private bool IsTopLevelXamlView() => throw new NotSupportedException();

		internal void SuspendRendering() => throw new NotSupportedException();

		internal void ResumeRendering() => throw new NotSupportedException();

		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		protected void SetBorder(Thickness thickness, Brush brush, CornerRadius cornerRadius)
		{
			var borderRadius = cornerRadius == CornerRadius.None
				? ""
				: $"{cornerRadius.TopLeft}px {cornerRadius.TopRight}px {cornerRadius.BottomRight}px {cornerRadius.BottomLeft}px";

			if (thickness == Thickness.Empty)
			{
				SetStyle(
					("border-style", "none"),
					("border-color", ""),
					("border-width", ""),
					("border-radius", borderRadius));
			}
			else
			{
				var borderColor = Colors.Transparent;
				var borderImage = "";

				switch (brush)
				{
					case SolidColorBrush solidColorBrush:
						borderColor = solidColorBrush.ColorWithOpacity;
						break;
					case LinearGradientBrush linearGradientBrush:
						borderImage = linearGradientBrush.ToCssString(RenderSize); // TODO: Reevaluate when size is changing
						break;
				}

				SetStyle(
					("border-style", "solid"),
					("border-color", borderColor.ToCssString()),
					("border-image", borderImage),
					("border-width", $"{thickness.Top}px {thickness.Right}px {thickness.Bottom}px {thickness.Left}px"),
					("border-radius", borderRadius));
			}
		}

		internal override bool IsEnabledOverride() => IsEnabled && base.IsEnabledOverride();

		#region Margin Dependency Property

		public static readonly DependencyProperty MarginProperty =
			DependencyProperty.Register(
				"Margin",
				typeof(Thickness),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: Thickness.Empty,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
		);

		public virtual Thickness Margin
		{
			get { return (Thickness)this.GetValue(MarginProperty); }
			set { this.SetValue(MarginProperty, value); }
		}
		#endregion

		#region HorizontalAlignment Dependency Property

		public static readonly DependencyProperty HorizontalAlignmentProperty =
			DependencyProperty.Register(
				"HorizontalAlignment",
				typeof(HorizontalAlignment),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: Xaml.HorizontalAlignment.Stretch,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public HorizontalAlignment HorizontalAlignment
		{
			get { return (HorizontalAlignment)this.GetValue(HorizontalAlignmentProperty); }
			set { this.SetValue(HorizontalAlignmentProperty, value); }
		}
		#endregion

		#region HorizontalAlignment Dependency Property

		public static readonly DependencyProperty VerticalAlignmentProperty =
			DependencyProperty.Register(
				"VerticalAlignment",
				typeof(VerticalAlignment),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: Xaml.VerticalAlignment.Stretch,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public VerticalAlignment VerticalAlignment
		{
			get { return (VerticalAlignment)this.GetValue(VerticalAlignmentProperty); }
			set { this.SetValue(VerticalAlignmentProperty, value); }
		}
		#endregion

		#region Width Dependency Property

		public static readonly DependencyProperty WidthProperty =
			DependencyProperty.Register(
				"Width",
				typeof(double),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: double.NaN,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public double Width
		{
			get { return (double)this.GetValue(WidthProperty); }
			set { this.SetValue(WidthProperty, value); }
		}
		#endregion

		#region Height Dependency Property

		public static readonly DependencyProperty HeightProperty =
			DependencyProperty.Register(
				"Height",
				typeof(double),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: double.NaN,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public double Height
		{
			get { return (double)this.GetValue(HeightProperty); }
			set { this.SetValue(HeightProperty, value); }
		}
		#endregion

		#region MinWidth Dependency Property

		public static readonly DependencyProperty MinWidthProperty =
			DependencyProperty.Register(
				"MinWidth",
				typeof(double),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: 0.0d,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public double MinWidth
		{
			get { return (double)this.GetValue(MinWidthProperty); }
			set { this.SetValue(MinWidthProperty, value); }
		}
		#endregion

		#region MinHeight Dependency Property

		public static readonly DependencyProperty MinHeightProperty =
			DependencyProperty.Register(
				"MinHeight",
				typeof(double),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: 0.0d,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public double MinHeight
		{
			get { return (double)this.GetValue(MinHeightProperty); }
			set { this.SetValue(MinHeightProperty, value); }
		}
		#endregion

		#region MaxWidth Dependency Property

		public static readonly DependencyProperty MaxWidthProperty =
			DependencyProperty.Register(
				"MaxWidth",
				typeof(double),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: double.PositiveInfinity,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public double MaxWidth
		{
			get { return (double)this.GetValue(MaxWidthProperty); }
			set { this.SetValue(MaxWidthProperty, value); }
		}
		#endregion

		#region MaxHeight Dependency Property

		public static readonly DependencyProperty MaxHeightProperty =
			DependencyProperty.Register(
				"MaxHeight",
				typeof(double),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: double.PositiveInfinity,
					options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		public double MaxHeight
		{
			get { return (double)this.GetValue(MaxHeightProperty); }
			set { this.SetValue(MaxHeightProperty, value); }
		}
		#endregion
	}
}
