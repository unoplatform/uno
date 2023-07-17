using Windows.UI.Core;

namespace Uno.UI.Xaml.Controls;

internal interface IWebView
{
	bool SwitchSourceBeforeNavigating { get; }

	bool IsLoaded { get; }

	CoreDispatcher Dispatcher { get; }
}
