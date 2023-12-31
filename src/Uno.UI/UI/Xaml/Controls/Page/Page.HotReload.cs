using System;
using System.Reflection.Metadata;
using Uno.Foundation.Logging;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Microsoft.UI.Xaml.Controls.Page), typeof(Microsoft.UI.Xaml.Controls.PageElementMetadataUpdateHandler))]

namespace Microsoft.UI.Xaml.Controls
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

				newPage.Frame = oldPage.Frame;

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
