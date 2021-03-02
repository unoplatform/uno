#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Uno.Helpers.Activities;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		public async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			var intent = new Intent(Intent.ActionOpenDocumentTree);
			intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
			var awaitableResultActivity = await AwaitableResultActivity.StartAsync();
			var result = await awaitableResultActivity.StartActivityForResultAsync(intent);

			if (result.Intent?.Data == null)
			{
				return null;
			}

			return StorageFolder.GetFromSafUri(result.Intent.Data);
		}
	}
}
