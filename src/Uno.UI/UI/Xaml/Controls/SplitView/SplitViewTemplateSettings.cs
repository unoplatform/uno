using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

// With temporary workaround for #20623: updating SplitView.OpenPaneLength has no effect at runtime
// that happens because we are using neither INotifyPropertyChanged or DependencyObject::SetValue here.
// For now, we are turning auto-properties and get-only calculated properties into
// DependencyProperties with handlers that updates its dependants.

// When eventually updating this file, in accordance with WinUI,
// these should all be DependencyProperties that are updated by the SplitView.

namespace Microsoft.UI.Xaml.Controls.Primitives;

public sealed partial class SplitViewTemplateSettings : DependencyObject
{
	private const double DefaultOpenPaneLength = 320;
	private const double DefaultCompactPaneLength = 48;
	private const double DefaultViewHeight = 2000;

	public SplitViewTemplateSettings(SplitView splitView)
	{
		InitializeBinder();

		CompactPaneLength = splitView.CompactPaneLength;
		OpenPaneLength = splitView.OpenPaneLength;

		UpdateCalculatedProperties();
	}

	#region DependencyProperty: OpenPaneLength

	public static DependencyProperty OpenPaneLengthProperty { get; } = DependencyProperty.Register(
		nameof(OpenPaneLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(DefaultOpenPaneLength, OnPropertyChanged));

	public double OpenPaneLength
	{
		get => (double)GetValue(OpenPaneLengthProperty);
		internal set => SetValue(OpenPaneLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: CompactPaneLength

	public static DependencyProperty CompactPaneLengthProperty { get; } = DependencyProperty.Register(
		nameof(CompactPaneLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(DefaultCompactPaneLength, OnPropertyChanged));

	public double CompactPaneLength
	{
		get => (double)GetValue(CompactPaneLengthProperty);
		internal set => SetValue(CompactPaneLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: ViewHeight

	public static DependencyProperty ViewHeightProperty { get; } = DependencyProperty.Register(
		nameof(ViewHeight),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(DefaultViewHeight, OnPropertyChanged));

	public double ViewHeight
	{
		get => (double)GetValue(ViewHeightProperty);
		internal set => SetValue(ViewHeightProperty, value);
	}

	#endregion
	#region DependencyProperty: CompactPaneGridLength

	public static DependencyProperty CompactPaneGridLengthProperty { get; } = DependencyProperty.Register(
		nameof(CompactPaneGridLength),
		typeof(GridLength),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(GridLength)));

	public GridLength CompactPaneGridLength
	{
		get => (GridLength)GetValue(CompactPaneGridLengthProperty);
		internal set => SetValue(CompactPaneGridLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: OpenPaneGridLength

	public static DependencyProperty OpenPaneGridLengthProperty { get; } = DependencyProperty.Register(
		nameof(OpenPaneGridLength),
		typeof(GridLength),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(GridLength)));

	public GridLength OpenPaneGridLength
	{
		get => (GridLength)GetValue(OpenPaneGridLengthProperty);
		internal set => SetValue(OpenPaneGridLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: NegativeOpenPaneLength

	public static DependencyProperty NegativeOpenPaneLengthProperty { get; } = DependencyProperty.Register(
		nameof(NegativeOpenPaneLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(double)));

	public double NegativeOpenPaneLength
	{
		get => (double)GetValue(NegativeOpenPaneLengthProperty);
		internal set => SetValue(NegativeOpenPaneLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: NegativeOpenPaneLengthMinusCompactLength

	public static DependencyProperty NegativeOpenPaneLengthMinusCompactLengthProperty { get; } = DependencyProperty.Register(
		nameof(NegativeOpenPaneLengthMinusCompactLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(double)));

	public double NegativeOpenPaneLengthMinusCompactLength
	{
		get => (double)GetValue(NegativeOpenPaneLengthMinusCompactLengthProperty);
		internal set => SetValue(NegativeOpenPaneLengthMinusCompactLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: OpenPaneLengthMinusCompactLength

	public static DependencyProperty OpenPaneLengthMinusCompactLengthProperty { get; } = DependencyProperty.Register(
		nameof(OpenPaneLengthMinusCompactLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(double)));

	public double OpenPaneLengthMinusCompactLength
	{
		get => (double)GetValue(OpenPaneLengthMinusCompactLengthProperty);
		internal set => SetValue(OpenPaneLengthMinusCompactLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: LeftClip

	public static DependencyProperty LeftClipProperty { get; } = DependencyProperty.Register(
		nameof(LeftClip),
		typeof(RectangleGeometry),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(RectangleGeometry)));

	public RectangleGeometry LeftClip
	{
		get => (RectangleGeometry)GetValue(LeftClipProperty);
		internal set => SetValue(LeftClipProperty, value);
	}

	#endregion
	#region DependencyProperty: RightClip

	public static DependencyProperty RightClipProperty { get; } = DependencyProperty.Register(
		nameof(RightClip),
		typeof(RectangleGeometry),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(RectangleGeometry)));

	public RectangleGeometry RightClip
	{
		get => (RectangleGeometry)GetValue(RightClipProperty);
		internal set => SetValue(RightClipProperty, value);
	}

	#endregion

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		(sender as SplitViewTemplateSettings)?.UpdateCalculatedProperties();
	}

	private void UpdateCalculatedProperties()
	{
		CompactPaneGridLength = new GridLength(CompactPaneLength, GridUnitType.Pixel);
		OpenPaneGridLength = new GridLength(OpenPaneLength, GridUnitType.Pixel);
		NegativeOpenPaneLength = -OpenPaneLength;
		NegativeOpenPaneLengthMinusCompactLength = NegativeOpenPaneLength - CompactPaneLength;
		OpenPaneLengthMinusCompactLength = OpenPaneLength - CompactPaneLength;

		// These properties were added to facilitate clipping while RectangleGeometry.Transform is not supported
		// TODO: Remove and use NegativeOpenPaneLengthMinusCompactLength and OpenPaneLengthMinusCompactLength instead
		LeftClip = new RectangleGeometry { Rect = new Rect(0, 0, CompactPaneLength, ViewHeight) };
		RightClip = new RectangleGeometry { Rect = new Rect(OpenPaneLengthMinusCompactLength, 0, CompactPaneLength, ViewHeight) };
	}
}
