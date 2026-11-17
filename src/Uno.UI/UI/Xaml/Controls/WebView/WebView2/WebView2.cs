// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls/dev/WebView2/WebView2.idl, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an object that enables the hosting of web content.
/// </summary>
public partial class WebView2 :
#if HAS_UNO
	// Uno's native element hosts use a ContentPresenter template.
	Control
#else
	FrameworkElement
#endif
{
}
