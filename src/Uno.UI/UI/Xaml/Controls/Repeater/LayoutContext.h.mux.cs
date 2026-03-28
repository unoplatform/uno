// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutContext.h, commit 5f9e851133b3

namespace Microsoft.UI.Xaml.Controls;

partial class LayoutContext
{
#if DEBUG
	// Inline accessors defined in C++ header under #ifdef DBG.
	public int Indent { get; set; }
#else
	public int Indent => 0;
#endif
}
