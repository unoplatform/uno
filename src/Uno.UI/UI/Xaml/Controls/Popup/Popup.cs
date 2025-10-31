﻿using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls.Primitives;

[ContentProperty(Name = nameof(Child))]
public partial class Popup
{
	private IDisposable _renderTransformChangedRegistration;
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
		KeyDown += OnKeyDown;
		this.RegisterDisposablePropertyChangedCallback(RenderTransformProperty, (dO, _) => ((Popup)dO).OnRenderTransformChanged());
	}

	private void OnRenderTransformChanged()
	{
		_renderTransformChangedRegistration?.Dispose();
		// since the Child is not a visual child, any visual properties set on the Popup won't apply, so
		// we have to apply them manually. To avoid interference with a RenderTransform on the child,
		// we set it on the uno-internal PopupPanel
		var renderTransform = RenderTransform;
		PopupPanel.RenderTransform = renderTransform;
		// RenderTransform could get its value by a binding with a relative source (e.g. a TemplateBinding).
		// In that case, directly setting `PopupPanel.RenderTransform = RenderTransform` wouldn't propagate
		// the value correctly to the PopupPanel.
		var matrixTransform = new MatrixTransform { Matrix = new Matrix(renderTransform.MatrixCore) };
		PopupPanel.RenderTransform = matrixTransform;
		PopupPanel.InvalidateArrange();
		// we set the anchor point of the child in PopupPanel.ArrangeOverride, so even though RenderTransform normally doesn't
		// AffectArrange, in this situation, it does.
		var callback = new EventHandler((_, _) => PopupPanel.InvalidateArrange());
		renderTransform.Changed += callback;
		_renderTransformChangedRegistration = Disposable.Create(() => renderTransform.Changed -= callback);
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs args)
	{
		if (args.Key == VirtualKey.Escape)
		{
			args.Handled = true;

			// Give the popup an opportunity to cancel closing
			var cancel = false;
			OnClosing(ref cancel);
			if (cancel)
			{
				return;
			}

			IsOpen = false;
		}
	}

	internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
	{
		if (property == IsLightDismissEnabledProperty)
		{
			defaultValue = Uno.UI.Helpers.Boxes.Box(FeatureConfiguration.Popup.EnableLightDismissByDefault);
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

	partial void OnVerticalOffsetChangedPartial(double oldVerticalOffset, double newVerticalOffset) =>
		PopupPanel?.InvalidateMeasure();

	partial void OnHorizontalOffsetChangedPartial(double oldHorizontalOffset, double newHorizontalOffset) =>
		PopupPanel?.InvalidateMeasure();

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
			new FrameworkPropertyMetadata(
				default(FrameworkElement),
				FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext
			));

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

	internal DependencyObject OverlayInputPassThroughElement
	{
		get => (DependencyObject)GetValue(OverlayInputPassThroughElementProperty);
		set => SetValue(OverlayInputPassThroughElementProperty, value);
	}

	internal static DependencyProperty OverlayInputPassThroughElementProperty { get; } =
		DependencyProperty.Register(nameof(OverlayInputPassThroughElement), typeof(DependencyObject), typeof(Popup), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

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

	private void OnHorizontalOffsetChanged(double oldHorizontalOffset, double newHorizontalOffset)
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

	private void OnVerticalOffsetChanged(double oldVerticalOffset, double newVerticalOffset)
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
		// In all cases, we need the background to not be null so that it can receive pointer events, or else it
		// will fail hit-testing.
		if (ShouldShowLightDismissOverlay)
		{
			return LightDismissOverlayBackground ?? SolidColorBrushHelper.Transparent;
		}
		else
		{
			return SolidColorBrushHelper.Transparent;
		}
	}

	/// <summary>
	/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc, static resource values.
	/// </summary>
	internal Brush LightDismissOverlayBackground
	{
		get => (Brush)GetValue(LightDismissOverlayBackgroundProperty);
		set => SetValue(LightDismissOverlayBackgroundProperty, value);
	}

	internal static DependencyProperty LightDismissOverlayBackgroundProperty { get; } =
		DependencyProperty.Register(nameof(LightDismissOverlayBackground), typeof(Brush), typeof(Popup), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (o, e) => ((Popup)o).ApplyLightDismissOverlayMode()));

	internal void SetAssociatedVisualTree(VisualTree visualTree)
	{
		// TODO Uno: Implement this
		//visualTree.AttachElement(this);
	}
}
