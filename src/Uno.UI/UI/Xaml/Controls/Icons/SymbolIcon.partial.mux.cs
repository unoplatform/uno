// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SymbolIcon_Partial.cpp

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class SymbolIcon
{
	/// <summary>
	/// Initializes a new instance of the SymbolIcon class.
	/// </summary>
	public SymbolIcon()
	{
	}

	/// <summary>
	/// Initializes a new instance of the SymbolIcon class using the specified symbol.
	/// </summary>
	/// <param name="symbol">A named constant of the enumeration that specifies the Segoe MDL2 Assets glyph to use.</param>
	public SymbolIcon(Symbol symbol) : this()
	{
		Symbol = symbol;
	}

	internal void SetFontSize(float fontSize) => SetFontSizeCore(fontSize);
}
