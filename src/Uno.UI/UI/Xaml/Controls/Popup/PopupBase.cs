using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media;
#if XAMARIN_IOS
using CoreGraphics;
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using CoreGraphics;
using AppKit;
using View = AppKit.NSView;
#elif XAMARIN_ANDROID
using View = Android.Views.View;
#elif NET461 || __WASM__
using View = Windows.UI.Xaml.FrameworkElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class PopupBase : FrameworkElement, IPopup
	{
		private IDisposable _openPopupRegistration;

		public event EventHandler<object> Closed;
		public event EventHandler<object> Opened;

		public PopupBase()
		{
		}

		partial void OnIsOpenChangedPartial(bool oldIsOpen, bool newIsOpen)
		{
			if (newIsOpen)
			{
				_openPopupRegistration = VisualTreeHelper.RegisterOpenPopup(this);
				Opened?.Invoke(this, newIsOpen);
			}
			else
			{
				_openPopupRegistration?.Dispose();
				Closed?.Invoke(this, newIsOpen);
			}
		}

		partial void OnChildChangedPartial(View oldChild, View newChild)
		{
			if (oldChild is IDependencyObjectStoreProvider provider)
			{
				provider.Store.ClearValue(provider.Store.DataContextProperty, DependencyPropertyValuePrecedences.Local);
				provider.Store.ClearValue(provider.Store.TemplatedParentProperty, DependencyPropertyValuePrecedences.Local);
			}

			UpdateDataContext();
			UpdateTemplatedParent();
		}

		protected internal override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			UpdateDataContext();
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			UpdateTemplatedParent();
		}

		private void UpdateDataContext()
		{
			if (Child is IDependencyObjectStoreProvider provider)
			{
				provider.Store.SetValue(provider.Store.DataContextProperty, this.DataContext, DependencyPropertyValuePrecedences.Local);
			}
		}

		private void UpdateTemplatedParent()
		{
			if (Child is IDependencyObjectStoreProvider provider)
			{
				provider.Store.SetValue(provider.Store.TemplatedParentProperty, this.TemplatedParent, DependencyPropertyValuePrecedences.Local);
			}
		}
	}
}
