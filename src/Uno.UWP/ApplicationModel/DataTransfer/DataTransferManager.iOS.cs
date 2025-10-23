#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;

using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		public static bool IsSupported() => true;

		private static async Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage)
		{
			var rootViewController = UIApplication.SharedApplication?.KeyWindow?.RootViewController;
			if (rootViewController == null)
			{
				if (_instance.Value.Log().IsEnabled(LogLevel.Error))
				{
					_instance.Value.Log().LogError("The Share API was called too early in the application lifecycle");
				}
				return false;
			}

			var dataPackageView = dataPackage.GetView();

			var sharedData = new List<NSObject>();

			var title = dataPackage.Properties.Title ?? string.Empty;

			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				var text = await dataPackageView.GetTextAsync();
				sharedData.Add(new DataActivityItemSource(new NSString(text), title));
			}

			var uri = await GetSharedUriAsync(dataPackageView);
			if (uri != null && NSUrl.FromString(uri.OriginalString) is { } nsUrl)
			{
				sharedData.Add(new DataActivityItemSource(nsUrl, title));
			}

			var activityViewController = new UIActivityViewController(sharedData.ToArray(), null);

			if (activityViewController.PopoverPresentationController != null && rootViewController.View != null)
			{
				activityViewController.PopoverPresentationController.SourceView = rootViewController.View;

				if (options.SelectionRect != null)
				{
					activityViewController.PopoverPresentationController.SourceRect = options.SelectionRect.Value.ToCGRect();
				}
				else
				{
					activityViewController.PopoverPresentationController.SourceRect = new CGRect(rootViewController.View.Bounds.Width / 2, rootViewController.View.Bounds.Height / 2, 0, 0);
					activityViewController.PopoverPresentationController.PermittedArrowDirections = 0;
				}
			}

			if (options.Theme != ShareUITheme.Default)
			{
				activityViewController.OverrideUserInterfaceStyle = options.Theme == ShareUITheme.Light ? UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;
			}
			else
			{
				// Theme should match the application theme
				activityViewController.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ? UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;
			}

			var completionSource = new TaskCompletionSource<bool>();

			activityViewController.CompletionWithItemsHandler = (activityType, completed, returnedItems, error) =>
			{
				completionSource.SetResult(completed);
			};

			await rootViewController.PresentViewControllerAsync(activityViewController, true);

			return await completionSource.Task;
		}

		internal class DataActivityItemSource : UIActivityItemSource
		{
			private NSObject _data;
			private string _title;

			internal DataActivityItemSource(NSObject data, string title) =>
				(_data, _title) = (data, title);

			public override NSObject GetItemForActivity(UIActivityViewController activityViewController, NSString? activityType) => _data;

			public override string GetSubjectForActivity(UIActivityViewController activityViewController, NSString? activityType) => _title;

			public override NSObject GetPlaceholderData(UIActivityViewController activityViewController) => _data;
		}
	}
}
