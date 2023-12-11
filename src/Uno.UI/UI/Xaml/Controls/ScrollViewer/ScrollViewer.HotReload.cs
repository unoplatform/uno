using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Windows.UI.Xaml.Controls.ScrollViewer), typeof(Windows.UI.Xaml.Controls.ScrollViewerMetadataUpdateHandler))]

namespace Windows.UI.Xaml.Controls;

internal static partial class ScrollViewerMetadataUpdateHandler
{
	public static void CaptureState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[] updatedTypes)
	{
		stateDictionary["VOffset"] = (element as ScrollViewer)?.VerticalOffset ?? 0.0;
		stateDictionary["HOffset"] = (element as ScrollViewer)?.HorizontalOffset ?? 0.0;
	}

	public static async Task RestoreState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[] updatedTypes)
	{
		if (element is ScrollViewer sv)
		{
			await Task.Yield();
			var voffset = stateDictionary.TryGetValue("VOffset", out var offset) ? (double)offset : 0.0;
			var hoffset = stateDictionary.TryGetValue("HOffset", out var offset_y) ? (double)offset_y : 0.0;
			sv.ChangeView(hoffset, voffset, sv.ZoomFactor, true);
		}
	}
}
