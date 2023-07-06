using System;
using System.Reflection.Metadata;
using Uno.UI.Helpers;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Windows.UI.Xaml.Controls.Page), typeof(Windows.UI.Xaml.Controls.PageElementMetadataUpdateHandler))]

namespace Windows.UI.Xaml.Controls
{
	public partial class PageElementMetadataUpdateHandler
	{
		public static void AfterElementReplaced(FrameworkElement oldView, FrameworkElement newView, Type[] updatedTypes)
		{
			if (oldView is Page oldPage &&
				newView is Page newPage)
			{
				newPage.Frame = oldPage.Frame;

				// If we've replaced the Page in its frame, we may need to
				// swap the content property as well. If may be required
				// if the frame is handled by a (native) FramePresenter.
				newPage.Frame.Content = newPage;
			}
		}
	}
}
