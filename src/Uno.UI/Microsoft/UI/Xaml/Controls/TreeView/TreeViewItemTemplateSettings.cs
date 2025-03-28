// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewItemTemplateSettings.properties.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a TreeViewItem control. Not intended for general use.
/// </summary>
public partial class TreeViewItemTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the visibilty of a collapsed glyph.
	/// </summary>
	public Visibility CollapsedGlyphVisibility
	{
		get => (Visibility)GetValue(CollapsedGlyphVisibilityProperty);
		internal set => SetValue(CollapsedGlyphVisibilityProperty, value);
	}

	/// <summary>
	/// Gets the number of items being dragged.
	/// </summary>
	public int DragItemsCount
	{
		get => (int)GetValue(DragItemsCountProperty);
		internal set => SetValue(DragItemsCountProperty, value);
	}

	/// <summary>
	/// Gets the visibilty of an expanded glyph.
	/// </summary>
	public Visibility ExpandedGlyphVisibility
	{
		get => (Visibility)GetValue(ExpandedGlyphVisibilityProperty);
		internal set => SetValue(ExpandedGlyphVisibilityProperty, value);
	}

	/// <summary>
	/// Gets the amount that the item is indented.
	/// </summary>
	public Thickness Indentation
	{
		get => (Thickness)GetValue(IndentationProperty);
		internal set => SetValue(IndentationProperty, value);
	}

	/// <summary>
	/// Identifies the CollapsedGlyphVisibility dependency property.
	/// </summary>
	public static DependencyProperty CollapsedGlyphVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(CollapsedGlyphVisibility), typeof(Visibility), typeof(TreeViewItemTemplateSettings), new FrameworkPropertyMetadata(Visibility.Collapsed));

	/// <summary>
	/// Identifies the DragItemsCount dependency property.
	/// </summary>
	public static DependencyProperty DragItemsCountProperty { get; } =
		DependencyProperty.Register(nameof(DragItemsCount), typeof(int), typeof(TreeViewItemTemplateSettings), new FrameworkPropertyMetadata(0));

	/// <summary>
	/// Identifies the ExpandedGlyphVisibility dependency property.
	/// </summary>
	public static DependencyProperty ExpandedGlyphVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(ExpandedGlyphVisibility), typeof(Visibility), typeof(TreeViewItemTemplateSettings), new FrameworkPropertyMetadata(Visibility.Collapsed));

	/// <summary>
	/// Identifies the Indentation dependency property.
	/// </summary>
	public static DependencyProperty IndentationProperty { get; } =
		DependencyProperty.Register(nameof(Indentation), typeof(Thickness), typeof(TreeViewItemTemplateSettings), new FrameworkPropertyMetadata(default(Thickness)));
}
