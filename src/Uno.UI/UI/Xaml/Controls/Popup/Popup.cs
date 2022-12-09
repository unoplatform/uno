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
				new FrameworkPropertyMetadata(default(FrameworkElement), FrameworkPropertyMetadataOptions.AffectsArrange));

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
				new FrameworkPropertyMetadata(PopupPlacementMode.Auto, FrameworkPropertyMetadataOptions.AffectsArrange));

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
		/// Gets or sets the distance between the left side of the application window and the left side of the popup.
		/// </summary>
		public double HorizontalOffset
		{
			get => (double)GetValue(HorizontalOffsetProperty);
			set => SetValue(HorizontalOffsetProperty, value);
		}

		/// <summary>
		/// Gets the identifier for the HorizontalOffset dependency property.
		/// </summary>
		public static DependencyProperty HorizontalOffsetProperty { get; } =
			DependencyProperty.Register(
				nameof(HorizontalOffset),
				typeof(double),
				typeof(Popup),
				new FrameworkPropertyMetadata(
					0.0,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((Popup)s)?.OnHorizontalOffsetChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnHorizontalOffsetChanged(double oldHorizontalOffset, double newHorizontalOffset)
		{
			OnHorizontalOffsetChangedPartial(oldHorizontalOffset, newHorizontalOffset);
			OnHorizontalOffsetChangedPartialNative(oldHorizontalOffset, newHorizontalOffset);
		}

		partial void OnHorizontalOffsetChangedPartial(double oldHorizontalOffset, double newHorizontalOffset);
		partial void OnHorizontalOffsetChangedPartialNative(double oldHorizontalOffset, double newHorizontalOffset);

		/// <summary>
		/// Gets or sets the distance between the top of the application window and the top of the popup.
		/// </summary>
		public double VerticalOffset
		{
			get => (double)GetValue(VerticalOffsetProperty);
			set => SetValue(VerticalOffsetProperty, value);
		}

		/// <summary>
		/// Gets the identifier for the VerticalOffset dependency property.
		/// </summary>
		public static DependencyProperty VerticalOffsetProperty { get; } =
			DependencyProperty.Register(
				nameof(VerticalOffset),
				typeof(double),
				typeof(Popup),
				new FrameworkPropertyMetadata(
					0.0,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((Popup)s)?.OnVerticalOffsetChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnVerticalOffsetChanged(double oldVerticalOffset, double newVerticalOffset)
		{
			OnVerticalOffsetChangedPartial(oldVerticalOffset, newVerticalOffset);
			OnVerticalOffsetChangedPartialNative(oldVerticalOffset, newVerticalOffset);
		}

		partial void OnVerticalOffsetChangedPartial(double oldVerticalOffset, double newVerticalOffset);
		partial void OnVerticalOffsetChangedPartialNative(double oldVerticalOffset, double newVerticalOffset);

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
