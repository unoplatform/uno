#nullable enable

using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UIKit;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.Storage.Pickers.Internal;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private Task<StorageFile?> PickSingleFileTaskAsync(CancellationToken token)
		{
			var tcs = new TaskCompletionSource<StorageFile?>();
			NativeDispatcher.Main.Enqueue(async () =>
			{
				var files = await PickFilesAsync(false, token);

				if (files.Count > 0)
				{
					tcs.SetResult(files[0]);
				}
				else
				{
					tcs.SetResult(null);
				}
			});

			return tcs.Task;
		}

		private Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token)
		{
			var tcs = new TaskCompletionSource<IReadOnlyList<StorageFile>>();
			NativeDispatcher.Main.Enqueue(async () =>
			{
				var files = await PickFilesAsync(true, token);
				tcs.SetResult(files);
			});

			return tcs.Task;
		}

		private UIViewController GetViewController(bool multiple, TaskCompletionSource<NSUrl?[]> completionSource)
		{
			switch (SuggestedStartLocation)
			{
				case PickerLocationId.PicturesLibrary:

					var configuration = new PHPickerConfiguration
					{
						Filter = PHPickerFilter.ImagesFilter,
						SelectionLimit = multiple ? 0 : 1
					};
					return new PHPickerViewController(configuration)
					{
						Delegate = new PhotoPickerDelegate(completionSource)
					};

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
			if (rootController is null)
			{
				throw new InvalidOperationException("Root controller not initialized yet. FolderPicker invoked too early.");
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Picking files. Multiple: {multiple}");
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

			rootController.DismissViewController(true, null);

			if (nsUrls is null || nsUrls.Length == 0)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("User cancelled file picking.");
				}

				return FilePickerSelectedFilesArray.Empty;
			}

			var files = nsUrls
				.Where(url => url is not null)
				.Select(nsUrl => StorageFile.GetFromSecurityScopedUrl(nsUrl!, null)).ToArray();

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Picked {files.Length} files from {nsUrls.Length} URLs.");
			}

			return new FilePickerSelectedFilesArray(files);
		}

		private class PhotoPickerDelegate : PHPickerViewControllerDelegate
		{
			private readonly TaskCompletionSource<NSUrl?[]> _taskCompletionSource;

			public PhotoPickerDelegate(TaskCompletionSource<NSUrl?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
			{
				var urls = results.Select(result => result.ItemProvider?.LoadObject(NSUrl.FromUrl, null)).OfType<NSUrl>().ToArray();
				_taskCompletionSource.SetResult(urls);
			}
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
