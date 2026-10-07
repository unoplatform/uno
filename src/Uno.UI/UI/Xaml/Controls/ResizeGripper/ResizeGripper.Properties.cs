// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\ResizeGripper.properties.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Private.Controls;

partial class ResizeGripper
{
	// The axis the drag is measured along, which for a divider is perpendicular to how it looks:
	// Horizontal drags left/right. Named DragOrientation rather than Orientation because the
	// primitive owns no shape - the host sizes it - so the control's own layout axis is not
	// something it can describe. Only horizontal mirrors under RTL.
	/// <summary>
	/// Gets or sets the axis the drag is measured along.
	/// </summary>
	public Orientation DragOrientation
	{
		get => (Orientation)GetValue(DragOrientationProperty);
		set => SetValue(DragOrientationProperty, value);
	}

	/// <summary>
	/// Identifies the DragOrientation dependency property.
	/// </summary>
	public static DependencyProperty DragOrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(DragOrientation),
			typeof(Orientation),
			typeof(ResizeGripper),
			new FrameworkPropertyMetadata(Orientation.Horizontal, OnPropertyChanged));

	/// <summary>
	/// Gets a value that indicates whether a drag is in progress.
	/// </summary>
	public bool IsDragging
	{
		get => (bool)GetValue(IsDraggingProperty);
		internal set => SetValue(IsDraggingProperty, value);
	}

	/// <summary>
	/// Identifies the IsDragging dependency property.
	/// </summary>
	public static DependencyProperty IsDraggingProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDragging),
			typeof(bool),
			typeof(ResizeGripper),
			new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

	// How far one arrow key travels; Shift multiplies it. Step size is host policy, so it is
	// settable rather than baked in.
	/// <summary>
	/// Gets or sets how far one arrow key travels.
	/// </summary>
	public double KeyboardIncrement
	{
		get => (double)GetValue(KeyboardIncrementProperty);
		set => SetValue(KeyboardIncrementProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the KeyboardIncrement dependency property.
	/// </summary>
	public static DependencyProperty KeyboardIncrementProperty { get; } =
		DependencyProperty.Register(
			nameof(KeyboardIncrement),
			typeof(double),
			typeof(ResizeGripper),
			new FrameworkPropertyMetadata(c_defaultKeyboardIncrement, OnPropertyChanged));

	// The frame drags are measured in. A host sets this to an ancestor that does NOT move while the
	// drag resizes something: the gripper travels with the edge it drags, so the default frame (the
	// gripper itself) feeds the resize back into the gesture and stalls it, while the XamlRoot
	// content picks up any scale or zoom applied between it and the gripper. Null falls back to the
	// XamlRoot content, which is correct whenever nothing between it and the gripper is scaled.
	/// <summary>
	/// Gets or sets the frame drags are measured in.
	/// </summary>
	public UIElement? ManipulationContainer
	{
		get => (UIElement?)GetValue(ManipulationContainerProperty);
		set => SetValue(ManipulationContainerProperty, value);
	}

	/// <summary>
	/// Identifies the ManipulationContainer dependency property.
	/// </summary>
	public static DependencyProperty ManipulationContainerProperty { get; } =
		DependencyProperty.Register(
			nameof(ManipulationContainer),
			typeof(UIElement),
			typeof(ResizeGripper),
			new FrameworkPropertyMetadata(default(UIElement), OnPropertyChanged));

	// Qualifier the automation peer prepends to its localized name, so N grippers in one band are
	// distinguishable ("Price, Resize gripper"). Declared rather than read off Tag: Tag is a
	// general-purpose slot a host may already be using, and a silent wrong name is an a11y bug.
	/// <summary>
	/// Gets or sets the qualifier the automation peer prepends to its localized name.
	/// </summary>
	public string OwnerName
	{
		get => (string)GetValue(OwnerNameProperty);
		set => SetValue(OwnerNameProperty, value);
	}

	/// <summary>
	/// Identifies the OwnerName dependency property.
	/// </summary>
	public static DependencyProperty OwnerNameProperty { get; } =
		DependencyProperty.Register(
			nameof(OwnerName),
			typeof(string),
			typeof(ResizeGripper),
			new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	/// <summary>
	/// Occurs when a drag ends or is canceled.
	/// </summary>
	public event TypedEventHandler<ResizeGripper, ResizeGripperDragCompletedEventArgs>? DragCompleted;

	/// <summary>
	/// Occurs while a drag moves.
	/// </summary>
	public event TypedEventHandler<ResizeGripper, ResizeGripperDragDeltaEventArgs>? DragDelta;

	/// <summary>
	/// Occurs when a drag starts.
	/// </summary>
	public event TypedEventHandler<ResizeGripper, object>? DragStarted;

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ResizeGripper)sender;
		owner.OnPropertyChanged(args);
	}
}
