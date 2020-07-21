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

		private protected virtual void OnLoading()
		{
			OnLoadingPartial();
		}

		private protected virtual void OnLoaded()
		{
		}

		private protected virtual void OnUnloaded()
		{
		}

		public TransitionCollection Transitions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public Size AdjustArrange(Size finalSize)
		{
			return finalSize;
		}

		public object FindName(string name)
			=> IFrameworkElementHelper.FindName(this, GetChildren(), name);

		#region IsEnabled DependencyProperty

#pragma warning disable 67
		public event DependencyPropertyChangedEventHandler IsEnabledChanged;
#pragma warning restore 67

		public bool IsEnabled
		{
			get { return (bool)GetValue(IsEnabledProperty); }
			set { SetValue(IsEnabledProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Enabled.  This enables animation, styling, binding, etc...
		public static DependencyProperty IsEnabledProperty { get; } =
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

		#region Background DependencyProperty

		public Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		// Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
		public static DependencyProperty BackgroundProperty { get; } =
			DependencyProperty.Register("Background", typeof(Brush), typeof(FrameworkElement), new PropertyMetadata(null, (s, e) => ((FrameworkElement)s)?.OnBackgroundChanged(e)));

		protected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		public int? RenderPhase
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public void ApplyBindingPhase(int phase) => throw new NotImplementedException();

		private void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)
		{
			OnGenericPropertyUpdatedPartial(args);
			this.InvalidateMeasure();
		}
	}
}
