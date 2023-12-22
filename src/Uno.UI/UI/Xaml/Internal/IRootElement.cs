using Windows.UI;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal interface IRootElement
{
	void NotifyFocusChanged();

	void ProcessPointerUp(PointerRoutedEventArgs args, bool isAfterHandledUp);

	void SetBackgroundColor(Color backgroundColor);
}
