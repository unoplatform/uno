#if __IOS__
using System;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Windows.UI.Core;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private static NSObject _subscriptionToken;

		public static void SetContent(DataPackage content)
		{
			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.High,
				async () =>
				{
					var data = content?.GetView(); // Freezes the DataPackage

					if (data?.Contains(StandardDataFormats.Text) ?? false)
					{
						var text = await data.GetTextAsync();

						// Setting to null doesn't reset the clipboard like for Android
						UIPasteboard.General.String = text ?? string.Empty;
					}
				});
		}

		public static DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			var clipText = UIPasteboard.General.String;
			if (clipText != null)
			{
				dataPackage.SetText(clipText);
			}

			return dataPackage.GetView();
		}

		public static void Clear()
		{
			UIPasteboard.General.Items = new NSDictionary[0];
		}

		private static void StartContentChanged()
		{
			_subscriptionToken = NSNotificationCenter.DefaultCenter.AddObserver(UIPasteboard.ChangedNotification, PasteboardChanged);
		}

		private static void StopContentChanged()
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(_subscriptionToken);
		}

		private static void PasteboardChanged(NSNotification notification) => OnContentChanged();
	}
}
#endif
