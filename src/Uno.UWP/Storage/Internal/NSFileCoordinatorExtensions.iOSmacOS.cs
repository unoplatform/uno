using System;
using System.Threading.Tasks;
using Foundation;

namespace Uno.Storage.Internal
{
	internal static class NSFileCoordinatorExtensions
	{
		public static async Task CoordinateAsync(this NSFileCoordinator coordinator, NSFileAccessIntent[] intents, NSOperationQueue queue, Action operation)
		{
			var completionSource = new TaskCompletionSource<bool>();
			coordinator.CoordinateAccess(intents, queue, (error) =>
			{
				try
				{
					if (error != null)
					{
						throw new UnauthorizedAccessException($"Could not coordinate file system operation. {error}");
					}

					operation();
					completionSource.SetResult(true);
				}
				catch (Exception ex)
				{
					completionSource.SetException(ex);
				}
			});

			await completionSource.Task;
		}
	}
}
