#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Uno.Helpers.Activities;
using Uno.UI;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		internal const int RequestCode = 6001;

		private static TaskCompletionSource<Intent>? _currentFolderPickerRequest;

		internal static bool TryHandleIntent(Intent intent)
		{
			if (_currentFolderPickerRequest == null)
			{
				return false;
			}
			_currentFolderPickerRequest.SetResult(intent);
			return true;
		}

		public async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			if (!(ContextHelper.Current is Activity appActivity))
			{
				throw new InvalidOperationException("Application activity is not yet set, API called too early.");
			}
			var intent = new Intent(Intent.ActionOpenDocumentTree);
			_currentFolderPickerRequest = new TaskCompletionSource<Intent>();

			appActivity.StartActivityForResult(intent, RequestCode);

			var resultIntent = await _currentFolderPickerRequest.Task;
			_currentFolderPickerRequest = null;

			if (resultIntent?.Data == null)
			{
				return null;
			}

			return StorageFolder.GetFromSafUri(resultIntent.Data);
		}
	}
}
