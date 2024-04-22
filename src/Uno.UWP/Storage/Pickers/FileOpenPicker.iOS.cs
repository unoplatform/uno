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

		private UIViewController GetViewController(bool multiple, TaskCompletionSource<StorageFile?[]> completionSource)
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
					var imageConfiguration = new PHPickerConfiguration
					{
						Filter = PHPickerFilter.ImagesFilter,
						SelectionLimit = 0
					};
					return new PHPickerViewController(imageConfiguration)
					{
						Delegate = new PhotoPickerDelegate(completionSource)
					};
				case PickerLocationId.VideosLibrary when multiple is true && iOS14AndAbove is true:
					var videoConfiguration = new PHPickerConfiguration
					{
						Filter = PHPickerFilter.VideosFilter,
						SelectionLimit = 0
					};
					return new PHPickerViewController(videoConfiguration)
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

			public PhotoPickerDelegate(TaskCompletionSource<StorageFile?[]> taskCompletionSource) =>
				_taskCompletionSource = taskCompletionSource;

			public override async void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
			{
				var storageFiles = await ConvertPickerResults(results);
				_taskCompletionSource.SetResult(storageFiles.ToArray());
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
	}
}
