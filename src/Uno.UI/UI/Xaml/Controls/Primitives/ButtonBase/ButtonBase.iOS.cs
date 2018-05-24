using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI.Views;
using Uno.UI.Views.Controls;
using Windows.UI.Xaml;
using Uno.Disposables;
using System.Windows.Input;
using Uno.Client;
using System.Linq;
using Foundation;
using CoreGraphics;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Input;
using Uno.Logging;

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

			var uiButton = GetContentElement() as UIButton;

			if (uiButton != null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("ControlTemplateRoot is a UIButton, hooking on to TouchUpInside");
				}

				CompositeDisposable subscriptions = new CompositeDisposable();
				_clickSubscription.Disposable = subscriptions;

				EventHandler clickHandler = (e, s) =>
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug("TouchUpInside, executing command");
					}

					OnClick();
				};

				uiButton.TouchUpInside += clickHandler;
				subscriptions.Add(() => uiButton.TouchUpInside -= clickHandler);

				//
				// Bind the enabled handler
				// 
				var enabledHandler = (DependencyPropertyChangedEventHandler)((e, s) => uiButton.Enabled = IsEnabled);
				IsEnabledChanged += enabledHandler;
				subscriptions.Add(() => IsEnabledChanged -= enabledHandler);
			}
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
