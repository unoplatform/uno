#if HAS_UNO // We don't have access to the instance of the page entry in the backstack on Windows

using System;
using System.Reflection.Metadata;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Windows.UI.Xaml.Controls.Frame), typeof(Windows.UI.Xaml.Controls.FrameElementMetadataUpdateHandler))]

namespace Windows.UI.Xaml.Controls
{
	internal static partial class FrameElementMetadataUpdateHandler
	{
		public static void ElementUpdate(FrameworkElement element, Type[] updatedTypes)
		{
			var frame = element as Frame;
			if (frame is null)
			{
				if (typeof(FrameElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(FrameElementMetadataUpdateHandler).Log().LogWarning($"AfterElementReplaced should only be called with Frame instances");
				}
				return;
			}

			foreach (var entry in frame.BackStack)
			{
				var expectedType = entry.SourcePageType.GetReplacementType();
				if (entry.Instance is not null &&
					entry.Instance.GetType() != expectedType)
				{
					if (typeof(FrameElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Trace))
					{
						typeof(FrameElementMetadataUpdateHandler).Log().Trace($"Backstack entry instance {entry.Instance.GetType().Name} replaced by instance of {expectedType.Name}");
					}
					var dc = entry.Instance.DataContext;
					entry.Instance = Activator.CreateInstance(expectedType) as Page;
					if (entry.Instance is not null)
					{
						entry.Instance.Frame = frame;
						entry.Instance.DataContext = dc;
					}
				}

				if (entry.SourcePageType is not null &&
					entry.SourcePageType != expectedType)
				{
					if (typeof(FrameElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Trace))
					{
						typeof(FrameElementMetadataUpdateHandler).Log().Trace($"Backstack entry SourcePageType changed from {entry.SourcePageType.Name} to {expectedType.Name}");
					}
					entry.SourcePageType = expectedType;
				}
			}
		}
	}
}
#endif
