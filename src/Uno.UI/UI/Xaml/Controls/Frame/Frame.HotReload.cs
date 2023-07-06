using System;
using System.Reflection.Metadata;
using Uno.UI.Helpers;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Windows.UI.Xaml.Controls.Frame), typeof(Windows.UI.Xaml.Controls.FrameElementMetadataUpdateHandler))]

namespace Windows.UI.Xaml.Controls
{
	public partial class FrameElementMetadataUpdateHandler
	{
		public static void ElementUpdate(FrameworkElement element, Type[] updatedTypes)
		{
			var frame = element as Frame;
			if (frame is null)
			{
				return;
			}

			foreach (var entry in frame.BackStack)
			{
				var expectedType = entry.SourcePageType.GetReplacementType();
				if (entry.Instance is not null &&
					entry.Instance.GetType() != expectedType)
				{
					var dc = entry.Instance.DataContext;
					entry.Instance = Activator.CreateInstance(expectedType) as Page;
					entry.Instance.Frame = frame;
					if (entry.Instance is not null)
					{
						entry.Instance.DataContext = dc;
					}
				}

				if (entry.SourcePageType is not null &&
					entry.SourcePageType != expectedType)
				{
					entry.SourcePageType = expectedType;
				}
			}
		}
	}
}
