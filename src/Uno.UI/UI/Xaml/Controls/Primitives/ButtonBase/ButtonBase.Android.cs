using Android.App;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Uno;
using Uno.Client;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase : ContentControl
	{
		private readonly SerialDisposable _touchSubscription = new SerialDisposable();
		private readonly SerialDisposable _isEnabledSubscription = new SerialDisposable();

		protected override void OnLoaded()
		{
			base.OnLoaded();

			Focusable = true;
			FocusableInTouchMode = true;

			RegisterEvents();

			OnCanExecuteChanged();
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_isEnabledSubscription.Disposable = null;
			_touchSubscription.Disposable = null;
		}


		partial void OnIsEnabledChangedPartial(bool oldValue, bool newValue)
		{
			Clickable = newValue;
		}

		partial void RegisterEvents()
		{
			_touchSubscription.Disposable = null;
			_isEnabledSubscription.Disposable = null;

			View uiControl = GetUIControl();

			var nativeButton = uiControl as Android.Widget.Button;
			if (nativeButton is Android.Widget.Button)
			{
				this.Log().Debug("Template contains Android.Widget.Button, hooking up to Click and syncing IsEnabled state");

				_touchSubscription.Disposable = uiControl
					.RegisterClick((e, s) =>
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug("TouchUpInside, executing command");
						}

						OnClick();
					});

				_isEnabledSubscription.Disposable =
					DependencyObjectExtensions.RegisterDisposablePropertyChangedCallback(
						this,
						IsEnabledProperty,
						(s, e) => uiControl.Enabled = IsEnabled
					);

				uiControl.Enabled = IsEnabled;
			}
			else if (uiControl != null)
			{
				PointerExited += OnPointerExited;
				PointerEntered += OnPointerEntered;
				PointerCanceled += OnPointerCanceled;
				PointerPressed += OnPointerPressed;
				PointerReleased += OnPointerReleased;

				_touchSubscription.Disposable = Disposable.Create(() =>
				{
					PointerExited -= OnPointerExited;
					PointerEntered -= OnPointerEntered;
					PointerCanceled -= OnPointerCanceled;
					PointerPressed -= OnPointerPressed;
					PointerReleased -= OnPointerReleased;
				});
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().WarnFormat("ControlTemplateRoot is not available, {0} will not be clickable", this.GetType());
				}
			}
		}

		/// <summary>
		/// Gets the native UI Control, if any.
		/// </summary>
		private View GetUIControl()
		{
			return
				// Check for non-templated ContentControl root (ContentPresenter bypass)
				ContentTemplateRoot

				// Then check for complex templated controls (where the native button is not at the root)
				?? (TemplatedRoot as ViewGroup)
					?.FindFirstChild<ContentPresenter>()
					?.ContentTemplateRoot

				// Finally check for templated ContentControl root
				?? TemplatedRoot as View
				;
		}


		// TODO: these event handlers should be removed when overrides are correctly called from Control
		private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			OnPointerPressed(args);
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
		{
			OnPointerReleased(args);
		}

		private void OnPointerCanceled(object sender, PointerRoutedEventArgs args)
		{
			OnPointerCanceled(args);
		}

		private void OnPointerExited(object sender, PointerRoutedEventArgs args)
		{
			OnPointerExited(args);
		}

		private void OnPointerEntered(object sender, PointerRoutedEventArgs args)
		{
			OnPointerEntered(args);
		}
	}
}
