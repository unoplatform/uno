using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Extensions;

/// <summary>
/// Provides attached properties for WebView2 to support MVVM scenarios without code-behind.
/// </summary>
public static class WebView2Extensions
{
	#region DependencyProperty: IsNavigating

	public static DependencyProperty IsNavigatingProperty { [DynamicDependency(nameof(GetIsNavigating))] get; } = DependencyProperty.RegisterAttached(
		"IsNavigating",
		typeof(bool),
		typeof(WebView2Extensions),
		new PropertyMetadata(false, OnIsNavigatingChanged));

	/// <summary>
	/// Gets the value of the IsNavigating attached property for the specified <see cref="DependencyObject"/>.
	/// </summary>
	/// <param name="obj">The object from which to get the property value.</param>
	/// <returns><see langword="true"/> if the WebView2 is currently navigating; otherwise, <see langword="false"/>.</returns>
	[DynamicDependency(nameof(SetIsNavigating))]
	public static bool GetIsNavigating(DependencyObject obj) => (bool)obj.GetValue(IsNavigatingProperty);
	[DynamicDependency(nameof(GetIsNavigating))]
	/// <summary>
	/// Sets the value of the IsNavigating attached property for the specified <see cref="DependencyObject"/>.
	/// When set, enables automatic tracking of navigation state on the WebView2 control.
	/// </summary>
	/// <param name="obj">The object on which to set the property value.</param>
	/// <param name="value"><see langword="true"/> to enable navigation tracking; otherwise, <see langword="false"/>.</param>
	public static void SetIsNavigating(DependencyObject obj, bool value) => obj.SetValue(IsNavigatingProperty, value);

	private static void OnIsNavigatingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is not WebView2 control) throw new InvalidOperationException("The attached property 'IsNavigating' can only be applied to a WebView2 control.");

		// Remove previous event handlers to prevent duplicate subscriptions
		control.NavigationStarting -= OnNavigationStarting;
		control.NavigationCompleted -= OnNavigationCompleted;

		// Always subscribe to events to track navigation state
		control.NavigationStarting += OnNavigationStarting;
		control.NavigationCompleted += OnNavigationCompleted;
	}

	private static void OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
	{
		SetIsNavigating(sender, true);
	}

	private static void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		SetIsNavigating(sender, false);
	}

	#endregion

	#region DependencyProperty: DocumentTitle

	public static DependencyProperty DocumentTitleProperty { [DynamicDependency(nameof(GetDocumentTitle))] get; } = DependencyProperty.RegisterAttached(
		"DocumentTitle",
		typeof(string),
		typeof(WebView2Extensions),
		new PropertyMetadata(string.Empty, OnDocumentTitleChanged));

	[DynamicDependency(nameof(SetDocumentTitle))]
	/// <summary>
	/// Gets the value of the DocumentTitle attached property for the specified <see cref="DependencyObject"/>.
	/// </summary>
	/// <param name="obj">The object from which to get the property value.</param>
	/// <returns>The document title of the WebView2 control.</returns>
	public static string GetDocumentTitle(DependencyObject obj) => (string)(obj.GetValue(DocumentTitleProperty) ?? string.Empty);
	[DynamicDependency(nameof(GetDocumentTitle))]
	/// <summary>
	/// Sets the value of the DocumentTitle attached property for the specified <see cref="DependencyObject"/>.
	/// When set, enables automatic tracking of document title changes on the WebView2 control.
	/// </summary>
	/// <param name="obj">The object on which to set the property value.</param>
	/// <param name="value">The initial document title value.</param>
	public static void SetDocumentTitle(DependencyObject obj, string value) => obj.SetValue(DocumentTitleProperty, value);

	private static void OnDocumentTitleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is not WebView2 control) throw new InvalidOperationException("The attached property 'DocumentTitle' can only be applied to a WebView2 control.");

		if (control.CoreWebView2 is null)
		{
			// CoreWebView2 may not be initialized yet, subscribe when it's ready
			// Consider using EnsureCoreWebView2Async or waiting for initialization
			return;
		}

		// Remove previous event handler
		control.CoreWebView2.DocumentTitleChanged -= OnCoreDocumentTitleChanged;

		// Subscribe to track document title changes
		control.CoreWebView2.DocumentTitleChanged += OnCoreDocumentTitleChanged;

		// Set initial value
		SetDocumentTitle(control, control.CoreWebView2.DocumentTitle ?? string.Empty);
	}

	private static void OnCoreDocumentTitleChanged(CoreWebView2 sender, object args)
	{
		if (sender.Owner is WebView2 control)
		{
			SetDocumentTitle(control, sender.DocumentTitle ?? string.Empty);
		}
	}

	#endregion
}
