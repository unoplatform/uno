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
		partial void OnUnloadedPartial();

		protected FrameworkElement()
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
			OnUnloadedPartial();
		}

		public event RoutedEventHandler Loaded;
		public event RoutedEventHandler Unloaded;
		public event TypedEventHandler<FrameworkElement, object> Loading;

		public TransitionCollection Transitions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public object FindName(string name)
			=> IFrameworkElementHelper.FindName(this, this, name);


		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public Size AdjustArrange(Size finalSize)
		{
			return finalSize;
		}

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
		public static DependencyProperty BackgroundProperty { get; } =
			DependencyProperty.Register("Background", typeof(Brush), typeof(FrameworkElement), new FrameworkPropertyMetadata(null, (s, e) => ((FrameworkElement)s)?.OnBackgroundChanged(e)));

		protected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region HorizontalAlignment Dependency Property

		[GeneratedDependencyProperty(DefaultValue = HorizontalAlignment.Stretch, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty HorizontalAlignmentProperty { get; } = CreateHorizontalAlignmentProperty();
		public HorizontalAlignment HorizontalAlignment
		{
			get => GetHorizontalAlignmentValue();
			set => SetHorizontalAlignmentValue(value);
		}
		#endregion

		#region VerticalAlignment Dependency Property

		[GeneratedDependencyProperty(DefaultValue = HorizontalAlignment.Stretch, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty VerticalAlignmentProperty { get; } = CreateVerticalAlignmentProperty();

		public VerticalAlignment VerticalAlignment
		{
			get => GetVerticalAlignmentValue();
			set => SetVerticalAlignmentValue(value);
		}
		#endregion

		#region Width Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.NaN, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty WidthProperty { get; } = CreateWidthProperty();

		public double Width
		{
			get => GetWidthValue();
			set => SetWidthValue(value);
		}
		#endregion

		#region Height Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.NaN, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty HeightProperty { get; } = CreateHeightProperty();


		public double Height
		{
			get => GetHeightValue();
			set => SetHeightValue(value);
		}
		#endregion

		#region MinWidth Dependency Property
		[GeneratedDependencyProperty(DefaultValue = 0.0, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty MinWidthProperty { get; } = CreateMinWidthProperty();

		public double MinWidth
		{
			get => GetMinWidthValue();
			set => SetMinWidthValue(value);
		}
		#endregion

		#region MinHeight Dependency Property
		[GeneratedDependencyProperty(DefaultValue = 0.0, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty MinHeightProperty { get; } = CreateMinHeightProperty();

		public double MinHeight
		{
			get => GetMinHeightValue();
			set => SetMinHeightValue(value);
		}
		#endregion

		#region MaxWidth Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty MaxWidthProperty { get; } = CreateMaxWidthProperty();

		public double MaxWidth
		{
			get => GetMaxWidthValue();
			set => SetMaxWidthValue(value);
		}
		#endregion

		#region MaxHeight Dependency Property
		[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity, Options = FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]
		public static DependencyProperty MaxHeightProperty { get; } = CreateMaxHeightProperty();

		public double MaxHeight
		{
			get => GetMaxHeightValue();
			set => SetMaxHeightValue(value);
		}
		#endregion

		private void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)
		{
			this.InvalidateMeasure();
		}
	}
}
