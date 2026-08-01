// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Microsoft.UI.Xaml.Controls.cs, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class SemanticZoom
{
	/// <summary>
	/// Gets or sets a value that declares whether the SemanticZoom can change display views.
	/// </summary>
	public bool CanChangeViews
	{
		get => (bool)GetValue(CanChangeViewsProperty);
		set => SetValue(CanChangeViewsProperty, value);
	}

	/// <summary>
	/// Identifies the CanChangeViews dependency property.
	/// </summary>
	public static DependencyProperty CanChangeViewsProperty { get; } =
		DependencyProperty.Register(
			nameof(CanChangeViews),
			typeof(bool),
			typeof(SemanticZoom),
			new FrameworkPropertyMetadata(defaultValue: true));

	/// <summary>
	/// Gets or sets a value that indicates whether the ZoomedInView shows a button that activates the ZoomedOutView.
	/// </summary>
	public bool IsZoomOutButtonEnabled
	{
		get => (bool)GetValue(IsZoomOutButtonEnabledProperty);
		set => SetValue(IsZoomOutButtonEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsZoomOutButtonEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsZoomOutButtonEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsZoomOutButtonEnabled),
			typeof(bool),
			typeof(SemanticZoom),
			new FrameworkPropertyMetadata(
				defaultValue: true,
				propertyChangedCallback: OnIsZoomOutButtonEnabledChanged));

	/// <summary>
	/// Gets or sets a value that determines whether the ZoomedInView is the active view.
	/// </summary>
	public bool IsZoomedInViewActive
	{
		get => (bool)GetValue(IsZoomedInViewActiveProperty);
		set => SetValue(IsZoomedInViewActiveProperty, value);
	}

	/// <summary>
	/// Identifies the IsZoomedInViewActive dependency property.
	/// </summary>
	public static DependencyProperty IsZoomedInViewActiveProperty { get; } =
		DependencyProperty.Register(
			nameof(IsZoomedInViewActive),
			typeof(bool),
			typeof(SemanticZoom),
			new FrameworkPropertyMetadata(
				defaultValue: true,
				propertyChangedCallback: OnIsZoomedInViewActiveChanged));

	/// <summary>
	/// Gets or sets the semantically more complete zoomed-in view of the SemanticZoom.
	/// </summary>
	public ISemanticZoomInformation? ZoomedInView
	{
		get => (ISemanticZoomInformation?)GetValue(ZoomedInViewProperty);
		set => SetValue(ZoomedInViewProperty, value);
	}

	/// <summary>
	/// Identifies the ZoomedInView dependency property.
	/// </summary>
	public static DependencyProperty ZoomedInViewProperty { get; } =
		DependencyProperty.Register(
			nameof(ZoomedInView),
			typeof(ISemanticZoomInformation),
			typeof(SemanticZoom),
			new FrameworkPropertyMetadata(
				default(ISemanticZoomInformation),
				OnZoomedInViewChanged));

	/// <summary>
	/// Gets or sets the zoomed-out view of the SemanticZoom.
	/// </summary>
	public ISemanticZoomInformation? ZoomedOutView
	{
		get => (ISemanticZoomInformation?)GetValue(ZoomedOutViewProperty);
		set => SetValue(ZoomedOutViewProperty, value);
	}

	/// <summary>
	/// Identifies the ZoomedOutView dependency property.
	/// </summary>
	public static DependencyProperty ZoomedOutViewProperty { get; } =
		DependencyProperty.Register(
			nameof(ZoomedOutView),
			typeof(ISemanticZoomInformation),
			typeof(SemanticZoom),
			new FrameworkPropertyMetadata(
				default(ISemanticZoomInformation),
				OnZoomedOutViewChanged));

	/// <summary>
	/// Occurs when a view change is requested.
	/// </summary>
	public event SemanticZoomViewChangedEventHandler? ViewChangeStarted;

	/// <summary>
	/// Occurs when a view change is complete and the view is displayed.
	/// </summary>
	public event SemanticZoomViewChangedEventHandler? ViewChangeCompleted;

	/// <summary>
	/// Switches the control between the zoomed-in and zoomed-out views.
	/// </summary>
	public void ToggleActiveView() => ToggleActiveViewImpl();

	private static void OnIsZoomOutButtonEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((SemanticZoom)sender).OnPropertyChanged2(IsZoomOutButtonEnabledProperty, args.NewValue);

	private static void OnIsZoomedInViewActiveChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((SemanticZoom)sender).OnPropertyChanged2(IsZoomedInViewActiveProperty, args.NewValue);

	private static void OnZoomedInViewChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((SemanticZoom)sender).InitializeSemanticZoomInformation(
			args.OldValue as ISemanticZoomInformation,
			args.NewValue as ISemanticZoomInformation,
			isZoomedInView: true);

	private static void OnZoomedOutViewChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((SemanticZoom)sender).InitializeSemanticZoomInformation(
			args.OldValue as ISemanticZoomInformation,
			args.NewValue as ISemanticZoomInformation,
			isZoomedInView: false);
}
