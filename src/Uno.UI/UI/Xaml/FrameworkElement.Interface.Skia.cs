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

		private protected virtual void OnLoaded()
        {

        }

		private protected virtual void OnUnloaded()
        {

        }

        public TransitionCollection Transitions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public object FindName(string name)
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

		public event DependencyPropertyChangedEventHandler IsEnabledChanged;

		[GeneratedDependencyProperty(DefaultValue = true, ChangedCallback = true, CoerceCallback = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty IsEnabledProperty { get; } = CreateIsEnabledProperty();

		public bool IsEnabled
		{
			get => GetIsEnabledValue();
			set => SetIsEnabledValue(value);
		}

		protected virtual void OnIsEnabledChanged(DependencyPropertyChangedEventArgs args)
		{
			OnIsEnabledChanged((bool)args.OldValue, (bool)args.NewValue);
			IsEnabledChanged?.Invoke(this, args);
		}

		protected virtual void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
		}

		#endregion

        public int? RenderPhase
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

		private static readonly Uri DefaultBaseUri = new Uri("ms-appx://local");
		public global::System.Uri BaseUri
		{
			get;
			internal set;
		} = DefaultBaseUri;

		public void ApplyBindingPhase(int phase) => throw new NotImplementedException();

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
    }
}
