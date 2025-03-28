using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Navigation;

/// <summary>
/// Represents the method to use as the Page.OnNavigatingFrom callback override.
/// </summary>
/// <param name="sender">The object where the method is implemented.</param>
/// <param name="e">Event data that is passed through the callback.</param>
public delegate void NavigatingCancelEventHandler(object sender, NavigatingCancelEventArgs e);
