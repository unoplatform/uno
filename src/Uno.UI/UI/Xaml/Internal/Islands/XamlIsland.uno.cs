#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Core;
using Windows.UI;

namespace Uno.UI.Xaml.Islands;

partial class XamlIsland : IRootElement
{
	private readonly UnoRootElementLogic _rootElementLogic;

	internal bool IsSiteVisible { get; set; }

	internal Window? OwnerWindow { get; set; }

	void IRootElement.NotifyFocusChanged() => _rootElementLogic.NotifyFocusChanged();

	void IRootElement.ProcessPointerUp(PointerRoutedEventArgs args, bool isAfterHandledUp) =>
		_rootElementLogic.ProcessPointerUp(args, isAfterHandledUp);

	void IRootElement.SetBackgroundColor(Color backgroundColor) =>
		SetValue(Panel.BackgroundProperty, new SolidColorBrush(backgroundColor));

	internal void SetHasTransparentBackground(bool hasTransparentBackground)
	{
		if (hasTransparentBackground)
		{
			this.Background = new SolidColorBrush(Colors.Transparent);
		}
	}
}
