// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference Expander.properties.cpp, commit 8d20a91

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class Expander
{
	/// <summary>
	/// Gets or sets a value that indicates the direction in which the content area expands.
	/// </summary>
	public ExpandDirection ExpandDirection
	{
		get => (ExpandDirection)GetValue(ExpandDirectionProperty);
		set => SetValue(ExpandDirectionProperty, value);
	}

	/// <summary>
	/// Identifies the ExpandDirection dependency property.
	/// </summary>
	public static DependencyProperty ExpandDirectionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandDirection),
			typeof(ExpandDirection),
			typeof(Expander),
			new FrameworkPropertyMetadata(
				ExpandDirection.Down,
				(s, e) => (s as Expander)?.OnExpandDirectionPropertyChanged(e)));

	/// <summary>
	/// Gets or sets the XAML content that is displayed in the header of the Expander.
	/// </summary>
	public object Header
	{
		get => GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(Expander),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the data template for the Expander.Header.
	/// </summary>
	public DataTemplate HeaderTemplate
	{
		get => GetValue(HeaderTemplateProperty) as DataTemplate;
		set => SetValue(HeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderTemplate),
			typeof(DataTemplate),
			typeof(Expander),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets a reference to a custom DataTemplateSelector logic class that returns a template to apply to the Header.
	/// </summary>
	public DataTemplateSelector HeaderTemplateSelector
	{
		get => GetValue(HeaderTemplateSelectorProperty) as DataTemplateSelector;
		set => SetValue(HeaderTemplateSelectorProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplateSelector dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateSelectorProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderTemplateSelector),
			typeof(DataTemplateSelector),
			typeof(Expander),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the HeaderTemplateSelector dependency property.
	/// </summary>
	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

	/// <summary>
	/// Identifies the IsExpanded dependency property.
	/// </summary>
	public static DependencyProperty IsExpandedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsExpanded),
			typeof(bool),
			typeof(Expander),
			new FrameworkPropertyMetadata(
				false,
				(s, e) => (s as Expander)?.OnIsExpandedPropertyChanged(e)));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced
	/// as TemplatedParent sources when defining templates for an Expander.
	/// </summary>
	public ExpanderTemplateSettings TemplateSettings
	{
		get => (ExpanderTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	private static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(ExpanderTemplateSettings),
			typeof(Expander),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Occurs when the content area of the Expander is hidden.
	/// </summary>
	public event TypedEventHandler<Expander, ExpanderCollapsedEventArgs> Collapsed;

	/// <summary>
	/// Occurs when the content area of the Expander starts to be shown.
	/// </summary>
	public event TypedEventHandler<Expander, ExpanderExpandingEventArgs> Expanding;
}
