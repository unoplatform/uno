#nullable enable

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
			return files.FirstOrDefault();
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token) =>
			await PickFilesAsync(true, token);

		private async Task<FilePickerSelectedFilesArray> PickFilesAsync(bool multiple, CancellationToken token)
		{
			var rootController = UIApplication.SharedApplication?.KeyWindow?.RootViewController;
			if (rootController == null)
			{
				throw new InvalidOperationException("Root controller not initialized yet. FolderPicker invoked too early.");
			}

			var documentTypes = UTTypeMapper.GetDocumentTypes(FileTypeFilter);
			using var documentPicker = new UIDocumentPickerViewController(documentTypes, UIDocumentPickerMode.Open);
			documentPicker.AllowsMultipleSelection = multiple;

			var completionSource = new TaskCompletionSource<NSUrl?[]>();

			documentPicker.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
				UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

			documentPicker.ShouldShowFileExtensions = true;
			documentPicker.Delegate = new FileOpenPickerDelegate(completionSource);
			documentPicker.PresentationController.Delegate = new FileOpenPickerPresentationControllerDelegate(completionSource);

			await rootController.PresentViewControllerAsync(documentPicker, true);

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
