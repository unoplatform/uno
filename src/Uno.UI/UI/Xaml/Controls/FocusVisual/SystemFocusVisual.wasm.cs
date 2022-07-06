#nullable enable

using Uno.Foundation;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Controls;

internal partial class SystemFocusVisual : Control
{
	private const string JsType = "Windows.UI.Xaml.Input.FocusVisual";

	partial void AttachVisualPartial()
	{
		if (FocusedElement == null)
		{
			return;
		}

		WebAssemblyRuntime.InvokeJS($"{JsType}.attachVisual({HtmlId}, {FocusedElement.HtmlId})");
	}

	partial void DetachVisualPartial()
    {
		WebAssemblyRuntime.InvokeJS($"{JsType}.detachVisual()");
	}

	[Preserve]
	public static int DispatchNativePositionChange(int focusVisualId)
	{
		var element = UIElement.GetElementFromHandle(focusVisualId) as SystemFocusVisual;
		element?.SetLayoutProperties();

		return 0;
	}
}
