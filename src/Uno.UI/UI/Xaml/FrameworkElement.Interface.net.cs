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
	}
}
