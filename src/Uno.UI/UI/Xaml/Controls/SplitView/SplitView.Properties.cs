// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SplitView.g.h, commit 67aeb8f23

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitView
{
	#region CompactPaneLength

	public double CompactPaneLength
	{
		get => (double)GetValue(CompactPaneLengthProperty);
		set => SetValue(CompactPaneLengthProperty, value);
	}

	public static DependencyProperty CompactPaneLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(CompactPaneLength),
			typeof(double),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				48.0,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => (s as SplitView)?.OnCompactPaneLengthPropertyChanged(e)));

	#endregion

	#region Content

	public UIElement Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(UIElement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region Pane

	public UIElement Pane
	{
		get => (UIElement)GetValue(PaneProperty);
		set => SetValue(PaneProperty, value);
	}

	public static DependencyProperty PaneProperty { get; } =
		DependencyProperty.Register(
			nameof(Pane),
			typeof(UIElement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region DisplayMode

	public SplitViewDisplayMode DisplayMode
	{
		get => (SplitViewDisplayMode)GetValue(DisplayModeProperty);
		set => SetValue(DisplayModeProperty, value);
	}

	public static DependencyProperty DisplayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(DisplayMode),
			typeof(SplitViewDisplayMode),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				SplitViewDisplayMode.Overlay,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => (s as SplitView)?.OnDisplayModePropertyChanged(e)));

	#endregion

	#region IsPaneOpen

	public bool IsPaneOpen
	{
		get => (bool)GetValue(IsPaneOpenProperty);
		set => SetValue(IsPaneOpenProperty, value);
	}

	public static DependencyProperty IsPaneOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsPaneOpen),
			typeof(bool),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				false,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => (s as SplitView)?.OnIsPaneOpenPropertyChanged(e)));

	#endregion

	#region OpenPaneLength

	public double OpenPaneLength
	{
		get => (double)GetValue(OpenPaneLengthProperty);
		set => SetValue(OpenPaneLengthProperty, value);
	}

	public static DependencyProperty OpenPaneLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(OpenPaneLength),
			typeof(double),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				320.0,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => (s as SplitView)?.OnOpenPaneLengthPropertyChanged(e)));

	#endregion

	#region PaneBackground

	public Brush PaneBackground
	{
		get => (Brush)GetValue(PaneBackgroundProperty);
		set => SetValue(PaneBackgroundProperty, value);
	}

	public static DependencyProperty PaneBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(PaneBackground),
			typeof(Brush),
			typeof(SplitView),
			new FrameworkPropertyMetadata(default(Brush)));

	#endregion

	#region PanePlacement

	public SplitViewPanePlacement PanePlacement
	{
		get => (SplitViewPanePlacement)GetValue(PanePlacementProperty);
		set => SetValue(PanePlacementProperty, value);
	}

	public static DependencyProperty PanePlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(PanePlacement),
			typeof(SplitViewPanePlacement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				SplitViewPanePlacement.Left,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => (s as SplitView)?.OnPanePlacementPropertyChanged(e)));

	#endregion

	#region LightDismissOverlayMode

	public LightDismissOverlayMode LightDismissOverlayMode
	{
		get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
		set => SetValue(LightDismissOverlayModeProperty, value);
	}

	public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LightDismissOverlayMode),
			typeof(LightDismissOverlayMode),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				LightDismissOverlayMode.Auto,
				(s, e) => (s as SplitView)?.OnLightDismissOverlayModePropertyChanged(e)));

	#endregion

	#region TemplateSettings

	public SplitViewTemplateSettings TemplateSettings
	{
		get => (SplitViewTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(SplitViewTemplateSettings),
			typeof(SplitView),
			new FrameworkPropertyMetadata(null));

	#endregion

	#region Events

	public event TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs> PaneClosing;
	public event TypedEventHandler<SplitView, object> PaneClosed;
	public event TypedEventHandler<SplitView, object> PaneOpening;
	public event TypedEventHandler<SplitView, object> PaneOpened;

	#endregion
}
