#if IS_UNO
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#elif NET461 || NETSTANDARD2_0
using View = Windows.UI.Xaml.FrameworkElement;
#else
using View = System.Object;
#endif

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A support element for the DeferLoadStrategy Lazy Xaml directive.
	/// </summary>
	/// <remarks>This control is added in the visual tree, in place of the original content.</remarks>
	public partial class ElementStub : FrameworkElement
    {
		private View _content;

		public bool Load
		{
			get { return (bool)GetValue(LoadProperty); }
			set { SetValue(LoadProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Load.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LoadProperty =
			DependencyProperty.Register("Load", typeof(bool), typeof(ElementStub), new PropertyMetadata(
				false, OnLoadChanged));

		private static void OnLoadChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if ((bool)args.NewValue)
			{
				((ElementStub)dependencyObject).Materialize();
			}
			else
			{
				((ElementStub)dependencyObject).Dematerialize();
			}
		}


		/// <summary>
		/// A function that will create the actual view.
		/// </summary>
		public Func<View> ContentBuilder { get; set; }

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);

			if (ContentBuilder != null
				&& oldValue == Visibility.Collapsed 
				&& newValue == Visibility.Visible
			)
			{
				Materialize(isVisibilityChanged: true);
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			if (ContentBuilder != null
				&& Visibility == Visibility.Visible
			)
			{
				Materialize();
			}
		}

		public void Materialize()
			=> Materialize(isVisibilityChanged: false);

		private void Materialize(bool isVisibilityChanged)
		{
			_content = SwapViews(oldView: this as View, newViewProvider: ContentBuilder);
			var targetDependencyObject = _content as DependencyObject;

			if (isVisibilityChanged && targetDependencyObject != null)
			{
				var visibilityProperty = GetVisibilityProperty(_content);

				// Set the visibility at the same precedence it was currently set with on the stub.
				var precedence = this.GetCurrentHighestValuePrecedence(visibilityProperty);

				targetDependencyObject.SetValue(visibilityProperty, Visibility.Visible, precedence);
			}
		}

		private void Dematerialize()
		{
			var newView = SwapViews(oldView: _content, newViewProvider: () => this as View);
			if (newView != null)
			{
				_content = null;
			}
		}

		private static DependencyProperty GetVisibilityProperty(View view)
		{
			if (view is FrameworkElement)
			{
				return VisibilityProperty;
			}
			else
			{
				return DependencyProperty.GetProperty(view.GetType(), nameof(Visibility));
			}
		}
	}
}
#endif
