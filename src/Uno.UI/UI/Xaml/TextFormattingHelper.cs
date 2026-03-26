using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Static helper for the TextFormatting system. Provides property mapping
/// (equivalent to WinUI's <c>InheritedProperties::GetCorrespondingInheritedProperty</c>),
/// field access, and reentrancy guards.
/// </summary>
internal static class TextFormattingHelper
{
	/// <summary>
	/// Reentrancy guard. Set to <see langword="true"/> when
	/// <see cref="UIElement.MarkInheritedPropertyDirty"/> is processing
	/// inherited notifications, preventing re-propagation from
	/// PropertyChanged callbacks triggered by synthetic notifications.
	/// </summary>
	[ThreadStatic]
	internal static bool IsProcessingInheritedNotification;

	/// <summary>
	/// The set of property names managed by TextFormatting.
	/// </summary>
	internal static readonly HashSet<string> TextFormattingPropertyNames = new(StringComparer.Ordinal)
	{
		"Foreground",
		"FontSize",
		"FontFamily",
		"FontWeight",
		"FontStyle",
		"FontStretch",
		"CharacterSpacing",
		"TextDecorations",
		"IsTextScaleFactorEnabled",
		"FlowDirection",
		"Language",
	};

	/// <summary>
	/// Returns <see langword="true"/> if the given property name is a
	/// text formatting property managed by the <see cref="TextFormatting"/> system.
	/// </summary>
	internal static bool IsTextFormattingPropertyName(string name)
		=> TextFormattingPropertyNames.Contains(name);

	/// <summary>
	/// Maps a text formatting property name to the corresponding
	/// <see cref="DependencyProperty"/> on the given element.
	/// Returns <see langword="null"/> if the element does not define
	/// the property (e.g., Grid, Border — transparent pass-through elements).
	/// </summary>
	/// <remarks>
	/// Mirrors WinUI's <c>InheritedProperties::GetCorrespondingInheritedProperty()</c>.
	/// The mapping is by property name because different control types define
	/// separate DPs with the same name (e.g., TextBlock.FontSizeProperty vs
	/// Control.FontSizeProperty).
	/// </remarks>
	internal static DependencyProperty GetCorrespondingTextProperty(DependencyObject element, string propertyName)
	{
		return element switch
		{
			TextBlock => GetTextBlockProperty(propertyName),
			Control => GetControlProperty(propertyName),
			ContentPresenter => GetContentPresenterProperty(propertyName),
			IconElement => propertyName == "Foreground" ? IconElement.ForegroundProperty : GetFrameworkElementProperty(propertyName),
			// RichTextBlock is NotImplemented in Uno — skip for now
			TextElement => GetTextElementProperty(propertyName),
			FrameworkElement => GetFrameworkElementProperty(propertyName),
			_ => null
		};
	}

	private static DependencyProperty GetTextBlockProperty(string name) => name switch
	{
		"Foreground" => TextBlock.ForegroundProperty,
		"FontSize" => TextBlock.FontSizeProperty,
		"FontFamily" => TextBlock.FontFamilyProperty,
		"FontWeight" => TextBlock.FontWeightProperty,
		"FontStyle" => TextBlock.FontStyleProperty,
		"FontStretch" => TextBlock.FontStretchProperty,
		"CharacterSpacing" => TextBlock.CharacterSpacingProperty,
		"TextDecorations" => TextBlock.TextDecorationsProperty,
		// FlowDirection and Language are on FrameworkElement, which TextBlock inherits
		"FlowDirection" => FrameworkElement.FlowDirectionProperty,
		_ => null
	};

	private static DependencyProperty GetControlProperty(string name) => name switch
	{
		"Foreground" => Control.ForegroundProperty,
		"FontSize" => Control.FontSizeProperty,
		"FontFamily" => Control.FontFamilyProperty,
		"FontWeight" => Control.FontWeightProperty,
		"FontStyle" => Control.FontStyleProperty,
		"FontStretch" => Control.FontStretchProperty,
		// Control does not have CharacterSpacing or TextDecorations DPs
		// FlowDirection is on FrameworkElement
		"FlowDirection" => FrameworkElement.FlowDirectionProperty,
		_ => null
	};

	private static DependencyProperty GetContentPresenterProperty(string name) => name switch
	{
		"Foreground" => ContentPresenter.ForegroundProperty,
		"FontSize" => ContentPresenter.FontSizeProperty,
		"FontFamily" => ContentPresenter.FontFamilyProperty,
		"FontWeight" => ContentPresenter.FontWeightProperty,
		"FontStyle" => ContentPresenter.FontStyleProperty,
		"FontStretch" => ContentPresenter.FontStretchProperty,
		// ContentPresenter does not have CharacterSpacing or TextDecorations DPs
		// FlowDirection is on FrameworkElement
		"FlowDirection" => FrameworkElement.FlowDirectionProperty,
		_ => null
	};

	private static DependencyProperty GetTextElementProperty(string name) => name switch
	{
		"Foreground" => TextElement.ForegroundProperty,
		"FontSize" => TextElement.FontSizeProperty,
		"FontFamily" => TextElement.FontFamilyProperty,
		"FontWeight" => TextElement.FontWeightProperty,
		"FontStyle" => TextElement.FontStyleProperty,
		"FontStretch" => TextElement.FontStretchProperty,
		"CharacterSpacing" => TextElement.CharacterSpacingProperty,
		"TextDecorations" => TextElement.TextDecorationsProperty,
		_ => null
	};

	private static DependencyProperty GetFrameworkElementProperty(string name) => name switch
	{
		"FlowDirection" => FrameworkElement.FlowDirectionProperty,
		// Language is auto-generated; include when available
		_ => null
	};
}
