using System;
using Android.Views;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.UI.Xaml.Input;
using Android.Runtime;
using Java.Interop;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase : ContentControl
	{
		private readonly SerialDisposable _touchSubscription = new SerialDisposable();
		private readonly SerialDisposable _isEnabledSubscription = new SerialDisposable();

		partial void PartialInitializeProperties()
		{
			// need the Tapped event to be registered for "Click" to work properly
			Tapped += (snd, evt) => { };
			Clickable = true;
		}

		partial void OnLoadedPartial()
		{
			Focusable = true;
			FocusableInTouchMode = true;

			OnCanExecuteChanged();
		}

		partial void OnUnloadedPartial()
		{
			_isEnabledSubscription.Disposable = null;
		}

		partial void OnIsEnabledChangedPartial(IsEnabledChangedEventArgs e)
		{
			Clickable = e.NewValue;
		}

		partial void RegisterEvents()
		{
			_touchSubscription.Disposable = null;

			View uiControl = GetUIControl();

			var nativeButton = uiControl as Android.Widget.Button;
			if (nativeButton is Android.Widget.Button)
			{
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
			}
			else
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Warn($"ControlTemplateRoot is not available, {this.GetType()} will not be clickable");
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
	}
}
