using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

#if __IOS__
using View = UIKit.UIView;
#elif __ANDROID__
using Android.Views;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Content))]
	public partial class Flyout : FlyoutBase
	{
		public Style FlyoutPresenterStyle
		{
			get { return (Style)this.GetValue(FlyoutPresenterStyleProperty); }
			set { this.SetValue(FlyoutPresenterStyleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FlyoutPresenterStyle.  This enables animation, styling, binding, etc...
		public static DependencyProperty FlyoutPresenterStyleProperty { get; } =
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

		public static DependencyProperty ContentProperty { get; } =
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

			flyout.SynchronizeNamescope();

			if (args.OldValue is DependencyObject oldDO)
			{
				oldDO.ClearValue(NameScope.NameScopeProperty);
			}
		}

		private void SynchronizeNamescope()
		{
			if (NameScope.GetNameScope(this) is { } scope && Content is DependencyObject content)
			{
				NameScope.SetNameScope(content, scope);
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

		protected internal override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			if (Content is IDependencyObjectStoreProvider binder)
			{
				binder.Store.SetValue(binder.Store.DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Local);
			}
		}

		protected override Control CreatePresenter()
		{
			_presenter = new FlyoutPresenter();
			SetPresenterStyle();
			_presenter.Content = Content;

			SynchronizeNamescope();

			return _presenter;
		}
	}
}
