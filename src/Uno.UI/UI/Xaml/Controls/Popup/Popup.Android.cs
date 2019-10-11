using Android.App;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using static Android.Views.View;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class Popup
	{
		private readonly PopupWindow _popupWindow;

		internal FlyoutPlacementMode Placement { get; set; }

		public Popup()
		{
			_popupWindow = new PopupWindow(this, WindowManagerLayoutParams.MatchParent, WindowManagerLayoutParams.MatchParent, true);

			_popupWindow.Width = WindowManagerLayoutParams.MatchParent;
			_popupWindow.Height = WindowManagerLayoutParams.MatchParent;
			_popupWindow.Focusable = true;
			_popupWindow.Touchable = true;

			OnIsLightDismissEnabledChanged(false, true);

			_popupWindow.DismissEvent += OnPopupDismissed;

			PopupPanel = new PopupPanel(this);
		}

		partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel)
		{
			previousPanel?.Children.Clear();

			if (PopupPanel != null)
			{
				if (Child != null)
				{
					PopupPanel.Children.Add(Child);
				}
			}

			_popupWindow.ContentView = newPanel;

			UpdatePopupPanelDismissibleBackground(IsLightDismissEnabled);
		}

		private void OnPopupDismissed(object sender, EventArgs e)
		{
			IsOpen = false;
		}

		protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
		{
			base.OnIsOpenChanged(oldIsOpen, newIsOpen);
			if (newIsOpen)
			{
				PopupPanel.Visibility = Visibility.Visible;
				_popupWindow.ShowAtLocation(Anchor ?? this, GravityFlags.Left | GravityFlags.Top, 0, 0);
			}
			else
			{
				if(_popupWindow.IsShowing)
				{
					_popupWindow.Dismiss();
				}
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}

		protected override void OnChildChanged(View oldChild, View newChild)
		{
			base.OnChildChanged(oldChild, newChild);

			PopupPanel.Children.Remove(oldChild);
			PopupPanel.Children.Add(newChild);
		}

		private void Panel_PointerPressed(object sender, Input.PointerRoutedEventArgs e)
		{
			if (IsLightDismissEnabled)
			{
				IsOpen = false;
			}
		}

		protected override void OnIsLightDismissEnabledChanged(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
		{
			base.OnIsLightDismissEnabledChanged(oldIsLightDismissEnabled, newIsLightDismissEnabled);
			if (newIsLightDismissEnabled)
			{
				_popupWindow.OutsideTouchable = true;

				_popupWindow.SetBackgroundDrawable(new ColorDrawable(Colors.Transparent));
			}
			else
			{
				_popupWindow.OutsideTouchable = false;

				_popupWindow.SetBackgroundDrawable(null);
			}

			UpdatePopupPanelDismissibleBackground(newIsLightDismissEnabled);
		}

		private void UpdatePopupPanelDismissibleBackground(bool isLightDismiss)
		{
			var popupPanel = PopupPanel;
			if (popupPanel == null)
			{
				return; // nothing to do
			}

			if (isLightDismiss)
			{
				PopupPanel.Background = new SolidColorBrush(Colors.Transparent);
			}
			else
			{
				PopupPanel.Background = null;
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			// Ensure Popup doesn't take any space.
			this.SetMeasuredDimension(0, 0);
		}

		/// <summary>
		/// Prevent the popup from stealing focus from views in the main window.
		/// </summary>
		internal void DisableFocus()
		{
			_popupWindow.Focusable = false;
			_popupWindow.InputMethodMode = InputMethod.Needed;
		}
	}
}
