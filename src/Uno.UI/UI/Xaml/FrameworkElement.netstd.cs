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
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		partial void OnLoadingPartial();

		private void OnLoading(object sender, RoutedEventArgs args)
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

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			IsLoaded = true;

			foreach (var child in _children)
			{
				(child as FrameworkElement)?.InternalDispatchEvent("loaded", args);
			}

			OnLoaded();
		}

		private void OnUnloaded(object sender, RoutedEventArgs args)
		{
			IsLoaded = false;

			foreach (var child in _children)
			{
				(child as FrameworkElement)?.InternalDispatchEvent("unloaded", args);
			}

			OnUnloaded();
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
		}

		static partial void OnGenericPropertyUpdatedPartial(object dependencyObject, DependencyPropertyChangedEventArgs args);

		public event RoutedEventHandler Loading
		{
			add => RegisterEventHandler("loading", value);
			remove => UnregisterEventHandler("loading", value);
		}

		public event RoutedEventHandler Loaded
		{
			add => RegisterEventHandler("loaded", value);
			remove => UnregisterEventHandler("loaded", value);
		}

		public event RoutedEventHandler Unloaded
		{
			add => RegisterEventHandler("unloaded", value);
			remove => UnregisterEventHandler("unloaded", value);
		}

		public bool IsLoaded { get; private set; } = false;

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
				new PropertyMetadata(Thickness.Empty, OnMeasurePropertyUpdated)
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
				new PropertyMetadata(HorizontalAlignment.Stretch, OnArrangePropertyUpdated)
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
				new PropertyMetadata(VerticalAlignment.Stretch, OnArrangePropertyUpdated)
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
					propertyChangedCallback: OnMeasurePropertyUpdated,
					options: FrameworkPropertyMetadataOptions.AutoConvert
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
					propertyChangedCallback: OnMeasurePropertyUpdated,
					options: FrameworkPropertyMetadataOptions.AutoConvert
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
					defaultValue: 0.0,
					propertyChangedCallback: OnMeasurePropertyUpdated,
					options: FrameworkPropertyMetadataOptions.AutoConvert
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
					defaultValue: 0.0,
					propertyChangedCallback: OnMeasurePropertyUpdated,
					options: FrameworkPropertyMetadataOptions.AutoConvert
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
					propertyChangedCallback: OnMeasurePropertyUpdated,
					options: FrameworkPropertyMetadataOptions.AutoConvert
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
					propertyChangedCallback: OnMeasurePropertyUpdated,
					options: FrameworkPropertyMetadataOptions.AutoConvert
				)
			);

		public double MaxHeight
		{
			get { return (double)this.GetValue(MaxHeightProperty); }
			set { this.SetValue(MaxHeightProperty, value); }
		}
		#endregion

		private static void OnMeasurePropertyUpdated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var element = dependencyObject as UIElement;
			element?.InvalidateMeasure();
		}

		private static void OnArrangePropertyUpdated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var element = dependencyObject as UIElement;
			element?.InvalidateArrange();
		}
	}
}
