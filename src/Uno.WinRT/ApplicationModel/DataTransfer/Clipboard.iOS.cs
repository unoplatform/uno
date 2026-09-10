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

			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.High,
				async () => await SetContentAsync(content));
		}

		internal static async Task SetContentAsync(DataPackage content)
		{
			var data = content?.GetView(); // Freezes the DataPackage

			if (data?.Contains(StandardDataFormats.Text) ?? false)
			{
				var text = await data.GetTextAsync();

				// Setting to null doesn't reset the clipboard like for Android
				UIPasteboard.General.String = text ?? string.Empty;
			}
		}

		public static DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			// Reading UIPasteboard.General.String eagerly triggers the iOS paste-permission
			// prompt. HasStrings is prompt-free, so defer the actual read until the content
			// is requested, matching a real paste intent.
			if (UIPasteboard.General.HasStrings)
			{
				dataPackage.SetDataProvider(StandardDataFormats.Text, ct => Task.FromResult<object>(UIPasteboard.General.String ?? string.Empty));
			}

			return dataPackage.GetView();
		}

		internal static bool IsTextAvailable() => UIPasteboard.General.HasStrings;

		public static void Clear()
		{
			UIPasteboard.General.Items = Array.Empty<NSDictionary>();
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
