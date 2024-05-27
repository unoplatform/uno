using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Toolkit;

public partial class MediaGallery
{
	private async Task<bool> CheckAccessPlatformAsync()
	{

	}

	public async Task SavePlatformAsync(MediaFileType type, Stream stream, string targetFileName)
	{
		var tempFile = Path.Combine(Path.GetTempPath(), targetFileName);
		try
		{
			// Write stream copy to temp
			using var fileStream = File.Create(tempFile);
			await stream.CopyToAsync(fileStream);

			// get the file uri
			var fileUri = new NSUrl(tempFile);

			await PhotoLibraryPerformChanges(() =>
			{
				using var request = type == MediaFileType.Video
				? PHAssetChangeRequest.FromVideo(fileUri)
				: PHAssetChangeRequest.FromImage(fileUri);
			}).ConfigureAwait(false);

		}
		finally
		{
			// Attempt to delete the file
			File.Delete(tempFile);
		}
	}

	static async Task PhotoLibraryPerformChanges(Action action)
	{
		var tcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

		PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(
			() =>
			{
				try
				{
					action.Invoke();
				}
				catch (Exception ex)
				{
					tcs.TrySetResult(ex);
				}
			},
			(success, error) =>
				tcs.TrySetResult(error != null ? new NSErrorException(error) : null));

		var exception = await tcs.Task;
		if (exception != null)
			throw exception;
	}
}
