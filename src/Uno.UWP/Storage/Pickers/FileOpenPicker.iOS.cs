#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using MobileCoreServices;
using Photos;
using PhotosUI;
using UIKit;
using Uno.Helpers.Theming;
using Uno.Storage.Pickers.Internal;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private int _multipleFileLimit;
		private bool _isReadOnly;

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

		internal void SetMultipleFileLimit(int limit) => _multipleFileLimit = limit;

		internal void SetReadOnlyMode(bool readOnly) => _isReadOnly = readOnly;

		private UIViewController GetViewController(bool multiple, int limit, TaskCompletionSource<StorageFile?[]> completionSource)
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
					var imageConfiguration = new PHPickerConfiguration(PHPhotoLibrary.SharedPhotoLibrary)
					{
						Filter = PHPickerFilter.ImagesFilter,
						SelectionLimit = limit
					};
					return new PHPickerViewController(imageConfiguration)
					{
						Delegate = new PhotoPickerDelegate(completionSource, _isReadOnly)
					};
				case PickerLocationId.VideosLibrary when multiple is true && iOS14AndAbove is true:
					var videoConfiguration = new PHPickerConfiguration(PHPhotoLibrary.SharedPhotoLibrary)
					{
						Filter = PHPickerFilter.VideosFilter,
						SelectionLimit = limit
					};
					return new PHPickerViewController(videoConfiguration)
					{
						Delegate = new PhotoPickerDelegate(completionSource, _isReadOnly)
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

			var completionSource = new TaskCompletionSource<StorageFile?[]>();

			using var viewController = this.GetViewController(multiple, _multipleFileLimit, completionSource);

			viewController.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
				UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

			if (viewController.PresentationController != null)
			{
				viewController.PresentationController.Delegate = new FileOpenPickerPresentationControllerDelegate(completionSource);
			}

			await rootController.PresentViewControllerAsync(viewController, true);

			var files = await completionSource.Task;

			// Dismiss if still shown
			viewController.DismissViewController(true, null);

			if (files is null || files.Length == 0)
			{
				return FilePickerSelectedFilesArray.Empty;
			}

			return new FilePickerSelectedFilesArray(files!);
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
			private readonly bool _readOnly;

			public PhotoPickerDelegate(TaskCompletionSource<StorageFile?[]> taskCompletionSource, bool readOnly)
			{
				_taskCompletionSource = taskCompletionSource;
				_readOnly = readOnly;
			}

			public override async void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
			{
				// Dismiss the picker early to get the user back to the app as soon as possible.
				picker.DismissViewController(true, null);

				var storageFiles = await ConvertPickerResults(results);

				// This callback can be called multiple times, user tapping multiple times over the "add" button,
				// we need to ensure that we only set the result once.
				_taskCompletionSource.TrySetResult(storageFiles.ToArray());
			}

			private async Task<IEnumerable<StorageFile>> ConvertPickerResults(PHPickerResult[] results)
			{
				List<StorageFile> storageFiles = new List<StorageFile>();
				if (_readOnly)
				{
					var assetIdentifiers = results
						.Select(res => res.AssetIdentifier!)
						.Where(id => id != null)
						.ToArray();

					var resultsByIdentifier = results.ToDictionary(res => res.AssetIdentifier!);
					var assets = PHAsset.FetchAssetsUsingLocalIdentifiers(assetIdentifiers, null);
					foreach (PHAsset asset in assets)
					{
						var file = StorageFile.GetFromPHPickerResult(resultsByIdentifier[asset.LocalIdentifier], asset, null);
						storageFiles.Add(file);
					}
				}
				else
				{
					var providers = results
						.Select(res => res.ItemProvider)
						.Where(provider => provider != null && provider.RegisteredTypeIdentifiers?.Length > 0)
						.ToArray();

					foreach (NSItemProvider provider in providers)
					{
						var identifier = StorageFile.GetUTIdentifier(provider.RegisteredTypeIdentifiers ?? []) ?? "public.data";
						var data = await provider.LoadDataRepresentationAsync(identifier);

						if (data is null)
						{
							continue;
						}

						var extension = StorageFile.GetUTFileExtension(identifier);

						var destinationUrl = NSFileManager.DefaultManager
							.GetTemporaryDirectory()
							.Append($"{NSProcessInfo.ProcessInfo.GloballyUniqueString}.{extension}", false);
						data.Save(destinationUrl, false);

						storageFiles.Add(StorageFile.GetFromSecurityScopedUrl(destinationUrl, null));
					}
				}
				return storageFiles;
			}
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
