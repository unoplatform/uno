using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Contains Uno-specific root element logic shared across RootVisual and XamlIsland.
/// </summary>
internal class UnoRootElementLogic
{
	public UnoRootElementLogic(Panel rootElement)
	{
		//Uno specific - flag as VisualTreeRoot for interop with existing logic
		rootElement.IsVisualTreeRoot = true;

		rootElement.SetValue(Panel.BackgroundProperty, new SolidColorBrush(ThemingHelper.GetRootVisualBackground()));
	}
}
