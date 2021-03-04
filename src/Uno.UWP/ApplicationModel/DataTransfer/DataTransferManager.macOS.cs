#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppKit;
using CoreGraphics;
using Foundation;
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		private const int DefaultPickerWidth = 120;
		private const int DefaultPickerHeight = 160;

		public static bool IsSupported() => true;

		private static async Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage)
		{
			var window = NSApplication.SharedApplication.MainWindow;

			if (window == null)
			{
				throw new InvalidOperationException("Sharing is not possible when no window is active.");
			}

			var view = window.ContentView;

			var dataPackageView = dataPackage.GetView();

			var sharedData = new List<NSObject>();

			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				var text = await dataPackageView.GetTextAsync();
				sharedData.Add(new NSString(text));
			}

			var uri = await GetSharedUriAsync(dataPackageView);
			if (uri != null)
			{
				sharedData.Add(NSUrl.FromString(uri.OriginalString));
			}

			CGRect targetRect;
			if (options.SelectionRect != null)
			{
				targetRect = options.SelectionRect.Value;
			}
			else
			{
				// Try to center the picker within the window
				targetRect = new CGRect(
					view.Bounds.Width / 2f - DefaultPickerWidth / 2,
					view.Bounds.Height / 2 - DefaultPickerHeight / 2,
					0,
					0);
			}

			var picker = new NSSharingServicePicker(sharedData.ToArray());

			var completionSource = new TaskCompletionSource<bool>();

			picker.DidChooseSharingService += (s, e) =>
			{
				completionSource.SetResult(e.Service != null);
			};

			picker.ShowRelativeToRect(targetRect, view, NSRectEdge.MinYEdge);

			return await completionSource.Task;
		}
	}
}
