using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Navigation;

/// <summary>
/// Represents a method that will handle the WebView.NavigationFailed and Frame.NavigationFailed events.
/// </summary>
/// <param name="sender">The object where the handler is attached.</param>
/// <param name="e">Event data for the event.</param>
public delegate void NavigationFailedEventHandler(object sender, NavigationFailedEventArgs e);
