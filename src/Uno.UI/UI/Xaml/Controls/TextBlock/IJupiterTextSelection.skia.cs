// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference JupiterTextHelper.h (IJupiterTextSelection), tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

// Marker for the text selection abstraction consumed by ITextView.TextSelectionToTextBounds.
// TODO Uno (Stage 7): flesh out with GetStartTextPosition / GetEndTextPosition once the
// selection layer (CTextPosition / IJupiterTextSelection) is ported.
internal interface IJupiterTextSelection
{
}
