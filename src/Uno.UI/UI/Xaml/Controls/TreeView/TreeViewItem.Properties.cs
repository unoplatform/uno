// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewItem.properties.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class TreeViewItem
{
	/// <summary>
	/// Gets or sets the glyph to show for a collapsed tree node.
	/// </summary>
	public string CollapsedGlyph
	{
		get => (string)GetValue(CollapsedGlyphProperty);
		set => SetValue(CollapsedGlyphProperty, value);
	}

	/// <summary>
	/// Gets or sets the glyph to show for an expanded tree node.
	/// </summary>
	public string ExpandedGlyph
	{
		get => (string)GetValue(ExpandedGlyphProperty);
		set => SetValue(ExpandedGlyphProperty, value);
	}

	/// <summary>
	/// Gets or sets the Brush used to paint node glyphs on a TreeView.
	/// </summary>
	public Brush GlyphBrush
	{
		get => (Brush)GetValue(GlyphBrushProperty);
		set => SetValue(GlyphBrushProperty, value);
	}

	/// <summary>
	/// Gets or sets the opacity of node glyphs on a TreeView.
	/// </summary>
	public double GlyphOpacity
	{
		get => (double)GetValue(GlyphOpacityProperty);
		set => SetValue(GlyphOpacityProperty, value);
	}

	/// <summary>
	/// Gets or sets the size of node glyphs on a TreeView.
	/// </summary>
	public double GlyphSize
	{
		get => (double)GetValue(GlyphSizeProperty);
		set => SetValue(GlyphSizeProperty, value);
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the current item has child items that haven't been shown.
	/// </summary>
	public bool HasUnrealizedChildren
	{
		get => (bool)GetValue(HasUnrealizedChildrenProperty);
		set => SetValue(HasUnrealizedChildrenProperty, value);
	}

	/// <summary>
	/// Gets or sets a value that indicates whether a tree node is expanded.
	/// </summary>
	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

	/// <summary>
	/// Gets or sets an object source used to generate the content of the TreeView.
	/// </summary>
	public object ItemsSource
	{
		get => (object)GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding} markup extension sources when defining templates for a TreeViewItem control.
	/// </summary>
	public TreeViewItemTemplateSettings TreeViewItemTemplateSettings
	{
		get => (TreeViewItemTemplateSettings)GetValue(TreeViewItemTemplateSettingsProperty);
		set => SetValue(TreeViewItemTemplateSettingsProperty, value);
	}

	/// <summary>
	/// Identifies the CollapsedGlyph dependency property.
	/// </summary>
	public static DependencyProperty CollapsedGlyphProperty { get; } =
		DependencyProperty.Register(nameof(CollapsedGlyph), typeof(string), typeof(TreeViewItem), new FrameworkPropertyMetadata("\uE76C"));

	/// <summary>
	/// Identifies the ExpandedGlyph dependency property.
	/// </summary>
	public static DependencyProperty ExpandedGlyphProperty { get; } =
		DependencyProperty.Register(nameof(ExpandedGlyph), typeof(string), typeof(TreeViewItem), new FrameworkPropertyMetadata("\uE70D"));

	/// <summary>
	/// Identifies the GlyphBrush dependency property.
	/// </summary>
	public static DependencyProperty GlyphBrushProperty { get; } =
		DependencyProperty.Register(nameof(GlyphBrush), typeof(Brush), typeof(TreeViewItem), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the GlyphOpacity dependency property.
	/// </summary>
	public static DependencyProperty GlyphOpacityProperty { get; } =
		DependencyProperty.Register(nameof(GlyphOpacity), typeof(double), typeof(TreeViewItem), new FrameworkPropertyMetadata(1.0));

	/// <summary>
	/// Identifies the GlyphSize dependency property.
	/// </summary>
	public static DependencyProperty GlyphSizeProperty { get; } =
		DependencyProperty.Register(nameof(GlyphSize), typeof(double), typeof(TreeViewItem), new FrameworkPropertyMetadata(8.0));

	/// <summary>
	/// Identifies the HasUnrealizedChildren dependency property.
	/// </summary>
	public static DependencyProperty HasUnrealizedChildrenProperty { get; } =
		DependencyProperty.Register(nameof(HasUnrealizedChildren), typeof(bool), typeof(TreeViewItem), new FrameworkPropertyMetadata(false, OnHasUnrealizedChildrenPropertyChanged));

	/// <summary>
	/// Identifies the IsExpanded dependency property.
	/// </summary>
	public static DependencyProperty IsExpandedProperty { get; } =
		DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TreeViewItem), new FrameworkPropertyMetadata(false, OnIsExpandedPropertyChanged));

	/// <summary>
	/// Identifies the ItemsSource dependency property.
	/// </summary>
	public static DependencyProperty ItemsSourceProperty { get; } =
		DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TreeViewItem), new FrameworkPropertyMetadata(null, OnItemsSourcePropertyChanged));

	/// <summary>
	/// Identifies the TreeViewItemTemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TreeViewItemTemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TreeViewItemTemplateSettings), typeof(TreeViewItemTemplateSettings), typeof(TreeViewItem), new FrameworkPropertyMetadata(null));

	private static void OnHasUnrealizedChildrenPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeViewItem)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnIsExpandedPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeViewItem)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnItemsSourcePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeViewItem)sender;
		owner.OnPropertyChanged(args);
	}
}
