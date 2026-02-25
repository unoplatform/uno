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
using PhotosUI;
using Uno.UI.Dispatching;
using MobileCoreServices;
using Windows.Foundation.Metadata;
using Uno.Foundation.Logging;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private int _multipleFileLimit;

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

		internal void SetMultipleFileLimit(int limit)
		{
			_multipleFileLimit = limit;
		}

		private UIViewController GetViewController(bool multiple, int limit, TaskCompletionSource<StorageFile?[]> completionSource)
		{
			var iOS14AndAbove = UIDevice.CurrentDevice.CheckSystemVersion(14, 0);
			switch (SuggestedStartLocation)
			{
				case PickerLocationId.PicturesLibrary when multiple is false || iOS14AndAbove is false:
				case PickerLocationId.VideosLibrary when multiple is false || iOS14AndAbove is false:
					var controller = new UIImagePickerController()
					{
						SourceType = UIImagePickerControllerSourceType.PhotoLibrary,
						ImagePickerControllerDelegate = new ImageOpenPickerDelegate(completionSource)
					};

					var mediaTypesFromPhotoLibrary = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary) ?? [];
					var types = new List<string>();

					if (FileTypeFilter.Count == 0 || FileTypeFilter.Contains("*"))
					{
						types.AddRange(mediaTypesFromPhotoLibrary);
					}
					else
					{
						var isImageSupported = mediaTypesFromPhotoLibrary.Contains(UTType.Image.ToString());
						var isVideoSupported = mediaTypesFromPhotoLibrary.Contains(UTType.Movie.ToString());

						if (isImageSupported && FilterHasImage(FileTypeFilter))
						{
							types.Add(UTType.Image.ToString());
						}

						if (isVideoSupported && FilterHasVideo(FileTypeFilter))
						{
							types.Add(UTType.Movie.ToString());
						}
					}

					controller.MediaTypes = [.. types];

					return controller;

				case PickerLocationId.PicturesLibrary when multiple is true && iOS14AndAbove is true:
				case PickerLocationId.VideosLibrary when multiple is true && iOS14AndAbove is true:
					var pickerFilter = GetPhotoPickerFilter(FileTypeFilter);
					var pickerConfiguration = new PHPickerConfiguration
					{
						Filter = pickerFilter,
						SelectionLimit = limit
					};
					return new PHPickerViewController(pickerConfiguration)
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
				NSUrl? nSUrl = null;

				// Video URL
				if (info.ValueForKey(new NSString("UIImagePickerControllerMediaURL")) is NSUrl videoUrl)
				{
					nSUrl = videoUrl;
				}
				// Image URL
				else if (info.ValueForKey(new NSString("UIImagePickerControllerImageURL")) is NSUrl imageUrl)
				{
					nSUrl = imageUrl;
				}

				if (nSUrl != null)
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
				var providers = results
					.Select(res => res.ItemProvider)
					.Where(provider => provider != null && provider.RegisteredTypeIdentifiers?.Length > 0)
					.ToArray();

				foreach (NSItemProvider provider in providers)
				{
					var identifier = GetIdentifier(provider.RegisteredTypeIdentifiers ?? []) ?? "public.data";
					var data = await provider.LoadDataRepresentationAsync(identifier);

					if (data is null)
					{
						continue;
					}

					var extension = GetExtension(identifier);

					var destinationUrl = NSFileManager.DefaultManager
						.GetTemporaryDirectory()
						.Append($"{NSProcessInfo.ProcessInfo.GloballyUniqueString}.{extension}", false);
					data.Save(destinationUrl, false);

					storageFiles.Add(StorageFile.GetFromSecurityScopedUrl(destinationUrl, null));
				}

				return storageFiles;
			}
			private static string? GetIdentifier(string[] identifiers)
			{
				if (!(identifiers?.Length > 0))
				{
					return null;
				}

				if (identifiers.Any(i => i.StartsWith(UTType.LivePhoto, StringComparison.InvariantCultureIgnoreCase)) && identifiers.Contains(UTType.JPEG))
				{
					return identifiers.FirstOrDefault(i => i == UTType.JPEG);
				}

				if (identifiers.Contains(UTType.QuickTimeMovie))
				{
					return identifiers.FirstOrDefault(i => i == UTType.QuickTimeMovie);
				}

				return identifiers.FirstOrDefault();
			}

			private string? GetExtension(string identifier)
			=> UTType.CopyAllTags(identifier, UTType.TagClassFilenameExtension)?.FirstOrDefault();

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

		private static PHPickerFilter? GetPhotoPickerFilter(IList<string> fileTypeFilter)
		{
			var acceptsAll = fileTypeFilter.Count == 0 || fileTypeFilter.Contains("*");
			var hasImages = acceptsAll || FilterHasImage(fileTypeFilter);
			var hasVideos = acceptsAll || FilterHasVideo(fileTypeFilter);

			return (hasImages, hasVideos) switch
			{
				(true, true) => PHPickerFilter.GetAnyFilterMatchingSubfilters([PHPickerFilter.ImagesFilter, PHPickerFilter.VideosFilter]),
				(true, false) => PHPickerFilter.ImagesFilter,
				(false, true) => PHPickerFilter.VideosFilter,
				_ => null
			};
		}

		private static bool FilterHasVideo(IList<string> filters)
		{
			var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".mov",
				".mp4",
				".m4v",
				".avi",
				".mkv",
				".3gp",
				".3g2",
				".wmv",
				".flv",
				".f4v",
				".mpg",
				".mpeg",
				".ts",
				".webm"
			};

			return filters.Any(videoExtensions.Contains);
		}

		private static bool FilterHasImage(IList<string> filters)
		{
			var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".jpg",
				".jpeg",
				".png",
				".gif",
				".tiff",
				".tif",
				".bmp",
				".heic",
				".heif",
				".webp",
				".ico",
				".raw",
				".svg",
				".pdf"
			};

			return filters.Any(imageExtensions.Contains);
		}
	}
}
