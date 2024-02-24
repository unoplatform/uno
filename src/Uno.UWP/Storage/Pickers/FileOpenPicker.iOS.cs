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
<<<<<<< HEAD
=======
using PhotosUI;
<<<<<<< HEAD
>>>>>>> f0e364b1c6 (feat: Add support for PHPicker on iOS)
=======
using Uno.UI.Dispatching;
<<<<<<< HEAD
>>>>>>> a8f5724a91 (chore: make non-ui thread ready)
=======
using Windows.Foundation.Metadata;
>>>>>>> ce5369f979 (chore: validate iOS version for feature)

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

<<<<<<< HEAD
		private UIViewController GetViewController(bool multiple, TaskCompletionSource<NSUrl?[]> completionSource)
=======
		private UIViewController GetViewController(bool multiple, TaskCompletionSource<StorageFile?[]> completionSource)
>>>>>>> f0e364b1c6 (feat: Add support for PHPicker on iOS)
		{
			var iOS14AndAbove = UIDevice.CurrentDevice.CheckSystemVersion(14, 0);
			switch (SuggestedStartLocation)
			{
				case PickerLocationId.PicturesLibrary when multiple is false || iOS14AndAbove is false:
				case PickerLocationId.VideosLibrary when multiple is false || iOS14AndAbove is false:
					return new UIImagePickerController()
					{
						SourceType = UIImagePickerControllerSourceType.PhotoLibrary,
						MediaTypes = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary),
						ImagePickerControllerDelegate = new ImageOpenPickerDelegate(completionSource)
					};

				case PickerLocationId.PicturesLibrary when multiple is true && iOS14AndAbove is true:
				case PickerLocationId.VideosLibrary when multiple is true && iOS14AndAbove is true:
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
			if (rootController == null)
			{
				throw new InvalidOperationException("Root controller not initialized yet. FolderPicker invoked too early.");
			}

<<<<<<< HEAD
			var completionSource = new TaskCompletionSource<NSUrl?[]>();
=======
			var completionSource = new TaskCompletionSource<StorageFile?[]>();
>>>>>>> f0e364b1c6 (feat: Add support for PHPicker on iOS)

			using var viewController = this.GetViewController(multiple, completionSource);

			viewController.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
				UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

			if (viewController.PresentationController != null)
			{
				viewController.PresentationController.Delegate = new FileOpenPickerPresentationControllerDelegate(completionSource);
			}

			await rootController.PresentViewControllerAsync(viewController, true);

			var files = await completionSource.Task;

			rootController.DismissViewController(true, null);

<<<<<<< HEAD
			if (nsUrls == null || nsUrls.Length == 0)
=======
			if (files is null || files.Length == 0)
>>>>>>> f0e364b1c6 (feat: Add support for PHPicker on iOS)
			{
				return FilePickerSelectedFilesArray.Empty;
			}

<<<<<<< HEAD
			var files = nsUrls
				.Where(url => url != null)
				.Select(nsUrl => StorageFile.GetFromSecurityScopedUrl(nsUrl!, null)).ToArray();
			return new FilePickerSelectedFilesArray(files);
=======
			return new FilePickerSelectedFilesArray(files!);
>>>>>>> f0e364b1c6 (feat: Add support for PHPicker on iOS)
		}

		private class ImageOpenPickerDelegate : UIImagePickerControllerDelegate
		{
			private readonly TaskCompletionSource<StorageFile?[]> _taskCompletionSource;

			public ImageOpenPickerDelegate(TaskCompletionSource<StorageFile?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override void Canceled(UIImagePickerController picker) =>
				_taskCompletionSource.SetResult(Array.Empty<StorageFile?>());

			public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
			{
				if (info.ValueForKey(new NSString("UIImagePickerControllerImageURL")) is NSUrl nSUrl)
				{
					var file = StorageFile.GetFromSecurityScopedUrl(nSUrl, null);
					_taskCompletionSource.SetResult([file]);
				}
				else
				{
					_taskCompletionSource.SetResult(Array.Empty<StorageFile?>());
				}
			}
		}

		private class PhotoPickerDelegate : PHPickerViewControllerDelegate
		{
			private readonly TaskCompletionSource<StorageFile?[]> _taskCompletionSource;

			public PhotoPickerDelegate(TaskCompletionSource<StorageFile?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
			{
				var storageFiles = ConvertPickerResults(results);
				_taskCompletionSource.SetResult(storageFiles.ToArray());
			}
			static IEnumerable<StorageFile> ConvertPickerResults(PHPickerResult[] results)
				=> results
					.Select(res => res.ItemProvider)
					.Where(provider => provider != null && provider.RegisteredTypeIdentifiers?.Length > 0)
					.Select(p => StorageFile.GetFromItemProvider(p, null))
					.ToArray();
		}

		private class FileOpenPickerDelegate : UIDocumentPickerDelegate
		{
			private readonly TaskCompletionSource<StorageFile?[]> _taskCompletionSource;

			public FileOpenPickerDelegate(TaskCompletionSource<StorageFile?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override void WasCancelled(UIDocumentPickerViewController controller) =>
				_taskCompletionSource.SetResult(Array.Empty<StorageFile?>());

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url) =>
				_taskCompletionSource.SetResult(new[] { StorageFile.GetFromSecurityScopedUrl(url, null) });

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
			{
				var files = urls
					.Where(url => url != null)
					.Select(nsUrl => StorageFile.GetFromSecurityScopedUrl(nsUrl!, null))
					.ToArray();
				_taskCompletionSource.SetResult(files);
			}
		}

		private class FileOpenPickerPresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
		{
			private readonly TaskCompletionSource<StorageFile?[]> _taskCompletionSource;

			public FileOpenPickerPresentationControllerDelegate(TaskCompletionSource<StorageFile?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override void DidDismiss(UIPresentationController controller) =>
				_taskCompletionSource.SetResult(Array.Empty<StorageFile?>());
		}
	}
}
