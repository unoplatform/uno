// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls/dev/Generated/WebView2.properties.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls;

partial class WebView2
{
	/// <summary>Gets or sets the URI of the current top level document.</summary>
	/// <remarks>The default value is null. Clearing this property does not navigate the underlying browser.</remarks>
	public Uri? Source
	{
		get => (Uri?)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>Gets or sets a value that indicates whether forward navigation is possible.</summary>
	public bool CanGoForward
	{
		get => (bool)GetValue(CanGoForwardProperty);
		set => SetValue(CanGoForwardProperty, value);
	}

	/// <summary>Gets or sets a value that indicates whether backward navigation is possible.</summary>
	public bool CanGoBack
	{
		get => (bool)GetValue(CanGoBackProperty);
		set => SetValue(CanGoBackProperty, value);
	}

	/// <summary>Gets or sets the color to use as the WebView2 background.</summary>
	public Color DefaultBackgroundColor
	{
		get => (Color)GetValue(DefaultBackgroundColorProperty);
		set => SetValue(DefaultBackgroundColorProperty, value);
	}

	/// <summary>Identifies the Source dependency property.</summary>
	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(WebView2),
			new FrameworkPropertyMetadata(null, (sender, args) => ((WebView2)sender).OnPropertyChanged(args)));

	/// <summary>Identifies the CanGoForward dependency property.</summary>
	public static DependencyProperty CanGoForwardProperty { get; } =
		DependencyProperty.Register(nameof(CanGoForward), typeof(bool), typeof(WebView2),
			new FrameworkPropertyMetadata(default(bool), (sender, args) => ((WebView2)sender).OnPropertyChanged(args)));

	/// <summary>Identifies the CanGoBack dependency property.</summary>
	public static DependencyProperty CanGoBackProperty { get; } =
		DependencyProperty.Register(nameof(CanGoBack), typeof(bool), typeof(WebView2),
			new FrameworkPropertyMetadata(default(bool), (sender, args) => ((WebView2)sender).OnPropertyChanged(args)));

	/// <summary>Identifies the DefaultBackgroundColor dependency property.</summary>
	public static DependencyProperty DefaultBackgroundColorProperty { get; } =
		DependencyProperty.Register(nameof(DefaultBackgroundColor), typeof(Color), typeof(WebView2),
			new FrameworkPropertyMetadata(sc_controllerDefaultBackgroundColor, (sender, args) => ((WebView2)sender).OnPropertyChanged(args)));

	/// <summary>Occurs when the WebView2 has completely loaded or loading stopped with error.</summary>
	public event TypedEventHandler<WebView2, CoreWebView2NavigationCompletedEventArgs>? NavigationCompleted;

	/// <summary>Dispatches after web content sends a message to the app host.</summary>
	public event TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs>? WebMessageReceived;

	/// <summary>Occurs when the main frame of the WebView2 navigates to a different URI.</summary>
	public event TypedEventHandler<WebView2, CoreWebView2NavigationStartingEventArgs>? NavigationStarting;

	/// <summary>Occurs when the core WebView2 process fails.</summary>
	public event TypedEventHandler<WebView2, CoreWebView2ProcessFailedEventArgs>? CoreProcessFailed;

	/// <summary>Occurs when the WebView2 object is initialized.</summary>
	public event TypedEventHandler<WebView2, CoreWebView2InitializedEventArgs>? CoreWebView2Initialized;
}
