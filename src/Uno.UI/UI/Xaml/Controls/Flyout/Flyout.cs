using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

#if XAMARIN_IOS
using View = UIKit.UIView;
#elif XAMARIN_ANDROID
using Android.Views;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Content")]
	public partial class Flyout : FlyoutBase
	{
		public Style FlyoutPresenterStyle
		{
			get { return (Style)this.GetValue(FlyoutPresenterStyleProperty); }
			set { this.SetValue(FlyoutPresenterStyleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FlyoutPresenterStyle.  This enables animation, styling, binding, etc...
		public static DependencyProperty FlyoutPresenterStyleProperty { get ; } =
			DependencyProperty.Register(
				"FlyoutPresenterStyle",
				typeof(Style),
				typeof(Flyout),
				new FrameworkPropertyMetadata(default(Style), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure, OnFlyoutPresenterStyleChanged));

		private static void OnFlyoutPresenterStyleChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var flyout = dependencyObject as Flyout;
			if (flyout._presenter != null)
			{
				flyout.SetPresenterStyle();
			}
		}

		private void SetPresenterStyle()
		{
			if (FlyoutPresenterStyle != null)
			{
				_presenter.Style = FlyoutPresenterStyle;
			}
			else
			{
				_presenter.ClearValue(FrameworkElement.StyleProperty);
			}
		}

		#region Content DependencyProperty
		public UIElement Content
		{
			get { return (UIElement)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		public static DependencyProperty ContentProperty { get ; } =
			DependencyProperty.Register(
				"Content",
				typeof(UIElement),
				typeof(Flyout),
				new FrameworkPropertyMetadata(default(UIElement), FrameworkPropertyMetadataOptions.AffectsMeasure, OnContentChanged));
		private FlyoutPresenter _presenter;

		private static void OnContentChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var flyout = dependencyObject as Flyout;

			if (flyout._presenter != null)
			{
				if (args.NewValue is IDependencyObjectStoreProvider binder)
				{
					binder.Store.SetValue(binder.Store.TemplatedParentProperty, flyout.TemplatedParent, DependencyPropertyValuePrecedences.Local);
				}

				flyout._presenter.Content = args.NewValue;
			}
		}
		#endregion

		public Flyout()
		{
		}

		protected internal override void Close()
		{
			// This overload is required for binary compatibility
			base.Close();
		}

		protected internal override void Open()
		{
			// This overload is required for binary compatibility
			base.Open();
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			if (Content is IDependencyObjectStoreProvider binder)
			{
				binder.Store.SetValue(binder.Store.TemplatedParentProperty, TemplatedParent, DependencyPropertyValuePrecedences.Local);
			}
		}

		protected override Control CreatePresenter()
		{
			_presenter = new FlyoutPresenter();
			SetPresenterStyle();
			_presenter.Content = Content;

			return _presenter;
		}
	}
}
