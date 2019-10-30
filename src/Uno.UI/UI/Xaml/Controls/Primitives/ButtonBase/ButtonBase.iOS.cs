using System;
using System.Linq;
using Uno.Extensions;
using Uno.Disposables;
using Uno.Logging;
using Windows.UI.Xaml.Input;

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.UIKit;
#endif

namespace Windows.UI.Xaml.Controls.Primitives
{
	public abstract partial class ButtonBase : ContentControl
	{
		private readonly SerialDisposable _clickSubscription = new SerialDisposable();

		protected override void OnLoaded()
		{
			base.OnLoaded();

			RegisterEvents();

			OnCanExecuteChanged();
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_clickSubscription.Disposable = null;
		}

		partial void RegisterEvents()
		{
			_clickSubscription.Disposable = null;

			if (Window == null)
			{
				// Will be invoked again when this control will be attached loaded.
				return;
			}

			if (!(GetContentElement() is UIControl uiControl))
			{
				// Button is using Windows template, no native events to register to
				return;
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("ControlTemplateRoot is a UIControl, hooking on to AllTouchEvents and TouchUpInside");
			}

			void clickHandler(object e, EventArgs s)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("TouchUpInside, executing command");
				}

				OnClick();

				RaiseEvent(TappedEvent, new TappedRoutedEventArgs { OriginalSource = this });
			}

			//
			// Bind the enabled handler
			// 
			void enabledHandler(object e, DependencyPropertyChangedEventArgs s)
			{
				uiControl.Enabled = IsEnabled;
			}

			uiControl.TouchUpInside += clickHandler;
			IsEnabledChanged += enabledHandler;

			void unregister()
			{
				uiControl.TouchUpInside -= clickHandler;
				IsEnabledChanged -= enabledHandler;
			}

			_clickSubscription.Disposable = Disposable.Create(unregister);
		}

		private UIView GetContentElement()
		{
			return TemplatedRoot
					?.FindFirstChild<ContentPresenter>()
					?.ContentTemplateRoot
				?? TemplatedRoot as UIView;
		}

	}
}
