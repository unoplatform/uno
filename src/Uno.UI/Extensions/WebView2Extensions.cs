using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Extensions;

/// <summary>
/// Provides attached properties for <see cref="WebView2"/> to support binding scenarios,
/// that will automatically update the UI, without requiring code-behind.
/// </summary>
public static class WebView2Extensions
{
	#region DependencyProperty: IsNavigating
	/// <summary>  
	/// Identifies the <see cref="IsNavigatingProperty">IsNavigating Attached Property</see> in Xaml.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if the attached property is applied to an object that is not a <see cref="WebView2"/> control.</exception>
	public static DependencyProperty IsNavigatingProperty { [DynamicDependency(nameof(GetIsNavigating))] get; } = DependencyProperty.RegisterAttached(
		"IsNavigating",
		typeof(bool),
		typeof(WebView2Extensions),
		new PropertyMetadata(false, OnIsNavigatingChanged));

	/// <summary>
	/// Gets the value of the <see cref="IsNavigatingProperty">IsNavigating Attached Property</see> for the specified <see cref="DependencyObject"/>.
	/// </summary>
	/// <param name="obj">The object from which to get the property value.</param>
	/// <returns><see langword="true"/> if the <see cref="WebView2"/> is currently navigating; otherwise, <see langword="false"/>.</returns>
	[DynamicDependency(nameof(SetIsNavigating))]
	public static bool GetIsNavigating(DependencyObject obj) => (bool)obj.GetValue(IsNavigatingProperty);

	/// <summary>
	/// Sets the value of the <see cref="IsNavigatingProperty">IsNavigating Attached Property</see> for the specified <see cref="DependencyObject"/>.<br/>
	/// When set, enables automatic tracking of navigation state on the <see cref="WebView2"/> control.
	/// </summary>
	/// <param name="obj">The object on which to set the property value.</param>
	/// <param name="value"><see langword="true"/> to enable navigation tracking; otherwise, <see langword="false"/>.</param>
	[DynamicDependency(nameof(GetIsNavigating))]
	public static void SetIsNavigating(DependencyObject obj, bool value) => obj.SetValue(IsNavigatingProperty, value);

	/// <summary>
	/// Handles changes to the <see cref="IsNavigatingProperty">IsNavigating Attached Property</see> and manages
	/// the related event subscriptions on the target <see cref="WebView2"/> control.
	/// </summary>
	/// <param name="sender">The <see cref="DependencyObject"/> on which the property value has changed.</param>
	/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> data for the property change.</param>
	/// <exception cref="InvalidOperationException">Thrown if the attached property is applied to an object that is not a <see cref="WebView2"/> control.</exception>
	private static void OnIsNavigatingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is not WebView2 control)
		{
			throw new InvalidOperationException("The attached property 'IsNavigating' can only be applied to a WebView2 control.");
		}

		// Remove previous event handlers to prevent duplicate subscriptions
		control.NavigationStarting -= OnNavigationStarting;
		control.NavigationCompleted -= OnNavigationCompleted;

	/// <summary>
	/// Handles the <see cref="WebView2.Unloaded"/> event for the attached <see cref="WebView2"/> control and removes any event subscriptions:
	/// <list type="bullet">
	/// <item><see cref="WebView2.NavigationStarting"/></item>
	/// <item><see cref="WebView2.NavigationCompleted"/></item>
	/// <item><see cref="CoreWebView2.DocumentTitleChanged"/></item>
	/// </list>
	/// </summary>
	/// <param name="sender">The <see cref="WebView2"/> control that was unloaded.</param>
	/// <param name="e"> The <see cref="RoutedEventArgs"/> event data.</param>
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
	/// <summary>
	/// Identifies the <see cref="DocumentTitleProperty">DocumentTitle Attached Property</see> in Xaml.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if the attached property is applied to an object that is not a <see cref="WebView2"/> control.</exception>
	public static DependencyProperty DocumentTitleProperty { [DynamicDependency(nameof(GetDocumentTitle))] get; } = DependencyProperty.RegisterAttached(
		"DocumentTitle",
		typeof(string),
		typeof(WebView2Extensions),
		new PropertyMetadata(string.Empty, OnDocumentTitleChanged));

	/// <summary>
	/// Gets the value of the <see cref="DocumentTitleProperty">DocumentTitle Attached Property</see> for the specified <see cref="DependencyObject"/>.
	/// </summary>
	/// <param name="obj">The object from which to get the property value.</param>
	/// <returns>The document title of the WebView2 control.</returns>
	[DynamicDependency(nameof(SetDocumentTitle))]
	public static string GetDocumentTitle(DependencyObject obj) => (string)(obj.GetValue(DocumentTitleProperty) ?? string.Empty);

	/// <summary>
	/// Sets the value of the <see cref="DocumentTitleProperty">DocumentTitle Attached Property</see> for the specified <see cref="DependencyObject"/>.<br/>
	/// When set, enables automatic tracking of document title changes on the <see cref="WebView2"/> control.
	/// </summary>
	/// <param name="obj">The object on which to set the property value.</param>
	/// <param name="value">The to-be-set document title value.</param>
	[DynamicDependency(nameof(GetDocumentTitle))]
	public static void SetDocumentTitle(DependencyObject obj, string value) => obj.SetValue(DocumentTitleProperty, value);

	/// <summary>
	/// Handles changes to the <see cref="DocumentTitleProperty">DocumentTitle Attached Property</see>, ensures the underlying
	/// <see cref="CoreWebView2"/> is initialized before subscribing to the <see cref="CoreWebView2.DocumentTitleChanged"/> event
	/// and sets the initial title on the attached property.
	/// </summary>
	/// <param name="sender">The <see cref="DependencyObject"/> on which the property value changed.</param>
	/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> event data.</param>
	/// <exception cref="InvalidOperationException">Thrown if the attached property is applied to an object that is not a <see cref="WebView2"/> control.</exception>
	private static async void OnDocumentTitleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is not WebView2 control)
		{
			throw new InvalidOperationException("The attached property 'DocumentTitle' can only be applied to a WebView2 control.");
		}

		if (control.CoreWebView2 is null)
		{
			await control.EnsureCoreWebView2Async();
		}

		// Remove previous event handler
		control.CoreWebView2!.DocumentTitleChanged -= OnCoreDocumentTitleChanged;

		// Subscribe to track document title changes
		control.CoreWebView2.DocumentTitleChanged += OnCoreDocumentTitleChanged;

		// Set initial value. CoreWebView2.DocumentTitle Property will either have the current title or be an empty string, never null.
		SetDocumentTitle(control, control.CoreWebView2.DocumentTitle);
	}

	/// <summary>
	/// Handles the <see cref="CoreWebView2.DocumentTitleChanged"/> event and updates
	/// the <see langword="string"/> value of the <see cref="DocumentTitleProperty">DocumentTitle Attached Property</see>.
	/// </summary>
	/// <param name="sender">The <see cref="CoreWebView2"/> instance that raised the event.</param>
	/// <param name="args">Event data.</param>
	private static void OnCoreDocumentTitleChanged(CoreWebView2 sender, object args)
	{
		if (sender.Owner is WebView2 control) // BUG: CoreWebView2 doesn't have a Owner Propery which we could use to provide to the DP. Evaluate creating a internal Eventhandler or a Backing field for the WebView2 Object?
		{
			SetDocumentTitle(control, sender.DocumentTitle ?? string.Empty);
		}
	}
	#endregion
}
