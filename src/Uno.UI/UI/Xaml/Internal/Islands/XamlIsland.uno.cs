#nullable enable

using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Xaml.Islands;

partial class XamlIsland : IRootElement
{
	private readonly UnoRootElementLogic _rootElementLogic;

	void IRootElement.NotifyFocusChanged() => _rootElementLogic.NotifyFocusChanged();

	void IRootElement.ProcessPointerUp(PointerRoutedEventArgs args, bool isAfterHandledUp) =>
		_rootElementLogic.ProcessPointerUp(args, isAfterHandledUp);

	void IRootElement.SetBackgroundColor(Color backgroundColor) =>
		SetValue(Panel.BackgroundProperty, new SolidColorBrush(backgroundColor));
}
