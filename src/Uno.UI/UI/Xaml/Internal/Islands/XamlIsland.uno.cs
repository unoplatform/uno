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
	void IRootElement.SetBackgroundColor(Color backgroundColor) =>
		SetValue(Panel.BackgroundProperty, new SolidColorBrush(backgroundColor));
}
