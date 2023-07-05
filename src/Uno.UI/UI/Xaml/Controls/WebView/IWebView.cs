#nullable disable

namespace Uno.UI.Xaml.Controls;

internal interface IWebView
{
	bool SwitchSourceBeforeNavigating { get; }

	bool IsLoaded { get; }
}
