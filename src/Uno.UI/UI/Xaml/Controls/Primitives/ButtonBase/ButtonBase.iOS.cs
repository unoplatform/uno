using System;
using Windows.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Disposables;
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

			PreRaiseTapped += OnPreRaiseTapped;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_clickSubscription.Disposable = null;

			PreRaiseTapped -= OnPreRaiseTapped;
		}

		private void OnPreRaiseTapped(object sender, EventArgs e)
		{
			// This even is raised only when the source is a Uno-managed control
			// (when not using native styling)
			OnClick();
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
				return; // non-native styling, no need to hook events: already done in UIElement
			}

			// When the "Content Element" is a UIControl, it means
			// we're using native styling: we need to "simulate" the same
			// events as UIElement (because there's no UIElement in this case)

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("ControlTemplateRoot is a UIControl, hooking on to AllTouchEvents and TouchUpInside");
			}

			void pressHandler(object e, EventArgs s)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("AllTouchEvents, trigger OnPointerPressed");
				}

				OnPointerPressed(new PointerRoutedEventArgs { OriginalSource = this });
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

			uiControl.AllTouchEvents += pressHandler;
			uiControl.TouchUpInside += clickHandler;
			IsEnabledChanged += enabledHandler;

			void unregister()
			{
				uiControl.AllTouchEvents -= pressHandler;
				uiControl.TouchUpInside -= clickHandler;
				IsEnabledChanged -= enabledHandler;
			}

			_clickSubscription.Disposable = Disposable.Create(unregister);
		}

		protected override void OnTapped(TappedRoutedEventArgs e) => base.OnTapped(e);

		private UIView GetContentElement()
		{
			return TemplatedRoot
					?.FindFirstChild<ContentPresenter>()
					?.ContentTemplateRoot
				?? TemplatedRoot as UIView;
		}

	}
}
