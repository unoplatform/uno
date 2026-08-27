// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ContentControl.idl, tag winui3/release/1.8.2, commit bac7a9c33

using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentControl
{
	/// <summary>
	/// Gets or sets the content of a ContentControl.
	/// </summary>
	public object Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="Content"/> dependency property.
	/// </summary>
	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(object),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				// Content is presented through the visual tree, so the DataContext reaches it from there.
				// Propagating it as a property too would double-propagate before the template is applied.
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((ContentControl)s)?.OnContentChanged(e.OldValue, e.NewValue)
			)
		);

	/// <summary>
	/// Gets or sets the data template that is used to display the content of the ContentControl.
	/// </summary>
	public DataTemplate ContentTemplate
	{
		get => (DataTemplate)GetValue(ContentTemplateProperty);
		set => SetValue(ContentTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="ContentTemplate"/> dependency property.
	/// </summary>
	public static DependencyProperty ContentTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentTemplate),
			typeof(DataTemplate),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentControl)s)?.OnContentTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate)
			)
		);

	/// <summary>
	/// Gets or sets a selection object that changes the DataTemplate to apply for content, based on
	/// processing information about the content item or its container at run time.
	/// </summary>
	public DataTemplateSelector ContentTemplateSelector
	{
		get => (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty);
		set => SetValue(ContentTemplateSelectorProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="ContentTemplateSelector"/> dependency property.
	/// </summary>
	public static DependencyProperty ContentTemplateSelectorProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentTemplateSelector),
			typeof(DataTemplateSelector),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				null,
				(s, e) => ((ContentControl)s)?.OnContentTemplateSelectorChanged(e.OldValue as DataTemplateSelector, e.NewValue as DataTemplateSelector)
			)
		);

	/// <summary>
	/// Gets the data template resolved from <see cref="ContentTemplate"/> or <see cref="ContentTemplateSelector"/>.
	/// </summary>
	/// <remarks>
	/// Not used by ContentControl itself; it exists for the templated ContentPresenter to bind to.
	/// Internal in WinUI too - it is not part of the public API surface.
	/// </remarks>
	internal DataTemplate SelectedContentTemplate
	{
		get => (DataTemplate)GetValue(SelectedContentTemplateProperty);
		set => SetValue(SelectedContentTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SelectedContentTemplate"/> dependency property.
	/// </summary>
	internal static DependencyProperty SelectedContentTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedContentTemplate),
			typeof(DataTemplate),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
				(s, e) => ((ContentControl)s)?.OnSelectedContentTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate)
			)
		);

	/// <summary>
	/// Gets or sets the collection of Transition style elements that apply to the content of a ContentControl.
	/// </summary>
	public TransitionCollection ContentTransitions
	{
		get => (TransitionCollection)GetValue(ContentTransitionsProperty);
		set => SetValue(ContentTransitionsProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="ContentTransitions"/> dependency property.
	/// </summary>
	public static DependencyProperty ContentTransitionsProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentTransitions),
			typeof(TransitionCollection),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				null,
				(s, e) => ((ContentControl)s)?.UpdateContentTransitions(e.OldValue as TransitionCollection, e.NewValue as TransitionCollection)));
}
