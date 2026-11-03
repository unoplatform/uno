using Windows.UI;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal interface IRootElement
{
	UnoRootElementLogic RootElementLogic { get; }

	void SetBackgroundColor(Color backgroundColor);
}
