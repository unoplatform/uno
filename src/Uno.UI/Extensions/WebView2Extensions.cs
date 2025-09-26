using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Extensions;

public static class WebView2Extensions
{
	#region DependencyProperty: IsNavigating

	public static DependencyProperty IsNavigatingProperty { [DynamicDependency(nameof(GetIsNavigating))] get; } = DependencyProperty.RegisterAttached(
		"IsNavigating",
		typeof(bool),
		typeof(WebView2Extensions),
		new PropertyMetadata(false, OnIsNavigatingChanged));

	[DynamicDependency(nameof(SetIsNavigating))]
	public static bool GetIsNavigating(DependencyObject obj) => (bool)obj.GetValue(IsNavigatingProperty);
	[DynamicDependency(nameof(GetIsNavigating))]
	public static void SetIsNavigating(DependencyObject obj, bool value) => obj.SetValue(IsNavigatingProperty, value);

	private static void OnIsNavigatingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is not WebView2 control) throw new InvalidOperationException("The attached property 'IsNavigating' can only be applied to a WebView2 control.");

		// Remove previous event handler if any
		control.NavigationStarting -= OnNavigationStarting;
		control.NavigationCompleted -= OnNavigationCompleted;

		// Only wire events if property is set to true
		if ((bool)e.NewValue)
		{
			control.NavigationStarting += OnNavigationStarting;
			control.NavigationCompleted += OnNavigationCompleted;
		}
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
	public static string GetDocumentTitle(DependencyObject obj) => (string)(obj.GetValue(DocumentTitleProperty) ?? string.Empty);
	[DynamicDependency(nameof(GetDocumentTitle))]
	public static void SetDocumentTitle(DependencyObject obj, string value) => obj.SetValue(DocumentTitleProperty, value);

	private static void OnDocumentTitleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is not WebView2 control) throw new InvalidOperationException("The attached property 'DocumentTitle' can only be applied to a WebView2 control.");

		// Remove previous event handler if any
		control.CoreWebView2.DocumentTitleChanged -= OnCoreDocumentTitleChanged;

		// Only wire event if property is set to non-empty string
		if (!string.IsNullOrEmpty((string)e.NewValue))
		{
			control.CoreWebView2.DocumentTitleChanged += OnCoreDocumentTitleChanged;
		}
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
