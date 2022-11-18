using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Uno;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Controls.Primitives
{
	[ContentProperty(Name = "Child")]
	public partial class Popup
	{
		private PopupPlacementMode _actualPlacement;


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

		internal bool IsForFlyout => AssociatedFlyout != null;

		private ManagedWeakReference _associatedFlyoutWeakRef;
		internal FlyoutBase AssociatedFlyout
		{
			get => _associatedFlyoutWeakRef?.Target as FlyoutBase;
			set
			{
				WeakReferencePool.ReturnWeakReference(this, _associatedFlyoutWeakRef);
				_associatedFlyoutWeakRef = WeakReferencePool.RentWeakReference(this, value);
			}
		}

		/// <summary>
		/// In WinUI, Popup has IsTabStop set to true by default.
		/// UWP does not include IsTabStop, but Popup is still focusable.
		/// </summary>
		private protected override bool IsTabStopDefaultValue => true;

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

		internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
		{
			if (property == IsLightDismissEnabledProperty)
			{
				defaultValue = FeatureConfiguration.Popup.EnableLightDismissByDefault;
				return true;
			}
			return base.GetDefaultValue2(property, out defaultValue);
		}

		private void Initialize()
		{
			InitializePartial();

			ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "PopupLightDismissOverlayBackground", isThemeResourceExtension: true, isHotReloadSupported: true);

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

		public static DependencyProperty PopupPanelProperty { get; } =
			DependencyProperty.Register("PopupPanel", typeof(PopupPanel), typeof(Popup), new FrameworkPropertyMetadata(null, (s, e) => ((Popup)s)?.OnPopupPanelChanged((PopupPanel)e.OldValue, (PopupPanel)e.NewValue)));

		private void OnPopupPanelChanged(PopupPanel oldHost, PopupPanel newHost)
		{
			OnPopupPanelChangedPartial(oldHost, newHost);

			ApplyLightDismissOverlayMode();
		}

		partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel);

		partial void OnVerticalOffsetChangedPartial(double oldVerticalOffset, double newVerticalOffset)
			=> PopupPanel?.InvalidateMeasure();

		partial void OnHorizontalOffsetChangedPartial(double oldHorizontalOffset, double newHorizontalOffset)
			=> PopupPanel?.InvalidateMeasure();

		internal override void UpdateThemeBindings(Data.ResourceUpdateReason updateReason)
		{
			base.UpdateThemeBindings(updateReason);

			// Ensure bindings are updated on the child, which may be part of an isolated visual tree on some platforms (ie Android).
			Application.PropagateResourcesChanged(Child, updateReason);
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

		/// <summary>
		/// Gets or sets the element to use as the popup's placement target.
		/// </summary>
		public FrameworkElement PlacementTarget
		{
			get => (FrameworkElement)GetValue(PlacementTargetProperty);
			set => SetValue(PlacementTargetProperty, value);
		}

		/// <summary>
		/// Identifies the PlacementTarget dependency property.
		/// </summary>
		public static DependencyProperty PlacementTargetProperty { get; } =
			DependencyProperty.Register(
				nameof(PlacementTarget),
				typeof(FrameworkElement),
				typeof(Popup),
				new FrameworkPropertyMetadata(default(FrameworkElement)));

		/// <summary>
		/// Gets or sets the preferred placement to be used for the popup, in relation to its placement target.
		/// </summary>
		public PopupPlacementMode DesiredPlacement
		{
			get => (PopupPlacementMode)GetValue(DesiredPlacementProperty);
			set => SetValue(DesiredPlacementProperty, value);
		}

		/// <summary>
		/// Identifies the DesiredPlacement dependency property.
		/// </summary>
		public static DependencyProperty DesiredPlacementProperty { get; } =
			DependencyProperty.Register(
				nameof(DesiredPlacement),
				typeof(PopupPlacementMode),
				typeof(Popup),
				new FrameworkPropertyMetadata(default(PopupPlacementMode)));

		/// <summary>
		/// Gets the actual placement of the popup, in relation to its placement target.
		/// </summary>
		public PopupPlacementMode ActualPlacement
		{
			get => _actualPlacement;
			set
			{
				if (_actualPlacement != value)
				{
					_actualPlacement = value;

					ActualPlacementChanged?.Invoke(this, value);
				}
			}
		}

		/// <summary>
		/// Raised when the ActualPlacement property changes.
		/// </summary>
		public event EventHandler<object> ActualPlacementChanged;

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

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get; } =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(Popup), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (o, e) => ((Popup)o).ApplyLightDismissOverlayMode()));
	}
}
