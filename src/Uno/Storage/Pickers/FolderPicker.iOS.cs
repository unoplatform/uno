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
using System.Linq;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			var tcs = new TaskCompletionSource<StorageFolder?>();
			NativeDispatcher.Main.Enqueue(async () =>
			{
				var folder = await PickFolderAsync(token);
				tcs.SetResult(folder);
			});

			return tcs.Task;
		}

		private async Task<StorageFolder?> PickFolderAsync(CancellationToken token)
		{
			var rootController = UIApplication.SharedApplication?.KeyWindow?.RootViewController;
			if (rootController is null)
			{
				throw new InvalidOperationException("Root controller not initialized yet. FolderPicker invoked too early.");
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("PickFolderAsync() Picking Folder.");
			}

			var documentTypes = new string[] { UTType.Folder };
			var completionSource = new TaskCompletionSource<NSUrl?>();

			using var documentPicker = new UIDocumentPickerViewController(documentTypes, UIDocumentPickerMode.Open)
			{
				Delegate = new FolderPickerDelegate(completionSource),
			};

			documentPicker.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
				UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

			if (documentPicker.PresentationController is not null)
			{
				documentPicker.PresentationController.Delegate = new FolderPickerPresentationControllerDelegate(completionSource);
			}

			await rootController.PresentViewControllerAsync(documentPicker, true);

			var nsUrl = await completionSource.Task;

			rootController.DismissViewController(true, null);

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
				this.Log().Debug($"Picked folder: {folder?.Path} from Url: {nsUrl}.");
			}

			return folder;
		}

		private class FolderPickerDelegate : UIDocumentPickerDelegate
		{
			private readonly TaskCompletionSource<NSUrl?> _taskCompletionSource;

			public FolderPickerDelegate(TaskCompletionSource<NSUrl?> taskCompletionSource)
			{
				_taskCompletionSource = taskCompletionSource;
			}

			public override void WasCancelled(UIDocumentPickerViewController controller)
			{

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("FolderPickerDelegate.WasCancelled");
				}

				_taskCompletionSource.SetResult(null);
			}

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"FolderPickerDelegate.DidPickDocument {url}");
				}

				_taskCompletionSource.SetResult(url);
			}

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"FolderPickerDelegate.DidPickDocument(s) {urls} Size: {urls?.Length}");
				}

				_taskCompletionSource.SetResult(urls?.FirstOrDefault());
			}
		}

		private class FolderPickerPresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
		{
			private readonly TaskCompletionSource<NSUrl?> _taskCompletionSource;

			public FolderPickerPresentationControllerDelegate(TaskCompletionSource<NSUrl?> taskCompletionSource)
			{
				_taskCompletionSource = taskCompletionSource;
			}

			public override void DidDismiss(UIPresentationController controller)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"FolderPickerPresentationControllerDelegate.DidDismiss");
				}

				_taskCompletionSource.SetResult(null);
			}
		}
	}
}
