#nullable enable

using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebViews that need a browser started before they can be used. That can take arbitrarily
/// long, so it happens outside of the layout pass applying the template, and the view is only used once it is done.
/// </summary>
internal interface IAsyncInitializedNativeWebView
{
	Task InitializeAsync();
}
