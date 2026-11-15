// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class SymbolIcon
{
	/// <summary>
	/// Gets or sets the Segoe MDL2 Assets glyph used as the icon content.
	/// </summary>
	public Symbol Symbol
	{
		get => (Symbol)GetValue(SymbolProperty);
		set => SetValue(SymbolProperty, value);
	}

	/// <summary>
	/// Identifies the Symbol dependency property.
	/// </summary>
	public static DependencyProperty SymbolProperty { get; } =
		DependencyProperty.Register(
			nameof(Symbol),
			typeof(Symbol),
			typeof(SymbolIcon),
			new FrameworkPropertyMetadata(Symbol.Emoji, propertyChangedCallback: (d, e) => ((SymbolIcon)d).UpdateChildTextBlockText()));
}
