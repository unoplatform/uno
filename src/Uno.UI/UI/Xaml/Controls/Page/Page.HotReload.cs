using System;
using System.Reflection.Metadata;
using Uno.Foundation.Logging;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Windows.UI.Xaml.Controls.Page), typeof(Windows.UI.Xaml.Controls.PageElementMetadataUpdateHandler))]

namespace Windows.UI.Xaml.Controls
{
	internal static partial class PageElementMetadataUpdateHandler
	{
		public static void AfterElementReplaced(FrameworkElement oldView, FrameworkElement newView, Type[] updatedTypes)
		{
			if (oldView is Page oldPage &&
				newView is Page newPage)
			{
				if (typeof(PageElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Trace))
				{
					typeof(PageElementMetadataUpdateHandler).Log().Trace($"Instance of {oldPage.GetType().Name} replaced by instance of {newPage.GetType().Name}");
				}

#if HAS_UNO
				newPage.Frame = oldPage.Frame;
#endif

				if (newPage.Frame is not null)
				{
					// If we've replaced the Page in its frame, we may need to
					// swap the content property as well. It may be required
					// if the frame is handled by a (native) FramePresenter.
					newPage.Frame.Content = newPage;
				}
			}
			else
			{
				if (typeof(PageElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(PageElementMetadataUpdateHandler).Log().LogWarning($"AfterElementReplaced should only be called with Page instances");
				}
			}
		}
	}
}
