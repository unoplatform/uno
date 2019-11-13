using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Text;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : UIElement, IFrameworkElement
	{
		public T FindFirstParent<T>() where T : class
		{
			var view = this.Parent;
			while (view != null)
			{
				var typed = view as T;
				if (typed != null)
				{
					return typed;
				}
				view = view.GetParent() as DependencyObject;
			}
			return null;
		}

		partial void Initialize();

		public FrameworkElement()
		{
			Initialize();
		}

		protected virtual void OnLoading()
		{
			Loading?.Invoke(this, null);
		}

		protected virtual void OnLoaded()
		{
			Loaded?.Invoke(this, new RoutedEventArgs(this));
		}

		protected virtual void OnUnloaded()
		{
			Unloaded?.Invoke(this, new RoutedEventArgs(this));
		}

		public event RoutedEventHandler Loaded;
		public event RoutedEventHandler Unloaded;
		public event TypedEventHandler<FrameworkElement, object> Loading;

		public TransitionCollection Transitions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public IFrameworkElement FindName(string name)
			=> IFrameworkElementHelper.FindName(this, GetChildren(), name);


		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public Size AdjustArrange(Size finalSize)
		{
			return finalSize;
		}

		#region IsEnabled DependencyProperty

		public bool IsEnabled
		{
			get { return (bool)GetValue(IsEnableddProperty); }
			set { SetValue(IsEnableddProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Enabled.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsEnableddProperty =
			DependencyProperty.Register("IsEnabled", typeof(bool), typeof(FrameworkElement), new PropertyMetadata(true, (s, e) => ((FrameworkElement)s)?.OnIsEnabledChanged((bool)e.OldValue, (bool)e.NewValue)));

		protected virtual void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
		}

		#endregion

		public int? RenderPhase
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public void ApplyBindingPhase(int phase) => throw new NotImplementedException();

		#region Background DependencyProperty

		public Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		// Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BackgroundProperty =
			DependencyProperty.Register("Background", typeof(Brush), typeof(FrameworkElement), new PropertyMetadata(null, (s, e) => ((FrameworkElement)s)?.OnBackgroundChanged(e)));


		protected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region HorizontalAlignment Dependency Property

		public static readonly DependencyProperty HorizontalAlignmentProperty =
			DependencyProperty.Register(
				"HorizontalAlignment",
				typeof(HorizontalAlignment),
				typeof(FrameworkElement),
				new PropertyMetadata(HorizontalAlignment.Stretch, OnGenericPropertyUpdated)
			);

		public HorizontalAlignment HorizontalAlignment
		{
			get { return (HorizontalAlignment)this.GetValue(HorizontalAlignmentProperty); }
			set { this.SetValue(HorizontalAlignmentProperty, value); }
		}
		#endregion

		#region VerticalAlignment Dependency Property

		public static readonly DependencyProperty VerticalAlignmentProperty =
			DependencyProperty.Register(
				"VerticalAlignment",
				typeof(VerticalAlignment),
				typeof(FrameworkElement),
				new PropertyMetadata(VerticalAlignment.Stretch, OnGenericPropertyUpdated)
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
					propertyChangedCallback: OnGenericPropertyUpdated,
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
					propertyChangedCallback: OnGenericPropertyUpdated,
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
					propertyChangedCallback: OnGenericPropertyUpdated,
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
					propertyChangedCallback: OnGenericPropertyUpdated,
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
					propertyChangedCallback: OnGenericPropertyUpdated,
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
					propertyChangedCallback: OnGenericPropertyUpdated,
					options: FrameworkPropertyMetadataOptions.AutoConvert
				)
			);

		public double MaxHeight
		{
			get { return (double)this.GetValue(MaxHeightProperty); }
			set { this.SetValue(MaxHeightProperty, value); }
		}
		#endregion

		private static void OnGenericPropertyUpdated(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			OnGenericPropertyUpdatedPartial(dependencyObject, args);
			((FrameworkElement)dependencyObject).InvalidateMeasure();
		}
	}
}
