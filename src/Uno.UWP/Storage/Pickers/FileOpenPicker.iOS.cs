using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Storage.Pickers.Internal;
using UIKit;
using Foundation;
using Windows.ApplicationModel.Core;
using Uno.Helpers.Theming;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private async Task<StorageFile?> PickSingleFileTaskAsync(CancellationToken token)
		{
			var files = await PickFilesAsync(false, token);
			return files.Count == 0 ? null : files[0];
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token) =>
			await PickFilesAsync(true, token);

		private UIViewController GetViewController(bool multiple, TaskCompletionSource<NSUrl?[]> completionSource)
		{
			switch (SuggestedStartLocation)
			{
				case PickerLocationId.PicturesLibrary:
					return new UIImagePickerController();
				default:
					var documentTypes = UTTypeMapper.GetDocumentTypes(FileTypeFilter);
					return new UIDocumentPickerViewController(documentTypes, UIDocumentPickerMode.Open)
					{
						AllowsMultipleSelection = multiple,
						ShouldShowFileExtensions = true,
						Delegate = new FileOpenPickerDelegate(completionSource)
					};
			}
		}

		private async Task<FilePickerSelectedFilesArray> PickFilesAsync(bool multiple, CancellationToken token)
		{
			var rootController = UIApplication.SharedApplication?.KeyWindow?.RootViewController;
			if (rootController == null)
			{
				throw new InvalidOperationException("Root controller not initialized yet. FolderPicker invoked too early.");
			}

			var completionSource = new TaskCompletionSource<NSUrl?[]>();

			using var viewController = this.GetViewController(multiple, completionSource);

			viewController.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
				UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

			if (viewController.PresentationController != null)
			{
				viewController.PresentationController.Delegate = new FileOpenPickerPresentationControllerDelegate(completionSource);
			}

			await rootController.PresentViewControllerAsync(viewController, true);

			var nsUrls = await completionSource.Task;
			if (nsUrls == null || nsUrls.Length == 0)
			{
				return FilePickerSelectedFilesArray.Empty;
			}

			var files = nsUrls
				.Where(url => url != null)
				.Select(nsUrl => StorageFile.GetFromSecurityScopedUrl(nsUrl!, null)).ToArray();
			return new FilePickerSelectedFilesArray(files);
		}


		private class FileOpenPickerDelegate : UIDocumentPickerDelegate
		{
			private readonly TaskCompletionSource<NSUrl?[]> _taskCompletionSource;

			public FileOpenPickerDelegate(TaskCompletionSource<NSUrl?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override void WasCancelled(UIDocumentPickerViewController controller) =>
				_taskCompletionSource.SetResult(Array.Empty<NSUrl?>());

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url) =>
				_taskCompletionSource.SetResult(new[] { url });

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls) =>
				_taskCompletionSource.SetResult(urls);
		}

		private class FileOpenPickerPresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
		{
			private readonly TaskCompletionSource<NSUrl?[]> _taskCompletionSource;

			public FileOpenPickerPresentationControllerDelegate(TaskCompletionSource<NSUrl?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override void DidDismiss(UIPresentationController controller) =>
				_taskCompletionSource.SetResult(Array.Empty<NSUrl?>());
		}
	}
}
