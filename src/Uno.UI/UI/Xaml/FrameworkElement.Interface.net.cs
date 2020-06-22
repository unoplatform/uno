using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI.Xaml;

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
			OnLoadingPartial();
			Loading?.Invoke(this, null);
		}

		private protected virtual void OnLoaded()
		{
			Loaded?.Invoke(this, new RoutedEventArgs(this));
		}

		private protected virtual void OnUnloaded()
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
			get { return (bool)GetValue(IsEnabledProperty); }
			set { SetValue(IsEnabledProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Enabled.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.Register(
				"IsEnabled",
				typeof(bool),
				typeof(FrameworkElement),
				new PropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((FrameworkElement)s)?.OnIsEnabledChanged((bool)e.OldValue, (bool)e.NewValue),
					coerceValueCallback: (s, v) => (s as FrameworkElement)?.CoerceIsEnabled(v)
				)
			);

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

		[GeneratedDependencyProperty(DefaultValue = HorizontalAlignment.Stretch, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty HorizontalAlignmentProperty = CreateHorizontalAlignmentProperty();
		public HorizontalAlignment HorizontalAlignment
		{
			get => GetHorizontalAlignmentValue();
			set => SetHorizontalAlignmentValue(value);
		}
		#endregion

		#region VerticalAlignment Dependency Property

		[GeneratedDependencyProperty(DefaultValue = HorizontalAlignment.Stretch, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty VerticalAlignmentProperty = CreateVerticalAlignmentProperty();

		public VerticalAlignment VerticalAlignment
		{
			get => GetVerticalAlignmentValue();
			set => SetVerticalAlignmentValue(value);
		}
		#endregion

		#region Width Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.NaN, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty WidthProperty = CreateWidthProperty();

		public double Width
		{
			get => GetWidthValue();
			set => SetWidthValue(value);
		}
		#endregion

		#region Height Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.NaN, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty HeightProperty = CreateHeightProperty();


		public double Height
		{
			get => GetHeightValue();
			set => SetHeightValue(value);
		}
		#endregion

		#region MinWidth Dependency Property
		[GeneratedDependencyProperty(DefaultValue = 0.0, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty MinWidthProperty = CreateMinWidthProperty();

		public double MinWidth
		{
			get => GetMinWidthValue();
			set => SetMinWidthValue(value);
		}
		#endregion

		#region MinHeight Dependency Property
		[GeneratedDependencyProperty(DefaultValue = 0.0, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty MinHeightProperty = CreateMinHeightProperty();

		public double MinHeight
		{
			get => GetMinHeightValue();
			set => SetMinHeightValue(value);
		}
		#endregion

		#region MaxWidth Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty MaxWidthProperty = CreateMaxWidthProperty();

		public double MaxWidth
		{
			get => GetMaxWidthValue();
			set => SetMaxWidthValue(value);
		}
		#endregion

		#region MaxHeight Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static readonly DependencyProperty MaxHeightProperty = CreateMaxHeightProperty();

		public double MaxHeight
		{
			get => GetMaxHeightValue();
			set => SetMaxHeightValue(value);
		}
		#endregion

		private void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)
		{
			OnGenericPropertyUpdatedPartial(args);
			this.InvalidateMeasure();
		}
	}
}
