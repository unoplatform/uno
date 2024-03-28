#nullable enable

using System;
using Uno.Foundation;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Xaml.Controls;

internal partial class SystemFocusVisual : Control
{
	partial void AttachVisualPartial()
	{
		if (FocusedElement == null)
		{
			return;
		}

		NativeMethods.AttachVisual(HtmlId, FocusedElement.HtmlId);
	}

	partial void DetachVisualPartial()
	{
		NativeMethods.DetachVisual();
	}

	[JSExport]
	public static int DispatchNativePositionChange(int focusVisualId)
	{
		var element = UIElement.GetElementFromHandle(focusVisualId) as SystemFocusVisual;
		element?.SetLayoutProperties();

		return 0;
	}

	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Windows.UI.Xaml.Input.FocusVisual.attachVisual")]
		internal static partial void AttachVisual(IntPtr htmlId, IntPtr focusedElementId);

		[JSImport("globalThis.Windows.UI.Xaml.Input.FocusVisual.detachVisual")]
		internal static partial void DetachVisual();
	}
}
