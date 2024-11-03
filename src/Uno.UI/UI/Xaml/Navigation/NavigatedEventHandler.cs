using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Navigation;

/// <summary>
/// Represents the method that will handle the Navigated event.
/// </summary>
/// <param name="sender">The object where the handler is attached.</param>
/// <param name="e">Event data for the event.</param>
public delegate void NavigatedEventHandler(object sender, NavigationEventArgs e);
