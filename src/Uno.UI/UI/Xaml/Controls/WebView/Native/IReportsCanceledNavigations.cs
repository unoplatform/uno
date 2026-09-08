#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebViews whose engine already raises a navigation completion
/// for a navigation cancelled from <c>CoreWebView2NavigationStartingEventArgs.Cancel</c>.
/// The shared layer then leaves the completion to the engine instead of synthesizing one.
/// </summary>
internal interface IReportsCanceledNavigations
{
}
