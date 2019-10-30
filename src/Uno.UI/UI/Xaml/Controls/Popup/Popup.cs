using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Child")]
	public partial class Popup : PopupBase
	{
		/// <summary>
		/// Overrides the default location of this popup (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// When a Popup is opened, the <see cref="PopupPanel"/> will top/left aligned the <see cref="Child"/> to the
		/// current location of the given popup in the visual tree.
		/// However when an Anchor is set on a popup, the Child will instead be top/left aligned to the location of this Anchor.
		/// </remarks>
		internal UIElement Anchor { get; set; }

		/// <summary>
		/// The <see cref="PopupPanel"/> which host this Popup
		/// </summary>
		internal PopupPanel PopupPanel
		{
			get => (PopupPanel)GetValue(PopupPanelProperty);
			set => SetValue(PopupPanelProperty, value);
		}

		public static readonly DependencyProperty PopupPanelProperty =
			DependencyProperty.Register("PopupPanel", typeof(PopupPanel), typeof(Popup), new PropertyMetadata(null, (s, e) => ((Popup)s)?.OnPopupPanelChanged((PopupPanel)e.OldValue, (PopupPanel)e.NewValue)));

		private void OnPopupPanelChanged(PopupPanel oldHost, PopupPanel newHost)
		{
			// Note: On UWP if the popup is light dismissable, the popup is closed as soon as we get a PointerPressed
			//		 Controls under the popup does not receive any Pointer event while the popup is open,
			//		 and when light dismissed they receive the PointerReleased but not the pressed

			if (oldHost != null)
			{
				oldHost.PointerPressed -= _handleIfOpenedAndTryDismiss; // Cf. comment about child pressed below

				oldHost.PointerEntered -= _handleIfOpened;
				oldHost.PointerExited -= _handleIfOpened;
				oldHost.PointerMoved -= _handleIfOpened;
				oldHost.PointerReleased -= _handleIfOpened;
				oldHost.PointerCanceled -= _handleIfOpened;
				oldHost.PointerCaptureLost -= _handleIfOpened;
				oldHost.PointerWheelChanged -= _handleIfOpened;
			}

			if (newHost != null)
			{
				newHost.PointerPressed += _handleIfOpenedAndTryDismiss; // Cf. comment about child pressed below

				newHost.PointerEntered += _handleIfOpened;
				newHost.PointerExited += _handleIfOpened;
				newHost.PointerMoved += _handleIfOpened;
				newHost.PointerReleased += _handleIfOpened;
				newHost.PointerCanceled += _handleIfOpened;
				newHost.PointerCaptureLost += _handleIfOpened;
				newHost.PointerWheelChanged += _handleIfOpened;
			}

			OnPopupPanelChangedPartial(oldHost, newHost);
		}

		partial void OnPopupPanelChangedPartial(PopupPanel oldHost, PopupPanel newHost);

		private static readonly PointerEventHandler _handleIfOpened = (snd, e) =>
		{
			var popup = ((PopupPanel)snd).Popup;
			if (!popup.IsOpen || !popup.IsLightDismissEnabled)
			{
				return;
			}

			e.Handled = true;
		};
		private static readonly PointerEventHandler _handleIfOpenedAndTryDismiss = (snd, e) =>
		{
			var popup = ((PopupPanel)snd).Popup;
			if (!popup.IsOpen || !popup.IsLightDismissEnabled)
			{
				return;
			}

			e.Handled = true;

			if (!(popup.Child is FrameworkElement content))
			{
				popup.Log().Warn("Dismiss is not supported if the 'Child' of the 'Popup' is not a 'FrameworkElement'.");

				return;
			}

			// Note: This is not the right way: we should instead listen for the PointerPressed directly on the Child
			//		 so we should be able to still dismiss the Popup if the background if the Child is `null`.
			//		 But this would require us to refactor more deeply the Popup which is not the purpose of the current work.
			var position = e.GetCurrentPoint(content).Position;
			if (
				position.X < 0 || position.X > content.ActualWidth
				|| position.Y < 0 || position.Y > content.ActualHeight)
			{
				popup.IsOpen = false;
			}
		};
	}
}
