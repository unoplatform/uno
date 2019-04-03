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
		private readonly SerialDisposable _sizeChangedDisposable = new SerialDisposable();
		protected internal readonly FlyoutPresenter _presenter = new FlyoutPresenter();
		protected internal Popup _popup;

		public Style FlyoutPresenterStyle
		{
			get { return (Style)this.GetValue(FlyoutPresenterStyleProperty); }
			set { this.SetValue(FlyoutPresenterStyleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FlyoutPresenterStyle.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FlyoutPresenterStyleProperty =
			DependencyProperty.Register("FlyoutPresenterStyle", typeof(Style), typeof(Flyout), new PropertyMetadata((Style)null, OnFlyoutPresenterStyleChanged));

		private static void OnFlyoutPresenterStyleChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var flyout = dependencyObject as Flyout;
			if (flyout._presenter != null)
			{
				flyout._presenter.Style = (Style)args.NewValue;
			}
		}

		#region Content DependencyProperty
		public IUIElement Content
		{
			get { return (IUIElement)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register("Content", typeof(IUIElement), typeof(Flyout), new PropertyMetadata(default(IUIElement), OnContentChanged));

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
			_popup = new Popup()
			{
				Child = _presenter = CreatePresenter() as FlyoutPresenter,
			};

			InitializePartial();

			_popup.Opened += OnPopupOpened;
			_popup.Closed += OnPopupClosed;
		}

		private void OnPopupOpened(object sender, object e)
		{
			var child = _popup.Child as FrameworkElement;

			if (child != null)
			{
				SizeChangedEventHandler handler = (_, __) => SetPopupPositionPartial(Target);

				child.SizeChanged += handler;

				_sizeChangedDisposable.Disposable = Disposable
					.Create(() => child.SizeChanged -= handler);
			}
		}

		partial void InitializePartial();

		private void OnPopupClosed(object sender, object e)
		{
			Hide(canCancel: false);
			_sizeChangedDisposable.Disposable = null;
		}

		protected internal override void Close()
		{
			_popup.IsOpen = false;
		}

		protected internal override void Open()
		{
			SetPopupPositionPartial(Target);

			_popup.IsOpen = true;
		}

		partial void SetPopupPositionPartial(View placementTarget);

		//This is present to force the dataContext to be passed to the popup of the flyout since it is not directly a child in the visual tree of the flyout. 
		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			_popup?.SetValue(Popup.DataContextProperty, this.DataContext, precedence: DependencyPropertyValuePrecedences.Local);
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			_popup?.SetValue(Popup.TemplatedParentProperty, TemplatedParent, precedence: DependencyPropertyValuePrecedences.Local);

			if (Content is IDependencyObjectStoreProvider binder)
			{
				binder.Store.SetValue(binder.Store.TemplatedParentProperty, TemplatedParent, DependencyPropertyValuePrecedences.Local);
			}
		}

		protected override Control CreatePresenter()
		{
			return new FlyoutPresenter()
			{
				Style = FlyoutPresenterStyle,
				Content = this.Content
			};
		}
	}
}
