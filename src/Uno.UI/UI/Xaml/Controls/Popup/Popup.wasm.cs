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

		internal UIElement Anchor { get; set; }

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
				previousPanel.PointerPressed -= Panel_PointerPressed;
			}
			if (newPanel != null)
			{
				newPanel.PointerPressed += Panel_PointerPressed;
			}
		}

		private void Panel_PointerPressed(object sender, Input.PointerRoutedEventArgs e)
		{
			if (IsLightDismissEnabled)
			{
				IsOpen = false;
			}
		}

	}
}
