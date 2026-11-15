// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class SymbolIcon
{
	private const float g_ClientCoreFontSize = 20.0f;

	// For now, font size is internal to allow AutoSuggestBox to set it
	// in order to get properly-sized SymbolIcon symbols for its purposes.
	// In the future, this may become a public property, in which case
	// it will need to be treated as FontIcon.FontSize is.
	private float m_fontSize = g_ClientCoreFontSize;
}
