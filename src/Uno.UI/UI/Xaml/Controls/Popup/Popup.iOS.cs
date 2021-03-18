using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
using UIKit;
using System.Linq;
using System.Drawing;
using Windows.UI.Xaml.Input;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class Popup
	{
		private UIView _mainWindow;

		public UIView MainWindow
		{
			get
			{
				if (_mainWindow == null)
				{
					_mainWindow = UIApplication.SharedApplication.KeyWindow ?? UIApplication.SharedApplication.Windows[0];
				}

				return _mainWindow;
			}
		}

		partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel)
		{
			if (previousPanel?.Superview != null)
			{
				// Remove the current child, if any.
				previousPanel.Children.Clear();

				previousPanel.RemoveFromSuperview();
			}

			if (newPanel != null)
			{
				if (Child != null)
				{
					// Make sure that the child does not find itself without a TemplatedParent
					if (newPanel.TemplatedParent == null)
					{
						newPanel.TemplatedParent = TemplatedParent;
					}

					RegisterPopupPanelChild();
				}

				newPanel.Background = GetPanelBackground();

				RegisterPopupPanel();
				RegisterPopupPanelChild();
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			RegisterPopupPanel();
			RegisterPopupPanelChild();
		}

		private void RegisterPopupPanel()
		{
			if (PopupPanel == null)
			{
				PopupPanel = new PopupPanel(this);
			}

			if (PopupPanel.Superview == null)
			{
				MainWindow.AddSubview(PopupPanel);
			}
		}

		private void RegisterPopupPanelChild(bool force = false)
		{
			if ((IsLoaded || force) && Child != null)
			{
				RegisterPopupPanel();

				if (!PopupPanel.Children.Contains(Child))
				{
					PopupPanel.Children.Add(Child);
				}
			}
		}

		private void UnregisterPopupPanelChild(UIElement child = null)
		{
			PopupPanel.Children.Remove(child ?? Child);
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			PopupPanel?.RemoveFromSuperview();
			UnregisterPopupPanelChild();
		}

		protected override void OnChildChanged(UIElement oldChild, UIElement newChild)
		{
			base.OnChildChanged(oldChild, newChild);

			if (PopupPanel != null)
			{
				if (oldChild != null)
				{
					UnregisterPopupPanelChild(oldChild);
				}

				RegisterPopupPanelChild();
			}
		}

		protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
		{
			base.OnIsOpenChanged(oldIsOpen, newIsOpen);

			if (newIsOpen)
			{
				RegisterPopupPanelChild(force: true);
			}
			else
			{
				UnregisterPopupPanelChild();
			}

			UpdateLightDismissLayer(newIsOpen);

			EnsureForward();
		}

		protected override void OnIsLightDismissEnabledChanged(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
		{
			base.OnIsLightDismissEnabledChanged(oldIsLightDismissEnabled, newIsLightDismissEnabled);

			if (PopupPanel != null)
			{
				PopupPanel.Background = GetPanelBackground();
			}
		}

		private void UpdateLightDismissLayer(bool newIsOpen)
		{
			if (PopupPanel != null)
			{
				if (newIsOpen)
				{
					if (PopupPanel.Bounds != MainWindow.Bounds)
					{
						// If the Bounds are different, the screen has probably been rotated.
						// We always want the light dismiss layer to have the same bounds (and frame) as the window.
						PopupPanel.Bounds = MainWindow.Bounds;
						PopupPanel.Frame = MainWindow.Frame;
					}

					PopupPanel.Visibility = Visibility.Visible;
				}
				else
				{
					PopupPanel.Visibility = Visibility.Collapsed;
				}
			}
		}

		/// <summary>
		/// Ensure that Popup panel is forward-most in the window. This ensures it isn't hidden behind the main content, which can happen when
		/// the Popup is created during initial launch.
		/// </summary>
		private void EnsureForward()
		{
			PopupPanel?.Superview?.BringSubviewToFront(PopupPanel);
		}
	}
}
