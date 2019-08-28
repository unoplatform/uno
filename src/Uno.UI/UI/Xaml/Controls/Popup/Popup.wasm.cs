using Uno.Extensions;
using Uno.Disposables;
using Uno.Logging;
using Windows.UI.Xaml.Controls.Primitives;
using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class Popup
	{
		private readonly SerialDisposable _closePopup = new SerialDisposable();

		public Popup()
		{
			PopupPanel = new PopupPanel(this);
		}

		protected override void OnChildChanged(FrameworkElement oldChild, FrameworkElement newChild)
		{
			base.OnChildChanged(oldChild, newChild);

			PopupPanel.Children.Remove(oldChild);
			PopupPanel.Children.Add(newChild);
		}

		protected override void OnIsLightDismissEnabledChanged(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
		{
			base.OnIsLightDismissEnabledChanged(oldIsLightDismissEnabled, newIsLightDismissEnabled);

			(PopupPanel.Parent as PopupRoot)?.UpdateLightDismissArea();
		}

		protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
		{
			base.OnIsOpenChanged(oldIsOpen, newIsOpen);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Popup.IsOpenChanged({oldIsOpen}, {newIsOpen})");
			}

			if (newIsOpen)
			{
				_closePopup.Disposable = Window.Current.OpenPopup(this);
				PopupPanel.Visibility = Visibility.Visible;
			}
			else
			{
				_closePopup.Disposable = null;
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}

		partial void OnPopupPanelChanged(DependencyPropertyChangedEventArgs e)
		{
			var previousPanel = e.OldValue as PopupPanel;
			var newPanel = e.NewValue as PopupPanel;

			previousPanel?.Children.Clear();

			if (PopupPanel != null)
			{
				if (Child != null)
				{
					PopupPanel.Children.Add(Child);
				}
			}

			if (previousPanel != null)
			{
				previousPanel.PointerPressed -= OnPanelPointerPressed;
				previousPanel.PointerReleased -= OnPanelPointerReleased;
			}
			if (newPanel != null)
			{
				newPanel.PointerPressed += OnPanelPointerPressed;
				newPanel.PointerReleased += OnPanelPointerReleased;
			}
		}

		private bool _pressed;

		private void OnPanelPointerPressed(object sender, Input.PointerRoutedEventArgs args)
		{
			// Both pressed & released must reach
			// the popup to close it.
			// (and, obviously, the popup must be light dismiss!)
			_pressed = IsLightDismissEnabled;
		}

		private void OnPanelPointerReleased(object sender, Input.PointerRoutedEventArgs args)
		{
			if (_pressed && IsLightDismissEnabled)
			{
				// Received the completed sequence
				// pressed + released: we can close.
				IsOpen = false;
			}
		}
	}
}
