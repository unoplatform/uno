using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DirectUI;

internal static class DXamlTestHooks
{
	public static ToolTip TestGetActualToolTip(UIElement element) => ToolTipService.GetActualToolTipObject(element);
}
