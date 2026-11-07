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
using UniformType = UniformTypeIdentifiers.UTType;
using UniformTypes = UniformTypeIdentifiers.UTTypes;

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
				try
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
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			});

			return tcs.Task;
		}

		private Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token)
		{
			var tcs = new TaskCompletionSource<IReadOnlyList<StorageFile>>();
			NativeDispatcher.Main.Enqueue(async () =>
			{
				try
				{
					var files = await PickFilesAsync(true, token);
					tcs.SetResult(files);
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
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
				case PickerLocationId.PicturesLibrary when iOS14AndAbove is false:
				case PickerLocationId.VideosLibrary when iOS14AndAbove is false:
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

				case PickerLocationId.PicturesLibrary:
				case PickerLocationId.VideosLibrary:
					var pickerFilter = GetPhotoPickerFilter(FileTypeFilter);
					var pickerConfiguration = new PHPickerConfiguration
					{
						Filter = pickerFilter,
						SelectionLimit = multiple ? limit : 1
					};
					return new PHPickerViewController(pickerConfiguration)
					{
						Delegate = new PhotoPickerDelegate(completionSource, FileTypeFilter.ToArray())
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
			private static readonly NSString _fileNameExtensionTagClass =
				UniformTypeIdentifiers.UTTagClassExtensions.GetConstant(UniformTypeIdentifiers.UTTagClass.FilenameExtension)!;

			private readonly TaskCompletionSource<StorageFile?[]> _taskCompletionSource;
			private readonly string[] _fileTypeFilter;
			private bool _isPicked;

			public PhotoPickerDelegate(TaskCompletionSource<StorageFile?[]> taskCompletionSource, string[] fileTypeFilter)
			{
				_taskCompletionSource = taskCompletionSource;
				_fileTypeFilter = fileTypeFilter;
			}

			public override async void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
			{
				// Tapping Add more than once calls this again; copy and complete only once.
				if (_isPicked)
				{
					return;
				}

				_isPicked = true;

				// Dismiss the picker early to get the user back to the app as soon as possible.
				picker.DismissViewController(true, null);

				try
				{
					var urls = await Task.WhenAll(results.Select(result => CopyPickedItemAsync(result.ItemProvider)));
					_taskCompletionSource.TrySetResult(urls.OfType<NSUrl>().Select(url => StorageFile.GetFromSecurityScopedUrl(url, null)).ToArray());
				}
				catch (Exception ex)
				{
					_taskCompletionSource.TrySetException(ex);
				}
			}

			private Task<NSUrl?> CopyPickedItemAsync(NSItemProvider? provider)
			{
				try
				{
					var registeredTypes = provider?.RegisteredTypeIdentifiers ?? [];
					if (provider is null || PhotoPickerRepresentation.Choose(registeredTypes, GetPhotoPickerType, _fileTypeFilter) is not { } representation)
					{
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().LogWarning(
								$"A picked item was left out: none of its types ({string.Join(", ", registeredTypes)}) is allowed by the file type filter " +
								$"({string.Join(", ", _fileTypeFilter)}).");
						}

						return Task.FromResult<NSUrl?>(null);
					}

					var fallbackName = NSProcessInfo.ProcessInfo.GloballyUniqueString;
					var fileName = representation.GetFileName(provider.SuggestedName, fallbackName);
					var fallbackFileName = representation.GetFileName(null, fallbackName);
					var completionSource = new TaskCompletionSource<NSUrl?>(TaskCreationOptions.RunContinuationsAsynchronously);

					// A file representation is loaded to a temporary file rather than into memory, as videos can be large.
					provider.LoadFileRepresentation(representation.TypeIdentifier, (url, error) =>
					{
						// The system deletes the temporary file when this handler returns, so it is copied here, to a folder of its own to keep
						// its name.
						try
						{
							if (url is null)
							{
								throw new InvalidOperationException(error?.LocalizedDescription ?? "No file was provided.");
							}

							var fileManager = NSFileManager.DefaultManager;
							var folderUrl = fileManager.GetTemporaryDirectory().Append(NSProcessInfo.ProcessInfo.GloballyUniqueString, true);

							if (!fileManager.CreateDirectory(folderUrl, true, null, out var folderError))
							{
								throw new InvalidOperationException(folderError?.LocalizedDescription ?? "The folder could not be created.");
							}

							var destinationUrl = folderUrl.Append(fileName, false);
							if (!fileManager.Copy(url, destinationUrl, out var copyError))
							{
								// The file system can still refuse the name, so the fallback name is tried before giving up.
								destinationUrl = folderUrl.Append(fallbackFileName, false);
								if (fileName == fallbackFileName || !fileManager.Copy(url, destinationUrl, out _))
								{
									throw new InvalidOperationException(copyError?.LocalizedDescription ?? "The file could not be copied.");
								}

								if (this.Log().IsEnabled(LogLevel.Warning))
								{
									this.Log().LogWarning(
										$"A picked item could not be copied as '{fileName}' ({copyError?.LocalizedDescription}), " +
										$"so it was named '{fallbackFileName}'.");
								}
							}

							completionSource.TrySetResult(destinationUrl);
						}
						catch (Exception ex)
						{
							LogLoadError(representation.TypeIdentifier, ex);
							completionSource.TrySetResult(null);
						}
					});

					return completionSource.Task;
				}
				catch (Exception ex)
				{
					LogLoadError(null, ex);
					return Task.FromResult<NSUrl?>(null);
				}
			}

			private void LogLoadError(string? typeIdentifier, Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					var type = typeIdentifier is null ? "" : " as " + typeIdentifier;
					this.Log().LogError($"A picked item could not be loaded{type}, so it was left out: {ex.Message}");
				}
			}

			private static PhotoPickerType? GetPhotoPickerType(string identifier)
			{
				using var type = UniformType.CreateFromIdentifier(identifier);
				if (type is null)
				{
					return null;
				}

				var preferredExtension = type.PreferredFilenameExtension;
				var extensions = type.Tags.TryGetValue(_fileNameExtensionTagClass, out var tags) ? tags.ToArray().Select(tag => tag.ToString()) : [];

				return new PhotoPickerType(
					(preferredExtension is null ? extensions : extensions.Prepend(preferredExtension)).Distinct().ToArray(),
					type.ConformsTo(UniformTypes.Data));
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
				_taskCompletionSource.TrySetResult(Array.Empty<StorageFile?>());
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
