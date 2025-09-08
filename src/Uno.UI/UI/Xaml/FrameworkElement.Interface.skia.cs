using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Text;
using Uno;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml
{

	public partial class FrameworkElement : UIElement, IFrameworkElement
	{
		public T FindFirstParent<T>() where T : class => FindFirstParent<T>(includeCurrent: false);

		public T FindFirstParent<T>(bool includeCurrent) where T : class
		{
			var view = includeCurrent ? (DependencyObject)this : this.Parent;
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

		#region Transitions Dependency Property

		[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true)]
		public static DependencyProperty TransitionsProperty { get; } = CreateTransitionsProperty();

		public TransitionCollection Transitions
		{
			get => GetTransitionsValue();
			set => SetTransitionsValue(value);
		}

		private void OnTransitionsChanged(DependencyPropertyChangedEventArgs args)
		{

		}
		#endregion

		public object FindName(string name)
			=> IFrameworkElementHelper.FindName(this, this, name);


		public void Dispose()
		{
		}

		public Size AdjustArrange(Size finalSize)
		{
			return finalSize;
		}

		[NotImplemented]
		public int? RenderPhase
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.FrameworkElement", "RenderPhase");
				return null;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.FrameworkElement", "RenderPhase");
			}
		}

		public void ApplyBindingPhase(int phase) { }

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
	}
}
