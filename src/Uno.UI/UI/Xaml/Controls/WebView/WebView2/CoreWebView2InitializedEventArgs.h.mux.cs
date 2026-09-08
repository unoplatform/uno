// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls/dev/WebView2/WebView2.h, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class CoreWebView2InitializedEventArgs
{
	internal CoreWebView2InitializedEventArgs(Exception? exception) => m_exception = exception;

	/// <summary>Gets the exception raised when a WebView2 is created.</summary>
	public Exception? Exception => m_exception;

	private readonly Exception? m_exception;
}
