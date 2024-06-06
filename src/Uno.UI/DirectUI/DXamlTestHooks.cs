using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.DirectUI;
internal static class DXamlTestHooks
{
	public static ToolTip TestGetActualToolTip(UIElement element)
	{
		// TODO:MZ: Should be GetActualToolTipObject
		return ToolTipService.GetToolTipReference(element);
	}
}
