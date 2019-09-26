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

		public Popup()
		{
		}

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

			if (PopupPanel != null)
			{
				if (Child != null)
				{
					// Make sure that the child does not find itself without a TemplatedParent
					if (PopupPanel.TemplatedParent == null)
					{
						PopupPanel.TemplatedParent = TemplatedParent;
					}

					PopupPanel.AddSubview(Child);
				}

				RegisterPopupPanel();
			}
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			RegisterPopupPanel();
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

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			PopupPanel?.RemoveFromSuperview();
		}

		protected override void OnChildChanged(UIView oldChild, UIView newChild)
		{
			base.OnChildChanged(oldChild, newChild);

			if (PopupPanel != null)
			{
				if (oldChild != null)
				{
					PopupPanel.RemoveChild(oldChild);
				}

				if (newChild != null)
				{
					PopupPanel.AddSubview(newChild);
				}
			}
		}

		protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
		{
			base.OnIsOpenChanged(oldIsOpen, newIsOpen);

			UpdateLightDismissLayer(newIsOpen);

			EnsureForward();
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
			PopupPanel.Superview?.BringSubviewToFront(PopupPanel);
		}
	}
}
