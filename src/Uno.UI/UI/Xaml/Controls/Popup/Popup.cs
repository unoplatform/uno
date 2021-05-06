using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml.Media;
using Uno.UI;

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

		internal bool IsSubMenu { get; set; }

		/// <summary>
		/// Returns true if the popup should show the light-dismiss overlay with its current configuration, false if not
		/// </summary>
		private bool ShouldShowLightDismissOverlay
		{
			get
			{
				switch (LightDismissOverlayMode)
				{
					case LightDismissOverlayMode.Auto:
						// For now, default to false for all platforms. This may be adjusted in the future.
						return false;
					case LightDismissOverlayMode.On:
						return true;
					case LightDismissOverlayMode.Off:
						return false;
				}

				throw new InvalidOperationException($"Invalid value {LightDismissOverlayMode} for {nameof(LightDismissOverlayMode)}");
			}
		}

		public Popup()
		{
			Initialize();
		}

		private void Initialize()
		{
			InitializePartial();

				ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "PopupLightDismissOverlayBackground", isThemeResourceExtension: true);

			ApplyLightDismissOverlayMode();
		}

		partial void InitializePartial();

		/// <summary>
		/// The <see cref="PopupPanel"/> which host this Popup
		/// </summary>
		internal PopupPanel PopupPanel
		{
			get => (PopupPanel)GetValue(PopupPanelProperty);
			set => SetValue(PopupPanelProperty, value);
		}

		public static DependencyProperty PopupPanelProperty { get ; } =
			DependencyProperty.Register("PopupPanel", typeof(PopupPanel), typeof(Popup), new FrameworkPropertyMetadata(null, (s, e) => ((Popup)s)?.OnPopupPanelChanged((PopupPanel)e.OldValue, (PopupPanel)e.NewValue)));

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

			ApplyLightDismissOverlayMode();
		}

		partial void OnPopupPanelChangedPartial(PopupPanel oldHost, PopupPanel newHost);

		protected override void OnVerticalOffsetChanged(double oldVerticalOffset, double newVerticalOffset)
		{
			base.OnVerticalOffsetChanged(oldVerticalOffset, newVerticalOffset);
			PopupPanel?.InvalidateMeasure();
		}

		protected override void OnHorizontalOffsetChanged(double oldHorizontalOffset, double newHorizontalOffset)
		{
			base.OnHorizontalOffsetChanged(oldHorizontalOffset, newHorizontalOffset);
			PopupPanel?.InvalidateMeasure();
		}

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

			if (popup.Child is FrameworkElement content)
			{
				var position = e.GetCurrentPoint(content).Position;
				if (
					position.X < 0 || position.X > content.ActualWidth
					               || position.Y < 0 || position.Y > content.ActualHeight)
				{
					popup.IsOpen = false;
				}
			}
			else if (popup.Child == null)
			{
				popup.Log().Warn($"Dismiss is not supported if the 'Child' is null.");
			}
			else
			{
				popup.Log().Warn($"Dismiss is not supported if the 'Child' ({popup.Child?.GetType()}) of the 'Popup' is not a 'FrameworkElement'.");
			}

			// Note: This is not the right way: we should instead listen for the PointerPressed directly on the Child
			//		 so we should be able to still dismiss the Popup if the background if the Child is `null`.
			//		 But this would require us to refactor more deeply the Popup which is not the purpose of the current work.
		};

		internal override void UpdateThemeBindings()
		{
			base.UpdateThemeBindings();

			// Ensure bindings are updated on the child, which may be part of an isolated visual tree on some platforms (ie Android).
			Application.PropagateThemeChanged(Child);
		}

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode),
			typeof(Popup),
			new FrameworkPropertyMetadata(defaultValue: default(LightDismissOverlayMode), propertyChangedCallback: (o, e) => ((Popup)o).ApplyLightDismissOverlayMode()));

		private void ApplyLightDismissOverlayMode()
		{
			if (PopupPanel != null)
			{
				PopupPanel.Background = GetPanelBackground();
			}
		}

		/// <summary>
		/// Get background Brush to use for the PopupPanel.
		/// </summary>
		private Brush GetPanelBackground()
		{
			if (ShouldShowLightDismissOverlay)
			{
				return LightDismissOverlayBackground;
			}
			else if (IsLightDismissEnabled)
			{
				return SolidColorBrushHelper.Transparent;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc, static resource values.
		/// </summary>
		internal Brush LightDismissOverlayBackground
		{
			get { return (Brush)GetValue(LightDismissOverlayBackgroundProperty); }
			set { SetValue(LightDismissOverlayBackgroundProperty, value); }
		}

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get ; } =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(Popup), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (o, e) => ((Popup)o).ApplyLightDismissOverlayMode()));

		[Uno.NotImplemented]
		public bool ShouldConstrainToRootBounds { get; set; }
	}
}
