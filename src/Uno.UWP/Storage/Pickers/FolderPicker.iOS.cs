#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using MobileCoreServices;
using UIKit;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			var rootController = UIApplication.SharedApplication?.KeyWindow?.RootViewController;
			if (rootController == null)
			{
				throw new InvalidOperationException("Root controller not initialized yet. FolderPicker invoked too early.");
			}

			var documentTypes = new string[] { UTType.Folder };
			using var documentPicker = new UIDocumentPickerViewController(documentTypes, UIDocumentPickerMode.Open);

			var completionSource = new TaskCompletionSource<NSUrl?>();

			documentPicker.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
				UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

			documentPicker.Delegate = new FolderPickerDelegate(completionSource);
			documentPicker.PresentationController.Delegate = new FolderPickerPresentationControllerDelegate(completionSource);

			await rootController.PresentViewControllerAsync(documentPicker, true);

			var nsUrl = await completionSource.Task;
			if (nsUrl == null)
			{
				return null;
			}

			return StorageFolder.GetFromSecurityScopedUrl(nsUrl, null);
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
