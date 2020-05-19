#if __IOS__
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private static NSObject _subscriptionToken;

		public static void SetContent(DataPackage content)
		{
			// Setting to null doesn't reset the clipboard like for Android
			UIPasteboard.General.String = content?.Text ?? string.Empty;
		}

		public static DataPackageView GetContent()
		{
			var dataPackageView = new DataPackageView();
			if (UIPasteboard.General.String != null)
			{
				dataPackageView.SetFormatTask(StandardDataFormats.Text, Task.FromResult(UIPasteboard.General.String));
			}
			return dataPackageView;
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
