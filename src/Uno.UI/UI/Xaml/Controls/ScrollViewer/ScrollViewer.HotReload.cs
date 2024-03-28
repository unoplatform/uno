using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Windows.UI.Xaml.Controls.ScrollViewer), typeof(Windows.UI.Xaml.Controls.ScrollViewerMetadataUpdateHandler))]

namespace Windows.UI.Xaml.Controls;

internal static partial class ScrollViewerMetadataUpdateHandler
{
	private static Logger _log = typeof(ScrollViewerMetadataUpdateHandler).Log();

	public static void CaptureState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[] updatedTypes)
	{
		if (element is ScrollViewer sv)
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.Debug($"Saving state of {element.GetDebugDepth()}-{element.GetDebugName()} (v: {sv.VerticalOffset} | h: {sv.HorizontalOffset})");
			}

			stateDictionary["VOffset"] = sv.VerticalOffset;
			stateDictionary["HOffset"] = sv.HorizontalOffset;
		}
	}

	public static Task RestoreState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[] updatedTypes)
	{
		if (element is ScrollViewer sv
			&& (stateDictionary.TryGetValue("VOffset", out var vOffset)
			| stateDictionary.TryGetValue("HOffset", out var hOffset)))
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.Debug($"Restoring state of {element.GetDebugDepth()}-{element.GetDebugName()} (v: {vOffset} | h: {hOffset})");
			}

			sv.ChangeView((double?)hOffset, (double?)vOffset, sv.ZoomFactor, true);
		}

		return Task.CompletedTask;
	}
}
