#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using MobileCoreServices;
using UIKit;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;
using Uno.UI.Dispatching;
using Uno.Foundation.Logging;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			async Task<StorageFolder?> PickFolderAsync()
			{
				var rootController = UIApplication.SharedApplication?.KeyWindow?.RootViewController;
				if (rootController is null)
				{
					throw new InvalidOperationException("Root controller not initialized yet. FolderPicker invoked too early.");
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("Picking Folder.");
				}

				var documentTypes = new string[] { UTType.Folder };
				using var documentPicker = new UIDocumentPickerViewController(documentTypes, UIDocumentPickerMode.Open);

				var completionSource = new TaskCompletionSource<NSUrl?>();

				documentPicker.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
					UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

				documentPicker.Delegate = new FolderPickerDelegate(completionSource);

				if (documentPicker.PresentationController is not null)
				{
					documentPicker.PresentationController.Delegate = new FolderPickerPresentationControllerDelegate(completionSource);
				}

				await rootController.PresentViewControllerAsync(documentPicker, true);

				var nsUrl = await completionSource.Task;
				if (nsUrl is null)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("User cancelled folder picking.");
					}
					return null;
				}

				var folder = StorageFolder.GetFromSecurityScopedUrl(nsUrl, null);
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Picked folder: {folder.Path} from Url: {nsUrl}.");
				}

				return folder;
			}

			var tcs = new TaskCompletionSource<StorageFolder?>();
			NativeDispatcher.Main.Enqueue(async () =>
			{
				var folder = await PickFolderAsync();
				tcs.SetResult(folder);
			});

			return tcs.Task;
		}

		private class FolderPickerDelegate : UIDocumentPickerDelegate
		{
			private readonly TaskCompletionSource<NSUrl?> _taskCompletionSource;

			public FolderPickerDelegate(TaskCompletionSource<NSUrl?> taskCompletionSource)
			{
				_taskCompletionSource = taskCompletionSource;
			}

			public override void WasCancelled(UIDocumentPickerViewController controller)
				=> _taskCompletionSource.SetResult(null);

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
				=> _taskCompletionSource.SetResult(url);
		}

		private class FolderPickerPresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
		{
			private readonly TaskCompletionSource<NSUrl?> _taskCompletionSource;

			public FolderPickerPresentationControllerDelegate(TaskCompletionSource<NSUrl?> taskCompletionSource)
			{
				_taskCompletionSource = taskCompletionSource;
			}

			public override void DidDismiss(UIPresentationController controller)
				=> _taskCompletionSource.SetResult(null);
		}
	}
}
